// EchoSpace.Infrastructure.Repositories/AuditLogRepository.cs
using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly EchoSpaceDbContext _db;

    public AuditLogRepository(EchoSpaceDbContext db) => _db = db;

    public async Task AddAsync(AuditLog log)
    {
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task BulkAddAsync(IEnumerable<AuditLog> logs)
    {
        _db.AuditLogs.AddRange(logs);
        await _db.SaveChangesAsync();
    }
}
