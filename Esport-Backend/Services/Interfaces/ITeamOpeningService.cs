using TForge.Common;
using TForge.Common.Filters;
using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface ITeamOpeningService
    {
        Task<TeamOpeningDto> CreateAsync(CreateTeamOpeningDto dto, int userId, bool isAdmin);
        Task<TeamOpeningDto> UpdateAsync(int id, UpdateTeamOpeningDto dto, int userId, bool isAdmin);
        Task<TeamOpeningDto> CloseAsync(int id, int userId, bool isAdmin);
        Task<TeamOpeningDto> GetByIdAsync(int id);
        Task<PagedResponse<TeamOpeningDto>> GetPagedAsync(TeamOpeningFilter filter);
        Task<IEnumerable<TeamOpeningDto>> GetByTeamAsync(int teamId);
    }
}
