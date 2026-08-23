using TForge.Common;
using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface IDraftService
    {
        /// <summary>Драфт матчу або null, якщо його немає.</summary>
        Task<DraftDto?> GetForMatchAsync(int matchId);

        Task<DraftDto> CreateAsync(int matchId, int? turnSeconds);
        Task<DraftDto> StartAsync(int draftId);
        Task<DraftDto> ApplyStepAsync(int draftId, int stepIndex, string value, bool wasAutoPicked);
        Task CancelAsync(int draftId);

        /// <summary>Контекст прав і команда, чий зараз хід (порожньо в Decider).</summary>
        Task<(DraftPolicy.Context Context, int? ActingTeamId)> GetPolicyContextAsync(int draftId);

        Task<DraftPolicy.Context> GetPolicyContextForMatchAsync(int matchId);

        /// <summary>Драфти, чий хід згорів. Для Services/DraftTimeoutService.cs.</summary>
        Task<IReadOnlyList<int>> FindExpiredDraftIdsAsync(DateTime asOf);

        /// <summary>Закрити прострочений хід випадковим доступним значенням.</summary>
        Task AutoPickAsync(int draftId);
    }
}
