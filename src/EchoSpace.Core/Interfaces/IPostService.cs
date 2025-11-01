using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs.Posts;

namespace EchoSpace.Core.Interfaces
{
    public interface IPostService
    {
        Task<IEnumerable<PostDto>> GetAllAsync();
        Task<PostDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<PostDto>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<PostDto>> GetRecentAsync(int count = 10);
        Task<PostDto> CreateAsync(CreatePostRequest request);
        Task<PostDto?> UpdateAsync(Guid id, UpdatePostRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> IsOwnerAsync(Guid postId, Guid userId);
        Task<IEnumerable<PostDto>> GetPostsFromFollowingAsync(Guid userId);
    }
}
