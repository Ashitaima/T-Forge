using TForge.Common;
using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface ILftService
    {
        Task<PagedResponse<TeamOpeningDto>> GetOpeningFeedAsync(PagedRequest request, int userId);
        Task<PagedResponse<PlayerSummaryDto>> GetCandidateFeedAsync(int openingId, PagedRequest request, int userId, bool isAdmin);
        Task<SwipeResultDto> SwipeAsync(SwipeDto dto, int userId, bool isAdmin);
    }
}
