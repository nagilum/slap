namespace Slap.Models;

public interface IError
{
    /// <summary>
    /// Error code.
    /// </summary>
    string Code { get; init; }
    
    /// <summary>
    /// When the error happened.
    /// </summary>
    DateTime Happened { get; }
    
    /// <summary>
    /// Error message.
    /// </summary>
    string Message { get; init; }
}