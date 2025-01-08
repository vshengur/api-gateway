using ApiGateway.Config;
using ApiGateway.Middleware;
using ApiGateway.Services;
using ApiGateway.Services.RateLimiter;

using Consul;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Serilog;

using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IRateLimiter>(sp => new TokenBucket(
    bucketCapacity: 100,    // ������������ ���������� �������
    refillRate: 10          // ������� � �������
));

// Service Discovery ����� Consul
var consulAddress = builder.Configuration.GetValue<string>("CONSUL_ADDRESS");
if (string.IsNullOrEmpty(consulAddress))
{
    throw new ArgumentNullException("CONSUL_ADDRESS", "Consul address configuration is missing or empty.");
}

builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(config =>
{
    config.Address = new Uri(consulAddress); // ����� ������ Consul-�������
}));
builder.Services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();

// ��������� ������ ���������� ��������� � appsettings.json
builder.Services.AddSingleton<EnvironmentVariableProcessor>();

// ��������� Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    var processor = services.GetRequiredService<EnvironmentVariableProcessor>();
    var processedConfig = processor.ReplaceEnvironmentVariables(context.Configuration, "Serilog");

    configuration
        .ReadFrom.Configuration(processedConfig)
        .Enrich.FromLogContext();
});


// ���������� ������������ ������������ ��� YARP
builder.Services.AddSingleton<IProxyConfigProvider, ConsulProxyConfigProvider>();
// ��������� YARP
builder.Services.AddReverseProxy();

// JWT ��������������
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Middleware
app
    .UseSerilogRequestLogging()
    .UseMiddleware<LoggingMiddleware>()
    .UseMiddleware<RateLimitingMiddleware>()
    .UseMiddleware<AuthenticationMiddleware>()
    .UseAuthentication()
    .UseAuthorization();

// YARP �������������
app.MapReverseProxy();

// �������� ��� ��������
app.MapGet("/health", () => Results.Ok("Gateway is running"));

app.Run();