using Deque.AxeCore.Commons;

namespace Slap.Models;

public class ItemNodeSelector
{
    /// <summary>
    /// Frame selectors.
    /// </summary>
    public string[]? FrameSelectors { get; init; }

    /// <summary>
    /// Frame shadow selectors.
    /// </summary>
    public List<string[]>? FrameShadowSelectors { get; init; }

    /// <summary>
    /// Selector.
    /// </summary>
    public string? Selector { get; init; }

    /// <summary>
    /// Initialize a new instance of a <see cref="ItemNodeSelector"/> class.
    /// </summary>
    /// <param name="selector">Selector.</param>
    public ItemNodeSelector(AxeSelector? selector)
    {
        if (selector is null)
        {
            return;
        }
        
        this.Selector = selector.Selector;
        this.FrameSelectors = selector.FrameSelectors.ToArray();
        this.FrameShadowSelectors = selector.FrameShadowSelectors
            .Select(n => n.ToArray())
            .ToList();
    }
}