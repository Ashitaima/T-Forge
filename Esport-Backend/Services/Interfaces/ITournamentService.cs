using Computational_Practice.DTOs;
using Computational_Practice.Common;
using Computational_Practice.Common.Filters;

namespace Computational_Practice.Services.Interfaces
{
    public interface ITournamentService
    {
        Task<IEnumerable<TournamentDto>> GetAllActiveAsync();
        Task<PagedResponse<TournamentDto>> GetPagedAsync(TournamentFilter filter);
        Task<TournamentDto?> GetByIdAsync(int id);
        Task<TournamentDto?> GetWithMatchesAsync(int id);
        Task<IEnumerable<TournamentDto>> GetByStatusAsync(string status);
        Task<IEnumerable<TournamentDto>> GetByOrganizerAsync(int organizerId);
        Task<IEnumerable<TournamentDto>> GetByGameAsync(string game);
        Task<IEnumerable<TournamentDto>> GetUpcomingAsync();
        Task<TournamentDto> CreateAsync(CreateTournamentDto createDto);
        Task<TournamentDto?> UpdateAsync(int id, UpdateTournamentDto updateDto);
        Task<bool> DeleteAsync(int id);
        Task<TournamentStatsDto> GetStatsAsync();
    }
}
