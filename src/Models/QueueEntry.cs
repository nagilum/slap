using Slap.Core;

namespace Slap.Models;

internal class QueueEntry
{
    /// <summary>
    /// When the queue entry was added.
    /// </summary>
    public DateTimeOffset Added { get; } = DateTimeOffset.Now;
    
    /// <summary>
    /// Unique ID.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
    
    /// <summary>
    /// Type of URL.
    /// </summary>
    public required UrlType UrlType { get; init; }
    
    /// <summary>
    /// URL.
    /// </summary>
    public required Uri Url { get; init; }
}