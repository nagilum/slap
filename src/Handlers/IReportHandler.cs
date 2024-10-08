namespace Slap.Handlers;

public interface IReportHandler
{
    /// <summary>
    /// Report path.
    /// </summary>
    string? ReportPath { get; set; }
    
    /// <summary>
    /// Setup report path.
    /// </summary>
    /// <returns>Success.</returns>
    bool Setup();
    
    /// <summary>
    /// Write reports to disk.
    /// </summary>
    Task WriteReports();
}