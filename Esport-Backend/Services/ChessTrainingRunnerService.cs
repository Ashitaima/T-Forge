using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TForge.Common;
using TForge.Data.Interfaces;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Виконує запуски навчання по одному.
    ///
    /// Форма та сама, що в DraftTimeoutService: опитування, свій скоуп на такт,
    /// збій такту логується й не валить службу. Опитування тут навіть дешевше —
    /// навчання запускають рідко, і черга майже завжди порожня.
    ///
    /// Один запуск одночасно: два навчання б'ються за процесор, і обидва хочуть
    /// перемкнути активну модель (Common/ChessTrainingPolicy.CanQueue).
    ///
    /// DbContext — scoped, і тримати його відкритим усі хвилини навчання не
    /// можна, тож скоупів два: один позначає Running, інший записує результат.
    /// Між ними процес живе сам.
    /// </summary>
    public class ChessTrainingRunnerService : BackgroundService
    {
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(2);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IChessTrainerProcess _trainer;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ChessTrainingRunnerService> _logger;

        public ChessTrainingRunnerService(
            IServiceScopeFactory scopeFactory,
            IChessTrainerProcess trainer,
            IWebHostEnvironment environment,
            ILogger<ChessTrainingRunnerService> logger)
        {
            _scopeFactory = scopeFactory;
            _trainer = trainer;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Без інтерпретатора черга не зможе нічого виконати, і опитувати
            // базу щодві секунди назавжди немає сенсу.
            if (!_trainer.IsAvailable)
            {
                _logger.LogInformation(
                    "Черга навчання не запускається: {Reason}", _trainer.UnavailableReason);
                return;
            }

            using var timer = new PeriodicTimer(Tick);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await TickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Збій такту черги навчання");
                }
            }
        }

        private async Task TickAsync(CancellationToken stoppingToken)
        {
            var runId = await ClaimNextAsync(stoppingToken);
            if (runId == null)
            {
                return;
            }

            await ExecuteRunAsync(runId.Value, stoppingToken);
        }

        /// <summary>
        /// Бере найстаріший Queued і позначає Running. Повертає null, якщо
        /// черга порожня або щось уже виконується.
        /// </summary>
        private async Task<int?> ClaimNextAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var alreadyRunning = await unitOfWork.ChessTrainingRuns.GetQueryable()
                .AnyAsync(r => r.Status == TrainingRunStatuses.Running, stoppingToken);

            if (alreadyRunning)
            {
                return null;
            }

            var next = await unitOfWork.ChessTrainingRuns.GetQueryable()
                .Where(r => r.Status == TrainingRunStatuses.Queued)
                .OrderBy(r => r.QueuedAt)
                .ThenBy(r => r.Id)
                .FirstOrDefaultAsync(stoppingToken);

            if (next == null)
            {
                return null;
            }

            next.Status = TrainingRunStatuses.Running;
            next.StartedAt = DateTime.UtcNow;
            unitOfWork.ChessTrainingRuns.Update(next);
            await unitOfWork.SaveChangesAsync();

            return next.Id;
        }

        private async Task ExecuteRunAsync(int runId, CancellationToken stoppingToken)
        {
            string datasetPath;

            using (var scope = _scopeFactory.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var run = await unitOfWork.ChessTrainingRuns.GetQueryable()
                    .Include(r => r.Dataset)
                    .FirstOrDefaultAsync(r => r.Id == runId, stoppingToken);

                if (run?.Dataset == null)
                {
                    _logger.LogError("Запуск {RunId} зник разом із датасетом", runId);
                    return;
                }

                datasetPath = Path.Combine(_environment.ContentRootPath, run.Dataset.StoredPath);
            }

            var outDirectory = ChessTrainingService.ModelDirectoryFor(_environment.ContentRootPath, runId);

            _logger.LogInformation("Починаю навчання: запуск {RunId}, датасет {Path}", runId, datasetPath);
            var result = await _trainer.TrainAsync(datasetPath, outDirectory, stoppingToken);

            await RecordResultAsync(runId, outDirectory, result);
        }

        private async Task RecordResultAsync(int runId, string outDirectory, TrainerResult result)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var run = await unitOfWork.ChessTrainingRuns.GetByIdAsync(runId);
            if (run == null)
            {
                return;
            }

            run.FinishedAt = DateTime.UtcNow;
            run.Log = result.Log;

            if (!result.Succeeded)
            {
                run.Status = TrainingRunStatuses.Failed;
                run.ErrorMessage = result.ErrorMessage;
                _logger.LogWarning("Навчання {RunId} не вдалося: {Error}", runId, result.ErrorMessage);
            }
            else
            {
                var details = ReadInfo(outDirectory);
                if (details == null)
                {
                    run.Status = TrainingRunStatuses.Failed;
                    run.ErrorMessage = "Навчання завершилося, але model_info.json не прочитано";
                }
                else
                {
                    run.Status = TrainingRunStatuses.Succeeded;
                    run.ModelPath = ChessTrainingService.RelativeModelPath(runId);
                    run.RowsTrain = details.Value.RowsTrain;
                    run.RowsTest = details.Value.RowsTest;
                    run.MetricsJson = details.Value.MetricsJson;
                    _logger.LogInformation("Навчання {RunId} успішне", runId);
                }
            }

            unitOfWork.ChessTrainingRuns.Update(run);
            await unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Дістає з model_info.json лише те, що зберігається в рядку запуску.
        /// Порядок ознак тут не перевіряється — це робить ChessModelRunner.Load
        /// під час активації, і перевіряти двічі означало б дві версії правила.
        /// </summary>
        private (int RowsTrain, int RowsTest, string MetricsJson)? ReadInfo(string outDirectory)
        {
            try
            {
                using var document = JsonDocument.Parse(
                    File.ReadAllText(Path.Combine(outDirectory, "model_info.json")));
                var root = document.RootElement;

                return (
                    root.GetProperty("rows_train").GetInt32(),
                    root.GetProperty("rows_test").GetInt32(),
                    root.GetProperty("metrics").GetRawText());
            }
            catch (Exception exception) when (exception is IOException
                                                  or JsonException
                                                  or KeyNotFoundException
                                                  or InvalidOperationException)
            {
                _logger.LogError(exception, "Не вдалося прочитати model_info.json із {Directory}", outDirectory);
                return null;
            }
        }
    }
}
