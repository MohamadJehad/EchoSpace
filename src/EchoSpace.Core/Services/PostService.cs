using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.Core.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly ILikeRepository _likeRepository;
        private readonly ITagRepository _tagRepository;

        public PostService(
            IPostRepository postRepository, 
            ILikeRepository likeRepository,
            ITagRepository tagRepository)
        {
            _postRepository = postRepository;
            _likeRepository = likeRepository;
            _tagRepository = tagRepository;
        }

        public async Task<IEnumerable<PostDto>> GetAllAsync()
        {
            var posts = await _postRepository.GetAllAsync();
            return posts.Select(p => MapToDto(p, null));
        }


        public async Task<IEnumerable<PostDto>> GetAllAsync(Guid? currentUserId)
        {
            var posts = await _postRepository.GetAllAsync();
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
            
            // Reload with tags included
            var postWithTags = await _postRepository.GetByIdAsync(createdPost.PostId);
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

