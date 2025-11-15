using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoSpace.Infrastructure.Repositories
{
    public class PostReportRepository : IPostReportRepository
    {
        private readonly EchoSpaceDbContext _dbContext;

        public PostReportRepository(EchoSpaceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> ReportPostAsync(Guid postId, Guid userId, string? reason)
        {
            // Check if already reported by this user
            var existingReport = await _dbContext.PostReports
                .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);

            if (existingReport != null)
            {
                return false; // Already reported
            }

            var report = new PostReport
            {
                PostId = postId,
                UserId = userId,
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.PostReports.Add(report);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasUserReportedPostAsync(Guid postId, Guid userId)
        {
            return await _dbContext.PostReports
                .AnyAsync(r => r.PostId == postId && r.UserId == userId);
        }

        public async Task<int> GetReportCountAsync(Guid postId)
        {
            return await _dbContext.PostReports
                .CountAsync(r => r.PostId == postId);
        }

        public async Task<IEnumerable<PostReport>> GetReportsByPostAsync(Guid postId)
        {
            return await _dbContext.PostReports
                .Where(r => r.PostId == postId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetReportedPostsAsync()
        {
            // Get all posts that have at least one report
            var reportedPostIds = await _dbContext.PostReports
                .Select(r => r.PostId)
                .Distinct()
                .ToListAsync();

            return await _dbContext.Posts
                .Where(p => reportedPostIds.Contains(p.PostId))
                .Include(p => p.User)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Include(p => p.Reports)
                    .ThenInclude(r => r.User)
                .OrderByDescending(p => p.Reports.Count)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PostReport>> GetAllReportsAsync()
        {
            return await _dbContext.PostReports
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}

