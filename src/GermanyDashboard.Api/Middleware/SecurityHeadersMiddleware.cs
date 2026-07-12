namespace GermanyDashboard.Api.Middleware;

/// <summary>
/// Adds defense-in-depth response headers. This is a pure JSON API (no server-rendered
/// HTML), so the CSP is locked down to "default-src 'none'" everywhere except the
/// dev-only Scalar API reference UI (/scalar, /openapi), which is itself an HTML page
/// that needs to load its own scripts/styles to render.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
        headers["Cross-Origin-Resource-Policy"] = "same-site";

        var path = context.Request.Path;
        var isDocsRoute = path.StartsWithSegments("/scalar") || path.StartsWithSegments("/openapi");

        headers["Content-Security-Policy"] = isDocsRoute
            ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
              "img-src 'self' data: https:; font-src 'self' https://fonts.scalar.com; " +
              "connect-src 'self' https://api.scalar.com; frame-ancestors 'none'"
            : "default-src 'none'; frame-ancestors 'none'";

        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
