namespace Slap.Models;

public interface IItemNodeSelector
{
    /// <summary>
    /// Frame selectors.
    /// </summary>
    string[]? FrameSelectors { get; }
    
    /// <summary>
    /// Frame shadow selectors.
    /// </summary>
    List<string[]>? FrameShadowSelectors { get; }
    
    /// <summary>
    /// Selector.
    /// </summary>
    string? Selector { get; }
}