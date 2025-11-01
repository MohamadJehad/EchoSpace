namespace EchoSpace.Core.Authorization.ABAC
{
    /// <summary>
    /// Action attributes used in ABAC policy evaluation
    /// </summary>
    public class ActionAttributes
    {
        public string Action { get; set; } = string.Empty; // e.g., "Create", "Read", "Update", "Delete", "Like", "Follow"
        public string? HttpMethod { get; set; } // GET, POST, PUT, DELETE, etc.
        public string? Controller { get; set; }
        public string? Endpoint { get; set; }
        
        // Additional action attributes
        public Dictionary<string, object> CustomAttributes { get; set; } = new();
    }
}

