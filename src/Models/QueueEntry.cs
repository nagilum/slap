using Slap.Core;

namespace Slap.Models;

internal class QueueEntry
{
    /// <summary>
    /// Results from Axe accessibility scan.
    /// </summary>
    public AccessibilityResult? AccessibilityResults { get; set; }
    
    /// <summary>
    /// When the queue entry was added.
    /// </summary>
    public DateTimeOffset Added { get; } = DateTimeOffset.Now;
    
    /// <summary>
    /// Error message, if any occurred.
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Type of error.
    /// </summary>
    public string? ErrorType { get; set; }
    
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
    /// Response data.
    /// </summary>
    public UrlResponse? Response { get; set; }
    
    /// <summary>
    /// When processing of the entry started.
    /// </summary>
    public DateTimeOffset? Started { get; set; }
    
    /// <summary>
    /// URL.
    /// </summary>
    public required Uri Url { get; init; }
    
    /// <summary>
    /// Type of URL.
    /// </summary>
    public required UrlType UrlType { get; init; }
}