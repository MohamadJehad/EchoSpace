using EchoSpace.Core.Entities;

namespace EchoSpace.Core.Interfaces
{
    public interface IPostRepository
    {
        Task<IEnumerable<Post>> GetAllAsync();
        Task<Post?> GetByIdAsync(Guid id);
        Task<IEnumerable<Post>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Post>> GetRecentAsync(int count = 10);
        Task<Post> AddAsync(Post post);
        Task<Post?> UpdateAsync(Post post);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
