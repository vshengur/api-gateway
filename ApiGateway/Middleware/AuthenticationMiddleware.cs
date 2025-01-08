using ApiGateway.Config;

namespace ApiGateway.Middleware;

public class AuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly Dictionary<string, bool> _routes = RouteConfigLoader.LoadRoutes(configuration);

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();

        // Найти префикс, соответствующий пути
        var requiresAuthorization = _routes
            .Where(route => path != null && path.StartsWith(route.Key))
            .Select(route => route.Value)
            .FirstOrDefault();

        if (!requiresAuthorization)
        {
            // Путь публичный, пропускаем без проверки
            await next(context);
            return;
        }

        // Проверяем наличие токена
        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        // Валидация токена
        /*
        if (!await ValidateTokenAsync(token))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden");
            return;
        }
        */

        // Передаём запрос дальше
        await next(context);
    }

    private static async Task<bool> ValidateTokenAsync(string token)
    {
        // Логика валидации токена через Auth-сервис
        using var httpClient = new HttpClient { BaseAddress = new Uri("http://auth-service:5000") };
        var response = await httpClient.GetAsync($"/validate?token={token}");
        return response.IsSuccessStatusCode;
    }
}