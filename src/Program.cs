using System.Reflection;
using System.Text.Json;

namespace Slap
{
    public static class Program
    {
        /// <summary>
        /// When the app started.
        /// </summary>
        public static DateTimeOffset AppStarted { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Analyzed app options.
        /// </summary>
        public static CmdArgs AppOptions { get; set; } = null!;

        /// <summary>
        /// Queue of items to scan.
        /// </summary>
        public static List<QueueEntry> QueueEntries { get; set; } = new();

        /// <summary>
        /// Init all the things..
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        private static async Task Main(string[] args)
        {
            // Analyze the command-line arguments.
            try
            {
                AppOptions = new CmdArgs(args);
            }
            catch (ConsoleObjectsException ex)
            {
                ConsoleEx.WriteException(ex);
                return;
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteException(ex);
                return;
            }

            // Do we need to show app options?
            if (AppOptions.ShowAppOptions)
            {
                ShowAppOptions();
                return;
            }

            // Make sure Playwright is setup correctly.
            if (!SetupPlaywright())
            {
                return;
            }

            // Init the scanner.
            QueueEntries.Add(
                new QueueEntry(
                    AppOptions.BaseUri,
                    AppOptions.Referer));

            ConsoleEx.WriteObjects(
                "Scan started ",
                ConsoleColor.Blue,
                AppStarted,
                Environment.NewLine);

            await Scanner.Init();

            var end = DateTimeOffset.Now;

            ConsoleEx.WriteObjects(
                "Scan ended ",
                ConsoleColor.Blue,
                end,
                Environment.NewLine);

            var duration = end - AppStarted;

            ConsoleEx.WriteObjects(
                "Scan took ",
                ConsoleColor.Blue,
                duration,
                Environment.NewLine);

            // Write JSON report.
            await WriteJsonReport(
                end,
                duration);

            // Write HTML report.
            await WriteHtmlReport(
                end,
                duration);
        }

        /// <summary>
        /// Compile a dictionary of config key and values.
        /// </summary>
        /// <returns>Dictionary of config key and values.</returns>
        public static Dictionary<string, string> GetConfigValues()
        {
            var dict = new Dictionary<string, string>();

            // Connection Timeout.
            dict.Add(
                "<span title=\"Timeout used for each request\">Timeout</span>",
                new TimeSpan(0, 0, 0, 0, (int)AppOptions.ConnectionTimeout).HumanReadable());

            // Use Referer.
            dict.Add(
                "<span title=\"Set the referer for each request\">Use Referer</span>",
                AppOptions.UseReferer ? "Yes" : "No");

            // Parent as Referer.
            dict.Add(
                "<span title=\"Set referer for each request to the parent the link was found on\">Parent as Referer</span>",
                AppOptions.UseParentAsReferer ? "Yes" : "No");

            // Initial Referer.
            dict.Add(
                "<span title=\"Referer set on first request and possibly all\">Initial Referer</span>",
                AppOptions.Referer ?? "<i>Not Set</i>");

            // Rendering Engine.
            dict.Add(
                "<span title=\"Which rendering engine was used for each request\">Rendering Engine</span>",
                AppOptions.RenderingEngine.ToString());

            // Headers to Verify.
            var list = AppOptions.HeadersToVerify
                .Select(n => $"<li>{n.Key}{(n.Value != null ? $": {n.Value}" : "")}</li>")
                .ToList();

            dict.Add(
                "<span title=\"Headers that were verified with each request\">Headers to Verify</span>",
                list.Count > 0
                    ? $"<ul>{string.Concat(list)}</li>"
                    : "<i>None</i>");

            // Request Headers.
            list = AppOptions.RequestHeaders?
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
                AppOptions.UserAgent ?? "<i>Not Set</i>");

            // Wait Until.
            dict.Add(
                "<span title=\"When to consider the request operation succeeded\">Wait Until</span>",
                AppOptions.WaitUntil?.ToString() ?? "<i>Not Set</i>");

            // Verify Html Title.
            dict.Add(
                "<span title=\"Warn if HTML title tag is missing or empty\">Verify HTML Title</span>",
                AppOptions.WarnHtmlTitle ? "Yes" : "No");

            // Verify Meta Keywords Tag.
            dict.Add(
                "<span title=\"Warn if HTML meta keywords tag is missing or empty\">Verify Meta Keywords Tag</span>",
                AppOptions.WarnHtmlMetaKeywords ? "Yes" : "No");

            // Verify Meta Description Tag.
            dict.Add(
                "<span title=\"Warn if HTML meta description tag is missing or empty\">Verify Meta Description Tag</span>",
                AppOptions.WarnHtmlMetaDescription ? "Yes" : "No");

            // Bypass CSP.
            dict.Add(
                "<span title=\"Bypass Content-Security-Policy\">Bypass CSP</span>",
                AppOptions.BypassContentSecurityPolicy ? "Yes" : "No");

            // HTTP Authentication Username.
            dict.Add(
                "<span title=\"HTTP authentication username\">HTTP Authentication Username</span>",
                AppOptions.HttpAuthUsername != null
                    ? $"<a class=\"toggle-text\" data-text=\"{AppOptions.HttpAuthUsername}\">Click to Reveal</a>"
                    : "<i>Not Set</i>");

            // HTTP Authentication Password.
            dict.Add(
                "<span title=\"HTTP authentication password\">HTTP Authentication Password</span>",
                AppOptions.HttpAuthPassword != null
                    ? $"<a class=\"toggle-text\" data-text=\"{AppOptions.HttpAuthPassword}\">Click to Reveal</a>"
                    : "<i>Not Set</i>");

            // Done.
            return dict;
        }

