using System.Text.Json;

namespace Slap
{
    public static class JsonReport
    {
        /// <summary>
        /// Write JSON files that represent the data in the report.
        /// </summary>
        public static async Task Write()
        {
            // Write metadata.
            await Write(
                Path.Combine(
                    Program.GetReportPath(),
                    "metadata.json"),
                new
                {
                    Program.AppOptions.BaseUri,
                    config = new
                    {
                        Program.AppOptions.ConnectionTimeout,
                        Program.AppOptions.UseReferer,
                        Program.AppOptions.UseParentAsReferer,
                        initialReferer = Program.AppOptions.Referer,
                        renderingEngine = Program.AppOptions.RenderingEngine.ToString(),
                        Program.AppOptions.HeadersToVerify,
                        Program.AppOptions.RequestHeaders,
                        Program.AppOptions.UserAgent,
                        waitUntil = Program.AppOptions.WaitUntil?.ToString(),
                        Program.AppOptions.WarnHtmlTitle,
                        Program.AppOptions.WarnHtmlMetaKeywords,
                        Program.AppOptions.WarnHtmlMetaDescription,
                        Program.AppOptions.BypassContentSecurityPolicy,
                        Program.AppOptions.HttpAuthUsername,
                        Program.AppOptions.HttpAuthPassword
                    },
                    scan = new
                    {
                        started = Scanner.ScanStarted,
                        ended = Scanner.ScanEnded,
                        took = Scanner.ScanTook
                    }
                });

            // Remove some of the properties before writing.
            foreach (var entry in Program.QueueEntries)
            {
                entry.Content = null;
            }

            // Write queue.
            await Write(
                Path.Combine(
                    Program.GetReportPath(),
                    "queue.json"),
                Program.QueueEntries);
        }

        /// <summary>
        /// Write the data to disk.
        /// </summary>
        /// <param name="path">Path to filename.</param>
        /// <param name="data">Data to write.</param>
        private static async Task Write(
            string path,
            object data)
        {
            try
            {
                ConsoleEx.WriteObjects(
                    "Writing JSON report to ",
                    ConsoleColor.Blue,
                    path,
                    Environment.NewLine);

                using var fileStream = File.Create(path);

                await JsonSerializer.SerializeAsync(
                    fileStream,
                    data,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    });

                await fileStream.DisposeAsync();
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteException(ex);
            }
        }
    }
}