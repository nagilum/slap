namespace Slap.Models;

public interface IResponseMetaTag
{
    /// <summary>
    /// Character set.
    /// </summary>
    string? Charset { get; init; }
    
    /// <summary>
    /// Content.
    /// </summary>
    string? Content { get; init; }
    
    /// <summary>
    /// HTTP equivalent.
    /// </summary>
    string? HttpEquiv { get; init; }
    
    /// <summary>
    /// Name.
    /// </summary>
    string? Name { get; init; }
    
    /// <summary>
    /// Property.
    /// </summary>
    string? Property { get; init; }
}