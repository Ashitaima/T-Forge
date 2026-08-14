using TForge.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace TForge.Controllers
{
    /// <summary>
    /// Спільні помічники для роботи з поточним користувачем.
    /// </summary>
    public abstract class ApiControllerBase : ControllerBase
    {
        protected bool IsAdmin => User.IsInRole("Admin");

        protected int GetUserIdOrThrow()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                throw new BusinessLogicException("Некоректний токен");
            }

            return userId;
        }

        /// <summary>
        /// Власника (капітана, організатора, користувача) визначає сервер за токеном.
        /// Явно передати чужий id може лише адміністратор — решті це поле ігнорується,
        /// тому вводити його вручну у формі не потрібно.
        /// </summary>
        protected int ResolveOwnerId(int? requestedOwnerId)
        {
            if (IsAdmin && requestedOwnerId.HasValue && requestedOwnerId.Value > 0)
            {
                return requestedOwnerId.Value;
            }

            return GetUserIdOrThrow();
        }
    }
}
