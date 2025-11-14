// EchoSpace.Core.Attributes/AuditAttribute.cs
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AuditAttribute : Attribute
{
    public string ActionType { get; }
    public AuditAttribute(string actionType) => ActionType = actionType;
}
