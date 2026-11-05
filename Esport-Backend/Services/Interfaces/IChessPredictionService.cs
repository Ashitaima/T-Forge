using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface IChessPredictionService
    {
        Task<DuelPredictionDto> PredictDuelAsync(int duelId);
        Task<MatchPredictionDto> PredictMatchAsync(int matchId);
    }
}
