using Slap.Core;
using Slap.Models.Interfaces;

namespace Slap.Models;

public class ErrorObject : IErrorObject
{
    /// <summary>
    /// <inheritdoc cref="IErrorObject.Data"/>
    /// </summary>
    public Dictionary<string, object>? Data { get; init; }

    /// <summary>
    /// <inheritdoc cref="IErrorObject.Message"/>
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IErrorObject.Namespace"/>
    /// </summary>
    public string? Namespace { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IErrorObject.Type"/>
    /// </summary>
    public required ErrorType Type { get; init; }
}