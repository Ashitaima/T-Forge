using TForge.Common;

namespace TForge.Services.Interfaces
{
    /// <summary>
    /// Дві навчені моделі й базова лінія Ело. Singleton: ONNX-сесії
    /// завантажуються раз на життя процесу.
    /// </summary>
    public interface IChessModelRunner
    {
        /// <summary>False — файли моделей не знайдено чи не прочитано; прогноз вимкнено.</summary>
        bool IsAvailable { get; }

        ChessModelInfo? Info { get; }

        /// <summary>Ключі — PredictionModels.All. Кидає InvalidOperationException, якщо !IsAvailable.</summary>
        IReadOnlyDictionary<string, ChessOutcome> Predict(int white, int black, string timeControl);
    }
}
