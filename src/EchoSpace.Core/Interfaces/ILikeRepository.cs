using EchoSpace.Core.Entities;

namespace EchoSpace.Core.Interfaces
{
    public interface ILikeRepository
    {
        Task<bool> LikePostAsync(Guid postId, Guid userId);
        Task<bool> UnlikePostAsync(Guid postId, Guid userId);
        Task<bool> IsLikedByUserAsync(Guid postId, Guid userId);
        Task<int> GetLikeCountAsync(Guid postId);
        Task<IEnumerable<Like>> GetLikesByPostAsync(Guid postId);
    }
}