        /// <summary>
        /// Get the full path for the current report.
        /// </summary>
        /// <returns>Path.</returns>
        public static string GetReportPath()
        {
            var path = Path.Combine(
                AppOptions.ReportPath,
                "reports",
                AppOptions.BaseUri.Host,
                AppStarted.ToString("yyyy-MM-dd-HH-mm-ss"));

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        /// Get the assembly version.
        /// </summary>
        /// <returns>Version.</returns>
        private static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;

            if (version == null)
            {
                throw new Exception(
                    "Panic. Unable to get assembly version.");
            }

            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        /// <summary>
        /// Make sure Playwright is setup correctly by running its installer.
        /// If it's already installed, it will be omitted.
        /// </summary>
        private static bool SetupPlaywright()
        {
            try
            {
                Console.WriteLine("Setting up Playwright..");

                Microsoft.Playwright.Program.Main(
                    new string[]
                    {
                        "install"
                    });

                return true;
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteException(ex);
                return false;
            }
        }

        /// <summary>
        /// Show the app options.
        /// </summary>
        private static void ShowAppOptions()
        {
            ConsoleEx.WriteObjects(
                "Slap v",
                GetVersion(),
                Environment.NewLine,
                Environment.NewLine,
                "Usage:",
                Environment.NewLine,
                "  slap <url> [<options>]",
                Environment.NewLine,
                Environment.NewLine,
                "Options:",
                Environment.NewLine);

            // Timeout, in milliseconds, to use for each request.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -t",
                ConsoleColor.Green,
                " <milliseconds>   ",
                (byte) 0x00,
                "Timeout to use for each request. Pass 0 to disable timeout.",
                Environment.NewLine);

            // Set the referer for each request.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -r",
                ConsoleColor.Green,
                " <url>            ",
                (byte) 0x00,
                "Set the referer for each request. If used with the ",
                ConsoleColor.Blue,
                "-rp",
                (byte) 0x00,
                " param, this will only be used for the first request.",
                Environment.NewLine);

            // Enable to set referer for each request to the parent the link was found on.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -rp                 ",
                (byte) 0x00,
                "Enable to set referer for each request to the parent the link was found on.",
                Environment.NewLine);

