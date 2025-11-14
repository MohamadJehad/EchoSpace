using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs;
using EchoSpace.Core.Enums;

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
        Task<User?> LockUserAsync(Guid userId);
        Task<User?> UnlockUserAsync(Guid userId);
        Task<User?> ChangeUserRoleAsync(Guid userId, UserRole newRole);
    }
}
