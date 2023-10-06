using Slap.Core;

namespace Slap.Models;

internal class Options
{
    /// <summary>
    /// All valid domains.
    /// </summary>
    public List<string> InternalDomains { get; } = new();

    /// <summary>
    /// Report path.
    /// </summary>
    public string? ReportPath { get; set; }
    
    /// <summary>
    /// Playwright config.
    /// </summary>
    public PlaywrightConfig? PlaywrightConfig { get; set; } 
    
    /// <summary>
    /// Rendering engine.
    /// </summary>
    public RenderingEngine RenderingEngine { get; set; } = RenderingEngine.Chromium;
    
    /// <summary>
    /// Whether to save screenshots for each URL scanned.
    /// </summary>
    public bool SaveScreenshots { get; set; }
}