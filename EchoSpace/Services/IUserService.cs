using EchoSpace.Models;
using EchoSpace.DTOs;

namespace EchoSpace.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);
        Task<User> CreateAsync(CreateUserRequest request);
        Task<User?> UpdateAsync(Guid id, UpdateUserRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}


