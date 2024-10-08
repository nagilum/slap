namespace Slap.Handlers;

public interface IQueueHandler
{
    /// <summary>
    /// Process queue and initiate scan of each item.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessQueue(CancellationToken cancellationToken);

    /// <summary>
    /// Setup HttpClient and Playwright.
    /// </summary>
    /// <param name="reportPath">Generated report path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> Setup(string? reportPath, CancellationToken cancellationToken);
}