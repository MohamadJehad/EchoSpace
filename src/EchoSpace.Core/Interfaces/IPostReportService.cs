using EchoSpace.Core.DTOs.Posts;

namespace EchoSpace.Core.Interfaces
{
    public interface IPostReportService
    {
        Task<bool> ReportPostAsync(Guid postId, Guid userId, string? reason);
        Task<bool> HasUserReportedPostAsync(Guid postId, Guid userId);
        Task<int> GetReportCountAsync(Guid postId);
        Task<IEnumerable<ReportedPostDto>> GetReportedPostsAsync();
    }
}

