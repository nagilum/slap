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
        /// Links found on this page.
        /// </summary>
        public List<string> Links { get; set; } = new();

        /// <summary>
        /// Headers that have been verified.
        /// </summary>
        public Dictionary<string, string?>? HeadersVerified { get; set; }

        /// <summary>
        /// Header that did not validate.
        /// </summary>
        public Dictionary<string, string?>? HeadersNotVerified { get; set; }

        /// <summary>
        /// Returns whether headers have been verified.
        /// </summary>
        public bool HeadersAreVerified
        {
            get
            {
                return HeadersNotVerified == null ||
                       HeadersNotVerified.Count == 0;
            }
        }

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