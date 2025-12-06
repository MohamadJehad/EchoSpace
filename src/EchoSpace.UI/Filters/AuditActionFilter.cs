// EchoSpace.UI.Filters/AuditActionFilter.cs
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
public class AuditActionFilter : IAsyncActionFilter
{
    private readonly IAuditLogDBService _audit;
    public AuditActionFilter(IAuditLogDBService audit) => _audit = audit;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var action = context.ActionDescriptor.DisplayName;
        var attr = context.ActionDescriptor.EndpointMetadata.OfType<AuditAttribute>().FirstOrDefault();
        var actionType = attr?.ActionType ?? action ?? "Unknown";

        var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString();
        // var userId = context.HttpContext.Items["UserId"] as Guid?;
        var ip = context.HttpContext.Items["UserIp"] as string;
        
        var httpUser = context.HttpContext.User;
        var userIdClaim = httpUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        Guid? userId = Guid.TryParse(userIdClaim, out var uid) ? uid : null;

        if (context.HttpContext?.Request?.Headers.ContainsKey("X-Forwarded-For") == true)
        {
            ip = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        }
        // Execute action
        var executed = await next();

        // Build details - include route values, and optionally request body keys
        var details = new
        {
            RouteValues = context.ActionArguments.Keys,
            Success = executed.Exception == null,
            Exception = executed.Exception?.Message
        };
        
            
        if(userId != null)
        {
        await _audit.LogAsync(actionType, userId, ip, details, resourceId: null, correlationId);
        }
    }
}
