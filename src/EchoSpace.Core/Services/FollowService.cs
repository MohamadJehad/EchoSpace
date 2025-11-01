using EchoSpace.Core.DTOs.Auth;
using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.Core.Services
{
    public class FollowService : IFollowService
    {
        private readonly IFollowRepository _followRepository;

        public FollowService(IFollowRepository followRepository)
        {
            _followRepository = followRepository;
        }

        public async Task<bool> FollowUserAsync(Guid followerId, Guid followedId)
        {
            return await _followRepository.FollowAsync(followerId, followedId);
        }

        public async Task<bool> UnfollowUserAsync(Guid followerId, Guid followedId)
        {
            return await _followRepository.UnfollowAsync(followerId, followedId);
        }

        public async Task<bool> IsFollowingAsync(Guid followerId, Guid followedId)
        {
            return await _followRepository.IsFollowingAsync(followerId, followedId);
        }

        public async Task<IEnumerable<UserDto>> GetFollowersAsync(Guid userId)
        {
            var follows = await _followRepository.GetFollowersAsync(userId);
            return follows.Select(f => MapToUserDto(f.Follower));
        }

        public async Task<IEnumerable<UserDto>> GetFollowingAsync(Guid userId)
        {
            var follows = await _followRepository.GetFollowingAsync(userId);
            return follows.Select(f => MapToUserDto(f.Followed));
        }

        public async Task<int> GetFollowerCountAsync(Guid userId)
        {
            return await _followRepository.GetFollowerCountAsync(userId);
        }

        public async Task<int> GetFollowingCountAsync(Guid userId)
        {
            return await _followRepository.GetFollowingCountAsync(userId);
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                UserName = user.UserName,
                Role = user.Role.ToString()
            };
        }
    }
}

