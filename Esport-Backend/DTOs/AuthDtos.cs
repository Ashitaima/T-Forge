using TForge.Common;

namespace TForge.DTOs
{
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        /// <summary>Типова роль — гравець; «User» більше не існує.</summary>
        public string Role { get; set; } = UserRoles.Player;

        /// <summary>Обов'язковий лише для ролі Player — з нього створюється профіль гравця.</summary>
        public string Nickname { get; set; } = string.Empty;
    }

    /// <summary>
    /// Користувач редагує власні дані. Username незмінний — це логін,
    /// на нього посилається виданий токен.
    /// </summary>
    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
