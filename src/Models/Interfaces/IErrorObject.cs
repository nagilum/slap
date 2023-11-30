using Slap.Core;

namespace Slap.Models.Interfaces;

public interface IErrorObject
{
    /// <summary>
    /// Error data.
    /// </summary>
    Dictionary<string, object>? Data { get; }
    
    /// <summary>
    /// Error message.
    /// </summary>
    string Message { get; }
    
    /// <summary>
    /// Error namespace, if unhandled.
    /// </summary>
    string? Namespace { get; }
    
    /// <summary>
    /// Type of error.
    /// </summary>
    ErrorType Type { get; }
}