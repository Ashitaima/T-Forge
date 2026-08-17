using TForge.DTOs;
using TForge.Models;

namespace TForge.Services.Interfaces
{
    public interface IRatingService
    {
        /// <summary>
        /// Нараховує рейтинг за завершений турнірний матч. Повторний виклик
        /// нічого не змінює: рядок журналу для цього матчу вже існує.
        /// </summary>
        Task RateMatchAsync(Match match);

        /// <summary>Програє всі ще не враховані матчі в хронологічному порядку.</summary>
        Task<int> BackfillAsync();

        Task<IEnumerable<RatingDto>> GetTeamRatingsAsync(int teamId);
        Task<IEnumerable<RatingDto>> GetPlayerRatingsAsync(int playerId);

        Task<IEnumerable<RatingChangeDto>> GetTeamHistoryAsync(int teamId, string? game, int take);
        Task<IEnumerable<RatingChangeDto>> GetPlayerHistoryAsync(int playerId, string? game, int take);

        /// <summary>Зміни рейтингу обох команд у матчі — для картки матчу.</summary>
        Task<MatchRatingDeltaDto> GetMatchDeltaAsync(int matchId);
    }
}
