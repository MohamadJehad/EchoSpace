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
            var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (existing == null)
            {
                return false;
            }

            _dbContext.Users.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
