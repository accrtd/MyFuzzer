using PluginBase;
using System.Reflection;

namespace Engine.Interfaces;

public interface IAssemblyLoader
{
    Assembly LoadPlugin(string pluginLocation);
    IEnumerable<IFuzzerPlugin> InstallPlugin(Assembly assembly);
}
