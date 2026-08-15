using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface IMembershipRequestService
    {
        Task<MembershipRequestDto> InviteAsync(int teamId, int playerId, int requestingUserId, bool isAdmin);
        Task<MembershipRequestDto> ApplyAsync(int playerId, int teamId, int requestingUserId, bool isAdmin);
        Task<MembershipRequestDto> AcceptAsync(int requestId, int requestingUserId, bool isAdmin);
        Task<MembershipRequestDto> DeclineAsync(int requestId, int requestingUserId, bool isAdmin);
        Task<MembershipRequestDto> CancelAsync(int requestId, int requestingUserId, bool isAdmin);
        Task<IEnumerable<MembershipRequestDto>> GetForTeamAsync(int teamId, string? status, int requestingUserId, bool isAdmin);
        Task<IEnumerable<MembershipRequestDto>> GetForPlayerAsync(int playerId, string? status, int requestingUserId, bool isAdmin);
    }
}
