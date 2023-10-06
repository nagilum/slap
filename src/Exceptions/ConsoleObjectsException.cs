using Slap.Core;

namespace Slap.Exceptions;

internal class ConsoleObjectsException : Exception
{
    /// <summary>
    /// Objects to write to console.
    /// </summary>
    public object[] Objects { get; set; }

    /// <summary>
    /// Init a new instance of ConsoleObjectsException.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="objects">Objects to write to console.</param>
    public ConsoleObjectsException(string message, params object[] objects)
        : base(message)
    {
        this.Objects = objects;
    }

    /// <summary>
    /// Init a new instance of ConsoleObjectsException.
    /// </summary>
    /// <param name="objects">Objects to write to console.</param>
    /// <returns>New instance.</returns>
    public static ConsoleObjectsException From(params object[] objects)
    {
        var message = string.Empty;

        foreach (var obj in objects)
        {
            if (obj is ConsoleColor or ConsoleColorEx)
            {
                // Do nothing.
            }
            else
            {
                message += obj.ToString();
            }
        }

        return new(message, objects);
    }
}