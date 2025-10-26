using Microsoft.AspNetCore.Authorization;

namespace EchoSpace.Core.Authorization.Attributes
{
    /// <summary>
    /// Authorization attribute that requires the current user to own the resource
    /// Usage: [RequireOwner("post")] or [RequireOwner("comment")]
    /// </summary>
    public class RequireOwnerAttribute : AuthorizeAttribute
    {
        public RequireOwnerAttribute(string resourceType)
        {
            Policy = $"OwnerOf{resourceType}";
        }
    }
}


