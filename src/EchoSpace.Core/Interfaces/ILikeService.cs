namespace EchoSpace.Core.Interfaces
{
    public interface ILikeService
    {
        Task<bool> LikePostAsync(Guid postId, Guid userId);
        Task<bool> UnlikePostAsync(Guid postId, Guid userId);
        Task<bool> IsLikedByUserAsync(Guid postId, Guid userId);
        Task<int> GetLikeCountAsync(Guid postId);
    }
}

