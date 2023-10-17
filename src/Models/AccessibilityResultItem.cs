using Deque.AxeCore.Commons;

namespace Slap.Models;

public class AccessibilityResultItem
{
    /// <summary>
    /// ID.
    /// </summary>
    public string? Id { get; init; }
    
    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Help text.
    /// </summary>
    public string? Help { get; init; }
    
    /// <summary>
    /// Help URL.
    /// </summary>
    public Uri? HelpUrl { get; init; }
    
    /// <summary>
    /// Impact assessment.
    /// </summary>
    public string? Impact { get; init; }
    
    /// <summary>
    /// Tags.
    /// </summary>
    public string[]? Tags { get; init; }
    
    /// <summary>
    /// Affected nodes.
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