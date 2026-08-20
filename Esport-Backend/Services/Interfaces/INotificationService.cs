using TForge.DTOs;

namespace TForge.Services.Interfaces
{
    public interface INotificationService
    {
        /// <summary>Останні сповіщення користувача, найновіші першими.</summary>
        Task<IEnumerable<NotificationDto>> GetForUserAsync(int userId);

        /// <summary>Лише лічильник: він оновлюється за таймером, список — ні.</summary>
        Task<int> GetUnreadCountAsync(int userId);

        /// <summary>Позначає все побачене поточним моментом.</summary>
        Task MarkSeenAsync(int userId);
    }
}
