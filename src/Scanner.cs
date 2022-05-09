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
        /// Options for each new page.
        /// </summary>
        private static BrowserNewPageOptions NewPageOptions { get; set; } = null!;

        /// <summary>
        /// Options for each request.
        /// </summary>
        private static PageGotoOptions GotoOptions { get; set; } = null!;

        /// <summary>
        /// Extract the HTML title and meta tags.
        /// </summary>
        /// <param name="page">Playwright page.</param>
        /// <param name="entry">Queue entry.</param>
        private static async Task ExtractHtmlData(
            IPage page,
            QueueEntry entry)
        {
            int count;

            try
            {
                // HTML title.
                var titles = page.Locator("//title");

                count = await titles.CountAsync();

                if (count > 0)
                {
                    var contents = await titles.Nth(0).AllTextContentsAsync();

                    if (contents.Count > 0)
                    {
                        entry.HtmlTitle = contents[0];
                    }
                }

                // Verify title.
                if (Program.AppOptions.WarnHtmlTitle &&
                    string.IsNullOrWhiteSpace(entry.HtmlTitle))
                {
                    entry.Warnings ??= new();
                    entry.Warnings.Add(
                        "HTML title tag is missing or empty.");
                }

                // Meta tag entries.
                var metas = page.Locator("//meta");

                count = await metas.CountAsync();

                entry.HtmlMetaEntries = new();

                var htmlMetaFound = false;

                for (var i = 0; i < count; i++)
                {
                    var name = await metas.Nth(i).GetAttributeAsync("name");
                    var property = await metas.Nth(i).GetAttributeAsync("property");
                    var content = await metas.Nth(i).GetAttributeAsync("content");

                    var key = name ?? property;

                    // Check for keywords.
                    if (key == "keywords")
                    {
                        htmlMetaFound = true;

                        if (Program.AppOptions.WarnHtmlMetaKeywords &&
                            string.IsNullOrWhiteSpace(content))
                        {
                            entry.Warnings ??= new();
                            entry.Warnings.Add(
                                "HTML meta tag for keywords is missing or empty.");
                        }
                    }

                    // If it has content, keep it.
                    if (string.IsNullOrWhiteSpace(key) ||
                        string.IsNullOrWhiteSpace(content) ||
                        entry.HtmlMetaEntries.ContainsKey(key))
                    {
                        continue;
                    }

                    entry.HtmlMetaEntries.Add(
                        key,
                        content);
                }

                // Keywords is missing.
                if (Program.AppOptions.WarnHtmlMetaKeywords &&
                    !htmlMetaFound)
                {
                    entry.Warnings ??= new();
                    entry.Warnings.Add(
                        "HTML meta tag for keywords is missing or empty.");
                }
            }
            catch
            {
                //
            }
        }

        /// <summary>
        /// Extract links for further analysis.
        /// </summary>
        /// <param name="page">Playwright page.</param>
        /// <param name="entry">Queue entry.</param>
        private static async Task ExtractLinks(
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
                                var referer = Program.AppOptions.Referer != null &&
                                              !Program.AppOptions.UseParentAsReferer
                                    ? Program.AppOptions.Referer
                                    : entry.Uri.ToString();

                                Program.QueueEntries.Add(
                                    new QueueEntry(
                                        uri,
                                        referer));
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
                if (Program.AppOptions.UseReferer)
                {
                    GotoOptions.Referer = entry.Referer;
                }

                page = await PlaywrightBrowser.NewPageAsync(
                    NewPageOptions);

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

                // Extract the HTML title and meta tags.
                await ExtractHtmlData(
                    page,
                    entry);

                // Verify the headers set in app options.
                VerifyHeaders(
                    entry);

                // Extract links for further analysis.
                await ExtractLinks(
                    page,
                    entry);

                // Take a screenshot of the current page and save to disk.
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
        /// Setup the Playwright wrapper and browser.
        /// </summary>
        private static async Task SetupPlaywrightObjects()
        {
            NewPageOptions ??= new()
            {
                UserAgent = Program.AppOptions.UserAgent
            };

            GotoOptions ??= new()
            {
                Timeout = Program.AppOptions.ConnectionTimeout,
                WaitUntil = Program.AppOptions.WaitUntil
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
        /// Verify the headers set in app options.
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