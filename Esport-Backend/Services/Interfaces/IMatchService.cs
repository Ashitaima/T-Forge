using Computational_Practice.DTOs;
using Computational_Practice.Common;
using Computational_Practice.Common.Filters;

namespace Computational_Practice.Services.Interfaces
{
    public interface IMatchService
    {
        Task<IEnumerable<MatchDto>> GetAllAsync();
        Task<PagedResponse<MatchDto>> GetPagedAsync(PagedRequest request, MatchFilter? filter = null);
        Task<MatchDto?> GetByIdAsync(int id);
        Task<MatchDto?> GetWithDetailsAsync(int id);
        Task<IEnumerable<MatchDto>> GetByTournamentAsync(int tournamentId);
        Task<IEnumerable<MatchDto>> GetByTeamAsync(int teamId);
        Task<IEnumerable<MatchDto>> GetByPlayerAsync(int playerId);
        Task<IEnumerable<MatchDto>> GetByStatusAsync(string status);
        Task<IEnumerable<MatchDto>> GetScheduledMatchesAsync();
        Task<IEnumerable<MatchDto>> GetCompletedMatchesAsync();
        Task<MatchDto> CreateAsync(CreateMatchDto createDto);
        Task<MatchDto?> UpdateAsync(int id, UpdateMatchDto updateDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> StartMatchAsync(int id);
        Task<bool> CompleteMatchAsync(int id, int? winnerTeamId, string? result);
        Task<bool> CancelMatchAsync(int id, string? reason);
    }
}
