using Microsoft.AspNetCore.Authorization;
using EchoSpace.Core.Authorization.Requirements;
using EchoSpace.Core.Authorization.ABAC;

namespace EchoSpace.Core.Authorization.Attributes
{
    /// <summary>
    /// Authorization attribute that uses ABAC (Attribute-Based Access Control)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireAbacAttribute : AuthorizeAttribute
    {
        public RequireAbacAttribute(string policyName, string resourceType, string? action = null)
        {
            Policy = policyName;
        }

        // Note: The actual policy registration happens in Program.cs
        // This attribute is used to reference the policy name
    }
}

