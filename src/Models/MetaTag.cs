using Slap.Models.Interfaces;

namespace Slap.Models;

public class MetaTag : IMetaTag
{
    /// <summary>
    /// <inheritdoc cref="IMetaTag.Charset"/>
    /// </summary>
    public string? Charset { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IMetaTag.Content"/>
    /// </summary>
    public string? Content { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IMetaTag.HttpEquiv"/>
    /// </summary>
    public string? HttpEquiv { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IMetaTag.Name"/>
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IMetaTag.Property"/>
    /// </summary>
    public string? Property { get; init; }
}