using EchoSpace.Core.Entities;

namespace EchoSpace.Core.Interfaces
{
    public interface IFollowRepository
    {
        Task<Follow?> GetFollowAsync(Guid followerId, Guid followedId);
        Task<bool> FollowAsync(Guid followerId, Guid followedId);
        Task<bool> UnfollowAsync(Guid followerId, Guid followedId);
        Task<bool> IsFollowingAsync(Guid followerId, Guid followedId);
        Task<IEnumerable<Follow>> GetFollowersAsync(Guid userId);
        Task<IEnumerable<Follow>> GetFollowingAsync(Guid userId);
        Task<int> GetFollowerCountAsync(Guid userId);
        Task<int> GetFollowingCountAsync(Guid userId);
    }
}

