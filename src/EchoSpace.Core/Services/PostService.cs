using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Enums;
using Microsoft.Extensions.Logging;

namespace EchoSpace.Core.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly ILikeRepository _likeRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IAiImageGenerationService? _aiImageGenerationService;
        private readonly IBlobStorageService? _blobStorageService;
        private readonly IImageRepository? _imageRepository;
        private readonly ILogger<PostService>? _logger;

        public PostService(
            IPostRepository postRepository, 
            ILikeRepository likeRepository,
            ITagRepository tagRepository,
            IAiImageGenerationService? aiImageGenerationService = null,
            IBlobStorageService? blobStorageService = null,
            IImageRepository? imageRepository = null,
            ILogger<PostService>? logger = null)
        {
            _postRepository = postRepository;
            _likeRepository = likeRepository;
            _tagRepository = tagRepository;
            // These are optional - only inject if available
            _aiImageGenerationService = aiImageGenerationService;
            _blobStorageService = blobStorageService;
            _imageRepository = imageRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<PostDto>> GetAllAsync()
        {
            var posts = await _postRepository.GetAllAsync();
            // Use async mapping to ensure AI-generated images get fresh SAS tokens
            if (_imageRepository != null && _blobStorageService != null)
            {
                var postDtoTasks = posts.Select(p => MapToDtoAsync(p, null));
                return await Task.WhenAll(postDtoTasks);
            }
            return posts.Select(p => MapToDto(p, null));
        }


        public async Task<IEnumerable<PostDto>> GetAllAsync(Guid? currentUserId)
        {
            var posts = await _postRepository.GetAllAsync();
            // Use async mapping to ensure AI-generated images get fresh SAS tokens
            if (_imageRepository != null && _blobStorageService != null)
            {
                var postDtoTasks = posts.Select(p => MapToDtoAsync(p, currentUserId));
                return await Task.WhenAll(postDtoTasks);
            }
            return posts.Select(p => MapToDto(p, currentUserId));
        }

        // public async Task<UserDto?> GetOwner(Guid id)
        // {
            
        // }

        public async Task<PostDto?> GetByIdAsync(Guid id)
        {
            var post = await _postRepository.GetByIdAsync(id);
            return post == null ? null : MapToDto(post, null);
        }

        public async Task<PostDto?> GetByIdAsync(Guid id, Guid? currentUserId)
        {
            var post = await _postRepository.GetByIdAsync(id);
            return post == null ? null : MapToDto(post, currentUserId);
        }

        public async Task<IEnumerable<PostDto>> GetByUserIdAsync(Guid userId)
        {
            var posts = await _postRepository.GetByUserIdAsync(userId);
            return posts.Select(p => MapToDto(p, null));
        }

        public async Task<IEnumerable<PostDto>> GetByUserIdAsync(Guid userId, Guid? currentUserId)
        {
            var posts = await _postRepository.GetByUserIdAsync(userId);
            return posts.Select(p => MapToDto(p, currentUserId));
        }

        public async Task<IEnumerable<PostDto>> GetRecentAsync(int count = 10)
        {
            var posts = await _postRepository.GetRecentAsync(count);
            return posts.Select(p => MapToDto(p, null));
        }

        public async Task<IEnumerable<PostDto>> GetRecentAsync(int count, Guid? currentUserId)
        {
            var posts = await _postRepository.GetRecentAsync(count);
            return posts.Select(p => MapToDto(p, currentUserId));
        }

        public async Task<PostDto> CreateAsync(CreatePostRequest request)
        {
            var post = new Post
            {
                PostId = Guid.NewGuid(),
                UserId = request.UserId,
                Content = request.Content,
                ImageUrl = request.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            // Add tags if provided
            if (request.TagIds != null && request.TagIds.Any())
            {
                foreach (var tagId in request.TagIds)
                {
                    var tag = await _tagRepository.GetByIdAsync(tagId);
                    if (tag != null)
                    {
                        post.PostTags.Add(new PostTag
                        {
                            PostTagId = Guid.NewGuid(),
                            PostId = post.PostId,
                            TagId = tagId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            var createdPost = await _postRepository.AddAsync(post);
            
            // Generate AI image if requested
            if (request.GenerateImage && _aiImageGenerationService != null && _blobStorageService != null && _imageRepository != null)
            {
                try
                {
                    _logger?.LogInformation("Generating AI image for post {PostId}", createdPost.PostId);
                    
                    // Generate image using AI service
                    var imageResult = await _aiImageGenerationService.GenerateImageAsync(request.Content);
                    
                    // Download image bytes from the generated URL
                    byte[] imageBytes;
                    using var httpClient = new System.Net.Http.HttpClient();
                    var response = await httpClient.GetAsync(imageResult.ImageUrl);
                    response.EnsureSuccessStatusCode();
                    imageBytes = await response.Content.ReadAsByteArrayAsync();
                    
                    // Determine content type (Pollinations typically returns PNG)
                    var contentType = "image/png";
                    if (imageBytes.Length > 0)
                    {
                        // Check magic bytes to determine actual type
                        if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
                            contentType = "image/jpeg";
                        else if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50)
                            contentType = "image/png";
                    }
                    
                    // Upload to blob storage
                    var containerName = "ai-images";
                    var imageId = Guid.NewGuid();
                    var blobName = imageId.ToString();
                    
                    var blobUrl = await _blobStorageService.UploadBlobAsync(
                        containerName,
                        blobName,
                        imageBytes,
                        contentType);
                    
                    // Generate SAS token URL for secure access (valid for 1 hour)
                    var accessibleUrl = await _blobStorageService.GetBlobUrlAsync(containerName, blobName, 60);
                    
                    // Create image entity
                    var image = new Image
                    {
                        ImageId = imageId,
                        Source = ImageSource.AIGenerated,
                        OriginalFileName = $"ai-generated-{imageId}.png",
                        ContentType = contentType,
                        SizeInBytes = imageBytes.Length,
                        BlobName = blobName,
                        ContainerName = containerName,
                        UserId = request.UserId,
                        PostId = createdPost.PostId,
                        Description = $"AI-generated image for post: {request.Content.Substring(0, Math.Min(100, request.Content.Length))}...",
                        Url = blobUrl,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    // Save image to database
                    await _imageRepository.AddAsync(image);
                    
                    // Update post with accessible SAS token URL
                    createdPost.ImageUrl = accessibleUrl;
                    await _postRepository.UpdateAsync(createdPost);
                    
                    _logger?.LogInformation("AI image generated and stored for post {PostId}, ImageId: {ImageId}", createdPost.PostId, imageId);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to generate AI image for post {PostId}", createdPost.PostId);
                    // Continue without image - don't fail post creation
                }
            }
            
            // Reload with tags included
            var postWithTags = await _postRepository.GetByIdAsync(createdPost.PostId);
            if (postWithTags != null && _imageRepository != null && _blobStorageService != null)
            {
                return await MapToDtoAsync(postWithTags, request.UserId);
            }
            return MapToDto(postWithTags ?? createdPost, request.UserId);
        }

        public async Task<PostDto?> UpdateAsync(Guid id, UpdatePostRequest request)
        {
            var existing = await _postRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return null;
            }

            existing.Content = request.Content;
            existing.ImageUrl = request.ImageUrl;
            existing.UpdatedAt = DateTime.UtcNow;

            var updatedPost = await _postRepository.UpdateAsync(existing);
            return updatedPost == null ? null : MapToDto(updatedPost, null);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _postRepository.DeleteAsync(id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _postRepository.ExistsAsync(id);
        }

        public async Task<bool> IsOwnerAsync(Guid postId, Guid userId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            return post?.UserId == userId;
        }

        public async Task<IEnumerable<PostDto>> GetPostsFromFollowingAsync(Guid userId)
        {
            var posts = await _postRepository.GetByFollowingUsersAsync(userId);
            return posts.Select(p => MapToDto(p, userId));
        }

        public async Task<IEnumerable<PostDto>> GetByTagIdAsync(Guid tagId)
        {
            var posts = await _postRepository.GetByTagIdAsync(tagId);
            return posts.Select(p => MapToDto(p, null));
        }

        public async Task<IEnumerable<PostDto>> GetByTagIdAsync(Guid tagId, Guid? currentUserId)
        {
            var posts = await _postRepository.GetByTagIdAsync(tagId);
            return posts.Select(p => MapToDto(p, currentUserId));
        }

        private async Task<PostDto> MapToDtoAsync(Post post, Guid? currentUserId)
        {
            var imageUrl = post.ImageUrl;
            
            // If post has an ImageUrl, try to get a fresh accessible URL if it's an AI-generated image
            if (!string.IsNullOrEmpty(imageUrl) && _imageRepository != null && _blobStorageService != null)
            {
                try
                {
                    // Check if there's an associated Image entity (AI-generated images have this)
                    var images = await _imageRepository.GetByPostIdAsync(post.PostId);
                    var aiImage = images.FirstOrDefault(i => i.Source == ImageSource.AIGenerated);
                    if (aiImage != null)
                    {
                        // Regenerate SAS token URL for secure access
                        imageUrl = await _blobStorageService.GetBlobUrlAsync(aiImage.ContainerName, aiImage.BlobName, 60);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to regenerate URL for post {PostId}, using stored URL", post.PostId);
                    // Continue with stored URL if regeneration fails
                }
            }
            
            return new PostDto
            {
                PostId = post.PostId,
                UserId = post.UserId,
                Content = post.Content,
                ImageUrl = imageUrl,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                LikesCount = post.Likes?.Count ?? 0,
                CommentsCount = post.Comments?.Count ?? 0,
                IsLikedByCurrentUser = false, // Will be set by controller using LikeService
                
                // Author information from User navigation property
                AuthorName = post.User?.Name ?? string.Empty,
                AuthorEmail = post.User?.Email ?? string.Empty,
                AuthorUserName = post.User?.UserName ?? string.Empty,
                
                // Tag information from PostTags navigation property
                Tags = post.PostTags?
                    .Select(pt => new TagInfoDto
                    {
                        TagId = pt.Tag.TagId,
                        Name = pt.Tag.Name,
                        Color = pt.Tag.Color
                    })
                    .ToList() ?? new List<TagInfoDto>()
            };
        }

        private PostDto MapToDto(Post post, Guid? currentUserId)
        {
            // Synchronous version - will be set by controller if currentUserId is provided
            return new PostDto
            {
                PostId = post.PostId,
                UserId = post.UserId,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                LikesCount = post.Likes?.Count ?? 0,
                CommentsCount = post.Comments?.Count ?? 0,
                IsLikedByCurrentUser = false, // Will be set by controller using LikeService
                
                // Author information from User navigation property
                AuthorName = post.User?.Name ?? string.Empty,
                AuthorEmail = post.User?.Email ?? string.Empty,
                AuthorUserName = post.User?.UserName ?? string.Empty,
                
                // Tag information from PostTags navigation property
                Tags = post.PostTags?
                    .Select(pt => new TagInfoDto
                    {
                        TagId = pt.Tag.TagId,
                        Name = pt.Tag.Name,
                        Color = pt.Tag.Color
                    })
                    .ToList() ?? new List<TagInfoDto>()
            };
        }
    }
}

