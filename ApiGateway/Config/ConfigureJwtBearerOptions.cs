using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace ApiGateway.Config;

public class ConfigureJwtBearerOptions(IServiceDiscovery serviceDiscovery) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        // Получаем адрес Auth-сервиса из Consul
        var authority = serviceDiscovery.GetServiceAddress("AUTH-SERVICE").GetAwaiter().GetResult();
        if (string.IsNullOrEmpty(authority))
        {
            throw new InvalidOperationException("Auth-service address not found in Consul.");
        }

        options.Authority = authority;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false
        };
    }
}