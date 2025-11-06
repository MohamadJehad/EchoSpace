namespace EchoSpace.Core.Authorization.ABAC
{
    /// <summary>
    /// Complete ABAC context containing all attribute categories
    /// </summary>
    public class AbacContext
    {
        public SubjectAttributes Subject { get; set; } = new();
        public ResourceAttributes Resource { get; set; } = new();
        public ActionAttributes Action { get; set; } = new();
        public EnvironmentAttributes Environment { get; set; } = new();
    }
}

