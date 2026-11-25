using TForge.Common;

namespace TForge.Services.Interfaces
{
    /// <summary>
    /// Дві навчені моделі й базова лінія Ело. Singleton: ONNX-сесії живуть
    /// разом із процесом, але версію можна перемкнути на льоту — активну
    /// модель обирає адміністратор, і перезапуск для цього не потрібен.
    /// </summary>
    public interface IChessModelRunner
    {
        /// <summary>False — файли моделей не знайдено чи не прочитано; прогноз вимкнено.</summary>
        bool IsAvailable { get; }

        ChessModelInfo? Info { get; }

        /// <summary>ChessTrainingRun.Id активної версії; null — вбудована модель.</summary>
        int? ActiveRunId { get; }

        /// <summary>Тека вбудованої моделі — та, з якої почали.</summary>
        string BaselineDirectory { get; }

        /// <summary>
        /// Назва набору, на якому навчено вбудовану модель, — знята один раз на
        /// старті. Через Info її читати не можна: щойно активною стає
        /// користувацька версія, Info описує вже її, і підпис «вбудована»
        /// показував би чужий датасет.
        /// </summary>
        string BaselineDataset { get; }

        /// <summary>
        /// Перемкнути активну версію. True — знімок замінено; false — файли не
        /// прочитано або вони суперечать ChessFeatures, і попередня версія
        /// лишилася чинною.
        /// </summary>
        bool Load(string modelDirectory, int? runId);

        /// <summary>Ключі — PredictionModels.All. Кидає InvalidOperationException, якщо !IsAvailable.</summary>
        IReadOnlyDictionary<string, ChessOutcome> Predict(int white, int black, string timeControl);
    }
}
