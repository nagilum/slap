namespace Slap.Services.Interfaces;

public interface IQueueService
{
    /// <summary>
    /// Process the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessQueue(CancellationToken cancellationToken);
}