using PluginBase;
using System;

namespace TemplateFuzzerPlugin;
#pragma warning disable CA1303

public class Fuzzer : IFuzzerPlugin
{
    public string Name { get => "Fuzzer"; }
    public string Description { get => "Template to create fuzzer"; }

    public int Execute()
    {
        Console.WriteLine("[INFO] Execute fuzzer's task ....");
        Console.WriteLine("[INFO] Finished fuzzer's task");
        return 0;
    }

    public void LoadArgs(string args)
    {
        Console.WriteLine($"[INFO] Loaded args: {args}");
    }

    public void SetCacheDir(string path)
    {
        Console.WriteLine("[ERROR] Not in use!");
        throw new System.NotImplementedException();
    }

    public IFuzzerPlugin ShallowCopy() => (Fuzzer)this.MemberwiseClone();
}
