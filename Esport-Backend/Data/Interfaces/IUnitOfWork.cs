using TForge.Models;

namespace TForge.Data.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        ITournamentRepository Tournaments { get; }
        ITeamRepository Teams { get; }
        IPlayerRepository Players { get; }
        IMatchRepository Matches { get; }
        IGenericRepository<MatchPlayer> MatchPlayers { get; }
        IGenericRepository<TeamMembershipRequest> MembershipRequests { get; }
        IGenericRepository<MatchChallenge> MatchChallenges { get; }
        IGenericRepository<TournamentInvitation> TournamentInvitations { get; }
        IGenericRepository<TeamRating> TeamRatings { get; }
        IGenericRepository<PlayerRating> PlayerRatings { get; }
        IGenericRepository<TeamRatingChange> TeamRatingChanges { get; }
        IGenericRepository<PlayerRatingChange> PlayerRatingChanges { get; }
        IGenericRepository<Duel> Duels { get; }
        IGenericRepository<PlayerGameProfile> PlayerGameProfiles { get; }
        IGenericRepository<MatchDraft> MatchDrafts { get; }
        IGenericRepository<DraftStep> DraftSteps { get; }
        IGenericRepository<OrganizerRequest> OrganizerRequests { get; }
        IGenericRepository<TeamOpening> TeamOpenings { get; }
        IGenericRepository<LftPass> LftPasses { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
