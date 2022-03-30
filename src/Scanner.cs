using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace Slap
{
    public static class Scanner
    {
        /// <summary>
        /// Playwright wrapper.
        /// </summary>
        private static IPlaywright PlaywrightInstance { get; set; } = null!;

        /// <summary>
        /// Playwright browser.
        /// </summary>
        private static IBrowser PlaywrightBrowser { get; set; } = null!;

        /// <summary>
        /// Options for each request.
        /// </summary>
        private static PageGotoOptions GotoOptions { get; set; } = null!;

        /// <summary>
        /// Init the scan.
        /// </summary>
        public static async Task Init()
        {
            ConsoleEx.WriteObjects(
                "Scan from ",
                ConsoleColor.Blue,
                Program.AppOptions.BaseUri,
                Environment.NewLine);

            var index = -1;

            while (true)
            {
                // Get next entry.
                index++;

                if (index == Program.QueueEntries.Count)
                {
                    break;
                }

                var entry = Program.QueueEntries[index];

                // Start.
                entry.Started = DateTimeOffset.Now;

                // Perform the playwright request, get telemetry, and take a screenshot.
                await PerformPlaywrightRequest(entry);

                // Update console with current request.
                WriteRequestToConsole(
                    entry,
                    index);

                // Finished.
                entry.Finished = DateTimeOffset.Now;
            }
        }

        /// <summary>
        /// Setup the Playwright wrapper and browser.
        /// </summary>
        private static async Task SetupPlaywrightObjects()
        {
            GotoOptions ??= new()
            {
                Timeout = Program.AppOptions.ConnectionTimeout
            };

            PlaywrightInstance ??= await Playwright.CreateAsync();

            if (PlaywrightBrowser != null)
            {
                return;
            }

            PlaywrightBrowser = Program.AppOptions.RenderingEngine switch
            {
                // Firefox.
                CmdArgs.RenderingEngineType.Firefox => await PlaywrightInstance.Firefox.LaunchAsync(),

                // Webkit.
                CmdArgs.RenderingEngineType.Webkit => await PlaywrightInstance.Webkit.LaunchAsync(),

                // Chromium.
                _ => await PlaywrightInstance.Chromium.LaunchAsync(),
            };
        }

        /// <summary>
        /// Perform the playwright request, get telemetry, and take a screenshot.
        /// </summary>
        /// <param name="entry">Queue entry.</param>
        private static async Task PerformPlaywrightRequest(
            QueueEntry entry)
        {
            // Setup the Playwright wrapper and browser.
            await SetupPlaywrightObjects();

            // Navigate to the page.
            IPage page;
            IResponse? res;

            try
            {
                GotoOptions.Referer = entry.Referer;

                page = await PlaywrightBrowser.NewPageAsync();
                res = await page.GotoAsync(
                    entry.Uri.ToString(),
                    GotoOptions);

                if (res == null)
                {
                    throw new Exception("Unable to get a response to the request.");
                }

                // Save status.
                entry.StatusCode = res.Status;
                entry.StatusDescription = res.StatusText;

                // Save headers.
                entry.Headers = await res.AllHeadersAsync();

                // Save body.
                entry.Content = await res.BodyAsync();

                // Save telemetry.
                entry.Telemetry = res.Request.Timing;

                // Verify the headers.
                VerifyHeaders(
                    entry);

                // Save links.
                await SaveLinks(
                    page,
                    entry);

                // Save screenshot.
                await SaveScreenshot(
                    page,
                    entry);
            }
            catch (TimeoutException)
            {
                entry.Errors ??= new();
                entry.Errors.Add($"Timeout after {Program.AppOptions.ConnectionTimeout}ms.");
            }
            catch (Exception ex)
            {
                entry.Errors ??= new();
                entry.Errors.Add($"{ex.Message}");
            }
        }

        /// <summary>
        /// Verify the headers.
        /// </summary>
        /// <param name="entry">Queue entry.</param>
        private static void VerifyHeaders(
            QueueEntry entry)
        {
            if (Program.AppOptions.HeadersToVerify.Count == 0 ||
                entry.Headers == null)
            {
                return;
            }

            entry.HeadersVerified = new();
            entry.HeadersNotVerified = new();

            foreach (var header in Program.AppOptions.HeadersToVerify)
            {
                var key = header.Key.ToLower();
                var verified = false;

                // Only verify the existence of the header.
                if (header.Value == null)
                {
                    if (entry.Headers.Any(n => n.Key.ToLower().Equals(key)))
                    {
                        verified = true;
                    }
                }

                // Verify both existence and value of header.
                else
                {
                    if (entry.Headers.Any(n => n.Key.ToLower().Equals(key)))
                    {
                        var temp = entry.Headers
                            .First(n => n.Key.ToLower().Equals(key));

                        var regex = new Regex(header.Value);
                        var matches = regex.Matches(temp.Value);

                        verified = matches.Count > 0;
                    }
                }

                // Update acordingly.
                if (verified)
                {
                    entry.HeadersVerified.Add(header.Key, header.Value);
                }
                else
                {
                    entry.HeadersNotVerified.Add(header.Key, header.Value);
                }
            }
        }

        /// <summary>
        /// Save links for further analysis.
        /// </summary>
        /// <param name="page">Playwright page.</param>
        /// <param name="entry">Queue entry.</param>
        private static async Task SaveLinks(
            IPage page,
            QueueEntry entry)
        {
            try
            {
                var hrefs = page.Locator("//a[@href]");
                var count = await hrefs.CountAsync();

                for (var i = 0; i < count; i++)
                {
                    var href = await hrefs.Nth(i).GetAttributeAsync("href");

                    if (href == null)
                    {
                        continue;
                    }

                    entry.Links.Add(href);

                    try
                    {
                        if (Program.AppOptions.BaseUri != null)
                        {
                            var uri = new Uri(entry.Uri, href);

                            if (Program.AppOptions.BaseUri.IsBaseOf(uri) &&
                                !Program.QueueEntries.Any(n => n.Uri == uri))
                            {
                                Program.QueueEntries.Add(
                                    new QueueEntry(
                                        uri,
                                        entry.Uri));
                            }
                        }
                    }
                    catch
                    {
                        //
                    }
                }
            }
            catch
            {
                //
            }
        }

        /// <summary>
        /// Take a screenshot of the current page and save to disk.
        /// </summary>
        /// <param name="page">Playwright page.</param>
        /// <param name="entry">Queue entry.</param>
        private static async Task SaveScreenshot(
            IPage page,
            QueueEntry entry)
        {
            var path = Path.Combine(
                Program.GetReportPath(),
                "assets",
                "img",
                "screenshots");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            entry.ScreenshotFullPath = Path.Combine(
                path,
                $"screenshot-{entry.Id}.png");

            await page.ScreenshotAsync(
                new()
                {
                    Path = entry.ScreenshotFullPath
                });
        }

        /// <summary>
        /// Write the request to console.
        /// </summary>
        /// <param name="entry">Queue entry.</param>
        /// <param name="index">Index in queue.</param>
        private static void WriteRequestToConsole(
            QueueEntry entry,
            int index)
        {
            var statusColor = ConsoleColor.Red;
            var status = "---";

            if (entry.StatusCode.HasValue)
            {
                status = entry.StatusCode.Value.ToString();

                if (status.StartsWith("2"))
                {
                    statusColor = ConsoleColor.Green;
                }
                else if (status.StartsWith("3"))
                {
                    statusColor = ConsoleColor.Yellow;
                }
            }

            ConsoleEx.WriteObjects(
                "[",
                ConsoleColor.Blue,
                index + 1,
                (byte) 0x00,
                "/",
                ConsoleColor.Blue,
                Program.QueueEntries.Count,
                (byte) 0x00,
                "] [",
                statusColor,
                status,
                (byte) 0x00,
                "] ",
                ConsoleColor.Blue,
                entry.Uri,
                Environment.NewLine);
        }
    }
}