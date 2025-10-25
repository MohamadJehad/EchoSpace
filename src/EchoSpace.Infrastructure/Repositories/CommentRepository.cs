using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoSpace.Infrastructure.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly EchoSpaceDbContext _dbContext;

        public CommentRepository(EchoSpaceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Comment>> GetAllAsync()
        {
            return await _dbContext.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Post)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentId == id);
        }

        public async Task<IEnumerable<Comment>> GetByPostIdAsync(Guid postId)
        {
            return await _dbContext.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Post)
                .Where(c => c.PostId == postId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Comment>> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Post)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment> AddAsync(Comment comment)
        {
            _dbContext.Comments.Add(comment);
            await _dbContext.SaveChangesAsync();
            return comment;
        }

        public async Task<Comment?> UpdateAsync(Comment comment)
        {
            var existing = await _dbContext.Comments.FirstOrDefaultAsync(c => c.CommentId == comment.CommentId);
            if (existing == null)
            {
                return null;
            }

            existing.Content = comment.Content;
            // Note: Comments don't have UpdatedAt field, but we could add one if needed

            await _dbContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _dbContext.Comments.FirstOrDefaultAsync(c => c.CommentId == id);
            if (existing == null)
            {
                return false;
            }

            _dbContext.Comments.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbContext.Comments.AnyAsync(c => c.CommentId == id);
        }

        public async Task<int> GetCountByPostIdAsync(Guid postId)
        {
            return await _dbContext.Comments.CountAsync(c => c.PostId == postId);
        }
    }
}

