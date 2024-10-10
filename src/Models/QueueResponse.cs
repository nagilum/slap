namespace Slap.Models;

public class QueueResponse : IQueueResponse
{
    /// <summary>
    /// <inheritdoc cref="IQueueResponse.AccessibilityResult"/>
    /// </summary>
    public AccessibilityResult? AccessibilityResult { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.BrowserType"/>
    /// </summary>
    public required BrowserType BrowserType { get; init; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.Error"/>
    /// </summary>
    public Error? Error { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.Headers"/>
    /// </summary>
    public Dictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.MetaTags"/>
    /// </summary>
    public List<ResponseMetaTag>? MetaTags { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.StatusCode"/>
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.StatusDescription"/>
    /// </summary>
    public string? StatusDescription { get; init; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.Size"/>
    /// </summary>
    public int? Size { get; init; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.Time"/>
    /// </summary>
    public long? Time { get; init; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.Timeout"/>
    /// </summary>
    public bool Timeout { get; init; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.Title"/>
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.GetContentType"/>
    /// </summary>
    public string? GetContentType()
    {
        if (this.Headers is null)
        {
            return default;
        }

        foreach (var (key, value) in this.Headers)
        {
            if (!key.Equals("content-type", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = value.Split(';');
            return parts[0];
        }
        
        return default;
    }
}