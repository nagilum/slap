using Deque.AxeCore.Commons;

namespace Slap.Models;

public class AccessibilityResultItem(
    AxeResultItem item) : IAccessibilityResultItem
{
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Id"/>
    /// </summary>
    public string? Id { get; } = item.Id;
    
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Description"/>
    /// </summary>
    public string? Description { get; } = item.Description;
    
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Help"/>
    /// </summary>
    public string? Help { get; } = item.Help;

    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.HelpUrl"/>
    /// </summary>
    public string? HelpUrl { get; } = item.HelpUrl;
    
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Impact"/>
    /// </summary>
    public string? Impact { get; } = item.Impact;
    
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Tags"/>
    /// </summary>
    public string[]? Tags { get; } = item.Tags;

    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Nodes"/>
    /// </summary>
    public ResultItemNode[]? Nodes { get; } =
        item.Nodes
            .Select(n => new ResultItemNode(n))
            .ToArray();
}