namespace Slap.Models.Interfaces;

public interface IMetaTag
{
    /// <summary>
    /// Character set.
    /// </summary>
    string? Charset { get; }
    
    /// <summary>
    /// Content.
    /// </summary>
    string? Content { get; }
    
    /// <summary>
    /// HTTP equivalent.
    /// </summary>
    string? HttpEquiv { get; }
    
    /// <summary>
    /// Name.
    /// </summary>
    string? Name { get; }
    
    /// <summary>
    /// Property.
    /// </summary>
    string? Property { get; }
}