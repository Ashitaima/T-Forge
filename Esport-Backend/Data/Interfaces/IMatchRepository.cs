using Computational_Practice.Models;

namespace Computational_Practice.Data.Interfaces
{
    public interface IMatchRepository : IGenericRepository<Match>
    {
        Task<IEnumerable<Match>> GetByTournamentAsync(int tournamentId);
        Task<IEnumerable<Match>> GetByTeamAsync(int teamId);
        Task<IEnumerable<Match>> GetByStatusAsync(string status);
        Task<Match?> GetWithPlayersAsync(int id);
        Task<IEnumerable<Match>> GetCompletedAsync();
        Task<IEnumerable<Match>> GetUpcomingAsync();
    }
}
