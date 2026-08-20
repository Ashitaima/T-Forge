using AutoMapper;
using TForge.Data.Interfaces;
using TForge.DTOs;
using TForge.Models;
using TForge.Services.Interfaces;
using TForge.Exceptions;
using TForge.Common;

namespace TForge.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, IPasswordHasher passwordHasher, IMapper mapper, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _unitOfWork.Users.GetByUsernameAsync(loginDto.Username);
            if (user == null)
            {
                // Той самий текст, що й для хибного пароля: інакше форма видає,
                // чи існує такий логін узагалі.
                throw new BusinessLogicException("Невірний логін або пароль");
            }

            if (!VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                throw new BusinessLogicException("Невірний логін або пароль");
            }

            if (!user.IsActive)
            {
                throw new BusinessLogicException("Акаунт деактивовано");
            }

            // Вхід — єдина мить, коли пароль відомий у відкритому вигляді, тож
            // саме тут акаунт зі старим хешем тихо переходить на BCrypt.
            // Помилка запису не має заважати входу: пароль уже перевірено.
            if (_passwordHasher.NeedsRehash(user.PasswordHash))
            {
                try
                {
                    user.PasswordHash = HashPassword(loginDto.Password);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Пароль користувача {UserId} перехешовано на BCrypt", user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не вдалося перехешувати пароль користувача {UserId}", user.Id);
                }
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
                throw new BusinessLogicException("Користувач з такою поштою вже існує");
            }

            // Роль організатора дає право створювати турніри, тож вибором у
            // формі вона не видається: акаунт створюється гравцем, а поруч
            // лягає заявка, яку розглядає адміністратор
            // (див. Common/OrganizerRequestPolicy.cs).
            var wantsOrganizer = registerDto.Role == UserRoles.Organizer;

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Role = UserRoles.Player,
                PasswordHash = HashPassword(registerDto.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Роль «гравець» без профілю нічого не дає — користувач не зміг би навіть
            // подати заявку до команди. Створюємо обидва записи або жоден.
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.Players.AddAsync(new Player
                {
                    UserId = user.Id,
                    Nickname = registerDto.Nickname,
                    Ranking = 9999,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                if (wantsOrganizer)
                {
                    await _unitOfWork.OrganizerRequests.AddAsync(new OrganizerRequest
                    {
                        UserId = user.Id,
                        Message = "Заявка подана під час реєстрації",
                        Status = OrganizerRequestStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _unitOfWork.SaveChangesAsync();
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
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

        public async Task<UserDto?> GetCurrentUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }

        public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto updateDto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("User", userId);

            // Пошта унікальна: дозволяємо залишити власну, але не зайняти чужу.
            var emailOwner = await _unitOfWork.Users.GetByEmailAsync(updateDto.Email);
            if (emailOwner != null && emailOwner.Id != userId)
            {
                throw new BusinessLogicException("Користувач з такою поштою вже існує");
            }

            user.FirstName = updateDto.FirstName;
            user.LastName = updateDto.LastName;
            user.Email = updateDto.Email;
            user.IsNameHidden = updateDto.IsNameHidden;

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<UserDto>(user);
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

        private string HashPassword(string password) => _passwordHasher.Hash(password);

        private bool VerifyPassword(string password, string hash) => _passwordHasher.Verify(password, hash);
    }
}
