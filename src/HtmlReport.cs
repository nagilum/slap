namespace Slap
{
    public static class HtmlReport
    {
        /// <summary>
        /// Copy the rest of the HTML report files to the correct folder.
        /// </summary>
        private static async Task CopyReportFiles()
        {
            var files = new[]
            {
                "report.css",
                "report.js",
                "error.png",
                "settings.png",
                "warning.png"
            };

            const string folder = "template";

            foreach (var file in files)
            {
                try
                {
                    var sourcePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        folder,
                        file);

                    var destPath = Path.Combine(
                        Program.GetReportPath(),
                        file);

                    using var sourceStream = File.Open(sourcePath, FileMode.Open);
                    using var destStream = File.Create(destPath);

                    await sourceStream.CopyToAsync(destStream);
                }
                catch (FileNotFoundException)
                {
                    ConsoleEx.WriteException(
                        new ConsoleObjectsException(
                            "File not found: ",
                            ConsoleColor.Blue,
                            $"{folder}/{file}"));
                }
                catch (Exception ex)
                {
                    ConsoleEx.WriteException(ex);
                }
            }
        }

        /// <summary>
        /// Compile a dictionary of config key and values.
        /// </summary>
        /// <returns>Dictionary of config key and values.</returns>
        private static Dictionary<string, string> GetConfigValues()
        {
            var dict = new Dictionary<string, string>();

            // Connection Timeout.
            dict.Add(
                "<span title=\"Timeout used for each request\">Timeout</span>",
                new TimeSpan(0, 0, 0, 0, (int)Program.AppOptions.ConnectionTimeout).HumanReadable());

            // Use Referer.
            dict.Add(
                "<span title=\"Set the referer for each request\">Use Referer</span>",
                Program.AppOptions.UseReferer ? "Yes" : "No");

            // Parent as Referer.
            dict.Add(
                "<span title=\"Set referer for each request to the parent the link was found on\">Parent as Referer</span>",
                Program.AppOptions.UseParentAsReferer ? "Yes" : "No");

            // Initial Referer.
            dict.Add(
                "<span title=\"Referer set on first request and possibly all\">Initial Referer</span>",
                Program.AppOptions.Referer ?? "<i>Not Set</i>");

            // Rendering Engine.
            dict.Add(
                "<span title=\"Which rendering engine was used for each request\">Rendering Engine</span>",
                Program.AppOptions.RenderingEngine.ToString());

            // Headers to Verify.
            var list = Program.AppOptions.HeadersToVerify
                .Select(n => $"<li>{n.Key}{(n.Value != null ? $": {n.Value}" : "")}</li>")
                .ToList();

            dict.Add(
                "<span title=\"Headers that were verified with each request\">Headers to Verify</span>",
                list.Count > 0
                    ? $"<ul>{string.Concat(list)}</li>"
                    : "<i>None</i>");

            // Request Headers.
            list = Program.AppOptions.RequestHeaders?
                .Select(n => $"<li>{n.Key}{(n.Value != null ? $": {n.Value}" : "")}</li>")
                .ToList() ?? new();

            dict.Add(
                "<span title=\"Headers added to each request\">Request Headers</span>",
                list.Count > 0
                    ? $"<ul>{string.Concat(list)}</li>"
                    : "<i>None</i>");

            // User Agent.
            dict.Add(
                "<span title=\"User agent sent with each request\">User Agent</span>",
                Program.AppOptions.UserAgent ?? "<i>Not Set</i>");

            // Wait Until.
            dict.Add(
                "<span title=\"When to consider the request operation succeeded\">Wait Until</span>",
                Program.AppOptions.WaitUntil?.ToString() ?? "<i>Not Set</i>");

            // Verify Html Title.
            dict.Add(
                "<span title=\"Warn if HTML title tag is missing or empty\">Verify HTML Title</span>",
                Program.AppOptions.WarnHtmlTitle ? "Yes" : "No");

            // Verify Meta Keywords Tag.
            dict.Add(
                "<span title=\"Warn if HTML meta keywords tag is missing or empty\">Verify Meta Keywords Tag</span>",
                Program.AppOptions.WarnHtmlMetaKeywords ? "Yes" : "No");

            // Verify Meta Description Tag.
            dict.Add(
                "<span title=\"Warn if HTML meta description tag is missing or empty\">Verify Meta Description Tag</span>",
                Program.AppOptions.WarnHtmlMetaDescription ? "Yes" : "No");

            // Bypass CSP.
            dict.Add(
                "<span title=\"Bypass Content-Security-Policy\">Bypass CSP</span>",
                Program.AppOptions.BypassContentSecurityPolicy ? "Yes" : "No");

            // HTTP Authentication Username.
            dict.Add(
                "<span title=\"HTTP authentication username\">HTTP Authentication Username</span>",
                Program.AppOptions.HttpAuthUsername != null
                    ? $"<a class=\"toggle-text\" data-text=\"{Program.AppOptions.HttpAuthUsername}\">Click to Reveal</a>"
                    : "<i>Not Set</i>");

            // HTTP Authentication Password.
            dict.Add(
                "<span title=\"HTTP authentication password\">HTTP Authentication Password</span>",
                Program.AppOptions.HttpAuthPassword != null
                    ? $"<a class=\"toggle-text\" data-text=\"{Program.AppOptions.HttpAuthPassword}\">Click to Reveal</a>"
                    : "<i>Not Set</i>");

            // Done.
            return dict;
        }

        /// <summary>
        /// Transfor the number of bytes to a more human readable format.
        /// </summary>
        /// <param name="bytes">Total bytes.</param>
        /// <returns>Human readable format.</returns>
        private static string HumanReadableSize(int? bytes)
        {
            if (!bytes.HasValue)
            {
                return string.Empty;
            }

            string? str;

            if (bytes > Math.Pow(1024, 2))
            {
                str = (int)(bytes / Math.Pow(1024, 2)) + " mB";
            }
            else if (bytes > 1024)
            {
                str = (int)(bytes / 1024) + " kB";
            }
            else
            {
                str = bytes + " bytes";
            }

            return str;
        }

        /// <summary>
        /// Write HTML and compile a report that represent the data.
        /// </summary>
        public static async Task Write()
        {
            // Header and Config.
            var html =
                "<!doctype html>" +
                "<html lang=\"en\">" +
                "  <head>" +
                "    <meta charset=\"utf-8\">" +
                $"    <title>Slap Report for {Program.AppOptions.BaseUri}</title>" +
                "    <link rel=\"stylesheet\" href=\"report.css\">" +
                "  </head>" +
                "  <body>" +
                "    <h1>Slap Report</h1>" +
                "    <header>" +
                "      <div class=\"right-box\">" +
                "        <h2>Config</h2>" +
                "        <table>" +
                "          <tbody>";

            // Get config values.
            foreach (var item in GetConfigValues())
            {
                html +=
                    "<tr>" +
                    $"  <td>{item.Key}</td>" +
                    $"  <td>{item.Value}</td>" +
                    "</tr>";
            }

            // Scan and Stats.
            var totalRequests = Program.QueueEntries.Count;
            var completedRequests = Program.QueueEntries.Count(n => n.StatusCode.HasValue);
            var failedRequests = Program.QueueEntries.Count(n => !n.StatusCode.HasValue);
            var requestsWithErrors = Program.QueueEntries.Count(n => n.Errors?.Count > 0);
            var requestsWithWarnings = Program.QueueEntries.Count(n => n.Warnings?.Count > 0);

            var completedRequestsPercent = 100D / totalRequests * completedRequests;
            var failedRequestsPercent = 100D / totalRequests * failedRequests;
            var requestsWithErrorsPercent = 100D / totalRequests * requestsWithErrors;
            var requestsWithWarningsPercent = 100D / totalRequests * requestsWithWarnings;

            html +=
                "          </tbody>" +
                "        </table>" +
                "      </div>" +
                "      <div>" +
                "        <h2>Scan</h2>" +
                "        <table>" +
                "          <tbody>" +
                "            <tr>" +
                "              <td>Start URL</td>" +
                "              <td>" +
                $"                <a href=\"{Program.AppOptions.BaseUri}\">{Program.AppOptions.BaseUri}</a>" +
                "              </td>" +
                "            </tr>" +
                "            <tr>" +
                "              <td>Started</td>" +
                "              <td>" +
                $"               <span title=\"{Scanner.ScanStarted}\">{Scanner.ScanStarted:yyyy-MM-dd HH:mm:ss}</span>" +
                "              </td>" +
                "            </tr>" +
                "            <tr>" +
                "              <td>Ended</td>" +
                "              <td>" +
                $"               <span title=\"{Scanner.ScanEnded}\">{Scanner.ScanEnded:yyyy-MM-dd HH:mm:ss}</span>" +
                "              </td>" +
                "            </tr>" +
                "            <tr>" +
                "              <td>Took</td>" +
                "              <td>" +
                $"               <span title=\"{Scanner.ScanTook}\">{Scanner.ScanTook?.HumanReadable(false)}</span>" +
                "              </td>" +
                "            </tr>" +
                "          </tbody>" +
                "        </table>" +
                "        <br><br>" +
                "        <h2>Stats</h2>" +
                "        <table>" +
                "          <tbody>" +
                "            <tr>" +
                "              <td>" +
                "                <span title=\"Total number of requests performed\">Total Requests</span>" +
                "              </td>" +
                "              <td>" +
                $"                {totalRequests}" +
                "              </td>" +
                "            </tr>" +
                "            <tr>" +
                "              <td>" +
                "                <span title=\"Total number of requests that completed\">Completed Requests</span>" +
                "              </td>" +
                "              <td>" +
                $"                {completedRequests} <small>({completedRequestsPercent:00.0}%)</small>" +
                "              </td>" +
                "            </tr>" +
                "            <tr>" +
                "              <td>" +
                "                <span title=\"Total number of requests that failed for whatever reason\">Failed Requests</span>" +
                "              </td>" +
                "              <td>" +
                $"                {failedRequests} <small>({failedRequestsPercent:00.0}%)</small>" +
                "              </td>" +
                "            </tr>" +
                "            <tr>" +
                "              <td>" +
                "                <span title=\"Total number of requests with errors\">Requests with Errors</span>" +
                "              </td>" +
                "              <td>" +
                $"                {requestsWithErrors} <small>({requestsWithErrorsPercent:00.0}%)</small>" +
                "              </td>" +
                "            </tr>" +
                "            <tr>" +
                "              <td>" +
                "                <span title=\"Total number of requests with warnings\">Requests with Warnings</span>" +
                "              </td>" +
                "              <td>" +
                $"                {requestsWithWarnings} <small>({requestsWithWarningsPercent:00.0}%)</small>" +
                "              </td>" +
                "            </tr>" +
                "          </tbody>" +
                "        </table>" +
                "      </div>" +
                "    </header>" +
                "    <article>" +
                "      <table>" +
                "        <thead>" +
                "          <tr>" +
                "            <th>&nbsp;</th>" +
                "            <th class=\"width-50\">&nbsp;</th>" +
                "            <th class=\"width-50\">&nbsp;</th>" +
                "            <th class=\"width-50\">&nbsp;</th>" +
                "            <th class=\"width-50\">&nbsp;</th>" +
                "            <th class=\"width-50\">&nbsp;</th>" +
                "          </tr>" +
                "        </thead>" +
                "        <tbody>";

            // Cycle queue entries.
            var isAlt = true;

            foreach (var entry in Program.QueueEntries)
            {
                isAlt = !isAlt;

                var httpStatusCssClass = "http-status-red";
                var httpStatusTooltip = entry.StatusDescription;
                var httpStatusText = entry.StatusCode?.ToString() ?? "---";

                var errorCssClass = entry.Warnings?.Count > 0 ? "warning" : string.Empty;
                var errorText = $"{entry.Errors?.Count ?? 0} error(s) and {entry.Warnings?.Count ?? 0} warning(s)";

                if (entry.Errors?.Count > 0)
                {
                    errorCssClass = "error";
                }

                TimeSpan? requestTime = null;

                if (entry.Telemetry != null)
                {
                    var ms = entry.Telemetry.ResponseEnd - entry.Telemetry.ResponseStart;
                    requestTime = new TimeSpan(0, 0, 0, 0, (int)ms);
                }

                if (entry.StatusCode.HasValue)
                {
                    if (entry.StatusCode.Value.ToString().StartsWith("2"))
                    {
                        httpStatusCssClass = "http-status-green";
                    }
                    else if (entry.StatusCode.Value.ToString().StartsWith("3"))
                    {
                        httpStatusCssClass = "http-status-yellow";
                    }
                }

                html +=
                    $"<tr class=\"overview {(isAlt ? "alt" : "")}\">" +
                    "  <td>" +
                    $"    <div>{entry.HtmlTitle ?? "<i>Untitled</i>"}</div>" +
                    $"    <a href=\"{entry.Uri}\">{entry.Uri}</a>" +
                    "  </td>" +
                    "  <td>" +
                    $"    <span class=\"{httpStatusCssClass}\" title=\"{httpStatusTooltip}\">{httpStatusText}</span>" +
                    "  </td>" +
                    "  <td>" +
                    $"    <span title=\"{requestTime}\">{requestTime?.HumanReadable()}</span>" +
                    "  </td>" +
                    "  <td>" +
                    $"    <span title=\"{entry.ContentLength} bytes\">{HumanReadableSize(entry.ContentLength)}</span>" +
                    "  </td>" +
                    "  <td class=\"right-content\">" +
                    $"    <span class=\"{errorCssClass}\" title=\"{errorText}\"></span>" +
                    "  </td>" +
                    "  <td class=\"right-content\">" +
                    $"    <a class=\"toggle-info-panel\" data-id=\"{entry.Id}\"></a>" +
                    "  </td>" +
                    "</tr>" +
                    $"<tr class=\"info collapsed\" id=\"{entry.Id}\">" +
                    "  <td colspan=\"6\">" +
                    "    * HTML" +
                    "      - META ENTRIES" +
                    "    * TELEMITRY" +
                    "    * HEADERS" +
                    "    * HEADERS VERIFIED" +
                    "    * HEADERS NOT VERIFIED" +
                    "    * SCREENSHOT" +
                    "    * LINKED FROM" +
                    "    * WARNINGS" +
                    "    * ERRORS" +
                    "  </td>" +
                    "</tr>";
            }

            // Footer.
            html +=
                "        </tbody>" +
                "      </table>" +
                "    </article>" +
                "    <footer>" +
                "      <p>" +
                $"        Created with Slap v{Program.GetVersion()}<br>" +
                "        <a href=\"https://github.com/nagilum/slap\">https://github.com/nagilum/slap</a>" +
                "      </p>" +
                "    </footer>" +
                "  </body>" +
                "  <script src=\"report.js\"></script>" +
                "</html>";

            // Write to disk.
            await Write(
                Path.Combine(
                    Program.GetReportPath(),
                    "report.html"),
                html);

            // Copy the rest of the HTML report files to the correct folder.
            await CopyReportFiles();
        }

        /// <summary>
        /// Write the data to disk.
        /// </summary>
        /// <param name="path">Path to filename.</param>
        /// <param name="html">HTML to write.</param>
        private static async Task Write(
            string path,
            string html)
        {
            try
            {
                ConsoleEx.WriteObjects(
                    "Writing HTML report to ",
                    ConsoleColor.Blue,
                    path,
                    Environment.NewLine);

                await File.WriteAllTextAsync(
                    path,
                    html);
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteException(ex);
            }
        }
    }
}