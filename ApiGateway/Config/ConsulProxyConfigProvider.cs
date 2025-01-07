using Consul;
using Microsoft.Extensions.Primitives;

using System.Collections.Immutable;

using Yarp.ReverseProxy.Configuration;

using DestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;
using RouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;

namespace ApiGateway.Config;

/// <summary>
/// Реализация динамической конфигурации YARP с использованием Consul
/// </summary>
public class ConsulProxyConfigProvider(IConsulClient consulClient) : IProxyConfigProvider
{
    public IProxyConfig GetConfig()
    {
        // Получаем список сервисов из Consul
        var services = consulClient.Agent.Services().Result.Response;

        // Формируем маршруты и кластеры для YARP
        var routes = services.Values.Select(service => new RouteConfig
        {
            RouteId = service.ID,
            ClusterId = $"{service.ID}-cluster",
            Match = new RouteMatch
            {
                Path = $"/{service.Meta["prefix"]}/{{**catch-all}}"
            },
            Transforms =
            [
                new Dictionary<string, string> { { "RequestHeadersCopy", "true" } },
                new Dictionary<string, string> { { "PathRemovePrefix", $"/{service.Meta["prefix"]}" } }
            ]
        }).ToList();

        var clusters = services.Values.Select(service => new ClusterConfig
        {
            ClusterId = $"{service.ID}-cluster",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                [service.ID] = new DestinationConfig
                {
                    Address = $"http://{service.Address}:{service.Port}"
                }
            }
        }).ToList();

        // Возвращаем новую конфигурацию
        return new ConsulProxyConfig(routes, clusters);
    }
}

// Класс для хранения динамической конфигурации YARP
public class ConsulProxyConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters) : IProxyConfig
{
    public IReadOnlyList<RouteConfig> Routes { get; } = routes;
    public IReadOnlyList<ClusterConfig> Clusters { get; } = clusters;
    public IChangeToken ChangeToken { get; } = new CancellationChangeToken(new CancellationToken());
}