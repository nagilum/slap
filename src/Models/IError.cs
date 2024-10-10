namespace Slap.Models;

public interface IError
{
    /// <summary>
    /// When the error happened.
    /// </summary>
    DateTime Happened { get; }
    
    /// <summary>
    /// Error message.
    /// </summary>
    string Message { get; init; }
    
    /// <summary>
    /// Error type.
    /// </summary>
    string Type { get; init; }
}