namespace Slap.Models;

internal class QueueEntryResponse
{
    /// <summary>
    /// Response body size, in bytes.
    /// </summary>
    public int? Size { get; set; }
    
    /// <summary>
    /// Response status code.
    /// </summary>
    public int? StatusCode { get; set; }
    
    /// <summary>
    /// Whether the status code count as success. Anything in the 200-299 range.
    /// </summary>
    public bool? StatusCodeIsSuccess { get; set; }
    
    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    public long? Time { get; set; }
}