namespace EchoSpace.Core.DTOs.Dashboard;

public class FailedLoginAttemptDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public bool IsLocked { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime? LastLoginAttempt { get; set; }
}

