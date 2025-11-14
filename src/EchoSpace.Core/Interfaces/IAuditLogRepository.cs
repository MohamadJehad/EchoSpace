// EchoSpace.Core.Interfaces/IAuditLogRepository.cs
using EchoSpace.Core.Entities;
using System.Threading.Tasks;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task BulkAddAsync(IEnumerable<AuditLog> logs);
}
