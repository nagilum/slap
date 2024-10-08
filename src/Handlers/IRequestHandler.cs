using Slap.Models;

namespace Slap.Handlers;

public interface IRequestHandler
{
    /// <summary>
    /// Path to save screenshots.
    /// </summary>
    string? ScreenshotPath { get; set; }
    
    /// <summary>
    /// Perform a HttpClient request. 
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PerformHttpClientRequest(QueueEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Perform a Playwright request.
    /// </summary>
    /// <param name="browserType">Browser type.</param>
    /// <param name="entry">Queue entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PerformPlaywrightRequest(BrowserType browserType, QueueEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Setup HttpClient.
    /// </summary>
    void SetupHttpClient();

    /// <summary>
    /// Setup Playwright and browser instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetupPlaywright(CancellationToken cancellationToken);
}