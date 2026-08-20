using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface IOrganizerRequestService
    {
        Task<OrganizerRequestDto> ApplyAsync(int userId, CreateOrganizerRequestDto createDto);

        /// <summary>Схвалення переводить User.Role у Organizer — це єдиний шлях до цієї ролі.</summary>
        Task<OrganizerRequestDto> ApproveAsync(int requestId, int adminUserId, bool isAdmin);

        Task<OrganizerRequestDto> DeclineAsync(
            int requestId, int adminUserId, bool isAdmin, RespondOrganizerRequestDto respondDto);

        Task<OrganizerRequestDto> CancelAsync(int requestId, int userId);

        /// <summary>Заявки одного користувача — його власна історія.</summary>
        Task<IEnumerable<OrganizerRequestDto>> GetForUserAsync(int userId);

        /// <summary>Список для адміністратора, за потреби звужений до одного статусу.</summary>
        Task<IEnumerable<OrganizerRequestDto>> GetAllAsync(string? status);
    }
}
