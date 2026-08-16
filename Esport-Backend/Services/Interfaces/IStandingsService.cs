using TForge.Common;
using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface IStandingsService
    {
        Task<IEnumerable<TournamentStandingDto>> GetTournamentStandingsAsync(int tournamentId);
        Task<TeamSummaryStatsDto> GetTeamSummaryAsync(int teamId);
        Task<PlayerRecordCalculator.PlayerRecord> GetPlayerCareerAsync(int playerId);
    }
}
