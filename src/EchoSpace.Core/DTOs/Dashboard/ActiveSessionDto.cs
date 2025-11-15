namespace EchoSpace.Core.DTOs.Dashboard;

public class ActiveSessionDto
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Duration { get; set; } = string.Empty; // ISO 8601 duration string
    public bool IsExpired { get; set; }
}

