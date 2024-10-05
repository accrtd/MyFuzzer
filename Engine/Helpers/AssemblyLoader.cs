using Engine.Exceptions;
using Engine.Interfaces;
using PluginBase;
using System.Reflection;

namespace Engine.Helpers;

public class AssemblyLoader : IAssemblyLoader
{
    /// <summary>
    /// Load plugin from specific location to assembly.
    /// </summary>
    /// <param name="pluginLocation"></param>
    /// <returns></returns>
    /// <exception cref="PluginException"></exception>
    public Assembly LoadPlugin(string pluginLocation)
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
    public IEnumerable<IFuzzerPlugin> InstallPlugin(Assembly assembly)
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
}
