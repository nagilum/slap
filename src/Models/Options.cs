using Microsoft.Playwright;

namespace Slap.Models;

public class Options : IOptions
{
    /// <summary>
    /// <inheritdoc cref="IOptions.BrowserNewPageOptions"/>
    /// </summary>
    public required BrowserNewPageOptions BrowserNewPageOptions { get; init;}
    
    /// <summary>
    /// <inheritdoc cref="IOptions.BrowserLaunchOptions"/>
    /// </summary>
    public required BrowserTypeLaunchOptions BrowserLaunchOptions { get; init;}

    /// <summary>
    /// <inheritdoc cref="IOptions.FollowRedirects"/>
    /// </summary>
    public bool FollowRedirects { get; set; }

    /// <summary>
    /// <inheritdoc cref="IOptions.InitialUrls"/>
    /// </summary>
    public List<Uri> InitialUrls { get; } = [];

    /// <summary>
    /// <inheritdoc cref="IOptions.InternalDomains"/>
    /// </summary>
    public List<string> InternalDomains { get; } = [];

    /// <summary>
    /// <inheritdoc cref="IOptions.PageGotoOptions"/>
    /// </summary>
    public required PageGotoOptions PageGotoOptions { get; init;}

    /// <summary>
    /// <inheritdoc cref="IOptions.ReportPath"/>
    /// </summary>
    public string ReportPath { get; set; } = Directory.GetCurrentDirectory();
    
    /// <summary>
    /// <inheritdoc cref="IOptions.SaveFullPageScreenshots"/>
    /// </summary>
    public bool SaveFullPageScreenshots { get; set; }
    
    /// <summary>
    /// <inheritdoc cref="IOptions.SaveScreenshots"/>
    /// </summary>
    public bool SaveScreenshots { get; set; }

    /// <summary>
    /// <inheritdoc cref="IOptions.SkipAssets"/>
    /// </summary>
    public bool SkipAssets { get; set; }
    
    /// <summary>
    /// <inheritdoc cref="IOptions.SkipExternal"/>
    /// </summary>
    public bool SkipExternal { get; set; }

    /// <summary>
    /// <inheritdoc cref="IOptions.SkipRegexMatches"/>
    /// </summary>
    public List<string> SkipRegexMatches { get; } = [];
    
    /// <summary>
    /// <inheritdoc cref="IOptions.ViewportHeight"/>
    /// </summary>
    public int ViewportHeight { get; set; }
    
    /// <summary>
    /// <inheritdoc cref="IOptions.ViewportWidth"/>
    /// </summary>
    public int ViewportWidth { get; set; }
}