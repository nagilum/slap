namespace Slap.Models;

public interface IAccessibilityResultItem
{
    /// <summary>
    /// ID.
    /// </summary>
    string? Id { get; }
    
    /// <summary>
    /// Description.
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// Help text.
    /// </summary>
    string? Help { get; }
    
    /// <summary>
    /// Help URL.
    /// </summary>
    string? HelpUrl { get; }
    
    /// <summary>
    /// Impact assessment.
    /// </summary>
    string? Impact { get; }
    
    /// <summary>
    /// Tags.
    /// </summary>
    string[]? Tags { get; }
    
    /// <summary>
    /// Affected nodes.
    /// </summary>
    ResultItemNode[]? Nodes { get; }
}