using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoSpace.Infrastructure.Repositories
{
    public class LikeRepository : ILikeRepository
    {
        private readonly EchoSpaceDbContext _dbContext;

        public LikeRepository(EchoSpaceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> LikePostAsync(Guid postId, Guid userId)
        {
            // Check if already liked
            var existingLike = await _dbContext.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (existingLike != null)
            {
                return false; // Already liked
            }

            var like = new Like
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Likes.Add(like);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlikePostAsync(Guid postId, Guid userId)
        {
            var like = await _dbContext.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (like == null)
            {
                return false; // Not liked
            }

            _dbContext.Likes.Remove(like);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsLikedByUserAsync(Guid postId, Guid userId)
        {
            return await _dbContext.Likes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId);
        }

        public async Task<int> GetLikeCountAsync(Guid postId)
        {
            return await _dbContext.Likes
                .CountAsync(l => l.PostId == postId);
        }

        public async Task<IEnumerable<Like>> GetLikesByPostAsync(Guid postId)
        {
            return await _dbContext.Likes
                .Where(l => l.PostId == postId)
                .Include(l => l.User)
                .ToListAsync();
        }
    }
}

