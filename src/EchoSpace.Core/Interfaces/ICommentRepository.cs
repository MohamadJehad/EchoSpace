using EchoSpace.Core.Entities;

namespace EchoSpace.Core.Interfaces
{
    public interface ICommentRepository
    {
        Task<IEnumerable<Comment>> GetAllAsync();
        Task<Comment?> GetByIdAsync(Guid id);
        Task<IEnumerable<Comment>> GetByPostIdAsync(Guid postId);
        Task<IEnumerable<Comment>> GetByUserIdAsync(Guid userId);
        Task<Comment> AddAsync(Comment comment);
        Task<Comment?> UpdateAsync(Comment comment);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<int> GetCountByPostIdAsync(Guid postId);
    }
}
