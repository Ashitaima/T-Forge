namespace Computational_Practice.Data.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        ITournamentRepository Tournaments { get; }
        ITeamRepository Teams { get; }
        IPlayerRepository Players { get; }
        IMatchRepository Matches { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
