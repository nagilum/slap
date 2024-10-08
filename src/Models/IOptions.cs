using Microsoft.Playwright;

namespace Slap.Models;

public interface IOptions
{
    /// <summary>
    /// Playwright browser new page options.
    /// </summary>
    BrowserNewPageOptions BrowserNewPageOptions { get; init; }
    
    /// <summary>
    /// Playwright browser launch options.
    /// </summary>
    BrowserTypeLaunchOptions BrowserLaunchOptions { get; init;}
    
    /// <summary>
    /// Follow redirects, such as 301.
    /// </summary>
    bool FollowRedirects { get; set; }
    
    /// <summary>
    /// Initial list of URLs to scan.
    /// </summary>
    List<Uri> InitialUrls { get; }
    
    /// <summary>
    /// All domains to be treated as internal, and will be analyzed.
    /// </summary>
    List<string> InternalDomains { get; }
    
    /// <summary>
    /// Playwright page goto options.
    /// </summary>
    PageGotoOptions PageGotoOptions { get; init;}
    
    /// <summary>
    /// Report base path.
    /// </summary>
    string ReportPath { get; set; }
    
    /// <summary>
    /// Capture fill page screenshots instead of just the viewport.
    /// </summary>
    bool SaveFullPageScreenshots { get; set; }
    
    /// <summary>
    /// Save screenshots of each Playwright render.
    /// </summary>
    bool SaveScreenshots { get; set; }
    
    /// <summary>
    /// Skip checking assets, anything other than HTML pages.
    /// </summary>
    bool SkipAssets { get; set; }
    
    /// <summary>
    /// Skip checking external links.
    /// </summary>
    bool SkipExternal { get; set; }
    
    /// <summary>
    /// Skip any URL matching one of the regular expressions.
    /// </summary>
    List<string> SkipRegexMatches { get; }
    
    /// <summary>
    /// Viewport height.
    /// </summary>
    int ViewportHeight { get; set; }
    
    /// <summary>
    /// Viewport width.
    /// </summary>
    int ViewportWidth { get; set; }
}