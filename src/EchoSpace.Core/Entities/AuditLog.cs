using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(200)]
        public string Action { get; set; } = string.Empty; // e.g. "POST /api/posts"

        [MaxLength(100)]
        public string? EntityType { get; set; } // "Post"

        public Guid? EntityId { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(1024)]
        public string? UserAgent { get; set; }

        [MaxLength(50)]
        public string Result { get; set; } = "unknown";

        // Store structured JSON for old/new values
        [Column(TypeName = "nvarchar(max)")]
        public string? OldValuesJson { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? NewValuesJson { get; set; }

        // Optional: store integrity HMAC if you implement chain
        [MaxLength(128)]
        public string? IntegrityHash { get; set; }
    }
}