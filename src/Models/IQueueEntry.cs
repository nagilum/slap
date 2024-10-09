namespace Slap.Models;

public interface IQueueEntry
{
    /// <summary>
    /// Whether all requests were finished.
    /// </summary>
    DateTime? Finished { get; set; }
    
    /// <summary>
    /// Unique ID.
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// When the entry was started.
    /// </summary>
    DateTime? Started { get; set; }
    
    /// <summary>
    /// Type of entry.
    /// </summary>
    EntryType Type { get; }
    
    /// <summary>
    /// Response data.
    /// </summary>
    List<IQueueResponse> Responses { get; }
    
    /// <summary>
    /// URL.
    /// </summary>
    Uri Url { get; }
}