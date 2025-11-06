using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EchoSpace.Core.Interfaces.Services;
using EchoSpace.Core.Models.Logging;
using System.Security.Claims;

namespace EchoSpace.Infrastructure.Services.Logging;

public class AuditLogService : IAuditLogService
{
    private readonly ILogger<AuditLogService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(ILogger<AuditLogService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task LogAsync(string action, string entityType, string entityId, 
        string result, Dictionary<string, object>? oldValues = null, 
        Dictionary<string, object>? newValues = null)
    {
        var context = _httpContextAccessor.HttpContext;
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            UserId = context?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = context?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown",
            UserAgent = context?.Request?.Headers["User-Agent"].ToString() ?? "Unknown",
            Result = result,
            OldValues = oldValues ?? new Dictionary<string, object>(),
            NewValues = newValues ?? new Dictionary<string, object>()
        };

        // Use structured logging - Serilog will handle the @ prefix for object serialization
        _logger.LogInformation("AuditLog: {@AuditLog}", entry);
        await Task.CompletedTask;
    }
}

