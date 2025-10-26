using Microsoft.AspNetCore.Authorization;

namespace EchoSpace.Core.Authorization.Attributes
{
    /// <summary>
    /// Authorization attribute that requires the user to be Admin/Moderator OR own the resource
    /// Usage: [RequireAdminOrOwner("post")] or [RequireAdminOrOwner("comment")]
    /// </summary>
    public class RequireAdminOrOwnerAttribute : AuthorizeAttribute
    {
        public RequireAdminOrOwnerAttribute(string resourceType)
        {
            Policy = $"AdminOrOwnerOf{resourceType}";
        }
    }
}


