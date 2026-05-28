using Computational_Practice.DTOs;
using Computational_Practice.Common;
using Computational_Practice.Common.Filters;

namespace Computational_Practice.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<PagedResponse<UserDto>> GetPagedAsync(PagedRequest request, UserFilter? filter = null);
        Task<UserDto?> GetByIdAsync(int id);
        Task<UserDto?> GetByUsernameAsync(string username);
        Task<UserDto?> GetByEmailAsync(string email);
        Task<UserDto> CreateAsync(CreateUserDto createDto);
        Task<UserDto?> UpdateAsync(int id, UpdateUserDto updateDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ActivateAsync(int id);
        Task<bool> DeactivateAsync(int id);
    }
}
