using Engine.Helpers;
using System.Diagnostics;
using System.Reflection;

namespace Engine;
#pragma warning disable CA1303

static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine($"[INFO] MyFuzzer [Version {(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)).ProductVersion}]");
        var engineCore = new Core(args, new Config(), new AssemblyLoader());
        engineCore.Execute();
    }
}