using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface IMatchChallengeService
    {
        Task<MatchChallengeDto> CreateAsync(CreateMatchChallengeDto createDto, int requestingUserId, bool isAdmin);
        /// <summary>
        /// Приймає виклик. `acceptingTeamId` потрібен лише для відкритого
        /// виклику, та й то лише коли капітан веде кілька команд — інакше
        /// команду визначає сервер.
        /// </summary>
        Task<MatchChallengeDto> AcceptAsync(
            int challengeId, int requestingUserId, bool isAdmin, int? acceptingTeamId = null);
        Task<MatchChallengeDto> DeclineAsync(int challengeId, int requestingUserId, bool isAdmin);
        Task<MatchChallengeDto> CancelAsync(int challengeId, int requestingUserId, bool isAdmin);
        Task<IEnumerable<MatchChallengeDto>> GetForTeamAsync(int teamId, string? status);
        Task<IEnumerable<MatchChallengeDto>> GetPendingForUserAsync(int userId);

        /// <summary>Відкриті виклики, за потреби звужені до однієї дисципліни.</summary>
        Task<IEnumerable<MatchChallengeDto>> GetOpenAsync(string? game);
    }
}
