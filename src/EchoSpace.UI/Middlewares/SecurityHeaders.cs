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
            // Prevent clickjacking - don't allow page to be embedded in iframes
            context.Response.Headers["X-Frame-Options"] = "DENY";
            
            // Block MIME sniffing - prevents browsers from guessing content type
            // This is critical for images to prevent them from being treated as scripts
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            
            // Content Security Policy to prevent XSS attacks
            // This tells the browser what resources are allowed to be loaded
            context.Response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " + // Allow scripts from self and inline (adjust based on your needs)
                "style-src 'self' 'unsafe-inline'; " + // Allow styles from self and inline
                "img-src 'self' data: https: blob:; " + // Allow images from self, data URIs, HTTPS, and blob storage
                "font-src 'self' data:; " + // Allow fonts from self and data URIs
                "connect-src 'self' https:; " + // Allow API calls to self and HTTPS endpoints
                "frame-ancestors 'none';"; // Prevent embedding in iframes
            
            // Additional security headers
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin"; // Reduce referrer leakage
            
            // Force HTTPS for one year and preload
            context.Response.Headers["Strict-Transport-Security"] =
                "max-age=31536000; includeSubDomains; preload";

            await _next(context);
        }
    }
}
