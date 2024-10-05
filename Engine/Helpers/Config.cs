using Engine.Exceptions;
using Engine.Interfaces;
using Engine.Models;
using System.Reflection;
using System.Text.Json;

namespace Engine.Helpers;
#pragma warning disable CA1303

public class Config : IConfig
{
    /// <summary>
    /// Load configuration from json file.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="MissingConfigurationException"></exception>
    public Configuration? LoadConfig()
    {
        var configFileLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "config.json");
        if (!File.Exists(configFileLocation))
        {
            Console.WriteLine("[ERROR] Configuration file 'config.json' is missing!");
            throw new MissingConfigurationException("Configuration file 'config.json' is missing!");
        }
        else
        {
            try
            {
                return JsonSerializer.Deserialize<Configuration>(File.ReadAllText(configFileLocation));
            }
            catch (JsonException e)
            {
                var toBeSearched = "including the following:";
                Console.WriteLine($"[ERROR] Missing those fields in config: {e.Message[(e.Message.IndexOf(toBeSearched, StringComparison.Ordinal) + toBeSearched.Length)..]}");
                throw new MissingConfigurationException("Configuration file 'config.json' is corrupted");
            }
        }
    }
}
