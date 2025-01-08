namespace ApiGateway.Config;

public class RouteConfigLoader
{
    public static Dictionary<string, bool> LoadRoutes(IConfiguration configuration)
    {
        var routes = configuration.GetSection("RouteConfig").Get<Dictionary<string, bool>>();
        return routes ?? [];
    }
}