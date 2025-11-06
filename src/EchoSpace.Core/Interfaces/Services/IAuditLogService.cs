namespace EchoSpace.Core.Interfaces.Services;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityType, string entityId, 
        string result, Dictionary<string, object>? oldValues = null, 
        Dictionary<string, object>? newValues = null);
}

