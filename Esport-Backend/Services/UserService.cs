using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TForge.Data.Interfaces;
using TForge.Exceptions;
using TForge.DTOs;
using TForge.Models;
using TForge.Services.Interfaces;
using TForge.Common;
using TForge.Common.Filters;
using TForge.Extensions;

namespace TForge.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAvatarService _avatarService;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, IAvatarService avatarService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _avatarService = avatarService;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<PagedResponse<UserDto>> GetPagedAsync(PagedRequest request, UserFilter? filter = null)
        {
            var query = _unitOfWork.Users.GetQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Role))
                {
                    query = query.Where(u => u.Role == filter.Role);
                }

                if (filter.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == filter.IsActive.Value);
                }

                if (filter.CreatedFrom.HasValue)
                {
                    query = query.Where(u => u.CreatedAt >= filter.CreatedFrom.Value);
                }

                if (filter.CreatedTo.HasValue)
                {
                    query = query.Where(u => u.CreatedAt <= filter.CreatedTo.Value);
                }
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.ApplySearch(request.Search, "Username", "Email");
            }

            if (!string.IsNullOrEmpty(request.SortBy))
            {
                query = query.ApplySorting(request.SortBy, request.SortDirection);
            }

            return await query.ToPagedResponseAsync<User, UserDto>(request, _mapper);
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }

        public async Task<UserDto?> GetByUsernameAsync(string username)
        {
            var user = await _unitOfWork.Users.GetByUsernameAsync(username);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }

        public async Task<IEnumerable<UserDto>> GetByRoleAsync(string role)
        {
            var users = await _unitOfWork.Users.GetByRoleAsync(role);
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto> CreateAsync(CreateUserDto createDto)
        {
            var user = _mapper.Map<User>(createDto);
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto?> UpdateAsync(int id, UpdateUserDto updateDto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return null;

            _mapper.Map(updateDto, user);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<UserDto>(user);
        }

        /// <summary>
        /// Видалення акаунта — це знеособлення, а не DELETE рядка.
        ///
        /// Ланцюг зовнішніх ключів User → Player → MatchPlayer каскадний, тож
        /// справжнє видалення тихо стерло б кожну появу гравця в кожному
        /// зіграному матчі й частину append-only журналу рейтингу. Це та сама
        /// причина, з якої MatchService не дає видалити зіграний матч: бали
        /// лишилися б, а пояснити їх було б нічим.
        ///
        /// Особисті дані натомість затираються, а рядки лишаються на місці.
        /// Значення виводяться з id, бо Username, Email і Player.Nickname
        /// несуть унікальні індекси (див. Common/AccountAnonymizer.cs).
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return false;

            if (AccountAnonymizer.IsAnonymized(user.Username))
            {
                // Повторне видалення нічого не змінює, але мовчазний успіх
                // приховав би від адміністратора, що акаунта вже немає.
                throw new BusinessLogicException("Цей акаунт уже видалено");
            }

            // Капітанство — єдине, що не можна просто знеособити: команда
            // лишилася б із капітаном-порожнечею, а Team.CaptainId міняє лише
            // PUT /api/teams/{id}/captain.
            var captainedTeam = await _unitOfWork.Teams.GetQueryable()
                .Where(t => t.CaptainId == id)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();

            if (captainedTeam != null)
            {
                throw new BusinessLogicException(
                    $"Спершу передайте капітанство команди «{captainedTeam}» — інакше вона лишиться без капітана");
            }

            user.Username = AccountAnonymizer.UsernameFor(user.Id);
            user.Email = AccountAnonymizer.EmailFor(user.Id);
            user.FirstName = string.Empty;
            user.LastName = string.Empty;
            user.PasswordHash = AccountAnonymizer.UnusablePasswordHash;
            user.AvatarPath = null;
            user.IsActive = false;

            // Профіль гравця лишається: на MatchPlayer тримається вся
            // статистика, і його рядки мусять і далі на когось посилатися.
            var player = await _unitOfWork.Players.GetFirstOrDefaultAsync(p => p.UserId == id);
            if (player != null)
            {
                player.Nickname = AccountAnonymizer.NicknameFor(player.Id);
                player.Country = string.Empty;
                player.Age = 0;
                player.RiotId = null;
                player.SteamId64 = null;
                player.BattleTag = null;
                player.IsActive = false;
                _unitOfWork.Players.Update(player);
            }

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Файл аватара прибираємо після збереження рядка — так само, як в
            // ImageUploadService: видалити спершу й не зберегти означало б
            // втратити файл ні за що.
            await _avatarService.ClearAsync(id);

            return true;
        }

        public async Task<bool> ActivateAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return false;

            user.IsActive = true;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return false;

            user.IsActive = false;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
