using AutoMapper;
using Computational_Practice.Data.Interfaces;
using Computational_Practice.DTOs;
using Computational_Practice.Models;
using Computational_Practice.Services.Interfaces;
using Computational_Practice.Common;
using Computational_Practice.Common.Filters;
using Computational_Practice.Extensions;

namespace Computational_Practice.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return false;

            user.IsActive = false;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
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
