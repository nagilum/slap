namespace Slap.Models;

internal class MetaTag
{
    /// <summary>
    /// Character set.
    /// </summary>
    public required string? Charset { get; init; }
    
    /// <summary>
    /// Content.
    /// </summary>
    public required string? Content { get; init; }
    
    /// <summary>
    /// HTTP equivalent.
    /// </summary>
    public required string? HttpEquiv { get; init; }
    
    /// <summary>
    /// Name.
    /// </summary>
    public required string? Name { get; init; }
    
    /// <summary>
    /// Property.
    /// </summary>
    public required string? Property { get; init; }
}