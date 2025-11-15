namespace EchoSpace.Core.DTOs.Dashboard;

public class DashboardOverviewDto
{
    public UserMetricsDto UserMetrics { get; set; } = new();
    public ContentMetricsDto ContentMetrics { get; set; } = new();
    public EngagementMetricsDto EngagementMetrics { get; set; } = new();
    public SecurityMetricsDto SecurityMetrics { get; set; } = new();
}

public class UserMetricsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; } // Logged in last 30 days
    public int NewUsersLast7Days { get; set; }
    public int NewUsersLast30Days { get; set; }
    public int LockedAccounts { get; set; }
    public int VerifiedEmails { get; set; }
    public int UnverifiedEmails { get; set; }
    public int UsersWithFailedAttempts { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
}

public class ContentMetricsDto
{
    public int TotalPosts { get; set; }
    public int PostsLast7Days { get; set; }
    public int PostsLast30Days { get; set; }
    public int TotalComments { get; set; }
    public int TotalLikes { get; set; }
    public double AveragePostsPerUser { get; set; }
}

public class EngagementMetricsDto
{
    public int TotalFollows { get; set; }
    public double AverageFollowersPerUser { get; set; }
    public double EngagementRate { get; set; } // (likes + comments) / posts
}

public class SecurityMetricsDto
{
    public int ActiveSessions { get; set; }
    public int TotalFailedAttempts { get; set; }
    public int FailedAttemptsLast24Hours { get; set; }
    public int FailedAttemptsLast7Days { get; set; }
    public int LockedAccountsCount { get; set; }
    public int OAuthLogins { get; set; } // Google OAuth
    public int PasswordLogins { get; set; }
}

