namespace EchoSpace.Core.Authorization.Requirements
{
    /// <summary>
    /// Authorization requirement to check if the user owns a resource
    /// </summary>
    public class OwnerRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public string ResourceType { get; }

        public OwnerRequirement(string resourceType)
        {
            ResourceType = resourceType;
        }
    }
}

