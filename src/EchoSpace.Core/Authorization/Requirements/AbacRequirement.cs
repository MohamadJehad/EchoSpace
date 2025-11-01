using EchoSpace.Core.Authorization.ABAC;

namespace EchoSpace.Core.Authorization.Requirements
{
    /// <summary>
    /// ABAC-based authorization requirement
    /// Evaluates access based on attributes: Subject, Resource, Action, and Environment
    /// </summary>
    public class AbacRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public AbacPolicy Policy { get; }
        public string ResourceType { get; }
        public string? Action { get; }
        
        public AbacRequirement(AbacPolicy policy, string resourceType, string? action = null)
        {
            Policy = policy;
            ResourceType = resourceType;
            Action = action;
        }
    }
}

