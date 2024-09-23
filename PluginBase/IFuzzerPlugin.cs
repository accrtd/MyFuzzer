namespace PluginBase;

public interface IFuzzerPlugin
{
    string Name { get; }
    string Description { get; }
    void LoadArgs(string args);
    void SetCacheDir(string path);
    int Execute();
    IFuzzerPlugin ShallowCopy();
}
