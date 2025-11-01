namespace EchoSpace.Core.Authorization.ABAC
{
    /// <summary>
    /// Subject (User) attributes used in ABAC policy evaluation
    /// </summary>
    public class SubjectAttributes
    {
        public Guid UserId { get; set; }
        public string? Role { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public bool IsAuthenticated { get; set; }
        
        // Additional attributes can be added here
        public Dictionary<string, object> CustomAttributes { get; set; } = new();
    }
}

