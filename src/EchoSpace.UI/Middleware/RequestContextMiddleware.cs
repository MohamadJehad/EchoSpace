// EchoSpace.UI/Middleware/RequestContextMiddleware.cs
public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;
    public RequestContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Correlation ID
        var correlationId = context.TraceIdentifier ?? Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;

        // Get user id if present
        Guid? userId = null;
        var userIdClaim = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var parsed)) userId = parsed;
        context.Items["UserId"] = userId;

        // IP address - prefer X-Forwarded-For header if behind proxy (trusted)
        string? ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? context.Connection.RemoteIpAddress?.ToString();
        context.Items["UserIp"] = ip;

        // Add CorrelationId header for downstream
        context.Response.Headers.TryAdd("X-Correlation-Id", correlationId);

        await _next(context);
    }
}
