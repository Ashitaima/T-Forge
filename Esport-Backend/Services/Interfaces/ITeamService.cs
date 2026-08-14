using TForge.DTOs;
using TForge.Common;
using TForge.Common.Filters;

namespace TForge.Services.Interfaces
{
    public interface ITeamService
    {
        Task<IEnumerable<TeamDto>> GetAllAsync();
        Task<PagedResponse<TeamDto>> GetPagedAsync(PagedRequest request, TeamFilter? filter = null);
        Task<TeamDto?> GetByIdAsync(int id);
        Task<TeamDto?> GetWithPlayersAsync(int id);
        Task<IEnumerable<TeamDto>> GetByTournamentAsync(int tournamentId);
        Task<TeamDto> CreateAsync(CreateTeamDto createDto, int captainId);
        Task<TeamDto?> UpdateAsync(int id, UpdateTeamDto updateDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> AddPlayerToTeamAsync(int teamId, int playerId);
        Task<bool> RemovePlayerFromTeamAsync(int teamId, int playerId);
    }
}
