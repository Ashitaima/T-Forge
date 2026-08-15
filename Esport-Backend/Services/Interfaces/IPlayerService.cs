using TForge.DTOs;
using TForge.Common;
using TForge.Common.Filters;

namespace TForge.Services.Interfaces
{
    public interface IPlayerService
    {
        Task<IEnumerable<PlayerDto>> GetAllAsync();
        Task<PagedResponse<PlayerDto>> GetPagedAsync(PagedRequest request, PlayerFilter? filter = null);
        Task<PlayerDto?> GetByIdAsync(int id);
        Task<PlayerProfileDto> GetProfileAsync(int id);
        Task<PlayerDto?> GetWithTeamAsync(int id);
        Task<PagedResponse<PlayerMatchDto>> GetMatchLogAsync(int id, PagedRequest request);
        Task<IEnumerable<PlayerDto>> GetByTeamAsync(int teamId);
        Task<IEnumerable<PlayerDto>> GetFreeAgentsAsync();
        Task<PlayerDto> CreateAsync(CreatePlayerDto createDto, int userId);
        Task<PlayerDto?> UpdateAsync(int id, UpdatePlayerDto updateDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> LeaveTeamAsync(int playerId);
    }
}
