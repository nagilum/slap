using Microsoft.Playwright;

namespace Slap.Models;

internal class PlaywrightConfig
{
    /// <summary>
    /// Playwright browser new-page options.
    /// </summary>
    public BrowserNewPageOptions? BrowserNewPageOptions { get; init; }
    
    /// <summary>
    /// Playwright browser launch options.
    /// </summary>
    public BrowserTypeLaunchOptions? BrowserTypeLaunchOptions { get; init; }
}