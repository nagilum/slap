using Slap.Core;

namespace Slap.Models.Interfaces;

public interface IOptions
{
    /// <summary>
    /// Whether to allow Slap to follow redirect responses.
    /// </summary>
    bool AllowAutoRedirect { get; }
    
    /// <summary>
    /// Whether to capture full page or just viewport when saving screenshots.
    /// </summary>
    bool CaptureFullPage { get; }
    
    /// <summary>
    /// Domain names to skip scanning.
    /// </summary>
    List<string> DomainsToSkip { get; }
    
    /// <summary>
    /// All domains to be treated as internal and will be scanned.
    /// </summary>
    List<string> InternalDomains { get; }
    
    /// <summary>
    /// Maximum number of concurrent URL scans.
    /// </summary>
    int? Parallelism { get; }
    
    /// <summary>
    /// Reg-ex matches for URLs to skip.
    /// </summary>
    List<string> RegExMatchesToSkip { get; }
    
    /// <summary>
    /// Rendering engine to use.
    /// </summary>
    RenderingEngine RenderingEngine { get; }
    
    /// <summary>
    /// Report path.
    /// </summary>
    string? ReportPath { get; }
    
    /// <summary>
    /// Whether to save a screenshot for every internal webpage scan.
    /// </summary>
    bool SaveScreenshots { get; }
    
    /// <summary>
    /// Request timeout.
    /// </summary>
    int Timeout { get; }
    
    /// <summary>
    /// URL types to skip scanning.
    /// </summary>
    List<UrlType> UrlTypesToSkip { get; }
    
    /// <summary>
    /// Set viewport height.
    /// </summary>
    int ViewportHeight { get; }
    
    /// <summary>
    /// Set viewport width.
    /// </summary>
    int ViewportWidth { get; }
}