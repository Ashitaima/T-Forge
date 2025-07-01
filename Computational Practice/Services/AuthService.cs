using AutoMapper;
using Computational_Practice.Data.Interfaces;
using Computational_Practice.DTOs;
using Computational_Practice.Models;
using Computational_Practice.Services.Interfaces;
using Computational_Practice.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace Computational_Practice.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, IMapper mapper, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _unitOfWork.Users.GetByUsernameAsync(loginDto.Username);
            if (user == null)
            {
                throw new EntityNotFoundException("User", loginDto.Username);
            }

            if (!VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                throw new BusinessLogicException("Некоректний пароль");
            }

            if (!user.IsActive)
            {
                throw new BusinessLogicException("Акаунт деактивовано");
            }

            var userDto = _mapper.Map<UserDto>(user);
            var token = _tokenService.GenerateToken(userDto);

            return new AuthResponseDto
            {
                Token = token,
                User = userDto,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var existingUser = await _unitOfWork.Users.GetByUsernameAsync(registerDto.Username);
            if (existingUser != null)
            {
                throw new BusinessLogicException("Користувач з таким іменем вже існує");
            }

            var existingEmail = await _unitOfWork.Users.GetByEmailAsync(registerDto.Email);
            if (existingEmail != null)
            {
                throw new BusinessLogicException("Користувач з таким email вже існує");
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Role = registerDto.Role,
                PasswordHash = HashPassword(registerDto.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var userDto = _mapper.Map<UserDto>(user);
            var token = _tokenService.GenerateToken(userDto);

            return new AuthResponseDto
            {
                Token = token,
                User = userDto,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        public async Task<UserDto?> GetCurrentUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                throw new EntityNotFoundException("User", userId);
            }

            if (!VerifyPassword(currentPassword, user.PasswordHash))
            {
                throw new BusinessLogicException("Поточний пароль некоректний");
            }

            user.PasswordHash = HashPassword(newPassword);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "SALT_KEY"));
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            var computedHash = HashPassword(password);
            return computedHash == hash;
        }
    }
}
