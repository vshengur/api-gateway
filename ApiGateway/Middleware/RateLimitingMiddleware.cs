namespace ApiGateway.Middleware;

public class RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
{
    private static readonly SemaphoreSlim _rateLimiter = new(100); // 100 запросов одновременно

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_rateLimiter.Wait(0))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync("Too Many Requests");
            logger.LogWarning("Rate limit exceeded");
            return;
        }

        try
        {
            await next(context);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
}