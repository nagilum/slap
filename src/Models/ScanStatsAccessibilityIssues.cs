namespace Slap.Models;

public class ScanStatsAccessibilityIssues
{
    /// <summary>
    /// Total number of incomplete tests.
    /// </summary>
    public required int Incomplete { get; init; }
        
    /// <summary>
    /// Total number of accessibility violations.
    /// </summary>
    public required int Violations { get; init; }
}