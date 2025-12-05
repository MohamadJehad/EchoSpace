// EchoSpace.Core.Entities/AuditLog.cs
using System;

namespace EchoSpace.Core.Entities
{
    public class AuditLog
    {
        public long Id { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public Guid? UserId { get; set; }
        public string? UserIpAddress { get; set; }
        public string ActionType { get; set; } = default!;
        public string? ResourceId { get; set; }
        public string? CorrelationId { get; set; }
        public string? ActionDetails { get; set; }   // JSON
    }
}
