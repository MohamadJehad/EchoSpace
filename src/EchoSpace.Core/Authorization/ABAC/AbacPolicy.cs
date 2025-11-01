using System.Linq;

namespace EchoSpace.Core.Authorization.ABAC
{
    /// <summary>
    /// ABAC policy definition that specifies access control rules
    /// </summary>
    public class AbacPolicy
    {
        public string PolicyName { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Policy rules: each rule is a condition that must be satisfied
        // Multiple rules are evaluated with AND logic
        public List<AbacPolicyRule> Rules { get; set; } = new();
        
        /// <summary>
        /// Evaluate if the ABAC context satisfies this policy
        /// </summary>
        public bool Evaluate(AbacContext context)
        {
            if (Rules.Count == 0)
                return false;
            
            // All rules must pass (AND logic)
            foreach (var rule in Rules)
            {
                if (!rule.Evaluate(context))
                {
                    return false;
                }
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// A single policy rule that evaluates a condition
    /// </summary>
    public class AbacPolicyRule
    {
        public string AttributeCategory { get; set; } = string.Empty; // "Subject", "Resource", "Action", "Environment"
        public string AttributeName { get; set; } = string.Empty; // e.g., "Role", "OwnerId", "Action"
        public string Operator { get; set; } = "Equals"; // Equals, NotEquals, In, NotIn, GreaterThan, LessThan, Contains, etc.
        public object? ExpectedValue { get; set; }
        
        public bool Evaluate(AbacContext context)
        {
            object? actualValue = GetAttributeValue(context, AttributeCategory, AttributeName);
            
            return EvaluateCondition(actualValue, Operator, ExpectedValue);
        }
        
        private object? GetAttributeValue(AbacContext context, string category, string attributeName)
        {
            return category.ToLower() switch
            {
                "subject" => GetSubjectAttribute(context.Subject, attributeName),
                "resource" => GetResourceAttribute(context.Resource, attributeName),
                "action" => GetActionAttribute(context.Action, attributeName),
                "environment" => GetEnvironmentAttribute(context.Environment, attributeName),
                _ => null
            };
        }
        
        private object? GetSubjectAttribute(SubjectAttributes subject, string name)
        {
            return name switch
            {
                "UserId" => subject.UserId,
                "Role" => subject.Role,
                "Email" => subject.Email,
                "Name" => subject.Name,
                "IsAuthenticated" => subject.IsAuthenticated,
                _ => subject.CustomAttributes.TryGetValue(name, out var value) ? value : null
            };
        }
        
        private object? GetResourceAttribute(ResourceAttributes resource, string name)
        {
            return name switch
            {
                "ResourceType" => resource.ResourceType,
                "ResourceId" => resource.ResourceId,
                "OwnerId" => resource.OwnerId,
                "OwnerEmail" => resource.OwnerEmail,
                _ => resource.CustomAttributes.TryGetValue(name, out var value) ? value : null
            };
        }
        
        private object? GetActionAttribute(ActionAttributes action, string name)
        {
            return name switch
            {
                "Action" => action.Action,
                "HttpMethod" => action.HttpMethod,
                "Controller" => action.Controller,
                "Endpoint" => action.Endpoint,
                _ => action.CustomAttributes.TryGetValue(name, out var value) ? value : null
            };
        }
        
        private object? GetEnvironmentAttribute(EnvironmentAttributes environment, string name)
        {
            return name switch
            {
                "RequestTime" => environment.RequestTime,
                "IpAddress" => environment.IpAddress,
                "UserAgent" => environment.UserAgent,
                _ => environment.CustomAttributes.TryGetValue(name, out var value) ? value : null
            };
        }
        
        private bool EvaluateCondition(object? actualValue, string operatorStr, object? expectedValue)
        {
            if (actualValue == null && expectedValue == null)
                return operatorStr == "Equals";
            
            if (actualValue == null || expectedValue == null)
                return operatorStr == "NotEquals";
            
            // Handle string comparison for "In" operator
            if (operatorStr.ToLower() == "in" && expectedValue is IEnumerable<object> collection)
            {
                var actualStr = actualValue.ToString();
                return collection.Any(x => x?.ToString() == actualStr);
            }
            
            return operatorStr.ToLower() switch
            {
                "equals" => actualValue.Equals(expectedValue) || actualValue.ToString() == expectedValue.ToString(),
                "notequals" => !actualValue.Equals(expectedValue) && actualValue.ToString() != expectedValue.ToString(),
                "in" => false, // Handled above
                "notin" => expectedValue is IEnumerable<object> coll && !coll.Any(x => x?.ToString() == actualValue.ToString()),
                "contains" => actualValue.ToString()?.Contains(expectedValue.ToString() ?? "") ?? false,
                "greaterthan" => CompareValues(actualValue, expectedValue) > 0,
                "lessthan" => CompareValues(actualValue, expectedValue) < 0,
                _ => false
            };
        }
        
        private int CompareValues(object value1, object value2)
        {
            if (value1 is IComparable comparable1 && value2 is IComparable comparable2)
            {
                try
                {
                    return comparable1.CompareTo(comparable2);
                }
                catch
                {
                    return 0;
                }
            }
            return 0;
        }
    }
}
