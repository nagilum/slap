namespace Slap.Models;

public class ResponseMetaTag : IResponseMetaTag
{
    /// <summary>
    /// <inheritdoc cref="IResponseMetaTag.Charset"/>
    /// </summary>
    public string? Charset { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IResponseMetaTag.Content"/>
    /// </summary>
    public string? Content { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IResponseMetaTag.HttpEquiv"/>
    /// </summary>
    public string? HttpEquiv { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IResponseMetaTag.Name"/>
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IResponseMetaTag.Property"/>
    /// </summary>
    public string? Property { get; init; }
}