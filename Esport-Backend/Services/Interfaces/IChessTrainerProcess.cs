using TForge.Common;

namespace TForge.Services.Interfaces
{
    /// <summary>Чим закінчився запуск train.py.</summary>
    public sealed record TrainerResult(bool Succeeded, string Log, string? ErrorMessage);

    /// <summary>
    /// Єдине місце в бекенді, яке знає, що Python існує. Singleton: шляхи й
    /// їхня наявність не змінюються за життя процесу.
    /// </summary>
    public interface IChessTrainerProcess
    {
        /// <summary>False — немає інтерпретатора чи скрипта; навчання вимкнено, старт не падає.</summary>
        bool IsAvailable { get; }

        /// <summary>Чому недоступне — показується адміністратору як є.</summary>
        string? UnavailableReason { get; }

        Task<ChessDatasetSummary> ValidateAsync(string csvPath, CancellationToken cancellationToken);

        Task<TrainerResult> TrainAsync(string csvPath, string outDirectory, CancellationToken cancellationToken);
    }
}
