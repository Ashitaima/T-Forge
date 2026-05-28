using Computational_Practice.Models;

namespace Computational_Practice.Data.Interfaces
{
    public interface IPlayerRepository : IGenericRepository<Player>
    {
        Task<Player?> GetByNicknameAsync(string nickname);
        Task<Player?> GetByUserIdAsync(int userId);
        Task<IEnumerable<Player>> GetByTeamAsync(int teamId);
        Task<IEnumerable<Player>> GetByPositionAsync(string position);
        Task<IEnumerable<Player>> GetTopByRankingAsync(int count);
        Task<IEnumerable<Player>> GetActiveAsync();
        Task<Player?> GetWithStatsAsync(int id);
    }
}
