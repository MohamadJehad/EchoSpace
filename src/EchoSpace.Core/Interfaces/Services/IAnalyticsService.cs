using EchoSpace.Core.DTOs.Dashboard;

namespace EchoSpace.Core.Interfaces.Services;

public interface IAnalyticsService
{
    Task<DashboardOverviewDto> GetDashboardOverviewAsync();
    Task<TimeSeriesDataDto> GetUserGrowthAsync(int days = 30);
    Task<TimeSeriesDataDto> GetPostActivityAsync(int days = 30);
    Task<TimeSeriesDataDto> GetLoginActivityAsync(int days = 30);
    Task<List<ActiveSessionDto>> GetActiveSessionsAsync();
    Task<List<ActiveSessionDto>> GetUserSessionsAsync(Guid userId);
    Task<List<FailedLoginAttemptDto>> GetFailedLoginAttemptsAsync(int top = 50);
    Task<bool> TerminateSessionAsync(Guid sessionId);
    Task<int> TerminateUserSessionsAsync(Guid userId);
}

