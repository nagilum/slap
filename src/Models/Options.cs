using Slap.Core;
using Slap.Models.Interfaces;

namespace Slap.Models;

public class Options : IOptions
{
    /// <summary>
    /// <inheritdoc cref="IOptions.InternalDomains"/>
    /// </summary>
    public List<string> InternalDomains { get; } = new();
    
    /// <summary>
    /// <inheritdoc cref="IOptions.RenderingEngine"/>
    /// </summary>
    public RenderingEngine RenderingEngine { get; set; } = RenderingEngine.Chromium;
    
    /// <summary>
    /// <inheritdoc cref="IOptions.ReportPath"/>
    /// </summary>
    public string? ReportPath { get; set; }

    /// <summary>
    /// <inheritdoc cref="IOptions.UrlTypesToSkip"/>
    /// </summary>
    public List<UrlType> UrlTypesToSkip { get; } = new();
}