using Slap.Core;

namespace Slap.Models;

internal class QueueEntry
{
    /// <summary>
    /// When the queue entry was added.
    /// </summary>
    public DateTimeOffset Added { get; } = DateTimeOffset.Now;
    
    /// <summary>
    /// Error message, if any occurred.
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// When processing of the entry finished.
    /// </summary>
    public DateTimeOffset? Finished { get; set; }
    
    /// <summary>
    /// Unique ID.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// ID of entries it's linked from.
    /// </summary>
    public List<Guid> LinkedFrom { get; } = new();
    
    /// <summary>
    /// Response body size, in bytes.
    /// </summary>
    public int? Size { get; set; }
    
    /// <summary>
    /// When processing of the entry started.
    /// </summary>
    public DateTimeOffset? Started { get; set; }
    
    /// <summary>
    /// Response status code.
    /// </summary>
    public int? StatusCode { get; set; }
    
    /// <summary>
    /// Whether the status code count as success. Anything in the 200-299 range.
    /// </summary>
    public bool? StatusCodeIsSuccess { get; set; }
    
    /// <summary>
    /// URL.
    /// </summary>
    public required Uri Url { get; init; }
    
    /// <summary>
    /// Type of URL.
    /// </summary>
    public required UrlType UrlType { get; init; }
}