namespace Slap.Models;

public class ScanStatsMeta
{
    /// <summary>
    /// When the scan finished.
    /// </summary>
    public required DateTimeOffset Finished { get; init; }
        
    /// <summary>
    /// When the scan started.
    /// </summary>
    public required DateTimeOffset Started { get; init; }

    /// <summary>
    /// How long the scan took.
    /// </summary>
    public TimeSpan Took =>
        this.Finished - this.Started;
}