            // Set the report path.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -p",
                ConsoleColor.Green,
                " <path>           ",
                (byte) 0x00,
                "Set the report path. Defaults to working directory.",
                Environment.NewLine);

            // Set Firefox as the rendering engine.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -ff                 ",
                (byte) 0x00,
                "Set Firefox as the rendering engine. Defaults to Chromium.",
                Environment.NewLine);

            // Set Webkit as the rendering engine.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -wk                 ",
                (byte)0x00,
                "Set Webkit as the rendering engine. Defaults to Chromium.",
                Environment.NewLine);

            // Specify the user agent to use for all requests.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -ua",
                ConsoleColor.Green,
                " <agent>         ",
                (byte)0x00,
                "Specify the user agent to use for all requests.",
                Environment.NewLine);

            // Verify that a header exists.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -vh",
                ConsoleColor.Green,
                " <header>        ",
                (byte)0x00,
                "Verify that a header exists.",
                Environment.NewLine);

            // Verify that a header and value exists.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -vh",
                ConsoleColor.Green,
                " <header:value>  ",
                (byte)0x00,
                "Verify that a header and value exists. Value can be regex.",
                Environment.NewLine);

            // Add request header and value.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -rh",
                ConsoleColor.Green,
                " <header:value>  ",
                (byte)0x00,
                "Add request header and value.",
                Environment.NewLine);

            // Add HTTP authentication username and password credentials.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -hac",
                ConsoleColor.Green,
                " <user:pwd>     ",
                (byte)0x00,
                "Add HTTP authentication username and password credentials.",
                Environment.NewLine);

            // Warn if HTML title tag is missing or empty.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -wht                ",
                (byte)0x00,
                "Warn if HTML title tag is missing or empty.",
                Environment.NewLine);

            // Warn if HTML meta keywords tag is missing or empty.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -whk                ",
                (byte)0x00,
                "Warn if HTML meta keywords tag is missing or empty.",
                Environment.NewLine);

            // Warn if HTML meta description tag is missing or empty.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -whd                ",
                (byte)0x00,
                "Warn if HTML meta description tag is missing or empty.",
                Environment.NewLine);

            // Bypass Content-Security-Policy.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -bcsp               ",
                (byte)0x00,
                "Bypass Content-Security-Policy.",
                Environment.NewLine);

            // When to consider the request operation succeeded.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -wu",
                ConsoleColor.Green,
                " <state>         ",
                (byte)0x00,
                "When to consider the request operation succeeded. Defaults to 'load'. Possible states are:",
                Environment.NewLine,
                "                       * ",
                ConsoleColor.Green,
                "domcontentloaded",
                (byte)0x00,
                " - When the DOMContentLoaded event is fired.",
                Environment.NewLine,
                "                       * ",
                ConsoleColor.Green,
                "load",
                (byte)0x00,
                " - When the load event is fired.",
                Environment.NewLine,
                "                       * ",
                ConsoleColor.Green,
                "networkidle",
                (byte)0x00,
                " - When there are no network connections for at least 500 ms.",
                Environment.NewLine,
                "                       * ",
                ConsoleColor.Green,
                "commit",
                (byte)0x00,
                " - When network response is received and the document started loading.",
                Environment.NewLine);
        }

        /// <summary>
        /// Write HTML and compile a report that represent the data.
        /// </summary>
        /// <param name="end">When scanning ended.</param>
        /// <param name="duration">How long the scanning took.</param>
        private static async Task WriteHtmlReport(
            DateTimeOffset end,
            TimeSpan duration)
        {
            // Header and Config.
            var html =
                "<!doctype html>" +
                "<html lang=\"en\">" +
                "  <head>" +
                "    <meta charset=\"utf-8\">" +
                $"    <title>Slap Report for {AppOptions.BaseUri}</title>" +
                "    <link rel=\"stylesheet\" href=\"report.css\">" +
                "  </head>" +
                "  <body>" +
                "    <h1>Slap Report</h1>" +
                "    <header>" +
                "      <div>" +
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

            // Scan.
            html +=
                "          </tbody>" +
                "        </table>" +
                "      </div>" +
                "      <div>" +
                "        <h2>Scan</h2>" +
                "        <table>" +
                "          <tbody>" +
                "            <tr>" +
                "              <td>URL</td>" +
                "              <td>" +
                $"                <a href=\"{AppOptions.BaseUri}\">{AppOptions.BaseUri}</a>" +
                "              </td>" +
                "            </tr>" +
                "            <tr>" +
                "              <td>Started</td>" +
                "              <td>" +
                $"               <span title=\"{AppStarted}\">{AppStarted:yyyy-MM-dd HH:mm:ss}</span>" +
                "              </td>" +
                "            </tr>" +
                "            <tr>" +
                "              <td>Ended</td>" +
                "              <td>" +
                $"               <span title=\"{end}\">{end:yyyy-MM-dd HH:mm:ss}</span>" +
                "              </td>" +
                "            </tr>" +
                "            <tr>" +
                "              <td>Took</td>" +
                "              <td>" +
                $"               <span title=\"{duration}\">{duration.HumanReadable()}</span>" +
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
                "            <th class=\"width-100\">&nbsp;</th>" +
                "            <th class=\"width-100\">&nbsp;</th>" +
                "            <th class=\"width-50\">&nbsp;</th>" +
                "            <th class=\"width-50\">&nbsp;</th>" +
                "          </tr>" +
                "        </thead>" +
                "        <tbody>";

            // Cycle queue entries.
            var isAlt = true;

            foreach (var entry in QueueEntries)
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
                    $"    <div>{entry.HtmlTitle}</div>" +
                    $"    <a href=\"{entry.Uri}\">{entry.Uri}</a>" +
                    "  </td>" +
                    "  <td>" +
                    $"    <span class=\"{httpStatusCssClass}\" title=\"{httpStatusTooltip}\">{httpStatusText}</span>" +
                    "  </td>" +
                    "  <td>" +
                    $"    {requestTime?.HumanReadable()}" +
                    "  </td>" +
                    "  <td class=\"right-content\">" +
                    $"    <span class=\"{errorCssClass}\" title=\"{errorText}\"></span>" +
                    "  </td>" +
                    "  <td class=\"right-content\">" +
                    $"    <a class=\"toggle-info-panel\" data-id=\"{entry.Id}\"></a>" +
                    "  </td>" +
                    "</tr>" +
                    $"<tr class=\"info collapsed\" id=\"{entry.Id}\">" +
                    "  <td colspan=\"5\">" +
                    "    * HTML" +
                    "      - TITLE" +
                    "      - META ENTRIES" +
                    "    * TELEMITRY" +
                    "    * HEADERS" +
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
                $"        Created with Slap v{GetVersion()}<br>" +
                "        <a href=\"https://github.com/nagilum/slap\">https://github.com/nagilum/slap</a>" +
                "      </p>" +
                "    </footer>" +
                "  </body>" +
                "  <script src=\"report.js\"></script>" +
                "</html>";

            // Write to disk.
            await WriteHtmlReport(
                Path.Combine(
                    GetReportPath(),
                    "report.html"),
                html);
        }

        /// <summary>
        /// Write the data to disk.
        /// </summary>
        /// <param name="path">Path to filename.</param>
        /// <param name="html">HTML to write.</param>
        private static async Task WriteHtmlReport(
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

        /// <summary>
        /// Write JSON files that represent the data in the report.
        /// </summary>
        /// <param name="end">When scanning ended.</param>
        /// <param name="duration">How long the scanning took.</param>
        private static async Task WriteJsonReport(
            DateTimeOffset end,
            TimeSpan duration)
        {
            // Write metadata.
            await WriteJsonReport(
                Path.Combine(
                    GetReportPath(),
                    "metadata.json"),
                new {
                    AppOptions.BaseUri,
                    config = new
                    {
                        AppOptions.ConnectionTimeout,
                        AppOptions.UseReferer,
                        AppOptions.UseParentAsReferer,
                        initialReferer = AppOptions.Referer,
                        renderingEngine = AppOptions.RenderingEngine.ToString(),
                        AppOptions.HeadersToVerify,
                        AppOptions.RequestHeaders,
                        AppOptions.UserAgent,
                        waitUntil = AppOptions.WaitUntil?.ToString(),
                        AppOptions.WarnHtmlTitle,
                        AppOptions.WarnHtmlMetaKeywords,
                        AppOptions.WarnHtmlMetaDescription,
                        AppOptions.BypassContentSecurityPolicy,
                        AppOptions.HttpAuthUsername,
                        AppOptions.HttpAuthPassword
                    },
                    scan = new
                    {
                        started = AppStarted,
                        ended = end,
                        took = duration
                    }
                });

            // Remove some of the properties before writing.
            foreach (var entry in QueueEntries)
            {
                entry.Content = null;
            }

            // Write queue.
            await WriteJsonReport(
                Path.Combine(
                    GetReportPath(),
                    "queue.json"),
                QueueEntries);
        }

        /// <summary>
        /// Write the data to disk.
        /// </summary>
        /// <param name="path">Path to filename.</param>
        /// <param name="data">Data to write.</param>
        private static async Task WriteJsonReport(
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