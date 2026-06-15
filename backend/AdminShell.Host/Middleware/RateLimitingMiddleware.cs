using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace AdminShell.Host.Middleware;

/// <summary>
/// Rate limiting middleware using token bucket per IP.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, TokenBucketRateLimiter> _clients = new();
    private readonly TokenBucketRateLimiterOptions _options;

    public RateLimitingMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        var limit = configuration.GetValue<int>("RateLimiting:PermitLimit", 100);
        var window = configuration.GetValue<int>("RateLimiting:WindowSeconds", 10);
        _options = new TokenBucketRateLimiterOptions
        {
            TokenLimit = limit,
            QueueLimit = 0,
            ReplenishmentPeriod = TimeSpan.FromSeconds(window),
            TokensPerPeriod = limit,
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var limiter = _clients.GetOrAdd(clientIp, _ => new TokenBucketRateLimiter(_options));

            using var lease = await limiter.AcquireAsync(permitCount: 1);
            if (!lease.IsAcquired)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = "10";
                await context.Response.WriteAsync("{\"error\":\"Rate limit exceeded. Try again later.\"}");
                return;
            }
        }

        await _next(context);
    }
}

// Extension method for clean registration
public static class RateLimitingExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        => app.UseMiddleware<RateLimitingMiddleware>();
}