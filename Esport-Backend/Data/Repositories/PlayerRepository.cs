using Microsoft.EntityFrameworkCore;
using Computational_Practice.Data.Context;
using Computational_Practice.Data.Interfaces;
using Computational_Practice.Models;

namespace Computational_Practice.Data.Repositories
{
    public class PlayerRepository : GenericRepository<Player>, IPlayerRepository
    {
        public PlayerRepository(EsportsDbContext context) : base(context)
        {
        }

        public async Task<Player?> GetByNicknameAsync(string nickname)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Nickname == nickname);
        }

        public async Task<Player?> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<IEnumerable<Player>> GetByTeamAsync(int teamId)
        {
            return await _dbSet
                .Include(p => p.User)
                .Where(p => p.TeamId == teamId && p.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<Player>> GetByPositionAsync(string position)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Team)
                .Where(p => p.Position == position && p.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<Player>> GetTopByRankingAsync(int count)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Team)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Ranking)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Player>> GetActiveAsync()
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Team)
                .Where(p => p.IsActive)
                .ToListAsync();
        }

        public async Task<Player?> GetWithStatsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Team)
                .Include(p => p.MatchPlayers)
                .ThenInclude(mp => mp.Match)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
