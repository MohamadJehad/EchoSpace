using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EchoSpace.UI.Security
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers["X-Frame-Options"] = "DENY"; // Prevent clickjacking in iframes
            context.Response.Headers["X-Content-Type-Options"] = "nosniff"; // Block MIME sniffing
            // context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin"; // Reduce referrer leakage
            // context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin"; // Isolate browsing context
            // context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp"; // Protect embedded resources
            // context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin"; // Only load same origin resources
            // context.Response.Headers["Content-Security-Policy"] =
            //     "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'"; 
                // Strong protection against external scripts

            context.Response.Headers["Strict-Transport-Security"] =
                "max-age=31536000; includeSubDomains; preload"; 
                // Force HTTPS for one year and preload

            await _next(context);
        }
    }
}
