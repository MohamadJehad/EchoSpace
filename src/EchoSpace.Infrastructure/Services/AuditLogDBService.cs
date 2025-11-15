// EchoSpace.Infrastructure.Services/AuditLogService.cs
using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using Newtonsoft.Json;

public class AuditLogDBService : IAuditLogDBService
{
    private readonly IAuditLogRepository _repo;

    public AuditLogDBService(IAuditLogRepository repo) => _repo = repo;

    public async Task LogAsync(string actionType, Guid? userId, string? ipAddress, object? details = null, string? resourceId = null, string? correlationId = null)
    {
        // Redact or shrink details if needed before serializing
        string? detailsJson = details != null ? JsonConvert.SerializeObject(RedactSensitive(details)) : null;

        var entry = new AuditLog
        {
            TimestampUtc = DateTime.UtcNow,
            UserId = userId,
            UserIpAddress = ipAddress,
            ActionType = actionType,
            ActionDetails = detailsJson,
            ResourceId = resourceId,
            CorrelationId = correlationId
        };

        await _repo.AddAsync(entry);
    }

    // Example redactor: remove fields named 'password' or 'token'
    private object RedactSensitive(object obj)
    {
        try
        {
            // Simple approach: convert to JObject and remove keys
            var j = Newtonsoft.Json.Linq.JObject.FromObject(obj);
            var sensitive = new[] { "password", "token", "accessToken", "refreshToken", "ssn" };
            foreach (var s in sensitive)
            {
                var tokens = j.SelectTokens($"$..{s}").ToList();
                foreach (var t in tokens) t.Replace("***REDACTED***");
            }
            return j;
        }
        catch
        {
            // If redaction fails, return minimal safe message
            return new { note = "redaction_failed" };
        }
    }
}
