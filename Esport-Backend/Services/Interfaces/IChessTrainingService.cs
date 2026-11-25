using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    /// <summary>
    /// Датасети й запуски навчання: читання, завантаження, черга, активація,
    /// видалення. Про Python тут не знають — його знає лише
    /// IChessTrainerProcess, а правила ухвалює Common/ChessTrainingPolicy.
    /// </summary>
    public interface IChessTrainingService
    {
        Task<ChessTrainingStatusDto> GetStatusAsync();

        Task<List<ChessDatasetDto>> GetDatasetsAsync();

        Task<ChessDatasetDto> UploadDatasetAsync(
            string name, IFormFile file, int userId, CancellationToken cancellationToken);

        Task DeleteDatasetAsync(int id);

        Task<List<TrainingRunDto>> GetRunsAsync();

        Task<TrainingRunDto> GetRunAsync(int id);

        Task<TrainingRunDto> QueueRunAsync(int datasetId, int userId);

        Task<TrainingRunDto> ActivateRunAsync(int id);

        /// <summary>Повернутись до вбудованої моделі — тобто зняти активність з усіх запусків.</summary>
        Task ActivateBaselineAsync();

        Task DeleteRunAsync(int id);
    }
}
