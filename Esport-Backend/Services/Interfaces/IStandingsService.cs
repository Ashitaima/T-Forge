using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface IStandingsService
    {
        Task<IEnumerable<TournamentStandingDto>> GetTournamentStandingsAsync(int tournamentId);
        Task<IEnumerable<TeamStandingDto>> GetTeamStandingsAsync();
        Task<IEnumerable<PlayerStandingDto>> GetPlayerStandingsAsync();
    }
}
