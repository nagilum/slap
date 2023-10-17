namespace Slap.Models;

public class ScanStats
{
    /// <summary>
    /// Metadata.
    /// </summary>
    public StatMeta? Meta { get; set; }
    
    /// <summary>
    /// List of all status codes and number of hits pr.
    /// </summary>
    public Dictionary<int, int>? StatusCodes { get; set; }
    
    /// <summary>
    /// Total number of accessibility issues.
    /// </summary>
    public StatAccessibility? Accessibility { get; set; }

    #region Subclasses

    public class StatMeta
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

    public class StatAccessibility
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
    
    #endregion
}