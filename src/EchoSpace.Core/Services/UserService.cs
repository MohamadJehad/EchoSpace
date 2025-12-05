using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace EchoSpace.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAnalyticsService _analyticsService;
        private readonly IImageService _imageService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository, 
            IAnalyticsService analyticsService,
            IImageService imageService,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _analyticsService = analyticsService;
            _imageService = imageService;
            _logger = logger;
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

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Name))
            {
                existing.Name = request.Name;
            }
            
            if (!string.IsNullOrEmpty(request.Email))
            {
                existing.Email = request.Email;
            }
            
            // Update role if provided
            if (request.Role.HasValue)
            {
                existing.Role = request.Role.Value;
            }
            
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

        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return false;
            }

            try
            {
                // Delete all images owned by the user (from both database and blob storage)
                var userImages = await _imageService.GetUserImagesAsync(id);
                foreach (var image in userImages)
                {
                    await _imageService.DeleteImageAsync(image.ImageId);
                }
                _logger.LogInformation("Deleted {Count} images for user {UserId}", userImages.Count(), id);

                // Delete user (repository will handle other related entities via cascade deletes)
                // Note: Comments, Likes, and Follows with Restrict behavior will be handled by repository
                return await _userRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                throw;
            }
        }

        public async Task<User?> LockUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // Effectively permanent lock until manually unlocked
            user.UpdatedAt = DateTime.UtcNow;

            // Terminate all active sessions for the user to revoke their tokens
            await _analyticsService.TerminateUserSessionsAsync(userId);

            return await _userRepository.UpdateAsync(user);
        }

        public async Task<User?> UnlockUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            // Fully unlock the account and reset failed attempts
            user.LockoutEnabled = false;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.UpdatedAt = DateTime.UtcNow;

            return await _userRepository.UpdateAsync(user);
        }
    }
}
