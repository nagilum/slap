﻿using Microsoft.Playwright;
using Slap.Core;

namespace Slap.Models.Interfaces;

public interface IQueueEntry
{
    /// <summary>
    /// Results from Axe accessibility scan.
    /// </summary>
    AccessibilityResult? AccessibilityResults { get; }
    
    /// <summary>
    /// When the entry was created.
    /// </summary>
    DateTimeOffset Created { get; }
    
    /// <summary>
    /// Error, if any.
    /// </summary>
    ErrorObject? Error { get; }
    
    /// <summary>
    /// Unique ID.
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Whether the URL is https or http.
    /// </summary>
    bool IsHttps { get; }
    
    /// <summary>
    /// All URLs this entry is linked from.
    /// </summary>
    List<Uri> LinkedFrom { get; }
    
    /// <summary>
    /// Playwright page.
    /// </summary>
    IPage? Page { get; }
    
    /// <summary>
    /// If the entry has been processed.
    /// </summary>
    bool Processed { get; }
    
    /// <summary>
    /// Response data.
    /// </summary>
    QueueResponse? Response { get; }
    
    /// <summary>
    /// Whether a screenshot was saved.
    /// </summary>
    bool ScreenshotSaved { get; }

    /// <summary>
    /// Whether the entry was skipped.
    /// </summary>
    bool Skipped { get; }

    /// <summary>
    /// URL.
    /// </summary>
    Uri Url { get; }
    
    /// <summary>
    /// Type of URL.
    /// </summary>
    UrlType UrlType { get; }
}