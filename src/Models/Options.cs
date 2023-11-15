using Slap.Core;
using Slap.Models.Interfaces;

namespace Slap.Models;

public class Options : IOptions
{
    /// <summary>
    /// <inheritdoc cref="IOptions.AllowAutoRedirect"/>
    /// </summary>
    public bool AllowAutoRedirect { get; set; }

    /// <summary>
    /// <inheritdoc cref="IOptions.CaptureFullPage"/>
    /// </summary>
    public bool CaptureFullPage { get; set; }

    /// <summary>
    /// <inheritdoc cref="IOptions.DomainsToSkip"/>
    /// </summary>
    public List<string> DomainsToSkip { get; } = new();

    /// <summary>
    /// <inheritdoc cref="IOptions.InternalDomains"/>
    /// </summary>
    public List<string> InternalDomains { get; } = new();

    /// <summary>
    /// <inheritdoc cref="IOptions.Parallelism"/>
    /// </summary>
    public int? Parallelism { get; set; }

    /// <summary>
    /// <inheritdoc cref="IOptions.RegExMatchesToSkip"/>
    /// </summary>
    public List<string> RegExMatchesToSkip { get; } = new();

    /// <summary>
    /// <inheritdoc cref="IOptions.RenderingEngine"/>
    /// </summary>
    public RenderingEngine RenderingEngine { get; set; } = RenderingEngine.Chromium;
    
    /// <summary>
    /// <inheritdoc cref="IOptions.ReportPath"/>
    /// </summary>
    public string? ReportPath { get; set; }

    /// <summary>
    /// <inheritdoc cref="IOptions.SaveScreenshots"/>
    /// </summary>
    public bool SaveScreenshots { get; set; }

    /// <summary>
    /// <inheritdoc cref="IOptions.Timeout"/>
    /// </summary>
    public int Timeout { get; set; } = 10;

    /// <summary>
    /// <inheritdoc cref="IOptions.UrlTypesToSkip"/>
    /// </summary>
    public List<UrlType> UrlTypesToSkip { get; } = new();

    /// <summary>
    /// <inheritdoc cref="IOptions.ViewportHeight"/>
    /// </summary>
    public int ViewportHeight { get; set; } = 1080;

    /// <summary>
    /// <inheritdoc cref="IOptions.ViewportWidth"/>
    /// </summary>
    public int ViewportWidth { get; set; } = 1920;
}