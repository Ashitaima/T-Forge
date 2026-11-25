using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TForge.Common;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Exceptions;
using TForge.Models;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Датасети й версії моделей.
    ///
    /// Файли лежать під MlData (поза wwwroot — їх не має віддавати статика), у
    /// базі лишається лише шлях: та сама схема, що в аватарів. Вміст CSV тут
    /// ніколи не розбирається — вердикт дає ml/chess/datasets.py через
    /// IChessTrainerProcess.ValidateAsync, щоб схема мала одне визначення.
    /// </summary>
    public class ChessTrainingService : IChessTrainingService
    {
        /// <summary>
        /// 50 МБ. Вбудована вибірка — 7,3 МБ, тож ліміт лишає запас на місячний
        /// дамп Lichess, але не дає залити на диск що завгодно.
        /// </summary>
        public const long MaxBytes = 50L * 1024 * 1024;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IChessTrainerProcess _trainer;
        private readonly IChessModelRunner _runner;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ChessTrainingService> _logger;

        public ChessTrainingService(
            IUnitOfWork unitOfWork,
            IChessTrainerProcess trainer,
            IChessModelRunner runner,
            IWebHostEnvironment environment,
            ILogger<ChessTrainingService> logger)
        {
            _unitOfWork = unitOfWork;
            _trainer = trainer;
            _runner = runner;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>Тека версії. Public static — фонова служба будує той самий шлях.</summary>
        public static string ModelDirectoryFor(string contentRoot, int runId) =>
            Path.Combine(contentRoot, "MlData", "models", runId.ToString());

        /// <summary>Шлях версії, який зберігається в базі — відносний до кореня вмісту.</summary>
        public static string RelativeModelPath(int runId) =>
            Path.Combine("MlData", "models", runId.ToString());

        private string DatasetDirectoryFor(int datasetId) =>
            Path.Combine(_environment.ContentRootPath, "MlData", "datasets", datasetId.ToString());

        private string Absolute(string relativePath) =>
            Path.Combine(_environment.ContentRootPath, relativePath);

        // ---- Статус ----

        public async Task<ChessTrainingStatusDto> GetStatusAsync()
        {
            var activeRunId = _runner.ActiveRunId;

            return new ChessTrainingStatusDto
            {
                Available = _trainer.IsAvailable,
                Reason = _trainer.UnavailableReason,
                ActiveRun = activeRunId == null ? null : await GetRunAsync(activeRunId.Value),
                // Саме BaselineDataset, а не Info.Dataset: Info описує активну
                // модель, тож після активації користувацької версії тут був би
                // її датасет замість вбудованого.
                BaselineDataset = _runner.BaselineDataset
            };
        }

        // ---- Датасети ----

        public async Task<List<ChessDatasetDto>> GetDatasetsAsync()
        {
            var datasets = await DatasetsWithRelations()
                .OrderByDescending(d => d.UploadedAt)
                .ThenByDescending(d => d.Id)
                .ToListAsync();

            return datasets.Select(ToDto).ToList();
        }

        public async Task<ChessDatasetDto> UploadDatasetAsync(
            string name, IFormFile file, int userId, CancellationToken cancellationToken)
        {
            if (!_trainer.IsAvailable)
            {
                throw new BusinessLogicException(
                    $"Перевірка датасетів недоступна: {_trainer.UnavailableReason}");
            }

            if (file == null || file.Length == 0)
            {
                throw new BusinessLogicException("Файл порожній");
            }

            if (file.Length > MaxBytes)
            {
                throw new BusinessLogicException("Файл завеликий: максимум 50 МБ");
            }

            if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessLogicException("Підтримується лише CSV");
            }

            // Спершу у тимчасовий файл: перевірку робить Python, і зберігати
            // рядок у базі до вердикту означало б лишати сміття після відмови.
            var temporaryPath = Path.Combine(Path.GetTempPath(), $"tforge-dataset-{Guid.NewGuid():N}.csv");

            try
            {
                await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                var summary = await _trainer.ValidateAsync(temporaryPath, cancellationToken);
                if (!summary.IsUsable)
                {
                    throw new BusinessLogicException(
                        summary.Issues.Count > 0
                            ? $"Датасет не підходить: {summary.IssueText}"
                            : "Датасет не підходить: перевірка не дала придатного результату");
                }

                var dataset = new ChessDataset
                {
                    Name = name.Trim(),
                    OriginalFileName = Path.GetFileName(file.FileName),
                    StoredPath = string.Empty,
                    Profile = summary.Profile!,
                    RowCount = summary.Rows,
                    SizeBytes = file.Length,
                    GamesByTimeControlJson = JsonSerializer.Serialize(summary.ByTimeControl),
                    IsTimeOrdered = summary.IsTimeOrdered,
                    UploadedByUserId = userId,
                    UploadedAt = DateTime.UtcNow
                };

                // Теку назвати нічим, поки немає Id, тож зберігаємо двічі:
                // спершу рядок, потім шлях. Файл переїжджає між збереженнями, і
                // якщо переїзд не вдасться — рядок знімається, щоб не лишилося
                // датасету без файлу.
                await _unitOfWork.ChessDatasets.AddAsync(dataset);
                await _unitOfWork.SaveChangesAsync();

                try
                {
                    Directory.CreateDirectory(DatasetDirectoryFor(dataset.Id));
                    File.Move(temporaryPath, Path.Combine(DatasetDirectoryFor(dataset.Id), "data.csv"),
                        overwrite: true);

                    dataset.StoredPath = Path.Combine("MlData", "datasets", dataset.Id.ToString(), "data.csv");
                    _unitOfWork.ChessDatasets.Update(dataset);
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    _logger.LogError(exception, "Не вдалося зберегти файл датасету {Id}", dataset.Id);
                    _unitOfWork.ChessDatasets.Remove(dataset);
                    await _unitOfWork.SaveChangesAsync();
                    throw new BusinessLogicException("Не вдалося зберегти файл датасету");
                }

                var saved = await DatasetsWithRelations().FirstAsync(d => d.Id == dataset.Id, cancellationToken);
                return ToDto(saved);
            }
            finally
            {
                DeleteFileQuietly(temporaryPath);
            }
        }

        public async Task DeleteDatasetAsync(int id)
        {
            var dataset = await _unitOfWork.ChessDatasets.GetQueryable()
                .Include(d => d.Runs)
                .FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new EntityNotFoundException("ChessDataset", id);

            if (!ChessTrainingPolicy.CanDeleteDataset(dataset.Runs.Count > 0))
            {
                throw new BusinessLogicException(
                    "Датасет використовують запуски навчання — спершу видаліть їх");
            }

            _unitOfWork.ChessDatasets.Remove(dataset);
            await _unitOfWork.SaveChangesAsync();

            DeleteDirectoryQuietly(DatasetDirectoryFor(id));
        }

        // ---- Запуски ----

        public async Task<List<TrainingRunDto>> GetRunsAsync()
        {
            var runs = await RunsWithRelations()
                .OrderByDescending(r => r.QueuedAt)
                .ThenByDescending(r => r.Id)
                .ToListAsync();

            // Лог у списку не віддаємо: він буває на 16 КБ на рядок, а
            // потрібен лише на сторінці одного запуску.
            return runs.Select(run => ToDto(run, includeLog: false)).ToList();
        }

        public async Task<TrainingRunDto> GetRunAsync(int id)
        {
            var run = await RunsWithRelations().FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new EntityNotFoundException("ChessTrainingRun", id);

            return ToDto(run, includeLog: true);
        }

        public async Task<TrainingRunDto> QueueRunAsync(int datasetId, int userId)
        {
            var dataset = await _unitOfWork.ChessDatasets.GetByIdAsync(datasetId)
                ?? throw new EntityNotFoundException("ChessDataset", datasetId);

            if (!_trainer.IsAvailable)
            {
                throw new BusinessLogicException($"Навчання недоступне: {_trainer.UnavailableReason}");
            }

            var hasUnfinished = await _unitOfWork.ChessTrainingRuns.GetQueryable()
                .AnyAsync(r => r.Status == TrainingRunStatuses.Queued
                            || r.Status == TrainingRunStatuses.Running);

            if (!ChessTrainingPolicy.CanQueue(hasUnfinished))
            {
                throw new BusinessLogicException(
                    "Уже є запуск у черзі або в роботі — дочекайтеся його завершення");
            }

            var run = new ChessTrainingRun
            {
                DatasetId = dataset.Id,
                Status = TrainingRunStatuses.Queued,
                QueuedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _unitOfWork.ChessTrainingRuns.AddAsync(run);
            await _unitOfWork.SaveChangesAsync();

            return await GetRunAsync(run.Id);
        }

        public async Task<TrainingRunDto> ActivateRunAsync(int id)
        {
            var run = await _unitOfWork.ChessTrainingRuns.GetByIdAsync(id)
                ?? throw new EntityNotFoundException("ChessTrainingRun", id);

            if (!ChessTrainingPolicy.CanActivate(run.Status))
            {
                throw new BusinessLogicException("Активувати можна лише успішно навчену версію");
            }

            if (run.ModelPath == null)
            {
                throw new BusinessLogicException("У запуску немає файлів моделі");
            }

            // Перемикаємо спершу в пам'яті: якщо файли не читаються або
            // суперечать ChessFeatures, активною лишиться попередня версія, і
            // база не має про це збрехати.
            if (!_runner.Load(Absolute(run.ModelPath), run.Id))
            {
                throw new BusinessLogicException(
                    "Не вдалося завантажити модель цієї версії — активною лишилася попередня");
            }

            await SetActiveAsync(run.Id);
            return await GetRunAsync(run.Id);
        }

        public async Task ActivateBaselineAsync()
        {
            if (!_runner.Load(_runner.BaselineDirectory, runId: null))
            {
                throw new BusinessLogicException("Не вдалося завантажити вбудовану модель");
            }

            await SetActiveAsync(null);
        }

        public async Task DeleteRunAsync(int id)
        {
            var run = await _unitOfWork.ChessTrainingRuns.GetByIdAsync(id)
                ?? throw new EntityNotFoundException("ChessTrainingRun", id);

            if (!ChessTrainingPolicy.CanDeleteRun(run.Status, run.IsActive))
            {
                throw new BusinessLogicException(run.IsActive
                    ? "Активну версію видалити не можна — спершу перемкніть активну модель"
                    : "Запуск ще не завершено");
            }

            _unitOfWork.ChessTrainingRuns.Remove(run);
            await _unitOfWork.SaveChangesAsync();

            DeleteDirectoryQuietly(ModelDirectoryFor(_environment.ContentRootPath, id));
        }

        /// <summary>
        /// Активною лишається рівно одна версія. Знімаємо старі й ставимо нову
        /// двома збереженнями в одній транзакції: фільтрований унікальний
        /// індекс перевіряється одразу, тож один UPDATE на обидва рядки впав би
        /// на конфлікті всередині транзакції.
        /// </summary>
        private async Task SetActiveAsync(int? runId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var active = await _unitOfWork.ChessTrainingRuns.FindAsync(r => r.IsActive);
                foreach (var row in active)
                {
                    row.IsActive = false;
                    _unitOfWork.ChessTrainingRuns.Update(row);
                }

                await _unitOfWork.SaveChangesAsync();

                if (runId != null)
                {
                    var run = await _unitOfWork.ChessTrainingRuns.GetByIdAsync(runId.Value)
                        ?? throw new EntityNotFoundException("ChessTrainingRun", runId.Value);
                    run.IsActive = true;
                    _unitOfWork.ChessTrainingRuns.Update(run);
                    await _unitOfWork.SaveChangesAsync();
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private IQueryable<ChessDataset> DatasetsWithRelations() =>
            _unitOfWork.ChessDatasets.GetQueryable()
                .AsNoTracking()
                .Include(d => d.UploadedBy)
                .Include(d => d.Runs);

        private IQueryable<ChessTrainingRun> RunsWithRelations() =>
            _unitOfWork.ChessTrainingRuns.GetQueryable()
                .AsNoTracking()
                .Include(r => r.Dataset)
                .Include(r => r.CreatedBy);

        private void DeleteDirectoryQuietly(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Рядка вже немає, тож залишений на диску файл — сміття, а не
                // розсинхрон. Валити запит через нього нема сенсу.
                _logger.LogWarning(exception, "Не вдалося видалити теку {Path}", path);
            }
        }

        private void DeleteFileQuietly(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(exception, "Не вдалося видалити тимчасовий файл {Path}", path);
            }
        }

        private static ChessDatasetDto ToDto(ChessDataset dataset) => new()
        {
            Id = dataset.Id,
            Name = dataset.Name,
            OriginalFileName = dataset.OriginalFileName,
            Profile = dataset.Profile,
            RowCount = dataset.RowCount,
            SizeBytes = dataset.SizeBytes,
            GamesByTimeControl = ParseCounts(dataset.GamesByTimeControlJson),
            IsTimeOrdered = dataset.IsTimeOrdered,
            UploadedAt = dataset.UploadedAt,
            UploadedByUsername = dataset.UploadedBy?.Username ?? string.Empty,
            RunCount = dataset.Runs?.Count ?? 0
        };

        private static TrainingRunDto ToDto(ChessTrainingRun run, bool includeLog) => new()
        {
            Id = run.Id,
            DatasetId = run.DatasetId,
            DatasetName = run.Dataset?.Name ?? string.Empty,
            Status = run.Status,
            IsActive = run.IsActive,
            QueuedAt = run.QueuedAt,
            StartedAt = run.StartedAt,
            FinishedAt = run.FinishedAt,
            RowsTrain = run.RowsTrain,
            RowsTest = run.RowsTest,
            Metrics = ParseMetrics(run.MetricsJson),
            Log = includeLog ? run.Log : null,
            ErrorMessage = run.ErrorMessage,
            CreatedByUsername = run.CreatedBy?.Username ?? string.Empty
        };

        private static Dictionary<string, int> ParseCounts(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new();
            }
            catch (JsonException)
            {
                return new();
            }
        }

        /// <summary>
        /// Розбирає блок metrics із model_info.json. Порядок — PredictionModels.All,
        /// щоб таблиця запусків і панель прогнозу перелічували моделі однаково.
        /// </summary>
        private static List<ModelMetricsDto> ParseMetrics(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new();
            }

            Dictionary<string, ChessModelMetrics>? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<Dictionary<string, ChessModelMetrics>>(
                    json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            }
            catch (JsonException)
            {
                return new();
            }

            if (parsed == null)
            {
                return new();
            }

            var byModel = parsed
                .Where(kv => PredictionModels.FileKeys.ContainsKey(kv.Key))
                .ToDictionary(kv => PredictionModels.FileKeys[kv.Key], kv => kv.Value);

            return PredictionModels.All
                .Where(byModel.ContainsKey)
                .Select(model => new ModelMetricsDto
                {
                    Model = model,
                    LogLoss = byModel[model].LogLoss,
                    Brier = byModel[model].Brier,
                    Accuracy = byModel[model].Accuracy
                })
                .ToList();
        }
    }
}
