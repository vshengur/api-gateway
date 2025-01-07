namespace ApiGateway.Services.RateLimiter;

public interface IRateLimiter
{
    bool AllowRequest();
}