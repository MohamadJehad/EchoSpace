using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace EchoSpace.UI.Middleware
{
    /// <summary>
    /// Middleware to apply rate limiting on forgot-password endpoint based on email address.
    /// Limits to 3 requests per hour per email address.
    /// </summary>
    public class ForgotPasswordRateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, RateLimiter> _rateLimiters = new();
        private static readonly object _lock = new();

        public ForgotPasswordRateLimitingMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only apply to forgot-password endpoint
            if (context.Request.Path.StartsWithSegments("/api/auth/forgot-password") &&
                context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                var email = await ExtractEmailFromRequest(context);

                if (string.IsNullOrEmpty(email))
                {
                    // If we can't extract email, let the request proceed (validation will handle it)
                    await _next(context);
                    return;
                }

                // Normalize email for rate limiting (lowercase)
                var normalizedEmail = email.ToLowerInvariant().Trim();
                var rateLimiter = GetOrCreateRateLimiter(normalizedEmail);

                var lease = await rateLimiter.AcquireAsync();

                if (!lease.IsAcquired)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(new { message = "Too many password reset requests. Please try again later." }),
                        cancellationToken: context.RequestAborted);
                    return;
                }

                try
                {
                    await _next(context);
                }
                finally
                {
                    lease.Dispose();
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task<string?> ExtractEmailFromRequest(HttpContext context)
        {
            try
            {
                // Enable buffering so we can read the body multiple times
                context.Request.EnableBuffering();

                // Read the request body
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();

                // Reset the stream position so the next middleware can read it
                context.Request.Body.Position = 0;

                // Parse JSON to extract email
                if (string.IsNullOrWhiteSpace(body))
                {
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("email", out var emailElement))
                {
                    return emailElement.GetString();
                }
            }
            catch
            {
                // If parsing fails, return null (request validation will handle it)
            }

            return null;
        }

        private RateLimiter GetOrCreateRateLimiter(string email)
        {
            if (_rateLimiters.TryGetValue(email, out var existingLimiter))
            {
                return existingLimiter;
            }

            lock (_lock)
            {
                // Double-check after acquiring lock
                if (_rateLimiters.TryGetValue(email, out existingLimiter))
                {
                    return existingLimiter;
                }

                var permitLimit = _configuration.GetValue<int>("RateLimiting:ForgotPasswordPolicy:PermitLimit", 3);
                var window = TimeSpan.Parse(_configuration.GetValue<string>("RateLimiting:ForgotPasswordPolicy:Window") ?? "01:00:00");

                var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                    AutoReplenishment = true
                });

                _rateLimiters[email] = limiter;
                return limiter;
            }
        }
    }
}

