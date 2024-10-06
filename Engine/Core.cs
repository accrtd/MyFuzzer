using Engine.Exceptions;
using Engine.Extensions;
using Engine.Interfaces;
using Engine.Models;
using PluginBase;

namespace Engine;
#pragma warning disable CA1303

public class Core
{
    private string[] _args;
    private Configuration? _configuration;
    private IConfig _config;
    private IAssemblyLoader _assemblyLoader;

    public Core(string[] args, IConfig config, IAssemblyLoader assemblyLoader)
    {
        _args = args;
        _configuration = null;
        _config = config;
        _assemblyLoader = assemblyLoader;
    }

    /// <summary>
    /// Engine's executer
    /// </summary>
    /// <exception cref="MissingConfigurationException"></exception>
    /// <exception cref="PluginException"></exception>
    public void Execute()
    {
        // Load config
        _configuration = _config.LoadConfig();
        if (_configuration == null)
        {
            var errMsg = "Config is null!";
            Console.WriteLine($"[ERROR] {errMsg}");
            throw new MissingConfigurationException(errMsg);
        }
        else
            CreateCacheDir();
        List<IFuzzerPlugin>? plugins;
        {
            var pluginPaths = Directory.GetFiles(_configuration.PluginsLocation);
            if (pluginPaths.ValidateIfNotEmpty("[WARNING] No files found in the plugin location!"))
                goto ENGINE_END;
            pluginPaths = pluginPaths.Where(p => p.Contains(".dll", StringComparison.Ordinal)).ToArray();
            if (pluginPaths.ValidateIfNotEmpty("[WARNING] No dll files found in the plugin location!"))
                goto ENGINE_END;

            plugins = pluginPaths.SelectMany(pluginPath =>
            {
                var pluginAssembly = _assemblyLoader.LoadPlugin(pluginPath);
                return _assemblyLoader.InstallPlugin(pluginAssembly);
            }).ToList();
        }

        // Check args
        if (_args.Length == 0)
        {
            Console.WriteLine("[INFO] Installed plugings:");
            foreach (IFuzzerPlugin plugin in plugins)
                Console.WriteLine($"{plugin.Name}\t - {plugin.Description}");
            Console.WriteLine("[INFO] Call program with this pattern: program.exe plugin.Name plugin.Args");
            goto ENGINE_END;
        }
        else if (_args.Length != 2)
        {
            Console.WriteLine("[ERROR] Not valid arguments! Run program without parameters to see available options or read docs!");
            goto ENGINE_END;
        }

        var selectedFuzzer = plugins.Where(p => p.Name.Equals(_args[0], StringComparison.Ordinal)).FirstOrDefault();
        if (selectedFuzzer == null)
        {
            var errMsg = $"[ERROR] Couldn't find {_args[0]} module!";
            Console.WriteLine(errMsg);
            throw new PluginException(errMsg);
        }
        selectedFuzzer.LoadArgs(_args[1]);
        var cacheDirLocation = CreateCacheDir();
        if (!string.IsNullOrWhiteSpace(cacheDirLocation))
            selectedFuzzer.SetCacheDir(cacheDirLocation);
        if (_configuration.AmountOfThreads > 1)
        {
            var threadArray = new Thread[_configuration.AmountOfThreads];
            for (int i = 0; i < _configuration.AmountOfThreads; i++)
                threadArray[i] = new Thread(() => EngineTask(selectedFuzzer.ShallowCopy()));
            foreach (var thread in threadArray)
                thread.Start();
            foreach (var thread in threadArray)
                thread.Join();
        }
        else
            EngineTask(selectedFuzzer);
        CleanUp(cacheDirLocation);
    ENGINE_END:
        Console.WriteLine("[INFO] Finished ;)");
        return;
    }

    /// <summary>
    /// Executing fuzzer loop.
    /// </summary>
    /// <param name="selectedFuzzer"></param>
    private void EngineTask(IFuzzerPlugin selectedFuzzer)
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
    /// Create cache directory
    /// </summary>
    /// <returns></returns>
    /// <exception cref="MissingConfigurationException"></exception>
    private string CreateCacheDir()
    {
        if (_configuration != null)
        {
            if (string.IsNullOrWhiteSpace(_configuration.CacheDirLocation) && string.IsNullOrWhiteSpace(_configuration.CacheDirName))
            {
                Console.WriteLine("[WARNING] Cache directory will not be used");
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
