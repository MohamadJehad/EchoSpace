using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<IEnumerable<User>> GetAllAsync()
        {
            return _userRepository.GetAllAsync();
        }

        public Task<User?> GetByIdAsync(Guid id)
        {
            return _userRepository.GetByIdAsync(id);
        }

        public async Task<User> CreateAsync(CreateUserRequest request)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
            return await _userRepository.AddAsync(user);
        }

        public async Task<User?> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            var existing = await _userRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return null;
            }

            existing.Name = request.Name;
            existing.Email = request.Email;
            existing.UpdatedAt = DateTime.UtcNow;

            return await _userRepository.UpdateAsync(existing);
        }

        public async Task<User?> UpdateProfilePhotoAsync(Guid userId, Guid? profilePhotoId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            user.ProfilePhotoId = profilePhotoId;
            user.UpdatedAt = DateTime.UtcNow;

            return await _userRepository.UpdateAsync(user);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            return _userRepository.DeleteAsync(id);
        }
    }
}
