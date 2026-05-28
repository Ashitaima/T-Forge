using Microsoft.EntityFrameworkCore;
using Computational_Practice.Data.Context;
using Computational_Practice.Data.Interfaces;
using Computational_Practice.Models;

namespace Computational_Practice.Data.Repositories
{
    public class TournamentRepository : GenericRepository<Tournament>, ITournamentRepository
    {
        public TournamentRepository(EsportsDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Tournament>> GetByStatusAsync(string status)
        {
            return await _dbSet
                .Include(t => t.Organizer)
                .Where(t => t.Status == status && t.IsActive)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tournament>> GetByOrganizerAsync(int organizerId)
        {
            return await _dbSet
                .Include(t => t.Organizer)
                .Where(t => t.OrganizerId == organizerId && t.IsActive)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tournament>> GetByGameAsync(string game)
        {
            return await _dbSet
                .Include(t => t.Organizer)
                .Where(t => t.Game == game && t.IsActive)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tournament>> GetActiveAsync()
        {
            return await _dbSet
                .Include(t => t.Organizer)
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tournament>> GetUpcomingAsync()
        {
            return await _dbSet
                .Include(t => t.Organizer)
                .Where(t => t.StartDate > DateTime.UtcNow && t.IsActive)
                .OrderBy(t => t.StartDate)
                .ToListAsync();
        }

        public async Task<Tournament?> GetWithMatchesAsync(int id)
        {
            return await _dbSet
                .Include(t => t.Organizer)
                .Include(t => t.Matches)
                .ThenInclude(m => m.HomeTeam)
                .Include(t => t.Matches)
                .ThenInclude(m => m.AwayTeam)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}
