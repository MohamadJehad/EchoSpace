using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Enums;
using Microsoft.Extensions.Logging;

namespace EchoSpace.Core.Services
{
    public class PostReportService : IPostReportService
    {
        private readonly IPostReportRepository _postReportRepository;
        private readonly IPostRepository _postRepository;
        private readonly IBlobStorageService? _blobStorageService;
        private readonly IImageRepository? _imageRepository;
        private readonly ILogger<PostReportService>? _logger;

        public PostReportService(
            IPostReportRepository postReportRepository, 
            IPostRepository postRepository,
            IBlobStorageService? blobStorageService = null,
            IImageRepository? imageRepository = null,
            ILogger<PostReportService>? logger = null)
        {
            _postReportRepository = postReportRepository;
            _postRepository = postRepository;
            _blobStorageService = blobStorageService;
            _imageRepository = imageRepository;
            _logger = logger;
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
            
            var result = new List<ReportedPostDto>();
            
            foreach (var p in reportedPosts)
            {
                var imageUrl = p.ImageUrl;
                
                // If post has an ImageUrl, try to get a fresh accessible URL if it's an AI-generated image
                if (!string.IsNullOrEmpty(imageUrl) && _imageRepository != null && _blobStorageService != null)
                {
                    try
                    {
                        // Check if there's an associated Image entity (AI-generated images have this)
                        var images = await _imageRepository.GetByPostIdAsync(p.PostId);
                        var aiImage = images.FirstOrDefault(i => i.Source == ImageSource.AIGenerated);
                        if (aiImage != null)
                        {
                            // Regenerate SAS token URL for secure access
                            imageUrl = await _blobStorageService.GetBlobUrlAsync(aiImage.ContainerName, aiImage.BlobName, 60);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to regenerate URL for reported post {PostId}, using stored URL", p.PostId);
                        // Continue with stored URL if regeneration fails
                    }
                }
                
                result.Add(new ReportedPostDto
                {
                    PostId = p.PostId,
                    UserId = p.UserId,
                    Content = p.Content,
                    ImageUrl = imageUrl,
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
            
            return result;
        }
    }
}

