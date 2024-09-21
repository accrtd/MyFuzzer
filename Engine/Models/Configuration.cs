namespace Engine.Models;
#pragma warning disable CA1812

internal sealed class Configuration
{
    public string? CacheDirLocation { get; set; }
    public required string CacheDirName { get; set; }
    public required string PluginsLocation { get; set; }
    public required int AmountOfExecutionPerThread { get; set; }
    public required int AmountOfThreads { get; set; }
}