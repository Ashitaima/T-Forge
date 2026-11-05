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

        private readonly InferenceSession? _logreg;
        private readonly InferenceSession? _gbt;

        public bool IsAvailable => _logreg != null && _gbt != null && Info != null;
        public ChessModelInfo? Info { get; }

        public ChessModelRunner(string modelDirectory, ILogger<ChessModelRunner> logger)
        {
            try
            {
                var file = JsonSerializer.Deserialize<ChessModelInfoFile>(
                    File.ReadAllText(Path.Combine(modelDirectory, "model_info.json")),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })
                    ?? throw new InvalidDataException("model_info.json is empty");

                if (!file.FeatureOrder.SequenceEqual(ExpectedFeatures) || !file.ClassOrder.SequenceEqual(ExpectedClasses))
                {
                    throw new InvalidDataException("model_info.json feature/class order does not match ChessFeatures");
                }

                _logreg = new InferenceSession(Path.Combine(modelDirectory, "logreg.onnx"));
                _gbt = new InferenceSession(Path.Combine(modelDirectory, "gbt.onnx"));

                Info = new ChessModelInfo(
                    file.TrainedAt,
                    file.Dataset,
                    file.RowsTrain,
                    file.EloDrawRate,
                    file.TrainGamesByTimeControl,
                    file.Metrics
                        .Where(kv => MetricKeys.ContainsKey(kv.Key))
                        .ToDictionary(kv => MetricKeys[kv.Key], kv => kv.Value));
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException
                                           or OnnxRuntimeException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex,
                    "Chess prediction models not loaded from {Directory}; predictions are disabled", modelDirectory);
                _logreg?.Dispose();
                _logreg = null;
                _gbt = null;
                Info = null;
            }
        }

        public IReadOnlyDictionary<string, ChessOutcome> Predict(int white, int black, string timeControl)
        {
            if (!IsAvailable)
            {
                throw new InvalidOperationException("Chess prediction models are not loaded");
            }

            var features = ChessFeatures.Vector(white, black, timeControl);

            return new Dictionary<string, ChessOutcome>
            {
                [PredictionModels.LogisticRegression] = Run(_logreg!, features),
                [PredictionModels.GradientBoosting] = Run(_gbt!, features),
                [PredictionModels.Elo] = EloBaseline.Predict(white, black, Info!.EloDrawRate)
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
            _logreg?.Dispose();
            _gbt?.Dispose();
        }
    }
}
