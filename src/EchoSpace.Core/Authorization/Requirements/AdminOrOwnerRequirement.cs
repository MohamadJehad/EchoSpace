namespace EchoSpace.Core.Authorization.Requirements
{
    /// <summary>
    /// Authorization requirement that allows access if user is Admin OR owns the resource
    /// </summary>
    public class AdminOrOwnerRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public string ResourceType { get; }

        public AdminOrOwnerRequirement(string resourceType)
        {
            ResourceType = resourceType;
        }
    }
}

