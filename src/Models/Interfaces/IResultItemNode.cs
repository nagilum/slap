namespace Slap.Models.Interfaces;

public interface IResultItemNode
{
    /// <summary>
    /// Unique ID.
    /// </summary>
    Guid Guid { get; }
    
    /// <summary>
    /// Source HTML.
    /// </summary>
    string? Html { get; }
    
    /// <summary>
    /// Impact assessment.
    /// </summary>
    string? Impact { get; }
    
    /// <summary>
    /// Message.
    /// </summary>
    string? Message { get; }
    
    /// <summary>
    /// Target selector.
    /// </summary>
    ItemNodeSelector? Target { get; }
    
    /// <summary>
    /// XPath selector.
    /// </summary>
    ItemNodeSelector? XPath { get; }
}