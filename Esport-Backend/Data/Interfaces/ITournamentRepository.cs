using Computational_Practice.Models;

namespace Computational_Practice.Data.Interfaces
{
    public interface ITournamentRepository : IGenericRepository<Tournament>
    {
        Task<IEnumerable<Tournament>> GetByStatusAsync(string status);
        Task<IEnumerable<Tournament>> GetByOrganizerAsync(int organizerId);
        Task<IEnumerable<Tournament>> GetByGameAsync(string game);
        Task<IEnumerable<Tournament>> GetActiveAsync();
        Task<IEnumerable<Tournament>> GetUpcomingAsync();
        Task<Tournament?> GetWithMatchesAsync(int id);
    }
}
