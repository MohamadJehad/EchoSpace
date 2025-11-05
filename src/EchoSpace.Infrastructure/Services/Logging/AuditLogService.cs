using Microsoft.AspNetCore.Http;
using EchoSpace.Core.Interfaces.Services;
using EchoSpace.Core.Models.Logging;
using Serilog;
using System.Security.Claims;

namespace EchoSpace.Infrastructure.Services.Logging;

public class AuditLogService : IAuditLogService
{
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(ILogger logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task LogAsync(string action, string entityType, string entityId, 
        string result, Dictionary<string, object> oldValues = null, 
        Dictionary<string, object> newValues = null)
    {
        var context = _httpContextAccessor.HttpContext;
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            UserId = context?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown", // Handle null
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = context?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown", // Handle null
            UserAgent = context?.Request?.Headers["User-Agent"].ToString() ?? "Unknown", // Handle null
            Result = result,
            OldValues = oldValues ?? new Dictionary<string, object>(), // Handle null
            NewValues = newValues ?? new Dictionary<string, object>()  // Handle null
        };

        _logger.Information("{@AuditLog}", entry);
        await Task.CompletedTask; // Ensure the method is async
    }
}