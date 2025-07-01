using Computational_Practice.DTOs;
using Computational_Practice.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Computational_Practice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            return CreatedAtAction(nameof(GetProfile), null, result);
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Некоректний токен");
            }

            var user = await _authService.GetCurrentUserAsync(userId);
            if (user == null)
            {
                return NotFound("Користувача не знайдено");
            }

            return Ok(user);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Некоректний токен");
            }

            await _authService.ChangePasswordAsync(userId, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            return Ok(new { message = "Пароль успішно змінено" });
        }

        [HttpPost("logout")]
        [Authorize]
        public ActionResult Logout()
        {
            return Ok(new { message = "Вихід виконано успішно" });
        }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
