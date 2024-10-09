namespace Slap.Models;

public class Error : IError
{
    /// <summary>
    /// <inheritdoc cref="IError.Code"/>
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// <inheritdoc cref="IError.Happened"/>
    /// </summary>
    public DateTime Happened { get; } = DateTime.Now;

    /// <summary>
    /// <inheritdoc cref="IError.Message"/>
    /// </summary>
    public required string Message { get; init; }
}