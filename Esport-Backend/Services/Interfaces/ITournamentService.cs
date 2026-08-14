using TForge.DTOs;
using TForge.Common;
using TForge.Common.Filters;

namespace TForge.Services.Interfaces
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
        Task<TournamentDto> CreateAsync(CreateTournamentDto createDto, int organizerId);
        Task<TournamentDto?> UpdateAsync(int id, UpdateTournamentDto updateDto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<TeamSummaryDto>> GetRegisteredTeamsAsync(int tournamentId);
        Task RegisterTeamAsync(int tournamentId, int teamId, int requestingUserId, bool isAdmin);
        Task WithdrawTeamAsync(int tournamentId, int teamId, int requestingUserId, bool isAdmin);
        Task<TournamentStatsDto> GetStatsAsync();
    }
}
