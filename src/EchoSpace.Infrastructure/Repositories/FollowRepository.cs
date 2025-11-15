using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoSpace.Infrastructure.Repositories
{
    public class FollowRepository : IFollowRepository
    {
        private readonly EchoSpaceDbContext _dbContext;

        public FollowRepository(EchoSpaceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Follow?> GetFollowAsync(Guid followerId, Guid followedId)
        {
            return await _dbContext.Follows
                .AsNoTracking()
                .Include(f => f.Follower)
                .Include(f => f.Followed)
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);
        }

        public async Task<bool> FollowAsync(Guid followerId, Guid followedId)
        {
            // Prevent self-follow
            if (followerId == followedId)
            {
                return false;
            }

            // Check if already following
            var existing = await _dbContext.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);

            if (existing != null)
            {
                return false; // Already following
            }

            var follow = new Follow
            {
                FollowerId = followerId,
                FollowedId = followedId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Follows.Add(follow);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnfollowAsync(Guid followerId, Guid followedId)
        {
            var follow = await _dbContext.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);

            if (follow == null)
            {
                return false;
            }

            _dbContext.Follows.Remove(follow);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsFollowingAsync(Guid followerId, Guid followedId)
        {
            return await _dbContext.Follows
                .AnyAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);
        }

        public async Task<IEnumerable<Follow>> GetFollowersAsync(Guid userId)
        {
            return await _dbContext.Follows
                .AsNoTracking()
                .Include(f => f.Follower)
                .Where(f => f.FollowedId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Follow>> GetFollowingAsync(Guid userId)
        {
            return await _dbContext.Follows
                .AsNoTracking()
                .Include(f => f.Followed)
                .Where(f => f.FollowerId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetFollowerCountAsync(Guid userId)
        {
            return await _dbContext.Follows
                .CountAsync(f => f.FollowedId == userId);
        }

        public async Task<int> GetFollowingCountAsync(Guid userId)
        {
            return await _dbContext.Follows
                .CountAsync(f => f.FollowerId == userId);
        }

        public async Task<Dictionary<Guid, bool>> GetFollowStatusesAsync(Guid followerId, IEnumerable<Guid> followedIds)
        {
            var followedIdsList = followedIds.ToList();
            if (!followedIdsList.Any())
            {
                return new Dictionary<Guid, bool>();
            }

            var followStatuses = await _dbContext.Follows
                .AsNoTracking()
                .Where(f => f.FollowerId == followerId && followedIdsList.Contains(f.FollowedId))
                .Select(f => f.FollowedId)
                .ToListAsync();

            var result = new Dictionary<Guid, bool>();
            foreach (var followedId in followedIdsList)
            {
                result[followedId] = followStatuses.Contains(followedId);
            }

            return result;
        }
    }
}

