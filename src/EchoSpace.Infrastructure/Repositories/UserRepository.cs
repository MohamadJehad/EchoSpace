using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoSpace.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly EchoSpaceDbContext _dbContext;

        public UserRepository(EchoSpaceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbContext.Users
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Users
                .Include(u => u.ProfilePhoto)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> AddAsync(User user)
        {
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        public async Task<User?> UpdateAsync(User user)
        {
            var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (existing == null)
            {
                return null;
            }

            existing.Name = user.Name;
            existing.Email = user.Email;
            existing.ProfilePhotoId = user.ProfilePhotoId;
            existing.Role = user.Role;
            
            // Update lockout-related fields if they are being modified
            existing.LockoutEnabled = user.LockoutEnabled;
            existing.LockoutEnd = user.LockoutEnd;
            existing.AccessFailedCount = user.AccessFailedCount;
            
            existing.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _dbContext.Users
                .Include(u => u.Comments)
                .Include(u => u.Likes)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (existing == null)
            {
                return false;
            }

            // Delete related entities that have Restrict or NoAction delete behavior
            // Images are handled by UserService before calling this method
            
            // Delete all comments by this user (Restrict behavior requires manual deletion)
            var userComments = await _dbContext.Comments
                .Where(c => c.UserId == id)
                .ToListAsync();
            if (userComments.Any())
            {
                _dbContext.Comments.RemoveRange(userComments);
            }

            // Delete all likes by this user (Restrict behavior requires manual deletion)
            var userLikes = await _dbContext.Likes
                .Where(l => l.UserId == id)
                .ToListAsync();
            if (userLikes.Any())
            {
                _dbContext.Likes.RemoveRange(userLikes);
            }

            // Delete all follow relationships where this user is the follower or followed
            // (Restrict behavior requires manual deletion)
            var userFollows = await _dbContext.Follows
                .Where(f => f.FollowerId == id || f.FollowedId == id)
                .ToListAsync();
            if (userFollows.Any())
            {
                _dbContext.Follows.RemoveRange(userFollows);
            }

            // Delete all images owned by this user (NoAction behavior requires manual deletion)
            // Note: This is a fallback - UserService should handle this via ImageService
            // to also delete from blob storage, but we'll handle orphaned images here
            var userImages = await _dbContext.Images
                .Where(i => i.UserId == id)
                .ToListAsync();
            if (userImages.Any())
            {
                // Set UserId to null instead of deleting, as images might be referenced by posts
                // Or delete if they're not referenced by posts
                foreach (var image in userImages)
                {
                    if (image.PostId == null)
                    {
                        // Only delete images not associated with posts
                        _dbContext.Images.Remove(image);
                    }
                    else
                    {
                        // Set UserId to null for post images
                        image.UserId = null;
                    }
                }
            }

            // Now delete the user
            // Posts will cascade delete automatically (DeleteBehavior.Cascade)
            // UserSessions, AuthProviders, PasswordResetTokens, AccountUnlockTokens will cascade
            _dbContext.Users.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
