namespace Slap.Extensions;

internal static class TimeSpanExtender
{
    /// <summary>
    /// Return a more human readable version of the TimeSpan value.
    /// </summary>
    /// <param name="ts">TimeSpan.</param>
    /// <returns>Human readable TimeSpan value.</returns>
    public static string ToHumanReadable(this TimeSpan ts)
    {
        var parts = new List<string>();

        if (ts.Days > 0)
        {
            parts.Add($"{ts.Days}d");
        }

        if (ts.Hours > 0)
        {
            parts.Add($"{ts.Hours}h");
        }

        if (ts.Minutes > 0)
        {
            parts.Add($"{ts.Minutes}m");
        }

        if (ts.Seconds > 0)
        {
            parts.Add($"{ts.Seconds}s");
        }

        return parts.Count switch
        {
            0 => string.Empty,
            1 => parts[0],
            _ => string.Join(" ", parts)
        };
    }
}