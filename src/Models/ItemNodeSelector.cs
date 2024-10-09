using Deque.AxeCore.Commons;

namespace Slap.Models;

public class ItemNodeSelector(
    AxeSelector? selector) : IItemNodeSelector
{
    /// <summary>
    /// <inheritdoc cref="IItemNodeSelector.FrameSelectors"/>
    /// </summary>
    public string[]? FrameSelectors { get; } = selector?.FrameSelectors.ToArray();

    /// <summary>
    /// <inheritdoc cref="IItemNodeSelector.FrameShadowSelectors"/>
    /// </summary>
    public List<string[]>? FrameShadowSelectors { get; } =
        selector?.FrameShadowSelectors
            .Select(n => n.ToArray())
            .ToList();

    /// <summary>
    /// <inheritdoc cref="IItemNodeSelector.Selector"/>
    /// </summary>
    public string? Selector { get; } = selector?.Selector;
}