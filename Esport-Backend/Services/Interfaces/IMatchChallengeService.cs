using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface IMatchChallengeService
    {
        Task<MatchChallengeDto> CreateAsync(CreateMatchChallengeDto createDto, int requestingUserId, bool isAdmin);
        Task<MatchChallengeDto> AcceptAsync(int challengeId, int requestingUserId, bool isAdmin);
        Task<MatchChallengeDto> DeclineAsync(int challengeId, int requestingUserId, bool isAdmin);
        Task<MatchChallengeDto> CancelAsync(int challengeId, int requestingUserId, bool isAdmin);
        Task<IEnumerable<MatchChallengeDto>> GetForTeamAsync(int teamId, string? status);
        Task<IEnumerable<MatchChallengeDto>> GetPendingForUserAsync(int userId);
    }
}
