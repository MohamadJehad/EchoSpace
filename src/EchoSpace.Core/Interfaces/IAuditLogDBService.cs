// EchoSpace.Core.Interfaces/IAuditLogService.cs
using System;
using System.Threading.Tasks;

public interface IAuditLogDBService
{
    Task LogAsync(string actionType, Guid? userId, string? ipAddress, object? details = null, string? resourceId = null, string? correlationId = null);
}
