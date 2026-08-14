using TForge.Models;

namespace TForge.Data.Interfaces
{
    public interface IMatchRepository : IGenericRepository<Match>
    {
        Task<IEnumerable<Match>> GetByTournamentAsync(int tournamentId);
        Task<IEnumerable<Match>> GetByTeamAsync(int teamId);
        Task<IEnumerable<Match>> GetByStatusAsync(string status);
        Task<Match?> GetWithPlayersAsync(int id);
        Task<Match?> GetWithDetailsAsync(int id);
        IQueryable<Match> GetQueryableWithDetails();
        Task<IEnumerable<Match>> GetCompletedAsync();
        Task<IEnumerable<Match>> GetUpcomingAsync();
    }
}
