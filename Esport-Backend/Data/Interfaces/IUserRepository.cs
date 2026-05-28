using Computational_Practice.Models;

namespace Computational_Practice.Data.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetByRoleAsync(string role);
        Task<IEnumerable<User>> GetActiveUsersAsync();
    }
}
