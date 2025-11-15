using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.Core.Services
{
    public class PostReportService : IPostReportService
    {
        private readonly IPostReportRepository _postReportRepository;
        private readonly IPostRepository _postRepository;

        public PostReportService(IPostReportRepository postReportRepository, IPostRepository postRepository)
        {
            _postReportRepository = postReportRepository;
            _postRepository = postRepository;
        }

        public async Task<bool> ReportPostAsync(Guid postId, Guid userId, string? reason)
        {
            // Check if post exists
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                throw new ArgumentException("Post not found", nameof(postId));
            }

            // Prevent users from reporting their own posts
            if (post.UserId == userId)
            {
                throw new InvalidOperationException("Users cannot report their own posts");
            }

            // Check if already reported
            if (await _postReportRepository.HasUserReportedPostAsync(postId, userId))
            {
                return false; // Already reported
            }

            return await _postReportRepository.ReportPostAsync(postId, userId, reason);
        }

        public async Task<bool> HasUserReportedPostAsync(Guid postId, Guid userId)
        {
            return await _postReportRepository.HasUserReportedPostAsync(postId, userId);
        }

        public async Task<int> GetReportCountAsync(Guid postId)
        {
            return await _postReportRepository.GetReportCountAsync(postId);
        }

        public async Task<IEnumerable<ReportedPostDto>> GetReportedPostsAsync()
        {
            var reportedPosts = await _postReportRepository.GetReportedPostsAsync();
            
            return reportedPosts.Select(p => new ReportedPostDto
            {
                PostId = p.PostId,
                UserId = p.UserId,
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ReportCount = p.Reports?.Count ?? 0,
                AuthorName = p.User?.Name ?? string.Empty,
                AuthorEmail = p.User?.Email ?? string.Empty,
                AuthorUserName = p.User?.UserName ?? string.Empty,
                LikesCount = p.Likes?.Count ?? 0,
                CommentsCount = p.Comments?.Count ?? 0,
                Reports = p.Reports?.Select(r => new ReportInfoDto
                {
                    ReportId = r.ReportId,
                    UserId = r.UserId,
                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt,
                    ReporterName = r.User?.Name ?? string.Empty,
                    ReporterEmail = r.User?.Email ?? string.Empty
                }).ToList() ?? new List<ReportInfoDto>()
            });
        }
    }
}

