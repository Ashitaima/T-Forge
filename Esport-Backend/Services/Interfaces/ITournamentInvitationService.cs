using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface ITournamentInvitationService
    {
        Task<TournamentInvitationDto> InviteAsync(
            int tournamentId, int teamId, string message, int requestingUserId, bool isAdmin);

        Task<TournamentInvitationDto> ApplyAsync(
            int tournamentId, int teamId, string message, int requestingUserId, bool isAdmin);

        Task<TournamentInvitationDto> AcceptAsync(int invitationId, int requestingUserId, bool isAdmin);
        Task<TournamentInvitationDto> DeclineAsync(int invitationId, int requestingUserId, bool isAdmin);
        Task<TournamentInvitationDto> CancelAsync(int invitationId, int requestingUserId, bool isAdmin);

        Task<IEnumerable<TournamentInvitationDto>> GetForTournamentAsync(int tournamentId, string? status);
        Task<IEnumerable<TournamentInvitationDto>> GetForTeamAsync(int teamId, string? status);
    }
}
