using Microsoft.EntityFrameworkCore.Storage;
using TForge.Data.Context;
using TForge.Data.Interfaces;
using TForge.Data.Repositories;
using TForge.Models;

namespace TForge.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EsportsDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(EsportsDbContext context)
        {
            _context = context;
        }

        private IUserRepository? _users;
        private ITournamentRepository? _tournaments;
        private ITeamRepository? _teams;
        private IPlayerRepository? _players;
        private IMatchRepository? _matches;
        private IGenericRepository<MatchPlayer>? _matchPlayers;
        private IGenericRepository<TeamMembershipRequest>? _membershipRequests;

        public IUserRepository Users =>
            _users ??= new UserRepository(_context);

        public ITournamentRepository Tournaments =>
            _tournaments ??= new TournamentRepository(_context);

        public ITeamRepository Teams =>
            _teams ??= new TeamRepository(_context);

        public IPlayerRepository Players =>
            _players ??= new PlayerRepository(_context);

        public IMatchRepository Matches =>
            _matches ??= new MatchRepository(_context);

        public IGenericRepository<MatchPlayer> MatchPlayers =>
            _matchPlayers ??= new GenericRepository<MatchPlayer>(_context);

        public IGenericRepository<TeamMembershipRequest> MembershipRequests =>
            _membershipRequests ??= new GenericRepository<TeamMembershipRequest>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
