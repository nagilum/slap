using Slap.Core;

namespace Slap.Models.Interfaces;

public interface IOptions
{
    /// <summary>
    /// All domains to be treated as internal and will be scanned.
    /// </summary>
    List<string> InternalDomains { get; }
    
    /// <summary>
    /// Rendering engine to use.
    /// </summary>
    RenderingEngine RenderingEngine { get; set; }
    
    /// <summary>
    /// Report path.
    /// </summary>
    string? ReportPath { get; set; }
    
    /// <summary>
    /// URL types to skip scanning.
    /// </summary>
    List<UrlType> UrlTypesToSkip { get; }
}