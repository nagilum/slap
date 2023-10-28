namespace Slap.Models.Interfaces;

public interface IQueueResponse
{
    /// <summary>
    /// HTML document title.
    /// </summary>
    string? DocumentTitle { get; }
    
    /// <summary>
    /// Headers.
    /// </summary>
    Dictionary<string, string>? Headers { get; }
    
    /// <summary>
    /// HTML meta tags.
    /// </summary>
    List<IMetaTag>? MetaTags { get; }
    
    /// <summary>
    /// Body size, in bytes.
    /// </summary>
    int? Size { get; }
    
    /// <summary>
    /// HTTP status code.
    /// </summary>
    int? StatusCode { get; }
    
    /// <summary>
    /// Status code description.
    /// </summary>
    string? StatusDescription { get; }
    
    /// <summary>
    /// Response time, in milliseconds.
    /// </summary>
    long? Time { get; }

    /// <summary>
    /// Get the response size, formatted to be more human readable.
    /// </summary>
    /// <returns>Size, formatted.</returns>
    string? GetSizeFormatted();

    /// <summary>
    /// Get the HTTP status code and description.
    /// </summary>
    /// <returns>HTTP status code and description.</returns>
    string? GetStatusFormatted();

    /// <summary>
    /// Get the response time, formatted to be more human readable.
    /// </summary>
    /// <returns>Time, formatted.</returns>
    string? GetTimeFormatted();
}