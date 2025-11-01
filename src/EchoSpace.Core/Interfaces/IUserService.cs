using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs;

namespace EchoSpace.Core.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);
        Task<User> CreateAsync(CreateUserRequest request);
        Task<User?> UpdateAsync(Guid id, UpdateUserRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task<User?> UpdateProfilePhotoAsync(Guid userId, Guid? profilePhotoId);
    }
}
