using Computational_Practice.DTOs;

namespace Computational_Practice.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto?> GetCurrentUserAsync(int userId);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
