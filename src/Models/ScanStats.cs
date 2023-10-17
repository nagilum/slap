namespace Slap.Models;

public class ScanStats
{
    /// <summary>
    /// Metadata.
    /// </summary>
    public ScanStatsMeta? Meta { get; set; }
    
    /// <summary>
    /// List of all status codes and number of hits pr.
    /// </summary>
    public Dictionary<int, int>? StatusCodes { get; set; }
    
    /// <summary>
    /// Total number of accessibility issues.
    /// </summary>
    public ScanStatsAccessibilityIssues? Accessibility { get; set; }
}