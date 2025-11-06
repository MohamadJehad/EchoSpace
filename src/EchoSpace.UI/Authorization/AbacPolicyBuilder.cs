using EchoSpace.Core.Authorization.ABAC;
using EchoSpace.Core.Authorization.Requirements;

namespace EchoSpace.UI.Authorization
{
    /// <summary>
    /// Builder class for creating ABAC policies
    /// </summary>
    public static class AbacPolicyBuilder
    {
        /// <summary>
        /// Create ABAC policy: User must be authenticated
        /// </summary>
        public static AbacPolicy CreateAuthenticatedUserPolicy()
        {
            return new AbacPolicy
            {
                PolicyName = "AuthenticatedUser",
                Description = "User must be authenticated",
                Rules = new List<AbacPolicyRule>
                {
                    new AbacPolicyRule
                    {
                        AttributeCategory = "Subject",
                        AttributeName = "IsAuthenticated",
                        Operator = "Equals",
                        ExpectedValue = true
                    }
                }
            };
        }

        /// <summary>
        /// Create ABAC policy: User must have Admin role
        /// </summary>
        public static AbacPolicy CreateAdminRolePolicy()
        {
            return new AbacPolicy
            {
                PolicyName = "AdminRole",
                Description = "User must have Admin role",
                Rules = new List<AbacPolicyRule>
                {
                    new AbacPolicyRule
                    {
                        AttributeCategory = "Subject",
                        AttributeName = "IsAuthenticated",
                        Operator = "Equals",
                        ExpectedValue = true
                    },
                    new AbacPolicyRule
                    {
                        AttributeCategory = "Subject",
                        AttributeName = "Role",
                        Operator = "Equals",
                        ExpectedValue = "Admin"
                    }
                }
            };
        }

        /// <summary>
        /// Create ABAC policy: User must have Admin or Moderator role
        /// </summary>
        public static AbacPolicy CreateModeratorOrAdminRolePolicy()
        {
            return new AbacPolicy
            {
                PolicyName = "ModeratorOrAdminRole",
                Description = "User must have Admin or Moderator role",
                Rules = new List<AbacPolicyRule>
                {
                    new AbacPolicyRule
                    {
                        AttributeCategory = "Subject",
                        AttributeName = "IsAuthenticated",
                        Operator = "Equals",
                        ExpectedValue = true
                    },
                    new AbacPolicyRule
                    {
                        AttributeCategory = "Subject",
                        AttributeName = "Role",
                        Operator = "In",
                        ExpectedValue = new[] { "Admin", "Moderator" }
                    }
                }
            };
        }

        /// <summary>
        /// Create ABAC policy: User must own the resource
        /// </summary>
        public static AbacPolicy CreateOwnerPolicy(string resourceType)
        {
            return new AbacPolicy
            {
                PolicyName = $"OwnerOf{resourceType}",
                Description = $"User must own the {resourceType} resource",
                Rules = new List<AbacPolicyRule>
                {
                    new AbacPolicyRule
                    {
                        AttributeCategory = "Subject",
                        AttributeName = "IsAuthenticated",
                        Operator = "Equals",
                        ExpectedValue = true
                    },
                    new AbacPolicyRule
                    {
                        AttributeCategory = "Subject",
                        AttributeName = "UserId",
                        Operator = "Equals",
                        ExpectedValue = "Resource.OwnerId" // Special case: compare with resource owner
                    }
                }
            };
        }

        /// <summary>
        /// Create ABAC policy: User must be Admin/Moderator OR own the resource
        /// This requires custom evaluation in the handler
        /// </summary>
        public static AbacPolicy CreateAdminOrOwnerPolicy(string resourceType)
        {
            return new AbacPolicy
            {
                PolicyName = $"AdminOrOwnerOf{resourceType}",
                Description = $"User must be Admin/Moderator OR own the {resourceType} resource",
                Rules = new List<AbacPolicyRule>
                {
                    // This policy is evaluated with OR logic in the handler
                    // Rule 1: Admin role
                    new AbacPolicyRule
                    {
                        AttributeCategory = "Subject",
                        AttributeName = "Role",
                        Operator = "In",
                        ExpectedValue = new[] { "Admin", "Moderator" }
                    }
                    // Rule 2: Owner check is handled separately in the handler
                }
            };
        }

        /// <summary>
        /// Create ABAC policy: User can perform action on their own resource
        /// </summary>
        public static AbacPolicy CreateSelfActionPolicy(string resourceType, string action)
        {
            return new AbacPolicy
            {
                PolicyName = $"Self{action}{resourceType}",
                Description = $"User can {action} their own {resourceType}",
                Rules = new List<AbacPolicyRule>
                {
                    new AbacPolicyRule
                    {
                        AttributeCategory = "Subject",
                        AttributeName = "IsAuthenticated",
                        Operator = "Equals",
                        ExpectedValue = true
                    },
                    new AbacPolicyRule
                    {
                        AttributeCategory = "Action",
                        AttributeName = "Action",
                        Operator = "Equals",
                        ExpectedValue = action
                    },
                    new AbacPolicyRule
                    {
                        AttributeCategory = "Subject",
                        AttributeName = "UserId",
                        Operator = "Equals",
                        ExpectedValue = "Resource.OwnerId"
                    }
                }
            };
        }
    }

    /// <summary>
    /// Extension methods for registering ABAC policies
    /// </summary>
    public static class AbacPolicyExtensions
    {
        /// <summary>
        /// Register an ABAC policy with the authorization options
        /// </summary>
        public static void AddAbacPolicy(this Microsoft.AspNetCore.Authorization.AuthorizationOptions options, AbacPolicy policy, string resourceType, string? action = null)
        {
            options.AddPolicy(policy.PolicyName, policyBuilder =>
            {
                policyBuilder.Requirements.Add(new AbacRequirement(policy, resourceType, action));
            });
        }
    }
}

