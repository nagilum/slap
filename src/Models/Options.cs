using Slap.Core;

namespace Slap.Models;

internal class Options
{
    /// <summary>
    /// Custom added domains to be treated as internal domains.
    /// </summary>
    public List<string> CustomDomains { get; } = new();

    /// <summary>
    /// Log path.
    /// </summary>
    public string LogPath { get; set; } = Directory.GetCurrentDirectory();
    
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
    
    /// <summary>
    /// Treat links to subdomains as same domain (internal).
    /// </summary>
    public bool SubDomainsAreEqual { get; set; }
}