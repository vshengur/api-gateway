using Consul;

using System.Text.RegularExpressions;

namespace ApiGateway.Config;

public partial class EnvironmentVariableProcessor(IConsulClient consulClient)
{
    /// <summary>
    /// Метод для замены переменных окружения
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="sectionName"></param>
    /// <returns></returns>
    public IConfigurationRoot ReplaceEnvironmentVariables(IConfiguration configuration, string sectionName)
    {
        void ReplaceInSection(IConfigurationSection section)
        {
            foreach (var child in section.GetChildren())
            {
                if (!string.IsNullOrEmpty(child.Value))
                {
                    var replacedValue = ConfigParamRegex().Replace(child.Value, match =>
                    {
                        var envVar = match.Groups[1].Value;

                        // Получение из переменных окружения
                        var envValue = Environment.GetEnvironmentVariable(envVar);
                        if (!string.IsNullOrEmpty(envValue))
                        {
                            return envValue;
                        }

                        // Получение из Consul KV-хранилища
                        var consulValue = GetConsulValue(envVar).GetAwaiter().GetResult();
                        if (!string.IsNullOrEmpty(consulValue))
                        {
                            return consulValue;
                        }

                        return match.Value;
                    });

                    child.Value = replacedValue;
                }

                ReplaceInSection(child);
            }
        }

        var memoryConfig = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .Build();

        ReplaceInSection(memoryConfig.GetSection(sectionName));
        return memoryConfig;
    }

    private async Task<string?> GetConsulValue(string key)
    {
        try
        {
            var response = await consulClient.KV.Get(key);
            if (response.Response != null)
            {
                return System.Text.Encoding.UTF8.GetString(response.Response.Value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing Consul for key {key}: {ex.Message}");
        }

        return null;
    }

    [GeneratedRegex(@"\${(\w+)}")]
    private static partial Regex ConfigParamRegex();
}
