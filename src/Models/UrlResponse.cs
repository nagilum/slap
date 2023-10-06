namespace Slap.Models;

internal class UrlResponse
{
    public bool HasMetaDescription =>
        this.MetaTags?.Any(n => n.Name == "description") == true;
    
    public bool HasMetaKeywords =>
        this.MetaTags?.Any(n => n.Name == "keywords") == true;

    public bool HasTitle =>
        !string.IsNullOrWhiteSpace(this.Title);
    
    /// <summary>
    /// Response headers.
    /// </summary>
    public required Dictionary<string, string> Headers { get; init; }
    
    /// <summary>
    /// HTML meta tags.
    /// </summary>
    public List<MetaTag>? MetaTags { get; set; }
    
    /// <summary>
    /// Response body size, in bytes.
    /// </summary>
    public required int Size { get; init; }
    
    /// <summary>
    /// Response status code.
    /// </summary>
    public required int StatusCode { get; init; }
    
    /// <summary>
    /// Whether the response status code count as success. Anything in the 200-299 range.
    /// </summary>
    public required bool StatusCodeIsSuccess { get; init; }
    
    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    public required long Time { get; init; }
    
    /// <summary>
    /// HTML document title.
    /// </summary>
    public string? Title { get; set; }
}