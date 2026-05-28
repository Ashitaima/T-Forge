using Microsoft.EntityFrameworkCore;
using Computational_Practice.Data.Context;
using Computational_Practice.Data.Interfaces;
using Computational_Practice.Models;

namespace Computational_Practice.Data.Repositories
{
    public class TeamRepository : GenericRepository<Team>, ITeamRepository
    {
        public TeamRepository(EsportsDbContext context) : base(context)
        {
        }

        public async Task<Team?> GetByNameAsync(string name)
        {
            return await _dbSet
                .Include(t => t.Captain)
                .FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<Team?> GetByTagAsync(string tag)
        {
            return await _dbSet
                .Include(t => t.Captain)
                .FirstOrDefaultAsync(t => t.Tag == tag);
        }

        public async Task<IEnumerable<Team>> GetByRegionAsync(string region)
        {
            return await _dbSet
                .Include(t => t.Captain)
                .Where(t => t.Region == region && t.IsActive)
                .ToListAsync();
        }

        public async Task<Team?> GetWithPlayersAsync(int id)
        {
            return await _dbSet
                .Include(t => t.Captain)
                .Include(t => t.Players)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Team>> GetByCaptainAsync(int captainId)
        {
            return await _dbSet
                .Include(t => t.Captain)
                .Where(t => t.CaptainId == captainId && t.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<Team>> GetActiveAsync()
        {
            return await _dbSet
                .Include(t => t.Captain)
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
