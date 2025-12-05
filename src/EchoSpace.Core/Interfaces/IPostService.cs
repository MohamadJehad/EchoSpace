using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.DTOs.Auth;

namespace EchoSpace.Core.Interfaces
{
    public interface IPostService
    {
        Task<IEnumerable<PostDto>> GetAllAsync();
        Task<IEnumerable<PostDto>> GetAllAsync(Guid? currentUserId);

        // Task<UserDto> GetOwner(Guid);

        Task<PostDto?> GetByIdAsync(Guid id);
        Task<PostDto?> GetByIdAsync(Guid id, Guid? currentUserId);
        Task<IEnumerable<PostDto>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<PostDto>> GetByUserIdAsync(Guid userId, Guid? currentUserId);
        Task<IEnumerable<PostDto>> GetRecentAsync(int count = 10);
        Task<IEnumerable<PostDto>> GetRecentAsync(int count, Guid? currentUserId);
        Task<PostDto> CreateAsync(CreatePostRequest request);
        Task<PostDto?> UpdateAsync(Guid id, UpdatePostRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> IsOwnerAsync(Guid postId, Guid userId);
        Task<IEnumerable<PostDto>> GetPostsFromFollowingAsync(Guid userId);
        Task<IEnumerable<PostDto>> GetByTagIdAsync(Guid tagId);
        Task<IEnumerable<PostDto>> GetByTagIdAsync(Guid tagId, Guid? currentUserId);
    }
}
