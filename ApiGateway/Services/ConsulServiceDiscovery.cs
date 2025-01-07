using Consul;

namespace ApiGateway.Services;
public class ConsulServiceDiscovery(IConsulClient consulClient) : IServiceDiscovery
{
    public async Task<string?> GetServiceAddress(string serviceName)
    {
        var services = await consulClient.Agent.Services();
        var service = services.Response.Values.FirstOrDefault(s => s.ID.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        return service != null ? $"{service.Address}:{service.Port}" : null;
    }
}