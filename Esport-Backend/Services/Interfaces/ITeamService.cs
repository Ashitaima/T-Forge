using Computational_Practice.DTOs;
using Computational_Practice.Common;
using Computational_Practice.Common.Filters;

namespace Computational_Practice.Services.Interfaces
{
    public interface ITeamService
    {
        Task<IEnumerable<TeamDto>> GetAllAsync();
        Task<PagedResponse<TeamDto>> GetPagedAsync(PagedRequest request, TeamFilter? filter = null);
        Task<TeamDto?> GetByIdAsync(int id);
        Task<TeamDto?> GetWithPlayersAsync(int id);
        Task<IEnumerable<TeamDto>> GetByTournamentAsync(int tournamentId);
        Task<TeamDto> CreateAsync(CreateTeamDto createDto);
        Task<TeamDto?> UpdateAsync(int id, UpdateTeamDto updateDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> AddPlayerToTeamAsync(int teamId, int playerId);
        Task<bool> RemovePlayerFromTeamAsync(int teamId, int playerId);
    }
}
