using Engine.Exceptions;
using Engine.Extensions;
using Engine.Helpers;
using Engine.Models;
using PluginBase;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace Engine;
#pragma warning disable CA1303

static class Program
{
    private static Configuration? _configuration;

    public static void Main(string[] args)
    {
        Console.WriteLine($"[INFO] MyFuzzer [Version {(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)).ProductVersion}]");
        // Load config
        LoadConfig();
        CreateCacheDir();
        if (_configuration == null)
        {
            var errMsg = "Config is null!";
            Console.WriteLine($"[ERROR] {errMsg}");
            throw new MissingConfigurationException(errMsg);
        }
        var pluginPaths = Directory.GetFiles(_configuration.PluginsLocation);
        if (pluginPaths.ValidateIfNotEmpty("[WARNING] No files found in the plugin location!"))
            goto ENGINE_END;
        pluginPaths = pluginPaths.Where(p => p.Contains(".dll", StringComparison.Ordinal)).ToArray();
        if (pluginPaths.ValidateIfNotEmpty("[WARNING] No dll files found in the plugin location!"))
            goto ENGINE_END;

        var plugins = pluginPaths.SelectMany(pluginPath =>
        {
            var pluginAssembly = LoadPlugin(pluginPath);
            return InstallPlugin(pluginAssembly);
        }).ToList();

        if (args.Length == 0)
        {
            Console.WriteLine("[INFO] Installed plugings:");
            foreach (IFuzzerPlugin plugin in plugins)
                Console.WriteLine($"{plugin.Name}\t - {plugin.Description}");
            Console.WriteLine("[INFO] Call program with this pattern: program.exe plugin.Name plugin.Args");
            goto ENGINE_END;
        }
        else if (args.Length != 2)
        {
            Console.WriteLine("[ERROR] Not valid arguments! Run program without parameters to see available options or read docs!");
            goto ENGINE_END;
        }

        var selectedFuzzer = plugins.Where(p => p.Name.Equals(args[0], StringComparison.Ordinal)).FirstOrDefault();
        if (selectedFuzzer == null)
        {
            var errMsg = $"[ERROR] Couldn't find {args[0]} module!";
            Console.WriteLine(errMsg);
            throw new PluginException(errMsg);
        }
        selectedFuzzer.LoadArgs(args[1]);
        var cacheDirLocation = CreateCacheDir();
        if (!string.IsNullOrWhiteSpace(cacheDirLocation))
            selectedFuzzer.SetCacheDir(cacheDirLocation);
        if (_configuration.AmountOfThreads > 1)
        {
            var threadArray = new Thread[_configuration.AmountOfThreads];
            for (int i = 0; i < _configuration.AmountOfThreads; i++)
                threadArray[i] = new Thread(() => ThreadTask(selectedFuzzer.ShallowCopy()));
            foreach (var thread in threadArray)
                thread.Start();
            foreach (var thread in threadArray)
                thread.Join();
        }
        else
            ThreadTask(selectedFuzzer);
        CleanUp(cacheDirLocation);
    ENGINE_END:
        Console.WriteLine("[INFO] Finished ;)");
        return;
    }

    /// <summary>
    /// Executing fuzzer loop.
    /// </summary>
    /// <param name="selectedFuzzer"></param>
    private static void ThreadTask(IFuzzerPlugin selectedFuzzer)
    {
        if (_configuration == null)
            return;

        for (int i = 0; i < _configuration.AmountOfExecutionPerThread; i++)
        {
            Console.WriteLine($"[INFO] TH: {Environment.CurrentManagedThreadId} LOOP: {i + 1}");
            var status = selectedFuzzer.Execute();
            if (status != 0)
            {
                Console.WriteLine($"[ERROR] Fuzzer returned {status}!");
                break;
            }
        }
    }

    /// <summary>
    /// Load plugin from specific location to assembly.
    /// </summary>
    /// <param name="pluginLocation"></param>
    /// <returns></returns>
    /// <exception cref="PluginException"></exception>
    private static Assembly LoadPlugin(string pluginLocation)
    {
        Console.WriteLine($"[INFO] Loading plugings from: {pluginLocation}");
        try
        {
            var loadContext = new PluginLoadContext(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }
        catch (Exception ex)
        {
            var errMsg = $"Failed to load plugins: {ex.Message}!";
            Console.WriteLine(errMsg);
            throw new PluginException(errMsg);
        }
    }

    /// <summary>
    /// Find plugin class in assembly.
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    /// <exception cref="PluginException"></exception>
    private static IEnumerable<IFuzzerPlugin> InstallPlugin(Assembly assembly)
    {
        int count = 0;

        foreach (Type type in assembly.GetTypes())
        {
            if (typeof(IFuzzerPlugin).IsAssignableFrom(type))
            {
                var result = Activator.CreateInstance(type) as IFuzzerPlugin;
                if (result != null)
                {
                    count++;
                    yield return result;
                }
            }
        }

        if (count == 0)
        {
            var availableTypes = string.Join(",", assembly.GetTypes().Select(t => t.FullName));
            var errMsg = $"Can't find any type which implements IFuzzerPlugin in {assembly} from {assembly.Location}. Available types: {availableTypes}";
            Console.WriteLine(errMsg);
            throw new PluginException(errMsg);
        }
    }

    /// <summary>
    /// Load configuration from json file.
    /// </summary>
    /// <exception cref="MissingConfigurationException"></exception>
    private static void LoadConfig()
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
                _configuration = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(configFileLocation));
            }
            catch (JsonException e)
            {
                var toBeSearched = "including the following:";
                Console.WriteLine($"[ERROR] Missing those fields in config: {e.Message[(e.Message.IndexOf(toBeSearched, StringComparison.Ordinal) + toBeSearched.Length)..]}");
                throw new MissingConfigurationException("Configuration file 'config.json' is corrupted");
            }
        }
    }

    /// <summary>
    /// Create cache directory
    /// </summary>
    /// <returns></returns>
    /// <exception cref="MissingConfigurationException"></exception>
    private static string CreateCacheDir()
    {
        if (_configuration != null)
        {
            if (string.IsNullOrWhiteSpace(_configuration.CacheDirLocation) && string.IsNullOrWhiteSpace(_configuration.CacheDirName))
            {
                Console.WriteLine("[INFO] Cache directory will not be used");
                return "";
            }
            var cacheDirLocation = Path.Combine(_configuration.CacheDirLocation ?? Directory.GetCurrentDirectory(), _configuration.CacheDirName);
            if (!Directory.Exists(cacheDirLocation))
            {
                Directory.CreateDirectory(cacheDirLocation);
                Console.WriteLine("[INFO] Created cache directory");
            }
            return cacheDirLocation;
        }
        else
        {
            Console.WriteLine("[ERROR] Issues with configuration!");
            throw new MissingConfigurationException("Issues with configuration!");
        }
    }

    /// <summary>
    /// Clean up some stuff. Here will delete cache directory if it's empty
    /// </summary>
    /// <param name="cacheDirLocation"></param>
    private static void CleanUp(string cacheDirLocation)
    {
        if ((!string.IsNullOrWhiteSpace(cacheDirLocation))
            && Directory.Exists(cacheDirLocation)
            && !Directory.EnumerateFileSystemEntries(cacheDirLocation).Any())
        {
            Console.WriteLine("[INFO] Cache directory is empty, will delete it");
            Directory.Delete(cacheDirLocation, false);
        }
    }
}
