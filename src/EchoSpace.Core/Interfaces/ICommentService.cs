using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs.Comments;

namespace EchoSpace.Core.Interfaces
{
    public interface ICommentService
    {
        Task<IEnumerable<CommentDto>> GetAllAsync();
        Task<CommentDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<CommentDto>> GetByPostIdAsync(Guid postId);
        Task<IEnumerable<CommentDto>> GetByUserIdAsync(Guid userId);
        Task<CommentDto> CreateAsync(CreateCommentRequest request);
        Task<CommentDto?> UpdateAsync(Guid id, UpdateCommentRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> IsOwnerAsync(Guid commentId, Guid userId);
        Task<int> GetCountByPostIdAsync(Guid postId);
    }
}
