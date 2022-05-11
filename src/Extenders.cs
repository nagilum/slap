namespace Slap
{
    public static class Extenders
    {
        /// <summary>
        /// Write the duration in a more human readable way.
        /// </summary>
        public static string HumanReadable(this TimeSpan ts)
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
            if (ts.Milliseconds > 0)
            {
                hr += $"{ts.Milliseconds}ms";
            }

            // Done.
            return hr.Trim();
        }
    }
}