using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1;

namespace EchoSpace.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly EchoSpaceDbContext _dbContext;

        public PostRepository(EchoSpaceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Post>> GetAllAsync()
        {
            return await _dbContext.Posts
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Post?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Posts
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.PostId == id);
        }

        public async Task<IEnumerable<Post>> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.Posts
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Post>> GetRecentAsync(int count = 10)
        {
            return await _dbContext.Posts
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Post> AddAsync(Post post)
        {
            _dbContext.Posts.Add(post);
            await _dbContext.SaveChangesAsync();
            return post;
        }

        public async Task<Post?> UpdateAsync(Post post)
        {
            var existing = await _dbContext.Posts.FirstOrDefaultAsync(p => p.PostId == post.PostId);
            if (existing == null)
            {
                return null;
            }

            existing.Content = post.Content;
            existing.ImageUrl = post.ImageUrl;
            existing.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _dbContext.Posts.FirstOrDefaultAsync(p => p.PostId == id);
            if (existing == null)
            {
                return false;
            }

            _dbContext.Posts.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbContext.Posts.AnyAsync(p => p.PostId == id);
        }

        public async Task<IEnumerable<Post>> GetByFollowingUsersAsync(Guid userId)
        {
            // Get posts from users that the current user is following
            var followedUserIds = await _dbContext.Follows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowedId)
                .ToListAsync();

            return await _dbContext.Posts
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Where(p => followedUserIds.Contains(p.UserId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}
