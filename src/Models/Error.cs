namespace Slap.Models;

public class Error : IError
{
    /// <summary>
    /// <inheritdoc cref="IError.Happened"/>
    /// </summary>
    public DateTime Happened { get; } = DateTime.Now;

    /// <summary>
    /// <inheritdoc cref="IError.Message"/>
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// <inheritdoc cref="IError.Type"/>
    /// </summary>
    public required string Type { get; init; }
}