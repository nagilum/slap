using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Slap.Models;
using Slap.Tools;

namespace Slap.Core;

internal class Scanner
{
    #region Properties and constructor

    /// <summary>
    /// Playwright browser.
    /// </summary>
    private IBrowser? Browser { get; set; }

    /// <summary>
    /// When the scanner finished.
    /// </summary>
    private DateTimeOffset? Finished { get; set; }

    /// <summary>
    /// Playwright page.
    /// </summary>
    private IPage? Page { get; set; }

    /// <summary>
    /// When the scanner started.
    /// </summary>
    private DateTimeOffset Started { get; } = DateTimeOffset.Now;

    /// <summary>
    /// HTTP client.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Options.
    /// </summary>
    private readonly Options _options;

    /// <summary>
    /// Queue.
    /// </summary>
    private readonly List<QueueEntry> _queue;

    /// <summary>
    /// Initialize a new instance of a <see cref="Scanner"/> class.
    /// </summary>
    /// <param name="url">Initial URL.</param>
    /// <param name="options">Options.</param>
    public Scanner(Uri url, Options options)
    {
        var host = url.Host.ToLower();

        if (!options.InternalDomains.Contains(host))
        {
            options.InternalDomains.Add(host);
        }

        this._httpClient = new HttpClient();
        this._options = options;
        this._queue = new List<QueueEntry>
        {
            new()
            {
                UrlType = UrlType.InternalWebpage,
                Url = url
            }
        };
    }

    #endregion

    #region Public functions

