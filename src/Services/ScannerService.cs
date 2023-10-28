using System.Diagnostics;
using Deque.AxeCore.Playwright;
using Serilog;
using Slap.Core;
using Slap.Models;
using Slap.Models.Interfaces;
using Slap.Services.Interfaces;

namespace Slap.Services;

public class ScannerService : IScannerService
{
    #region Fields

    /// <summary>
    /// HTTP client.
    /// </summary>
    private readonly HttpClient _httpClient;

    #endregion
    
    #region Properties
    
    /// <summary>
    /// Playwright page.
    /// </summary>
    private Microsoft.Playwright.IPage? Page { get; set; }
    
    #endregion
    
    #region Constructor

    /// <summary>
    /// Initialize a new instance of a <see cref="ScannerService"/> class.
    /// </summary>
    public ScannerService()
    {
        this._httpClient = new HttpClient();
        this._httpClient.Timeout = TimeSpan.FromSeconds(Program.Options.Timeout);
    }
    
    #endregion
    
    #region Implementation functions
    
    /// <summary>
    /// <inheritdoc cref="IScannerService.PerformRequest"/>
    /// </summary>
    public async Task PerformRequest(QueueEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            if (entry.UrlType is UrlType.InternalWebpage or UrlType.ExternalWebpage)
            {
                await this.PerformPlaywrightRequest(entry);
            }
            else
            {
                await this.PerformHttpClientRequest(entry, cancellationToken);
            }

            if (entry.Response is not null)
            {
                this.LogResponse(entry.Response);
            }

            if (entry.UrlType is UrlType.InternalWebpage)
            {
                await this.ExtractNewUrls(entry);
                await this.RunAxeAccessibilityScan(entry);
            }

            if (entry.UrlType is UrlType.InternalWebpage or UrlType.ExternalWebpage &&
                entry.Response is not null)
            {
                await this.ExtractHtmlData(entry.Response);
            }

            if (entry.UrlType is UrlType.InternalWebpage &&
                Program.Options.SaveScreenshots)
            {
                await this.SaveScreenshot(entry);
            }

            entry.Processed = true;
        }
        catch (Microsoft.Playwright.PlaywrightException ex)
        {
            if (ex.Message.IndexOf("ERR_NAME_NOT_RESOLVED", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                Log.Error(
                    "Unresolvable hostname {host}",
                    entry.Url.Host);

                entry.Error = $"Unresolvable hostname {entry.Url.Host}";
                entry.ErrorType = "ERR_NAME_NOT_RESOLVED";
            }
            else
            {
                if (await this.SetupPlaywright())
                {
                    return;
                }

                Log.Error(
                    "Error while scanning {url}",
                    entry.Url);

                entry.Error = ex.Message;
                entry.ErrorType = ex.GetType().ToString();
            }

            entry.Processed = true;
        }
        catch (HttpRequestException ex)
        {
            if (ex.Message.IndexOf("The requested name is valid", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                Log.Error(
                    "Unresolvable hostname {host}",
                    entry.Url.Host);
                
                entry.Error = $"Unresolvable hostname {entry.Url.Host}";
                entry.ErrorType = "ERR_NAME_NOT_RESOLVED";
            }
            else
            {
                Log.Error(
                    "Error while scanning {url}",
                    entry.Url);
                
                entry.Error = ex.Message;
                entry.ErrorType = ex.GetType().ToString();
            }
            
            entry.Processed = true;
        }
        catch (TimeoutException)
        {
            Log.Error(
                "Timeout after {seconds} seconds from {url}",
                Program.Options.Timeout,
                entry.Url);

            entry.Error = $"Timeout after {Program.Options.Timeout} seconds from {entry.Url}";
            entry.ErrorType = "ERR_TIMEOUT";
            entry.Processed = true;
        }
        catch (Exception ex)
        {
            if (ex.Message.IndexOf("ERR_NAME_NOT_RESOLVED", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                Log.Error(
                    "Unresolvable hostname {host}",
                    entry.Url.Host);
                
                entry.Error = $"Unresolvable hostname {entry.Url.Host}";
                entry.ErrorType = "ERR_NAME_NOT_RESOLVED";
            }
            else
            {
                Log.Error(
                    "Error while scanning {url}",
                    entry.Url);
                
                entry.Error = ex.Message;
                entry.ErrorType = ex.GetType().ToString();
            }
            
            entry.Processed = true;
        }
    }

    /// <summary>
    /// <inheritdoc cref="IScannerService.SetupPlaywright"/>
    /// </summary>
    public async Task<bool> SetupPlaywright()
    {
        try
        {
            Microsoft.Playwright.Program.Main(
                new[]
                {
                    "install"
                });
            
            var instance = await Microsoft.Playwright.Playwright.CreateAsync();
            var browser = Program.Options.RenderingEngine switch
            {
                RenderingEngine.Chromium => await instance.Chromium.LaunchAsync(),
                RenderingEngine.Firefox => await instance.Firefox.LaunchAsync(),
                RenderingEngine.Webkit => await instance.Webkit.LaunchAsync(),
                _ => throw new Exception("Invalid rendering engine value.")
            };

            if (this.Page is not null)
            {
                this.Page = null;
            }

            this.Page = await browser.NewPageAsync();

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unable to setup Playwright.");
            return false;
        }
    }
    
    #endregion
    
    #region Helper functions
    
    /// <summary>
    /// Extract document title and meta tags from HTML content.
    /// </summary>
    /// <param name="response">Queue response.</param>
    private async Task ExtractHtmlData(QueueResponse response)
    {
        // Document title.
        var titles = this.Page!.Locator("//title");
        var count = await titles.CountAsync();

        if (count > 0)
        {
            response.DocumentTitle = await titles.Nth(0).TextContentAsync();
        }
        
        // Meta tags.
        var metas = this.Page!.Locator("//meta");
        count = await metas.CountAsync();

        if (count > 0)
        {
            response.MetaTags ??= new();
        }

        for (var i = 0; i < count; i++)
        {
            response.MetaTags!.Add(new MetaTag
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
    /// Extract new URLs to scan from Playwright page.
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    private async Task ExtractNewUrls(IQueueEntry entry)
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
                
                if (!Uri.TryCreate(entry.Url, url, out var uri) ||
                    !uri.IsAbsoluteUri ||
                    string.IsNullOrWhiteSpace(uri.DnsSafeHost))
                {
                    continue;
                }

                var newEntry = Program.Queue
                    .FirstOrDefault(n => n.Url == uri);

                if (newEntry is null)
                {
                    Log.Information(
                        "Added {url} to queue.",
                        uri.ToString().Replace(" ", "%20"));

                    newEntry = new(uri, this.GetUrlType(uri));
                    Program.Queue.Add(newEntry);
                }

                if (!newEntry.LinkedFrom.Contains(entry.Url))
                {
                    newEntry.LinkedFrom.Add(entry.Url);
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
    /// Get the URL type for the given URL.
    /// </summary>
    /// <param name="uri">URL.</param>
    /// <returns>URL type.</returns>
    private UrlType GetUrlType(Uri uri)
    {
        var webpageExtensions = new[]
        {
            ".asp",
            ".aspx",
            ".htm",
            ".html",
            ".php"
        };
        
        UrlType urlType;
        var last = uri.Segments.Last();

        if (Program.Options.InternalDomains.Contains(uri.Host.ToLower()))
        {
            if (last.IndexOf('.') > -1)
            {
                urlType = webpageExtensions.Any(ext => last.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
                    ? UrlType.InternalWebpage
                    : UrlType.InternalAsset;
            }
            else
            {
                urlType = UrlType.InternalWebpage;
            }
        }
        else
        {
            if (last.IndexOf('.') > -1)
            {
                urlType = webpageExtensions.Any(ext => last.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
                    ? UrlType.ExternalWebpage
                    : UrlType.ExternalAsset;
            }
            else
            {
                urlType = UrlType.ExternalWebpage;
            }
        }

        return urlType;
    }
    
    /// <summary>
    /// Log response to console.
    /// </summary>
    /// <param name="response">Queue response.</param>
    private void LogResponse(IQueueResponse response)
    {
        switch (response.StatusCode)
        {
            case >= 200 and <= 299:
                Log.Information(
                    "{size}, {time}, {status}",
                    response.GetSizeFormatted(),
                    response.GetTimeFormatted(),
                    response.GetStatusFormatted());
                
                break;
            
            case <= 399:
                Log.Warning(
                    "{size}, {time}, {status}",
                    response.GetSizeFormatted(),
                    response.GetTimeFormatted(),
                    response.GetStatusFormatted());
                
                break;
            
            default:
                Log.Error(
                    "{size}, {time}, {status}",
                    response.GetSizeFormatted(),
                    response.GetTimeFormatted(),
                    response.GetStatusFormatted());
                
                break;
        }
    }
    
    /// <summary>
    /// Perform a Playwright request, analyze the response, and update queue entry.
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    /// <exception cref="Exception">Thrown if the GET request fails on a network level.</exception>
    private async Task PerformPlaywrightRequest(QueueEntry entry)
    {
        var stopwatch = Stopwatch.StartNew();
        var options = new Microsoft.Playwright.PageGotoOptions
        {
            Timeout = Program.Options.Timeout * 1000
        };
        
        var res = await this.Page!.GotoAsync(entry.Url.ToString(), options)
                  ?? throw new Exception($"Unable to get a valid HTTP response from {entry.Url}");
        
        stopwatch.Stop();
        
        int? size = null;
        string? contentType = null;

        foreach (var (key, value) in res.Headers)
        {
            if (!key.Equals("content-type", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            contentType = value;
            break;
        }

        if (contentType?.IndexOf("text/html", StringComparison.InvariantCultureIgnoreCase) > -1)
        {
            var body = await res.BodyAsync();
            size = body.Length;
        }

        entry.Response = new QueueResponse
        {
            Headers = res.Headers,
            Size = size,
            StatusCode = res.Status,
            StatusDescription = this.GetStatusDescription(res.Status),
            Time = stopwatch.ElapsedMilliseconds
        };
    }
    
    /// <summary>
    /// Perform a HTTP client and update queue entry.
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exception">Thrown if the GET request fails on a network level.</exception>
    private async Task PerformHttpClientRequest(QueueEntry entry, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var res = await this._httpClient.GetAsync(entry.Url, cancellationToken)
                  ?? throw new Exception($"Unable to get a valid HTTP response from {entry.Url}");
        
        stopwatch.Stop();

        var body = await res.Content.ReadAsByteArrayAsync(cancellationToken);

        entry.Response = new QueueResponse
        {
            Headers = res.Headers.ToDictionary(
                n => n.Key.ToString().ToLower(),
                n => n.Value.First().ToString()),
            Size = body.Length,
            StatusCode = (int)res.StatusCode,
            StatusDescription = this.GetStatusDescription((int)res.StatusCode),
            Time = stopwatch.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// Run Axe accessibility scan.
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    private async Task RunAxeAccessibilityScan(QueueEntry entry)
    {
        try
        {
            var results = await this.Page!.RunAxe();
            entry.AccessibilityResults = new(results);
        }
        catch (Exception ex)
        {
            entry.Error = ex.Message;
            entry.ErrorType = ex.GetType().ToString();
        }
    }

    /// <summary>
    /// Save a screenshot of the current page.
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    private async Task SaveScreenshot(QueueEntry entry)
    {
        var path = Path.Combine(
            Program.Options.ReportPath!,
            "screenshots");

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        path = Path.Combine(
            path,
            $"screenshot-{entry.Id}.png");

        await this.Page!.ScreenshotAsync(new()
        {
            FullPage = Program.Options.CaptureFullPage,
            Path = path,
            Timeout = Program.Options.Timeout * 1000
        });

        entry.ScreenshotSaved = true;
    }

    #endregion
}