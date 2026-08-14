using TForge.DTOs;
using TForge.Models;

namespace TForge.Services.Interfaces
{
    public interface IMatchRosterService
    {
        Task<IEnumerable<MatchPlayerDto>> GetRosterAsync(int matchId);
        Task<IEnumerable<MatchPlayerDto>> AutoFillAsync(int matchId);
        Task<MatchPlayerDto> AddPlayerAsync(int matchId, CreateMatchPlayerDto dto);
        Task<MatchPlayerDto> UpdateEntryAsync(int matchId, int entryId, UpdateMatchPlayerDto dto);
        Task RemoveEntryAsync(int matchId, int entryId);

        /// <summary>Переносить результат завершеного матчу в статистику гравців.</summary>
        Task ApplyMatchResultAsync(Match match);
    }
}