    /// <summary>
    /// Process the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ProcessQueue(CancellationToken cancellationToken)
    {
        ConsoleEx.Write(
            "Started ",
            ConsoleColor.Yellow,
            this.Started.ToString("yyyy-MM-dd HH:mm:ss"),
            Environment.NewLine,
            Environment.NewLine);

        var queueIndex = -1;

        while (!cancellationToken.IsCancellationRequested)
        {
            queueIndex++;

            if (queueIndex == this._queue.Count)
            {
                break;
            }

            var queueEntry = this._queue[queueIndex];

            try
            {
                ConsoleEx.Write(
                    "[",
                    ConsoleColor.DarkCyan,
                    DateTime.Now.ToString("HH:mm:ss"),
                    ConsoleColorEx.ResetColor,
                    "] [",
                    ConsoleColor.DarkCyan,
                    queueIndex + 1,
                    ConsoleColorEx.ResetColor,
                    "/",
                    ConsoleColor.DarkCyan,
                    this._queue.Count,
                    ConsoleColorEx.ResetColor,
                    "] ",
                    ConsoleColor.Yellow,
                    queueEntry.Url,
                    Environment.NewLine);

                queueEntry.Started = DateTimeOffset.Now;

                if (queueEntry.UrlType is UrlType.InternalWebpage or UrlType.ExternalWebpage)
                {
                    await this.PerformPlaywrightRequest(queueEntry);
                }
                else
                {
                    await this.PerformHttpClientRequest(queueEntry, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                queueEntry.Error = ex.Message;
                queueEntry.ErrorType = ex.GetType().ToString();

                ConsoleEx.Write(
                    ConsoleColor.Red,
                    "Error: ",
                    ConsoleColorEx.ResetColor,
                    ex.Message,
                    Environment.NewLine);
            }

            queueEntry.Finished = DateTimeOffset.Now;
        }

        this.Finished = DateTimeOffset.Now;

        ConsoleEx.Write(
            Environment.NewLine,
            "Finished ",
            ConsoleColor.Yellow,
            this.Finished.Value.ToString("yyyy-MM-dd HH:mm:ss"),
            ConsoleColorEx.ResetColor,
            Environment.NewLine,
            "Took ",
            ConsoleColor.Yellow,
            this.Finished - this.Started,
            Environment.NewLine,
            Environment.NewLine);
    }

    /// <summary>
    /// Install and setup Playwright.
    /// </summary>
    /// <returns>Success.</returns>
    public async Task<bool> SetupPlaywright()
    {
        try
        {
            Microsoft.Playwright.Program.Main(
                new[]
                {
                    "install"
                });

            var instance = await Playwright.CreateAsync();

            this.Browser = this._options.RenderingEngine switch
            {
                RenderingEngine.Chromium => await instance.Chromium.LaunchAsync(
                    this._options.PlaywrightConfig?.BrowserTypeLaunchOptions),

                RenderingEngine.Firefox => await instance.Firefox.LaunchAsync(
                    this._options.PlaywrightConfig?.BrowserTypeLaunchOptions),

                RenderingEngine.Webkit => await instance.Webkit.LaunchAsync(
                    this._options.PlaywrightConfig?.BrowserTypeLaunchOptions),

                _ => throw new Exception(
                    "Invalid rendering engine value.")
            };

            this.Page = await this.Browser.NewPageAsync(
                this._options.PlaywrightConfig?.BrowserNewPageOptions);

            return true;
        }
        catch (Exception ex)
        {
            ConsoleEx.Write(
                "Error while setting up Playwright. ",
                Environment.NewLine,
                ex.Message,
                Environment.NewLine);

            return false;
        }
    }

    /// <summary>
    /// Write reports to disk.
    /// </summary>
    public async Task WriteReports()
    {
        var generator = new ReportGenerator(
            this._options,
            this._queue,
            this.CompileStatsObject());

        await generator.WriteJsonReports();
        await generator.WriteHtmlReport();
    }

    #endregion

    #region Helper functions

    /// <summary>
    /// Compile the object for stats report.
    /// </summary>
    /// <returns>Stats object.</returns>
    private ScanStats CompileStatsObject()
    {
        var statusCodes = this._queue
            .Select(n => n.Response?.StatusCode ?? 0)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var obj = new ScanStats
        {
            Meta = new()
            {
                Finished = this.Finished!.Value,
                Started = this.Started
            },
            StatusCodes = statusCodes
                .ToDictionary(
                    n => n,
                    n => this._queue.Count(m => m.Response?.StatusCode == n)),
            Accessibility = new()
            {
                Incomplete = this._queue.Sum(n => n.AccessibilityResults?.Incomplete?.Length ?? 0),
                Violations = this._queue.Sum(n => n.AccessibilityResults?.Violations?.Length ?? 0)
            }
        };

        if (obj.StatusCodes.ContainsKey(0))
        {
            obj.StatusCodes[0] = this._queue.Count(n => n.Response is null);
        }

        return obj;
    }

    /// <summary>
    /// Extract various HTML metadata.
    /// </summary>
    /// <param name="queueEntry">Queue entry.</param>
    private async Task ExtractHtmlData(QueueEntry queueEntry)
    {
        var titles = this.Page!.Locator("//title");
        var count = await titles.CountAsync();

        if (count > 0)
        {
            queueEntry.Response!.Title = await titles.Nth(0).TextContentAsync();
        }

        var metas = this.Page!.Locator("//meta");
        count = await metas.CountAsync();

        if (count > 0)
        {
            queueEntry.Response!.MetaTags ??= new();
        }

        for (var i = 0; i < count; i++)
        {
            queueEntry.Response!.MetaTags!.Add(new()
            {
                Charset = await metas.Nth(i).GetAttributeAsync("charset"),
                Content = await metas.Nth(i).GetAttributeAsync("content"),
                HttpEquiv = await metas.Nth(i).GetAttributeAsync("http-equiv"),
                Name = await metas.Nth(i).GetAttributeAsync("name"),
                Property = await metas.Nth(i).GetAttributeAsync("property")
            });
        }
    }

    /// <summary>
    /// Extract various links.
    /// </summary>
    /// <param name="queueEntry">Queue entry.</param>
    private async Task ExtractLinks(QueueEntry queueEntry)
    {
        var selectors = new Dictionary<string, string>
        {
            { "a", "href" },
            { "img", "src" },
            { "link", "href" },
            { "script", "src" }
        };

        foreach (var (tag, attr) in selectors)
        {
            var hrefs = this.Page!.Locator($"//{tag}[@{attr}]");
            var count = await hrefs.CountAsync();

            for (var i = 0; i < count; i++)
            {
                var url = await hrefs.Nth(i).GetAttributeAsync(attr);

                if (url?.Trim().StartsWith("mailto:", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    continue;
                }

                if (!Uri.TryCreate(queueEntry.Url, url, out var uri) ||
                    !uri.IsAbsoluteUri ||
                    string.IsNullOrWhiteSpace(uri.DnsSafeHost))
                {
                    continue;
                }

                UrlType urlType;

                if (this._options.InternalDomains.Contains(uri.Host.ToLower()))
                {
                    urlType = tag == "a"
                        ? UrlType.InternalWebpage
                        : UrlType.InternalAsset;
                }
                else
                {
                    urlType = tag == "a"
                        ? UrlType.ExternalWebpage
                        : UrlType.ExternalAsset;
                }

                var entry = this._queue
                    .FirstOrDefault(n => n.Url == uri);

                if (entry is null)
                {
                    ConsoleEx.Write(
                        "+ ",
                        ConsoleColor.Green,
                        uri,
                        Environment.NewLine);

                    entry = new()
                    {
                        Url = uri,
                        UrlType = urlType
                    };

                    this._queue.Add(entry);
                }

                if (!entry.LinkedFrom.Contains(queueEntry.Id))
                {
                    entry.LinkedFrom.Add(queueEntry.Id);
                }
            }
        }
    }

    /// <summary>
    /// Get a description matching the given HTTP status code.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <returns>Status description.</returns>
    private string GetStatusDescription(int statusCode)
    {
        return statusCode switch
        {
            100 => "Continue",
            101 => "Switching Protocols",
            102 => "Processing",
            103 => "Early Hints",

            200 => "Ok",
            201 => "Created",
            202 => "Accepted",
            203 => "Non-Authoritative Information",
            204 => "No Content",
            205 => "Reset Content",
            206 => "Partial Content",
            207 => "Multi-Status",
            208 => "Already Reported",
            226 => "IM Used",

            300 => "Multiple Choices",
            301 => "Moved Permanently",
            302 => "Found",
            303 => "See Other",
            304 => "Not Modified",
            305 => "Use Proxy",
            307 => "Temporary Redirect",
            308 => "Permanent Redirect",

            400 => "Bad Request",
            401 => "Unauthorized",
            402 => "Payment Required",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            406 => "Not Acceptable",
            407 => "Proxy Authentication Required",
            408 => "Request Timeout",
            409 => "Conflict",
            410 => "Gone",
            411 => "Length Required",
            412 => "Precondition Failed",
            413 => "Payload Too Large",
            414 => "URI Too Long",
            415 => "Unsupported Media Type",
            416 => "Range Not Satisfiable",
            417 => "Expectation Failed",
            418 => "I'm a teapot",
            421 => "Misdirected Request",
            422 => "Unprocessable Content",
            423 => "Locked",
            424 => "Failed Dependency",
            425 => "Too Early",
            426 => "Upgrade Required",
            428 => "Precondition Required",
            429 => "Too Many Requests",
            431 => "Request Header Fields Too Large",
            451 => "Unavailable For Legal Reasons",

            500 => "Internal Server Error",
            501 => "Not Implemented",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            505 => "HTTP Version Not Supported",
            506 => "Variant Also Negotiates",
            507 => "Insufficient Storage",
            508 => "Loop Detected",
            510 => "Not Extended",
            511 => "Network Authentication Required",

            _ => string.Empty
        };
    }

    /// <summary>
    /// Log response to console.
    /// </summary>
    /// <param name="response">Response.</param>
    private void LogResponse(UrlResponse response)
    {
        var culture = new CultureInfo("en-US");

        // Size.
        var sizeColor = response.Size switch
        {
            > 10000000 => ConsoleColor.Red,
            > 5000000 => ConsoleColor.Yellow,
            _ => ConsoleColor.Green
        };

        var sizeFormatted = response.Size switch
        {
            > 1000000 => $"{(response.Size / 1000000M).ToString(culture)} MB",
            > 1000 => $"{(response.Size / 1000M).ToString(culture)} KB",
            _ => $"{response.Size} B"
        };

        // Time.
        var timeColor = response.Time switch
        {
            > 1000 => ConsoleColor.Red,
            > 300 => ConsoleColor.Yellow,
            _ => ConsoleColor.Green
        };

        var timeFormatted = response.Time switch
        {
            > 60 * 1000 => $"{(response.Time / (60M * 1000M)).ToString(culture)} m",
            > 1000 => $"{(response.Time / 1000M).ToString(culture)} s",
            _ => $"{response.Time} ms"
        };

        // Status.
        var statusColor = response.StatusCode switch
        {
            >= 200 and <= 299 => ConsoleColor.Green,
            <= 399 => ConsoleColor.Yellow,
            <= 599 => ConsoleColor.Red,
            _ => ConsoleColor.White
        };

        // Output.
        ConsoleEx.Write(
            "> ",
            sizeColor,
            sizeFormatted,
            "   ",
            timeColor,
            timeFormatted,
            "   ",
            statusColor,
            response.StatusCode,
            " ",
            response.StatusDescription,
            Environment.NewLine);
    }

    /// <summary>
    /// Perform a HTTP client and update queue entry.
    /// </summary>
    /// <param name="queueEntry">Queue entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task PerformHttpClientRequest(QueueEntry queueEntry, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var res = await this._httpClient.GetAsync(
                      queueEntry.Url,
                      cancellationToken)
                  ?? throw new Exception(
                      $"Unable to get a valid HTTP response from {queueEntry.Url}");

        stopwatch.Stop();

        var body = await res.Content.ReadAsByteArrayAsync(cancellationToken);

        queueEntry.Response = new()
        {
            Headers = res.Headers
                .ToDictionary(
                    n => n.Key.ToString().ToLower(),
                    n => n.Value.First().ToString()),
            Size = body.Length,
            StatusCode = (int)res.StatusCode,
            StatusCodeIsSuccess = res.IsSuccessStatusCode,
            StatusDescription = this.GetStatusDescription((int)res.StatusCode),
            Time = stopwatch.ElapsedMilliseconds
        };

        this.LogResponse(queueEntry.Response);
    }

    /// <summary>
    /// Perform a Playwright request, analyze the output, and work with the response.
    /// </summary>
    /// <param name="queueEntry">Queue entry.</param>
    private async Task PerformPlaywrightRequest(QueueEntry queueEntry)
    {
        var stopwatch = Stopwatch.StartNew();
        var res = await this.Page!.GotoAsync(
                      queueEntry.Url.ToString(),
                      this._options.PlaywrightConfig?.PageGotoOptions)
                  ?? throw new Exception(
                      $"Unable to get valid Playwright response from {queueEntry.Url}");

        stopwatch.Stop();

        var body = await res.BodyAsync();

        queueEntry.Response = new()
        {
            Headers = res.Headers,
            Size = body.Length,
            StatusCode = res.Status,
            StatusCodeIsSuccess = res.Status is >= 200 and <= 299,
            StatusDescription = this.GetStatusDescription(res.Status),
            Time = stopwatch.ElapsedMilliseconds
        };

        this.LogResponse(queueEntry.Response);

        await this.ExtractHtmlData(queueEntry);

        if (queueEntry.UrlType is UrlType.ExternalWebpage)
        {
            return;
        }

        await this.ExtractLinks(queueEntry);
        await this.SaveScreenshot(queueEntry);
        await this.RunAxeAccessibilityScan(queueEntry);
    }

    /// <summary>
    /// Run Axe accessibility scan.
    /// </summary>
    /// <param name="queueEntry">Queue entry.</param>
    private async Task RunAxeAccessibilityScan(QueueEntry queueEntry)
    {
        try
        {
            var results = await this.Page!.RunAxe();
            queueEntry.AccessibilityResults = new(results);
        }
        catch (Exception ex)
        {
            queueEntry.Error = ex.Message;
            queueEntry.ErrorType = ex.GetType().ToString();
        }
    }

    /// <summary>
    /// Save screenshot of the page to disk.
    /// </summary>
    /// <param name="queueEntry">Queue entry.</param>
    private async Task SaveScreenshot(QueueEntry queueEntry)
    {
        if (!this._options.SaveScreenshots)
        {
            return;
        }

        var path = Path.Combine(
            Program.ReportPath,
            "screenshots");

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        path = Path.Combine(
            path,
            $"{queueEntry.Id}.png");

        queueEntry.Response!.ScreenshotPath = path;

        await this.Page!.ScreenshotAsync(
            new()
            {
                Path = path
            });
    }

    #endregion
}