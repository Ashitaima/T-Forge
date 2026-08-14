using TForge.Common;
using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface IStandingsService
    {
        Task<IEnumerable<TournamentStandingDto>> GetTournamentStandingsAsync(int tournamentId);
        Task<IEnumerable<TeamStandingDto>> GetTeamStandingsAsync();
        Task<IEnumerable<PlayerStandingDto>> GetPlayerStandingsAsync();
        Task<TeamSummaryStatsDto> GetTeamSummaryAsync(int teamId);
        Task<PlayerRecordCalculator.PlayerRecord> GetPlayerCareerAsync(int playerId);
    }
}
