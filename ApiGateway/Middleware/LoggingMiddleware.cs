namespace ApiGateway.Middleware;

public class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("Request: {method} {path}", context.Request.Method, context.Request.Path);
        await next(context);
        logger.LogInformation("Response: {statusCode}", context.Response.StatusCode);
    }
}