﻿using System.Diagnostics;
using Deque.AxeCore.Playwright;
using Serilog;
using Slap.Core;
using Slap.Models;
using Slap.Models.Interfaces;
using Slap.Services.Interfaces;

namespace Slap.Services;

public class ScannerService : IScannerService
{
    #region Constructor, fields, and properties

    /// <summary>
    /// HTTP client.
    /// </summary>
    private readonly HttpClient _httpClient;
    
    /// <summary>
    /// Playwright browser.
    /// </summary>
    private Microsoft.Playwright.IBrowser? Browser { get; set; }
    
    /// <summary>
    /// User-agent, from Playwright. Used in the HTTP client requests.
    /// </summary>
    private string? UserAgent { get; set; }

    /// <summary>
    /// Initialize a new instance of a <see cref="ScannerService"/> class.
    /// </summary>
    public ScannerService()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = Program.Options.AllowAutoRedirect
        };
        
        this._httpClient = new HttpClient(handler);
        this._httpClient.Timeout = TimeSpan.FromSeconds(Program.Options.Timeout);
    }

    #endregion

    #region Implementation functions
    /// <summary>
    /// <inheritdoc cref="IScannerService.DisposePlaywright"/>
    /// </summary>
    public async Task DisposePlaywright()
    {
        await this.Browser!.CloseAsync();
    }

    /// <summary>
    /// <inheritdoc cref="IScannerService.PerformRequest"/>
    /// </summary>
    public async Task PerformRequest(QueueEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await this.PerformHttpClientRequest(entry, cancellationToken);

            var playwrightRequest = false;

            if (entry.Response?.StatusCode == 200 &&
                entry.Response?.ContentType?.IndexOf("text/html", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                await this.SetupPlaywrightPage(entry);
                await this.PerformPlaywrightRequest(entry);

                playwrightRequest = true;
            }

            if (playwrightRequest &&
                entry.UrlType is UrlType.InternalPage)
            {
                await this.ExtractNewUrls(entry);
                await this.RunAxeAccessibilityScan(entry);
            }

            if (playwrightRequest &&
                entry.UrlType is UrlType.InternalPage or UrlType.ExternalPage &&
                entry.Response is not null)
            {
                await this.ExtractHtmlData(entry);
            }

            if (playwrightRequest &&
                entry.UrlType is UrlType.InternalPage &&
                Program.Options.SaveScreenshots)
            {
                await this.SaveScreenshot(entry);
            }
        }
        catch (Microsoft.Playwright.PlaywrightException ex)
        {
            if (ex.Message.IndexOf("ERR_NAME_NOT_RESOLVED", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                Log.Error(
                    "Unresolvable hostname {host}",
                    entry.Url.Host);

                entry.Error = new()
                {
                    Data = new()
                    {
                        {"Hostname", entry.Url.Host}
                    },
                    Message = "Unresolvable hostname",
                    Type = ErrorType.UnresolvableHostname
                };
            }
            else
            {
                Log.Error(
                    "Error while scanning {url}",
                    entry.Url);

                entry.Error = new()
                {
                    Message = ex.Message,
                    Namespace = ex.GetType().ToString(),
                    Type = ErrorType.Unhandled
                };
            }
        }
        catch (HttpRequestException ex)
        {
            if (ex.Message.IndexOf("The requested name is valid", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                Log.Error(
                    "Unresolvable hostname {host}",
                    entry.Url.Host);

                entry.Error = new()
                {
                    Data = new()
                    {
                        {"Hostname", entry.Url.Host}
                    },
                    Message = "Unresolvable hostname",
                    Type = ErrorType.UnresolvableHostname
                };
            }
            else
            {
                Log.Error(
                    "Error while scanning {url}",
                    entry.Url);

                entry.Error = new()
                {
                    Message = ex.Message,
                    Namespace = ex.GetType().ToString(),
                    Type = ErrorType.Unhandled
                };
            }
        }
        catch (TimeoutException)
        {
            Log.Error(
                "Timeout after {seconds} seconds from {url}",
                Program.Options.Timeout,
                entry.Url);

            entry.Error = new()
            {
                Data = new()
                {
                    {"Seconds", Program.Options.Timeout}
                },
                Message = "Request timeout",
                Type = ErrorType.RequestTimeout
            };
        }
        catch (TaskCanceledException ex)
        {
            if (ex.Message.IndexOf("HttpClient.Timeout", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                entry.Error = new()
                {
                    Data = new()
                    {
                        {"Seconds", Program.Options.Timeout}
                    },
                    Message = "Request timeout",
                    Type = ErrorType.RequestTimeout
                };
            }
            else
            {
                entry.Error = new()
                {
                    Message = ex.Message,
                    Namespace = ex.GetType().ToString(),
                    Type = ErrorType.Unhandled
                };
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.IndexOf("ERR_NAME_NOT_RESOLVED", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                Log.Error(
                    "Unresolvable hostname {host}",
                    entry.Url.Host);

                entry.Error = new()
                {
                    Data = new()
                    {
                        {"Hostname", entry.Url.Host}
                    },
                    Message = "Unresolvable hostname",
                    Type = ErrorType.UnresolvableHostname
                };
            }
            else
            {
                Log.Error(
                    "Error while scanning {url}",
                    entry.Url);

                entry.Error = new()
                {
                    Message = ex.Message,
                    Namespace = ex.GetType().ToString(),
                    Type = ErrorType.Unhandled
                };
            }
        }

        if (entry.Page is not null)
        {
            await entry.Page.CloseAsync();
            entry.Page = null;
        }
        
        entry.Processed = true;
    }

    /// <summary>
    /// <inheritdoc cref="IScannerService.SetupPlaywright"/>
    /// </summary>
    public async Task<bool> SetupPlaywright()
    {
        try
        {
            if (Program.Options.LogLevel == LogLevel.Verbose)
            {
                Log.Information("Setting up Playwright..");                
            }
            
            Microsoft.Playwright.Program.Main(
                new[]
                {
                    "install"
                });

            var instance = await Microsoft.Playwright.Playwright.CreateAsync();
            
            this.Browser = Program.Options.RenderingEngine switch
            {
                RenderingEngine.Chromium => await instance.Chromium.LaunchAsync(),
                RenderingEngine.Firefox => await instance.Firefox.LaunchAsync(),
                RenderingEngine.Webkit => await instance.Webkit.LaunchAsync(),
                _ => throw new Exception("Invalid rendering engine value.")
            };

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
    /// <param name="entry">Queue entry.</param>
    private async Task ExtractHtmlData(IQueueEntry entry)
    {
        // Document title.
        var titles = entry.Page!.Locator("//title");
        var count = await titles.CountAsync();

        if (count > 0)
        {
            entry.Response!.DocumentTitle = await titles.Nth(0).TextContentAsync();
        }

        // Meta tags.
        var metas = entry.Page!.Locator("//meta");
        count = await metas.CountAsync();

        if (count > 0)
        {
            entry.Response!.MetaTags ??= new();
        }

        for (var i = 0; i < count; i++)
        {
            entry.Response!.MetaTags!.Add(new MetaTag
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

        var added = 0;

        foreach (var (tag, attr) in selectors)
        {
            var hrefs = entry.Page!.Locator($"//{tag}[@{attr}]");
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
                    if (Program.Options.LogLevel == LogLevel.Verbose)
                    {
                        Log.Information(
                            "Added {url} to queue.",
                            uri.ToString().Replace(" ", "%20"));    
                    }

                    added++;

                    newEntry = new(uri, this.GetUrlType(uri, tag));
                    Program.Queue.Add(newEntry);
                }

                if (!newEntry.LinkedFrom.Contains(entry.Url))
                {
                    newEntry.LinkedFrom.Add(entry.Url);
                }
            }
        }

        if (added > 0)
        {
            Log.Information(
                "Added {count} to queue. Total URLs in queue {total}. {left} left to process.",
                added,
                Program.Queue.Count,
                Program.Queue.Count(n => !n.Processed) - 1);
        }
    }

    /// <summary>
    /// Get the URL type for the given URL.
    /// </summary>
    /// <param name="uri">URL.</param>
    /// <param name="tag">Originating tag.</param>
    /// <returns>URL type.</returns>
    private UrlType GetUrlType(Uri uri, string tag)
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

        if (tag == "a")
        {
            var last = uri.Segments.Last();

            if (last.IndexOf('.') > -1)
            {
                if (Program.Options.InternalDomains.Contains(uri.Host.ToLower()))
                {
                    urlType = webpageExtensions.Any(ext =>
                        last.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
                        ? UrlType.InternalPage
                        : UrlType.InternalAsset;
                }
                else
                {
                    urlType = webpageExtensions.Any(ext =>
                        last.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
                        ? UrlType.ExternalPage
                        : UrlType.ExternalAsset;
                }
            }
            else
            {
                urlType = Program.Options.InternalDomains.Contains(uri.Host.ToLower())
                    ? UrlType.InternalPage
                    : UrlType.ExternalPage;
            }
        }
        else
        {
            urlType = Program.Options.InternalDomains.Contains(uri.Host.ToLower())
                ? UrlType.InternalAsset
                : UrlType.ExternalAsset;
        }

        return urlType;
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

        if (this.UserAgent is null)
        {
            await entry.Page!.RouteAsync("**/*", async route =>
            {
                var (_, userAgent) = route.Request.Headers
                    .FirstOrDefault(n => n.Key.Equals("user-agent", StringComparison.InvariantCultureIgnoreCase));

                if (!string.IsNullOrWhiteSpace(userAgent))
                {
                    this.UserAgent = userAgent;
                }
                
                await route.ContinueAsync();
            });            
        }

        var res = await entry.Page!.GotoAsync(entry.Url.ToString(), options)
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
            Headers = res.Headers.ToDictionary(n => n.Key, n => (string?)n.Value),
            Size = size,
            StatusCode = res.Status,
            StatusDescription = GetStatusDescription(res.Status),
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

        var req = new HttpRequestMessage(HttpMethod.Get, entry.Url);
        
        req.Headers.Add("Accept", "*/*");
        req.Headers.Add("Accept-Encoding", "gzip, deflate, br");
        req.Headers.Add("Connection", "keep-alive");
        req.Headers.Add("Host", entry.Url.Host);
        req.Headers.Add("User-Agent", this.UserAgent ?? $"Slap/{Program.Version}");
        
        var res = await this._httpClient.SendAsync(req, cancellationToken)
            ?? throw new Exception($"Unable to get a valid HTTP response from {entry.Url}");

        stopwatch.Stop();
        
        var body = await res.Content.ReadAsByteArrayAsync(cancellationToken);

        string? redirectUrl = null;
        string? contentType = null;
        var headers = new Dictionary<string, string?>();

        foreach (var (key, values) in res.Headers)
        {
            var value = values
                .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
            
            if (contentType is null &&
                key.Equals("content-type", StringComparison.InvariantCultureIgnoreCase) &&
                value is not null)
            {
                contentType = value;
            }

            if (key.Equals("location", StringComparison.InvariantCultureIgnoreCase))
            {
                redirectUrl = value;
            }

            headers.TryAdd(key, value);
        }

        foreach (var (key, values) in res.Content.Headers)
        {
            var value = values
                .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
            
            if (contentType is null &&
                key.Equals("content-type", StringComparison.InvariantCultureIgnoreCase) &&
                value is not null)
            {
                contentType = value;
            }

            headers.TryAdd(key, value);
        }

        if (redirectUrl is not null &&
            Program.Options.AllowAutoRedirect &&
            Uri.TryCreate(entry.Url, redirectUrl, out var uri) &&
            uri.IsAbsoluteUri &&
            !string.IsNullOrWhiteSpace(uri.DnsSafeHost))
        {
            var newEntry = Program.Queue
                .FirstOrDefault(n => n.Url == uri);

            if (newEntry is null)
            {
                if (Program.Options.LogLevel == LogLevel.Verbose)
                {
                    Log.Information(
                        "Added {url} to queue.",
                        uri.ToString().Replace(" ", "%20"));    
                }
                else
                {
                    Log.Information("Added 1 to queue.");
                }

                newEntry = new(uri, this.GetUrlType(uri, "a"));
                Program.Queue.Add(newEntry);
            }

            if (!newEntry.LinkedFrom.Contains(entry.Url))
            {
                newEntry.LinkedFrom.Add(entry.Url);
            }
        }

        headers = headers
            .OrderBy(n => n.Key)
            .ToDictionary(n => n.Key,
                n => n.Value);

        entry.Response = new QueueResponse
        {
            ContentType = contentType,
            Headers = headers,
            Size = body.Length,
            StatusCode = (int)res.StatusCode,
            StatusDescription = GetStatusDescription((int)res.StatusCode),
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
            var results = await entry.Page!.RunAxe();
            entry.AccessibilityResults = new(results);
        }
        catch
        {
            // Ignore error, for now.
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

        await entry.Page!.ScreenshotAsync(new()
        {
            FullPage = Program.Options.CaptureFullPage,
            Path = path,
            Timeout = Program.Options.Timeout * 1000
        });

        entry.ScreenshotSaved = true;
    }

    /// <summary>
    /// Setup the Playwright page for the request.
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    private async Task SetupPlaywrightPage(QueueEntry entry)
    {
        var options = new Microsoft.Playwright.BrowserNewPageOptions
        {
            ScreenSize = new Microsoft.Playwright.ScreenSize
            {
                Height = Program.Options.ViewportHeight,
                Width = Program.Options.ViewportWidth
            },
            ViewportSize = new Microsoft.Playwright.ViewportSize
            {
                Height = Program.Options.ViewportHeight,
                Width = Program.Options.ViewportWidth
            }
        };

        entry.Page = await this.Browser!.NewPageAsync(options);
    }

    #endregion
    
    #region Static helper functions
    
    /// <summary>
    /// Get a description matching the given HTTP status code.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <returns>Status description.</returns>
    public static string GetStatusDescription(int statusCode)
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
    
    #endregion
}