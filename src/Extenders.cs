namespace Slap
{
    public static class Extenders
    {
        /// <summary>
        /// Write the duration in a more human readable way.
        /// </summary>
        /// <param name="skipMilliseconds">Include milliseconds.</param>
        /// <returns>Human readable string.</returns>
        public static string HumanReadable(
            this TimeSpan ts,
            bool includeMilliseconds = true)
        {
            var hr = string.Empty;

            // Days.
            if (ts.Days > 0)
            {
                hr += $"{ts.Days}d ";
            }

            // Hours.
            if (ts.Hours > 0)
            {
                hr += $"{ts.Hours}h ";
            }

            // Minutes.
            if (ts.Minutes > 0)
            {
                hr += $"{ts.Minutes}m ";
            }

            // Seconds.
            if (ts.Seconds > 0)
            {
                hr += $"{ts.Seconds}s ";
            }

            // Milliseconds.
            if (includeMilliseconds &&
                ts.Milliseconds > 0)
            {
                hr += $"{ts.Milliseconds}ms";
            }

            // Done.
            return hr.Trim();
        }
    }
}