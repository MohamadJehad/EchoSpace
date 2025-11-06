using EchoSpace.Core.Interfaces;

namespace EchoSpace.Core.Services
{
    public class LikeService : ILikeService
    {
        private readonly ILikeRepository _likeRepository;

        public LikeService(ILikeRepository likeRepository)
        {
            _likeRepository = likeRepository;
        }

        public async Task<bool> LikePostAsync(Guid postId, Guid userId)
        {
            return await _likeRepository.LikePostAsync(postId, userId);
        }

        public async Task<bool> UnlikePostAsync(Guid postId, Guid userId)
        {
            return await _likeRepository.UnlikePostAsync(postId, userId);
        }

        public async Task<bool> IsLikedByUserAsync(Guid postId, Guid userId)
        {
            return await _likeRepository.IsLikedByUserAsync(postId, userId);
        }

        public async Task<int> GetLikeCountAsync(Guid postId)
        {
            return await _likeRepository.GetLikeCountAsync(postId);
        }
    }
}

