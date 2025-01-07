namespace ApiGateway.Services;

public interface IServiceDiscovery
{
    Task<string?> GetServiceAddress(string serviceName);
}