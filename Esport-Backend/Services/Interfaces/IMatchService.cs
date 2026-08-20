using TForge.DTOs;
using TForge.Common;
using TForge.Common.Filters;

namespace TForge.Services.Interfaces
{
    public interface IMatchService
    {
        Task<IEnumerable<MatchDto>> GetAllAsync();
        Task<PagedResponse<MatchDto>> GetPagedAsync(PagedRequest request, MatchFilter? filter = null);
        Task<MatchDto?> GetByIdAsync(int id);
        Task<MatchDto?> GetWithDetailsAsync(int id);
        Task<IEnumerable<MatchDto>> GetByTournamentAsync(int tournamentId);
        Task<IEnumerable<MatchDto>> GetByTeamAsync(int teamId);
        Task<IEnumerable<MatchDto>> GetByStatusAsync(string status);
        Task<IEnumerable<MatchDto>> GetScheduledMatchesAsync();
        Task<IEnumerable<MatchDto>> GetCompletedMatchesAsync();
        /// <summary>Домашню команду сервіс виводить із капітанства того, хто створює.</summary>
        Task<MatchDto> CreateAsync(CreateMatchDto createDto, int requestingUserId);

        /// <summary>Приєднатися до відкритого матчу — назватися командою-гостем.</summary>
        Task<MatchDto> JoinAsync(int id, int requestingUserId, bool isAdmin);

        /// <summary>Дані, потрібні MatchCreationPolicy, щоб вирішити, хто може створити цей матч.</summary>
        Task<MatchCreationPolicy.Context> GetCreateContextAsync(
            CreateMatchDto createDto, int requestingUserId);
        Task<MatchDto?> UpdateAsync(int id, UpdateMatchDto updateDto);

        /// <summary>Дані, потрібні FriendlyMatchPolicy, щоб вирішити, хто веде цей матч.</summary>
        Task<FriendlyMatchPolicy.Context> GetManageContextAsync(int id);

        /// <summary>Зовнішні посилання матчу. Порожній рядок прибирає відповідне.</summary>
        Task<MatchDto> UpdateLinksAsync(int id, string? streamUrl, string? trackerUrl);
        Task<bool> DeleteAsync(int id);
        Task<bool> StartMatchAsync(int id);
        Task<MatchDto> UpdateScoreAsync(int id, UpdateScoreDto dto);
        Task<bool> CompleteMatchAsync(int id, int? winnerTeamId, string? result, string? trackerUrl);
        Task<bool> CancelMatchAsync(int id, string? reason);
    }
}
