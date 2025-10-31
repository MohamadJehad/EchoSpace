using EchoSpace.Core.DTOs.Auth;

namespace EchoSpace.Core.Interfaces
{
    public interface IFollowService
    {
        Task<bool> FollowUserAsync(Guid followerId, Guid followedId);
        Task<bool> UnfollowUserAsync(Guid followerId, Guid followedId);
        Task<bool> IsFollowingAsync(Guid followerId, Guid followedId);
        Task<IEnumerable<UserDto>> GetFollowersAsync(Guid userId);
        Task<IEnumerable<UserDto>> GetFollowingAsync(Guid userId);
        Task<int> GetFollowerCountAsync(Guid userId);
        Task<int> GetFollowingCountAsync(Guid userId);
    }
}

