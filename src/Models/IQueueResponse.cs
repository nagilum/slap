namespace Slap.Models;

public interface IQueueResponse
{
    /// <summary>
    /// Browser type.
    /// </summary>
    BrowserType BrowserType { get; init; }
    
    /// <summary>
    /// Exception message, if any.
    /// </summary>
    string? Error { get; init; }
    
    /// <summary>
    /// Specific error code.
    /// </summary>
    string? ErrorCode { get; init; }
    
    /// <summary>
    /// Headers.
    /// </summary>
    Dictionary<string, string>? Headers { get; init; }
    
    /// <summary>
    /// Meta tags.
    /// </summary>
    List<ResponseMetaTag>? MetaTags { get; set; }
    
    /// <summary>
    /// Response status code.
    /// </summary>
    int? StatusCode { get; init; }
    
    /// <summary>
    /// Response status description.
    /// </summary>
    string? StatusDescription { get; init; }
    
    /// <summary>
    /// Document size.
    /// </summary>
    int? Size { get; init; }
    
    /// <summary>
    /// Response time.
    /// </summary>
    long? Time { get; init; }
    
    /// <summary>
    /// Whether the request timed out.
    /// </summary>
    bool Timeout { get; init; }
    
    /// <summary>
    /// Document title.
    /// </summary>
    string? Title { get; set; }
    
    /// <summary>
    /// Get content type header value.
    /// </summary>
    /// <returns>Content type.</returns>
    string? GetContentType();
}