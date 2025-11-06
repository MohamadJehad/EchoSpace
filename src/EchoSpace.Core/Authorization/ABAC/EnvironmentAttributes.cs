using System;

namespace EchoSpace.Core.Authorization.ABAC
{
    /// <summary>
    /// Environment attributes used in ABAC policy evaluation
    /// </summary>
    public class EnvironmentAttributes
    {
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        
        // Additional environment attributes
        public Dictionary<string, object> CustomAttributes { get; set; } = new();
    }
}

