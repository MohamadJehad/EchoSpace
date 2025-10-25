using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs.Comments;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.Core.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;

        public CommentService(ICommentRepository commentRepository)
        {
            _commentRepository = commentRepository;
        }

        public async Task<IEnumerable<CommentDto>> GetAllAsync()
        {
            var comments = await _commentRepository.GetAllAsync();
            return comments.Select(MapToDto);
        }

        public async Task<CommentDto?> GetByIdAsync(Guid id)
        {
            var comment = await _commentRepository.GetByIdAsync(id);
            return comment == null ? null : MapToDto(comment);
        }

        public async Task<IEnumerable<CommentDto>> GetByPostIdAsync(Guid postId)
        {
            var comments = await _commentRepository.GetByPostIdAsync(postId);
            return comments.Select(MapToDto);
        }

        public async Task<IEnumerable<CommentDto>> GetByUserIdAsync(Guid userId)
        {
            var comments = await _commentRepository.GetByUserIdAsync(userId);
            return comments.Select(MapToDto);
        }

        public async Task<CommentDto> CreateAsync(CreateCommentRequest request)
        {
            var comment = new Comment
            {
                CommentId = Guid.NewGuid(),
                PostId = request.PostId,
                UserId = request.UserId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            var createdComment = await _commentRepository.AddAsync(comment);
            return MapToDto(createdComment);
        }

        public async Task<CommentDto?> UpdateAsync(Guid id, UpdateCommentRequest request)
        {
            var existing = await _commentRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return null;
            }

            existing.Content = request.Content;
            var updatedComment = await _commentRepository.UpdateAsync(existing);
            return updatedComment == null ? null : MapToDto(updatedComment);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _commentRepository.DeleteAsync(id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _commentRepository.ExistsAsync(id);
        }

        public async Task<bool> IsOwnerAsync(Guid commentId, Guid userId)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            return comment?.UserId == userId;
        }

        public async Task<int> GetCountByPostIdAsync(Guid postId)
        {
            return await _commentRepository.GetCountByPostIdAsync(postId);
        }

        private static CommentDto MapToDto(Comment comment)
        {
            return new CommentDto
            {
                CommentId = comment.CommentId,
                PostId = comment.PostId,
                UserId = comment.UserId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserName = comment.User?.Name ?? string.Empty,
                UserEmail = comment.User?.Email ?? string.Empty
            };
        }
    }
}

