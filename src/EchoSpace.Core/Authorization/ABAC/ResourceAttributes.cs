namespace EchoSpace.Core.Authorization.ABAC
{
    /// <summary>
    /// Resource attributes used in ABAC policy evaluation
    /// </summary>
    public class ResourceAttributes
    {
        public string ResourceType { get; set; } = string.Empty; // e.g., "Post", "Comment", "User"
        public Guid? ResourceId { get; set; }
        public Guid? OwnerId { get; set; } // Resource owner ID
        public string? OwnerEmail { get; set; }
        
        // Additional resource attributes
        public Dictionary<string, object> CustomAttributes { get; set; } = new();
    }
}

