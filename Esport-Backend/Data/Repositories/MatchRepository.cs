using Microsoft.EntityFrameworkCore;
using Computational_Practice.Data.Context;
using Computational_Practice.Data.Interfaces;
using Computational_Practice.Models;

namespace Computational_Practice.Data.Repositories
{
    public class MatchRepository : GenericRepository<Match>, IMatchRepository
    {
        public MatchRepository(EsportsDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Match>> GetByTournamentAsync(int tournamentId)
        {
            return await _dbSet
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.WinnerTeam)
                .Where(m => m.TournamentId == tournamentId)
                .OrderBy(m => m.ScheduledAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Match>> GetByTeamAsync(int teamId)
        {
            return await _dbSet
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Tournament)
                .Include(m => m.WinnerTeam)
                .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
                .OrderByDescending(m => m.ScheduledAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Match>> GetByStatusAsync(string status)
        {
            return await _dbSet
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Tournament)
                .Where(m => m.Status == status)
                .OrderBy(m => m.ScheduledAt)
                .ToListAsync();
        }

        public async Task<Match?> GetWithPlayersAsync(int id)
        {
            return await _dbSet
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Tournament)
                .Include(m => m.WinnerTeam)
                .Include(m => m.MatchPlayers)
                .ThenInclude(mp => mp.Player)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Match>> GetCompletedAsync()
        {
            return await _dbSet
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Tournament)
                .Include(m => m.WinnerTeam)
                .Where(m => m.Status == "Completed")
                .OrderByDescending(m => m.EndedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Match>> GetUpcomingAsync()
        {
            return await _dbSet
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Tournament)
                .Where(m => m.ScheduledAt > DateTime.UtcNow && m.Status == "Scheduled")
                .OrderBy(m => m.ScheduledAt)
                .ToListAsync();
        }
    }
}
