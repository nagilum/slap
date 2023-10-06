using Slap.Core;
using Slap.Exceptions;

namespace Slap.Tools;

internal static class ConsoleEx
{
    /// <summary>
    /// Lock object.
    /// </summary>
    private static readonly object ConsoleLock = new();

    /// <summary>
    /// Write a custom ConsoleObjectsException objects to console.
    /// </summary>
    /// <param name="ex">ConsoleObjectsException.</param>
    public static void WriteException(ConsoleObjectsException ex)
    {
        var list = new List<object>
        {
            ConsoleColor.Red,
            "Error",
            ConsoleColorEx.ResetColor,
            ": "
        };

        list.AddRange(ex.Objects);

        if (!list.Last().Equals(Environment.NewLine))
        {
            list.Add(Environment.NewLine);
        }

        list.Add(ConsoleColorEx.ResetColor);

        Write(list.ToArray());
    }

    /// <summary>
    /// Write a generic error to console.
    /// </summary>
    /// <param name="ex">Exception.</param>
    public static void WriteException(Exception ex)
    {
        var list = new List<object>
        {
            ConsoleColor.Red,
            "Error",
            ConsoleColorEx.ResetColor,
            ": "
        };

        while (true)
        {
            list.Add(ex.Message);
            list.Add(Environment.NewLine);

            if (ex.InnerException == null)
            {
                break;
            }

            ex = ex.InnerException;
        }

        list.Add(ConsoleColorEx.ResetColor);

        Write(list.ToArray());
    }

    /// <summary>
    /// Write objects to console.
    /// </summary>
    /// <param name="objects">Objects to write.</param>
    public static void Write(params object[] objects)
    {
        WriteAt(null, null, objects);
    }

    /// <summary>
    /// Write objects to console at a specific location.
    /// </summary>
    /// <param name="top">Top location.</param>
    /// <param name="left">Left location.</param>
    /// <param name="objects">Objects to write.</param>
    public static void WriteAt(int? top, int? left, params object[] objects)
    {
        lock (ConsoleLock)
        {
            if (top > -1)
            {
                Console.CursorTop = top.Value;
            }

            if (left > -1)
            {
                Console.CursorLeft = left.Value;
            }

            var nextIsBg = false;

            foreach (var obj in objects)
            {
                // Check for new color.
                if (obj is ConsoleColor cc)
                {
                    if (nextIsBg)
                    {
                        Console.BackgroundColor = cc;
                        nextIsBg = false;
                    }
                    else
                    {
                        Console.ForegroundColor = cc;
                    }
                }

                // Check for function.
                else if (obj is ConsoleColorEx cce)
                {
                    switch (cce)
                    {
                        // Indicate that the next specified color will be a background color.
                        case ConsoleColorEx.NextColorIsBackground:
                            nextIsBg = true;
                            break;

                        // Reset color.
                        case ConsoleColorEx.ResetColor:
                            Console.ResetColor();
                            break;
                    }
                }

                // Treat rest as text.
                else
                {
                    Console.Write(obj);
                }
            }

            // Always reset the color after manipulating the console.
            Console.ResetColor();
        }
    }
}