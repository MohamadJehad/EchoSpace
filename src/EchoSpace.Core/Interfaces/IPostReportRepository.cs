using EchoSpace.Core.Entities;

namespace EchoSpace.Core.Interfaces
{
    public interface IPostReportRepository
    {
        Task<bool> ReportPostAsync(Guid postId, Guid userId, string? reason);
        Task<bool> HasUserReportedPostAsync(Guid postId, Guid userId);
        Task<int> GetReportCountAsync(Guid postId);
        Task<IEnumerable<PostReport>> GetReportsByPostAsync(Guid postId);
        Task<IEnumerable<Post>> GetReportedPostsAsync();
        Task<IEnumerable<PostReport>> GetAllReportsAsync();
    }
}

