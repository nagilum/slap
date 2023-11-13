namespace Slap.Models.Interfaces;

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
    /// Unique ID.
    /// </summary>
    Guid Guid { get; }
    
    /// <summary>
    /// Help text.
    /// </summary>
    string? Help { get; }
    
    /// <summary>
    /// Help URL.
    /// </summary>
    Uri? HelpUrl { get; }
    
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