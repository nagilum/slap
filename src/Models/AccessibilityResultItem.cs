using Deque.AxeCore.Commons;
using Slap.Models.Interfaces;

namespace Slap.Models;

public class AccessibilityResultItem : IAccessibilityResultItem
{
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Id"/>
    /// </summary>
    public string? Id { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Description"/>
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Help"/>
    /// </summary>
    public string? Help { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.HelpUrl"/>
    /// </summary>
    public Uri? HelpUrl { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Impact"/>
    /// </summary>
    public string? Impact { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Tags"/>
    /// </summary>
    public string[]? Tags { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IAccessibilityResultItem.Nodes"/>
    /// </summary>
    public ResultItemNode[] Nodes { get; init; }

    /// <summary>
    /// Initialize a new instance of a <see cref="AccessibilityResultItem"/> class.
    /// </summary>
    /// <param name="item">Result item.</param>
    public AccessibilityResultItem(AxeResultItem item)
    {
        this.Id = item.Id;
        this.Description = item.Description;
        this.Help = item.Help;
        this.Impact = item.Impact;
        this.Tags = item.Tags;

        if (Uri.TryCreate(item.HelpUrl, UriKind.Absolute, out var uri))
        {
            this.HelpUrl = uri;
        }

        this.Nodes = item.Nodes
            .Select(n => new ResultItemNode(n))
            .ToArray();
    }
}