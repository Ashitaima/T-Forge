using TForge.Models;

namespace TForge.Data.Interfaces
{
    public interface ITeamRepository : IGenericRepository<Team>
    {
        Task<Team?> GetByNameAsync(string name);
        Task<Team?> GetByTagAsync(string tag);
        Task<IEnumerable<Team>> GetByRegionAsync(string region);
        Task<Team?> GetWithPlayersAsync(int id);
        Task<IEnumerable<Team>> GetByCaptainAsync(int captainId);
        Task<IEnumerable<Team>> GetActiveAsync();
    }
}
