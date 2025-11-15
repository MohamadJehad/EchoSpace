using EchoSpace.Core.DTOs.Dashboard;
using EchoSpace.Core.Entities;
using EchoSpace.Core.Enums;
using EchoSpace.Core.Interfaces.Services;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EchoSpace.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly EchoSpaceDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(EchoSpaceDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync()
    {
        var now = DateTime.UtcNow;
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        var overview = new DashboardOverviewDto();

        // User Metrics
        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context.Users
            .CountAsync(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= last30Days);
        var newUsersLast7Days = await _context.Users
            .CountAsync(u => u.CreatedAt >= last7Days);
        var newUsersLast30Days = await _context.Users
            .CountAsync(u => u.CreatedAt >= last30Days);
        var lockedAccounts = await _context.Users
            .CountAsync(u => u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow);
        var verifiedEmails = await _context.Users
            .CountAsync(u => u.EmailVerified || u.EmailConfirmed);
        var unverifiedEmails = totalUsers - verifiedEmails;
        var usersWithFailedAttempts = await _context.Users
            .CountAsync(u => u.AccessFailedCount > 0);

        var usersByRole = await _context.Users
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Role.ToString(), x => x.Count);

        overview.UserMetrics = new UserMetricsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            NewUsersLast7Days = newUsersLast7Days,
            NewUsersLast30Days = newUsersLast30Days,
            LockedAccounts = lockedAccounts,
            VerifiedEmails = verifiedEmails,
            UnverifiedEmails = unverifiedEmails,
            UsersWithFailedAttempts = usersWithFailedAttempts,
            UsersByRole = usersByRole
        };

        // Content Metrics
        var totalPosts = await _context.Posts.CountAsync();
        var postsLast7Days = await _context.Posts
            .CountAsync(p => p.CreatedAt >= last7Days);
        var postsLast30Days = await _context.Posts
            .CountAsync(p => p.CreatedAt >= last30Days);
        var totalComments = await _context.Comments.CountAsync();
        var totalLikes = await _context.Likes.CountAsync();
        var averagePostsPerUser = totalUsers > 0 ? (double)totalPosts / totalUsers : 0;

        overview.ContentMetrics = new ContentMetricsDto
        {
            TotalPosts = totalPosts,
            PostsLast7Days = postsLast7Days,
            PostsLast30Days = postsLast30Days,
            TotalComments = totalComments,
            TotalLikes = totalLikes,
            AveragePostsPerUser = Math.Round(averagePostsPerUser, 2)
        };

        // Engagement Metrics
        var totalFollows = await _context.Follows.CountAsync();
        var averageFollowersPerUser = totalUsers > 0 ? (double)totalFollows / totalUsers : 0;
        var engagementRate = totalPosts > 0 
            ? Math.Round((double)(totalLikes + totalComments) / totalPosts, 2) 
            : 0;

        overview.EngagementMetrics = new EngagementMetricsDto
        {
            TotalFollows = totalFollows,
            AverageFollowersPerUser = Math.Round(averageFollowersPerUser, 2),
            EngagementRate = engagementRate
        };

        // Security Metrics
        var activeSessions = await _context.UserSessions
            .CountAsync(s => s.ExpiresAt > now);
        var totalFailedAttempts = await _context.Users
            .SumAsync(u => u.AccessFailedCount);
        var failedAttemptsLast24Hours = await _context.Users
            .Where(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= now.AddHours(-24))
            .SumAsync(u => u.AccessFailedCount);
        var failedAttemptsLast7Days = await _context.Users
            .Where(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value >= last7Days)
            .SumAsync(u => u.AccessFailedCount);
        var lockedAccountsCount = lockedAccounts;
        
        // Count OAuth vs Password logins (approximate by checking AuthProviders)
        var oauthLogins = await _context.AuthProviders
            .CountAsync(ap => ap.Provider == "Google");
        var passwordLogins = await _context.Users
            .CountAsync(u => !string.IsNullOrEmpty(u.PasswordHash));

        overview.SecurityMetrics = new SecurityMetricsDto
        {
            ActiveSessions = activeSessions,
            TotalFailedAttempts = totalFailedAttempts,
            FailedAttemptsLast24Hours = failedAttemptsLast24Hours,
            FailedAttemptsLast7Days = failedAttemptsLast7Days,
            LockedAccountsCount = lockedAccountsCount,
            OAuthLogins = oauthLogins,
            PasswordLogins = passwordLogins
        };

        return overview;
    }

    public async Task<TimeSeriesDataDto> GetUserGrowthAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        
        var users = await _context.Users
            .Where(u => u.CreatedAt >= startDate)
            .Select(u => new { Date = u.CreatedAt.Date })
            .ToListAsync();

        var grouped = users
            .GroupBy(u => u.Date)
            .Select(g => new TimeSeriesPointDto
            {
                Date = g.Key,
                Value = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        // Fill in missing dates with 0
        var result = new List<TimeSeriesPointDto>();
        for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            var existing = grouped.FirstOrDefault(g => g.Date.Date == date.Date);
            result.Add(existing ?? new TimeSeriesPointDto { Date = date, Value = 0 });
        }

        return new TimeSeriesDataDto { Data = result };
    }

    public async Task<TimeSeriesDataDto> GetPostActivityAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        
        var posts = await _context.Posts
            .Where(p => p.CreatedAt >= startDate)
            .Select(p => new { Date = p.CreatedAt.Date })
            .ToListAsync();

        var grouped = posts
            .GroupBy(p => p.Date)
            .Select(g => new TimeSeriesPointDto
            {
                Date = g.Key,
                Value = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        // Fill in missing dates with 0
        var result = new List<TimeSeriesPointDto>();
        for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            var existing = grouped.FirstOrDefault(g => g.Date.Date == date.Date);
            result.Add(existing ?? new TimeSeriesPointDto { Date = date, Value = 0 });
        }

        return new TimeSeriesDataDto { Data = result };
    }

    public async Task<TimeSeriesDataDto> GetLoginActivityAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days).Date;
        
        // Get login activity from UserSessions (created when user logs in)
        var sessions = await _context.UserSessions
            .Where(s => s.CreatedAt >= startDate)
            .Select(s => new { Date = s.CreatedAt.Date })
            .ToListAsync();

        var grouped = sessions
            .GroupBy(s => s.Date)
            .Select(g => new TimeSeriesPointDto
            {
                Date = g.Key,
                Value = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        // Fill in missing dates with 0
        var result = new List<TimeSeriesPointDto>();
        for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            var existing = grouped.FirstOrDefault(g => g.Date.Date == date.Date);
            result.Add(existing ?? new TimeSeriesPointDto { Date = date, Value = 0 });
        }

        return new TimeSeriesDataDto { Data = result };
    }

    public async Task<List<ActiveSessionDto>> GetActiveSessionsAsync()
    {
        var now = DateTime.UtcNow;
        
        var sessions = await _context.UserSessions
            .Include(s => s.User)
            .Where(s => s.ExpiresAt > now)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ActiveSessionDto
            {
                SessionId = s.SessionId,
                UserId = s.UserId,
                UserName = s.User.Name,
                UserEmail = s.User.Email,
                DeviceInfo = s.DeviceInfo,
                IpAddress = s.IpAddress,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                Duration = (now - s.CreatedAt).ToString(@"d\.hh\:mm\:ss"),
                IsExpired = false
            })
            .ToListAsync();

        return sessions;
    }

    public async Task<List<ActiveSessionDto>> GetUserSessionsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        
        var sessions = await _context.UserSessions
            .Include(s => s.User)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ActiveSessionDto
            {
                SessionId = s.SessionId,
                UserId = s.UserId,
                UserName = s.User.Name,
                UserEmail = s.User.Email,
                DeviceInfo = s.DeviceInfo,
                IpAddress = s.IpAddress,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                Duration = s.ExpiresAt > now 
                    ? (now - s.CreatedAt).ToString(@"d\.hh\:mm\:ss")
                    : (s.ExpiresAt - s.CreatedAt).ToString(@"d\.hh\:mm\:ss"),
                IsExpired = s.ExpiresAt <= now
            })
            .ToListAsync();

        return sessions;
    }

    public async Task<List<FailedLoginAttemptDto>> GetFailedLoginAttemptsAsync(int top = 50)
    {
        var users = await _context.Users
            .Where(u => u.AccessFailedCount > 0)
            .OrderByDescending(u => u.AccessFailedCount)
            .Take(top)
            .Select(u => new FailedLoginAttemptDto
            {
                UserId = u.Id,
                UserName = u.Name,
                UserEmail = u.Email,
                FailedAttempts = u.AccessFailedCount,
                IsLocked = u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
                LockoutEnd = u.LockoutEnd,
                LastLoginAttempt = u.LastLoginAt
            })
            .ToListAsync();

        return users;
    }

    public async Task<bool> TerminateSessionAsync(Guid sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session == null)
        {
            return false;
        }

        _context.UserSessions.Remove(session);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> TerminateUserSessionsAsync(Guid userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var count = sessions.Count;
        _context.UserSessions.RemoveRange(sessions);
        await _context.SaveChangesAsync();
        return count;
    }
}

