using Microsoft.Playwright;

namespace Slap
{
    public class QueueEntry
    {
        /// <summary>
        /// Unique entry ID.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// When the entry was started.
        /// </summary>
        public DateTimeOffset? Started { get; set; }

        /// <summary>
        /// When the entry finished.
        /// </summary>
        public DateTimeOffset? Finished { get; set; }

        /// <summary>
        /// URL to scan.
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// Response status code.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Response status text.
        /// </summary>
        public string? StatusDescription { get; set; }

        /// <summary>
        /// Response headers.
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Document body.
        /// </summary>
        public byte[]? Content { get; set; }

        /// <summary>
        /// Request telemetry.
        /// </summary>
        public RequestTimingResult? Telemetry { get; set; }

        /// <summary>
        /// Full path to the screenshot file.
        /// </summary>
        public string? ScreenshotFullPath { get; set; }

        /// <summary>
        /// Create a new instance of a queue entry.
        /// </summary>
        /// <param name="uri">URL to scan.</param>
        public QueueEntry(Uri uri)
        {
            this.Uri = uri;
        }
    }
}