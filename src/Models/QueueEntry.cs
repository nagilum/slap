using System.Text.Json.Serialization;
using Microsoft.Playwright;
using Slap.Core;
using Slap.Models.Interfaces;

namespace Slap.Models;

public class QueueEntry : IQueueEntry
{
    /// <summary>
    /// <inheritdoc cref="IQueueEntry.AccessibilityResults"/>
    /// </summary>
    public AccessibilityResult? AccessibilityResults { get; set; }
    
    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Error"/>
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// <inheritdoc cref="IQueueEntry.ErrorType"/>
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Id"/>
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.LinkedFrom"/>
    /// </summary>
    public List<Uri> LinkedFrom { get; } = new();

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Page"/>
    /// </summary>
    [JsonIgnore]
    public IPage? Page { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Processed"/>
    /// </summary>
    public bool Processed { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Response"/>
    /// </summary>
    public QueueResponse? Response { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.ScreenshotSaved"/>
    /// </summary>
    public bool ScreenshotSaved { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Skipped"/>
    /// </summary>
    public bool Skipped { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Url"/>
    /// </summary>
    public Uri Url { get; }

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.UrlType"/>
    /// </summary>
    public UrlType UrlType { get; }

    /// <summary>
    /// Initialize a new instance of a <see cref="QueueEntry"/> class.
    /// </summary>
    /// <param name="url">URL.</param>
    /// <param name="urlType">Type of URL.</param>
    public QueueEntry(Uri url, UrlType urlType = UrlType.InternalWebpage)
    {
        this.Url = url;
        this.UrlType = urlType;
    }
}