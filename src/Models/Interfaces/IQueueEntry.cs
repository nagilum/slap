using Slap.Core;

namespace Slap.Models.Interfaces;

public interface IQueueEntry
{
    /// <summary>
    /// Results from Axe accessibility scan.
    /// </summary>
    AccessibilityResult? AccessibilityResults { get; }
    
    /// <summary>
    /// Error message, if any occurred.
    /// </summary>
    string? Error { get; }
    
    /// <summary>
    /// Type of error.
    /// </summary>
    string? ErrorType { get; }
    
    /// <summary>
    /// Unique ID.
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// All URLs this entry is linked from.
    /// </summary>
    List<Uri> LinkedFrom { get; }
    
    /// <summary>
    /// If the entry has been processed.
    /// </summary>
    bool Processed { get; }
    
    /// <summary>
    /// Response data.
    /// </summary>
    QueueResponse? Response { get; }
    
    /// <summary>
    /// URL.
    /// </summary>
    Uri Url { get; }
    
    /// <summary>
    /// Type of URL.
    /// </summary>
    UrlType UrlType { get; }
}