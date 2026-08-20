using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TForge.DTOs;
using TForge.Services.Interfaces;

namespace TForge.Controllers
{
    /// <summary>
    /// Сповіщення поточного користувача.
    ///
    /// Жоден маршрут не приймає id користувача — його завжди беремо з токена.
    /// Тому тут просто немає перевірки власності, яку можна було б забути:
    /// сповіщення адресують, а не володіють ним, і «адмін вище за будь-яке
    /// правило власності» не означає, що адмін читає чужу пошту.
    ///
    /// Читання тут навмисно не публічне — на відміну від таблиць і профілів,
    /// які є фактами про гру. Так само влаштовано GET /api/match-challenges/pending.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ApiControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications()
        {
            return Ok(await _notificationService.GetForUserAsync(GetUserIdOrThrow()));
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            return Ok(await _notificationService.GetUnreadCountAsync(GetUserIdOrThrow()));
        }

        [HttpPost("seen")]
        public async Task<ActionResult> MarkSeen()
        {
            await _notificationService.MarkSeenAsync(GetUserIdOrThrow());
            return NoContent();
        }
    }
}
