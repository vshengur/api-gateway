using ApiGateway.Services.RateLimiter;

namespace ApiGateway.Middleware;

public class RateLimitingMiddleware(RequestDelegate next, IRateLimiter rateLimiter)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!rateLimiter.AllowRequest())
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync("Too Many Requests");
            return;
        }

        await next(context);
    }
}