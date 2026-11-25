using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using TForge.Common;
using TForge.Services.Interfaces;

namespace TForge.Services
{
    /// <summary>
    /// Вміст model_info.json, який пише ml/chess/train.py (snake_case у файлі).
    /// Назовні йде лише ChessModelInfo (Common) — з ключами PredictionModels.
    /// </summary>
    public sealed class ChessModelInfoFile
    {
        public string TrainedAt { get; set; } = string.Empty;
        public string Dataset { get; set; } = string.Empty;
        public int RowsTrain { get; set; }
        public int RowsTest { get; set; }
        public List<string> FeatureOrder { get; set; } = new();
        public List<string> ClassOrder { get; set; } = new();
        public double EloDrawRate { get; set; }
        public Dictionary<string, int> TrainGamesByTimeControl { get; set; } = new();
        public Dictionary<string, ChessModelMetrics> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Обидві ONNX-моделі, завантажені раз на життя процесу. Відсутній чи
    /// зіпсований файл вимикає прогноз із попередженням у лог — але ніколи не
    /// зупиняє застосунок: прогноз — додаток до турнірів, а не їхня умова.
    /// </summary>
    public sealed class ChessModelRunner : IChessModelRunner, IDisposable
    {
        // Контракт із графом. Модель, навчена з іншим порядком ознак чи класів,
        // мовчки рахувала б не те, тож такий файл відкидаємо одразу.
        private static readonly string[] ExpectedFeatures =
            { "rating_diff", "rating_mean", "tc_bullet", "tc_blitz", "tc_rapid", "tc_classical" };
        private static readonly string[] ExpectedClasses = { "white", "draw", "black" };

        // Ключі метрик у model_info.json → назви моделей у DTO.
        private static readonly Dictionary<string, string> MetricKeys = new()
        {
            ["logreg"] = PredictionModels.LogisticRegression,
            ["gbt"] = PredictionModels.GradientBoosting,
            ["elo"] = PredictionModels.Elo
        };

        /// <summary>
        /// Усе завантажене — одним незмінним знімком. Predict бере посилання
        /// один раз у локальну змінну, тож перемикання посеред прогнозу не
        /// може розірвати стан.
        /// </summary>
        private sealed record Loaded(
            InferenceSession Logreg, InferenceSession Gbt, ChessModelInfo Info, int? RunId);

        private volatile Loaded? _loaded;

        private readonly ILogger<ChessModelRunner> _logger;

        public bool IsAvailable => _loaded != null;
        public ChessModelInfo? Info => _loaded?.Info;
        public int? ActiveRunId => _loaded?.RunId;
        public string BaselineDirectory { get; }

        public ChessModelRunner(string modelDirectory, ILogger<ChessModelRunner> logger)
        {
            _logger = logger;
            BaselineDirectory = modelDirectory;
            Load(modelDirectory, runId: null);
        }

        /// <summary>
        /// Новий знімок збирається повністю і лише потім присвоюється. Якщо
        /// файли не читаються, лишається попередній — зламана версія не має
        /// права лишити застосунок зовсім без моделі. Те саме правило, що
        /// захищає ImageUploadService: старе йде тільки після того, як нове
        /// стало на місце.
        ///
        /// Старі сесії при цьому не диспоузяться: прогноз, що вже в польоті,
        /// тримає посилання, а дешевого способу дізнатися, що він закінчився,
        /// немає. Ціна — десятки кілобайтів графа на перемикання, і для рідкої
        /// дії адміністратора це дешевше за ObjectDisposedException у
        /// відповіді користувачеві.
        /// </summary>
        public bool Load(string modelDirectory, int? runId)
        {
            InferenceSession? logreg = null;
            InferenceSession? gbt = null;

            try
            {
                var file = JsonSerializer.Deserialize<ChessModelInfoFile>(
                    File.ReadAllText(Path.Combine(modelDirectory, "model_info.json")),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })
                    ?? throw new InvalidDataException("model_info.json is empty");

                if (!file.FeatureOrder.SequenceEqual(ExpectedFeatures)
                    || !file.ClassOrder.SequenceEqual(ExpectedClasses))
                {
                    throw new InvalidDataException(
                        "model_info.json feature/class order does not match ChessFeatures");
                }

                logreg = new InferenceSession(Path.Combine(modelDirectory, "logreg.onnx"));
                gbt = new InferenceSession(Path.Combine(modelDirectory, "gbt.onnx"));

                var info = new ChessModelInfo(
                    file.TrainedAt,
                    file.Dataset,
                    file.RowsTrain,
                    file.EloDrawRate,
                    file.TrainGamesByTimeControl,
                    file.Metrics
                        .Where(kv => MetricKeys.ContainsKey(kv.Key))
                        .ToDictionary(kv => MetricKeys[kv.Key], kv => kv.Value));

                _loaded = new Loaded(logreg, gbt, info, runId);
                _logger.LogInformation(
                    "Активна модель прогнозу: {Directory} (запуск {RunId})", modelDirectory, runId);
                return true;
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException
                                           or OnnxRuntimeException or UnauthorizedAccessException)
            {
                logreg?.Dispose();
                gbt?.Dispose();

                if (_loaded == null)
                {
                    _logger.LogWarning(ex,
                        "Моделі прогнозу не завантажено з {Directory}; прогноз вимкнено", modelDirectory);
                }
                else
                {
                    _logger.LogError(ex,
                        "Не вдалося перемкнути модель на {Directory}; лишається попередня версія",
                        modelDirectory);
                }

                return false;
            }
        }

        public IReadOnlyDictionary<string, ChessOutcome> Predict(int white, int black, string timeControl)
        {
            // Один раз у локальну: далі перемикання не впливає на цей виклик.
            var loaded = _loaded
                ?? throw new InvalidOperationException("Chess prediction models are not loaded");

            var features = ChessFeatures.Vector(white, black, timeControl);

            return new Dictionary<string, ChessOutcome>
            {
                [PredictionModels.LogisticRegression] = Run(loaded.Logreg, features),
                [PredictionModels.GradientBoosting] = Run(loaded.Gbt, features),
                [PredictionModels.Elo] = EloBaseline.Predict(white, black, loaded.Info.EloDrawRate)
            };
        }

        private static ChessOutcome Run(InferenceSession session, float[] features)
        {
            var tensor = new DenseTensor<float>(features, new[] { 1, ChessFeatures.Count });
            using var results = session.Run(new[] { NamedOnnxValue.CreateFromTensor("input", tensor) });
            var p = results.First(r => r.Name == "probabilities").AsTensor<float>();

            return new ChessOutcome(p[0, 0], p[0, 1], p[0, 2]);
        }

        public void Dispose()
        {
            var loaded = _loaded;
            _loaded = null;
            loaded?.Logreg.Dispose();
            loaded?.Gbt.Dispose();
        }
    }
}
