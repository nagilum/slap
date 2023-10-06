using System.Text.Json.Serialization;

namespace Slap.Core;

public enum ConsoleColorEx
{
    /// <summary>
    /// Reset both fore- and background color.
    /// </summary>
    ResetColor,
    
    /// <summary>
    /// Next color specified will be used as background color.
    /// </summary>
    NextColorIsBackground
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RenderingEngine
{
    /// <summary>
    /// Chromium.
    /// </summary>
    Chromium,
    
    /// <summary>
    /// Firefox.
    /// </summary>
    Firefox,
    
    /// <summary>
    /// Webkit.
    /// </summary>
    Webkit
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UrlType
{
    /// <summary>
    /// External asset.
    /// </summary>
    ExternalAsset,
    
    /// <summary>
    /// External webpage.
    /// </summary>
    ExternalWebpage,
    
    /// <summary>
    /// Internal asset.
    /// </summary>
    InternalAsset,
    
    /// <summary>
    /// Internal webpage.
    /// </summary>
    InternalWebpage
}