namespace ExifFuzzerPlugin.Models;
#pragma warning disable CA1812

internal sealed class InputArgsModel
{
    public required string TargetFileLocation { get; set; }
    public required string TargetSampleDataLocation { get; set; }
}
