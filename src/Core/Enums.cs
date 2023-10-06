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

public enum UrlType
{
    /// <summary>
    /// Webpage to be scanned.
    /// </summary>
    Webpage,
    
    /// <summary>
    /// Scripts, stylesheets, etc..
    /// </summary>
    Asset,
    
    /// <summary>
    /// External links.
    /// </summary>
    External
}