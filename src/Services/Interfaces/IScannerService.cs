using Slap.Models;

namespace Slap.Services.Interfaces;

public interface IScannerService
{
    /// <summary>
    /// Perform the appropriate request for the entry.
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PerformRequest(QueueEntry entry, CancellationToken cancellationToken);
    
    /// <summary>
    /// Setup Playwright, launch and instance, and prepare a page.
    /// </summary>
    /// <returns>Success.</returns>
    Task<bool> SetupPlaywright();
}