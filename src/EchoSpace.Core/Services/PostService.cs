using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.Core.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;

        public PostService(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<IEnumerable<PostDto>> GetAllAsync()
        {
            var posts = await _postRepository.GetAllAsync();
            return posts.Select(MapToDto);
        }

        public async Task<PostDto?> GetByIdAsync(Guid id)
        {
            var post = await _postRepository.GetByIdAsync(id);
            return post == null ? null : MapToDto(post);
        }

        public async Task<IEnumerable<PostDto>> GetByUserIdAsync(Guid userId)
        {
            var posts = await _postRepository.GetByUserIdAsync(userId);
            return posts.Select(MapToDto);
        }

        public async Task<IEnumerable<PostDto>> GetRecentAsync(int count = 10)
        {
            var posts = await _postRepository.GetRecentAsync(count);
            return posts.Select(MapToDto);
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

            var createdPost = await _postRepository.AddAsync(post);
            return MapToDto(createdPost);
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
            return updatedPost == null ? null : MapToDto(updatedPost);
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

        private static PostDto MapToDto(Post post)
        {
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
                IsLikedByCurrentUser = false, // This would need to be set based on current user context
                
                // Author information from User navigation property
                AuthorName = post.User?.Name ?? string.Empty,
                AuthorEmail = post.User?.Email ?? string.Empty,
                AuthorUserName = post.User?.UserName ?? string.Empty
            };
        }
    }
}

