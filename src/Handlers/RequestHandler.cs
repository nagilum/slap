using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Deque.AxeCore.Playwright;
using HtmlAgilityPack;
using Microsoft.Playwright;
using Slap.Models;

namespace Slap.Handlers;

public class RequestHandler(IOptions options) : IRequestHandler
{
    /// <summary>
    /// Chromium browser instance.
    /// </summary>
    private IBrowser? BrowserChromium { get; set; }

    /// <summary>
    /// Firefox browser instance.
    /// </summary>
    private IBrowser? BrowserFirefox { get; set; }

    /// <summary>
    /// Webkit browser instance.
    /// </summary>
    private IBrowser? BrowserWebkit { get; set; }

    /// <summary>
    /// HttpClient.
    /// </summary>
    private HttpClient HttpClient { get; } = new();
    
    /// <summary>
    /// <inheritdoc cref="IRequestHandler.ScreenshotPath"/>
    /// </summary>
    public string? ScreenshotPath { get; set; }

    /// <summary>
    /// <inheritdoc cref="IRequestHandler.PerformHttpClientRequest"/>
    /// </summary>
    public async Task PerformHttpClientRequest(QueueEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var req = new HttpRequestMessage(HttpMethod.Get, entry.Url);
            var res = await this.HttpClient.SendAsync(req, cancellationToken);

            stopwatch.Stop();

            var content = await res.Content.ReadAsByteArrayAsync(cancellationToken);
            var response = new QueueResponse
            {
                BrowserType = BrowserType.HttpClient,
                Headers = [],
                StatusCode = (int)res.StatusCode,
                StatusDescription = this.GetStatusCodeDescription((int)res.StatusCode),
                Size = content.Length,
                Time = stopwatch.ElapsedMilliseconds
            };
            
            foreach (var (key, values) in res.Headers)
            {
                response.Headers.TryAdd(key, values.First());
            }

            foreach (var (key, values) in res.TrailingHeaders)
            {
                response.Headers.TryAdd(key, values.First());
            }

            foreach (var (key, values) in res.Content.Headers)
            {
                response.Headers.TryAdd(key, values.First());
            }

            entry.Responses.Add(response);
            
            this.IncrementResponseTypeCount($"{response.StatusCode} {this.GetStatusCodeDescription(response.StatusCode)}");

            if (entry.Type is not EntryType.HtmlDocument)
            {
                return;
            }

            var html = Encoding.UTF8.GetString(content);
            
            this.ParseContentForMetadata(response, html);
            this.ParseContentForNewUrls(entry, html);
        }
        catch (TaskCanceledException)
        {
            // Do nothing.
        }
        catch (Exception ex)
        {
            entry.Responses.Add(new QueueResponse()
            {
                BrowserType = BrowserType.HttpClient,
                Error = new()
                {
                    Message = ex.Message,
                    Type = ex.GetType().ToString()
                },
                Timeout = ex is TimeoutException
            });
            
            this.IncrementResponseTypeCount("Error");
        }
    }

    /// <summary>
    /// <inheritdoc cref="IRequestHandler.PerformPlaywrightRequest"/>
    /// </summary>
    public async Task PerformPlaywrightRequest(BrowserType browserType, QueueEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var browser = browserType switch
            {
                BrowserType.Chromium => this.BrowserChromium,
                BrowserType.Firefox => this.BrowserFirefox,
                BrowserType.Webkit => this.BrowserWebkit,
                _ => throw new Exception("Invalid browser type.")
            };
            
            var stopwatch = Stopwatch.StartNew();

            var page = await browser!.NewPageAsync(options.BrowserNewPageOptions);
            var res = await page.GotoAsync(entry.Url.ToString(), options.PageGotoOptions)
                      ?? throw new Exception("Unable to perform Playwright request.");

            stopwatch.Stop();

            var content = await res.BodyAsync();
            var response = new QueueResponse
            {
                BrowserType = browserType,
                Headers = await res.AllHeadersAsync(),
                StatusCode = res.Status,
                StatusDescription = this.GetStatusCodeDescription(res.Status),
                Size = content.Length,
                Time = stopwatch.ElapsedMilliseconds
            };

            entry.Responses.Add(response);
            
            this.IncrementResponseTypeCount($"{response.StatusCode} {this.GetStatusCodeDescription(response.StatusCode)}");

            await this.RunAxeAccessibilityScan(response, page);
            await this.ParseContentForMetadata(response, page);
            await this.ParseContentForNewUrls(entry, page);
        }
        catch (TaskCanceledException)
        {
            // Do nothing.
        }
        catch (TimeoutException)
        {
            entry.Responses.Add(
                new QueueResponse
                {
                    BrowserType = browserType,
                    Timeout = true
                });
            
            this.IncrementResponseTypeCount("Timeout");
        }
        catch (Exception ex)
        {
            entry.Responses.Add(new QueueResponse()
            {
                BrowserType = browserType,
                Error = new()
                {
                    Message = ex.Message,
                    Type = ex.GetType().ToString()
                },
                Timeout = ex is TimeoutException
            });
            
            this.IncrementResponseTypeCount("Error");
        }
    }

    /// <summary>
    /// <inheritdoc cref="IRequestHandler.SetupHttpClient"/>
    /// </summary>
    public void SetupHttpClient()
    {
        Console.WriteLine("Setting up HTTP client...");

        this.HttpClient.Timeout = TimeSpan.FromSeconds((int)options.PageGotoOptions.Timeout!);

        this.HttpClient.DefaultRequestHeaders.Clear();
        this.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        this.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Program.Name, Program.Version));
    }

    /// <summary>
    /// <inheritdoc cref="IRequestHandler.SetupPlaywright"/>
    /// </summary>
    public async Task SetupPlaywright(CancellationToken cancellationToken)
    {
        Console.WriteLine("Setting up Playwright browser instances...");

        Microsoft.Playwright.Program.Main(["install"]);

        var playwright = await Playwright.CreateAsync();

        this.BrowserChromium = await playwright.Chromium.LaunchAsync(options.BrowserLaunchOptions);
        this.BrowserFirefox = await playwright.Firefox.LaunchAsync(options.BrowserLaunchOptions);
        this.BrowserWebkit = await playwright.Webkit.LaunchAsync(options.BrowserLaunchOptions);
    }
    
    /// <summary>
    /// Get a description matching the given HTTP status code.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <returns>Status description.</returns>
    private string? GetStatusCodeDescription(int? statusCode)
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

            _ => null
        };
    }

    /// <summary>
    /// Increment the response count for a type.
    /// </summary>
    /// <param name="responseType">Type of response.</param>
    private void IncrementResponseTypeCount(string responseType)
    {
        if (Globals.ResponseTypeCounts.TryGetValue(responseType, out var value))
        {
            Globals.ResponseTypeCounts[responseType] = ++value;
        }
        else
        {
            Globals.ResponseTypeCounts.TryAdd(responseType, 1);
        }
    }
    
    /// <summary>
    /// Parse HTML for metadata.
    /// </summary>
    /// <param name="response">Queue entry response.</param>
    /// <param name="html">HTML.</param>
    private void ParseContentForMetadata(QueueResponse response, string html)
    {
        var doc = new HtmlDocument();

        doc.LoadHtml(html);

        // Document title.
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        response.Title = titleNode?.InnerText;
        
        // Meta tags.
        var metaNodes = doc.DocumentNode.SelectNodes("//meta");

        if (metaNodes is null)
        {
            return;
        }
        
        response.MetaTags = [];

        foreach (var meta in metaNodes)
        {
            response.MetaTags.Add(new()
            {
                Charset = meta.GetAttributeValue("charset", string.Empty),
                Content = meta.GetAttributeValue("content", string.Empty),
                HttpEquiv = meta.GetAttributeValue("http-equiv", string.Empty),
                Name = meta.GetAttributeValue("name", string.Empty),
                Property = meta.GetAttributeValue("property", string.Empty)
            });
        }
    }
    
    /// <summary>
    /// Parse Playwright page for metadata.
    /// </summary>
    /// <param name="response">Queue entry response.</param>
    /// <param name="page">Playwright page.</param>
    private async Task ParseContentForMetadata(QueueResponse response, IPage page)
    {
        // Document title.
        var titles = page.Locator("//title");
        var count = await titles.CountAsync();

        if (count > 0)
        {
            response.Title = await titles.Nth(0).TextContentAsync();
        }
        
        // Meta tags.
        var metas = page.Locator("//meta");
        count = await metas.CountAsync();

        if (count is 0)
        {
            return;
        }

        response.MetaTags = [];
        
        for (var i = 0; i < count; i++)
        {
            response.MetaTags.Add(new()
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
    /// Parse HTML for new URLs.
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    /// <param name="html">HTML.</param>
    private void ParseContentForNewUrls(QueueEntry entry, string html)
    {
        var doc = new HtmlDocument();

        doc.LoadHtml(html);

        var selectors = new List<Tuple<string, string, EntryType>>
        {
            new("a", "href", EntryType.HtmlDocument),
            new("img", "src", EntryType.Asset),
            new("link", "href", EntryType.Asset),
            new("script", "src", EntryType.Asset)
        };

        foreach (var (tag, attr, type) in selectors)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//{tag}[@{attr}]");

            if (nodes is null)
            {
                continue;
            }

            foreach (var node in nodes)
            {
                var url = node.GetAttributeValue(attr, string.Empty);

                if (!Uri.TryCreate(entry.Url, url, out var uri) ||
                    !uri.IsAbsoluteUri ||
                    string.IsNullOrWhiteSpace(uri.DnsSafeHost) ||
                    Globals.QueueEntries.Any(n => n.Url.ToString() == uri.ToString()) ||
                    uri.Scheme is not "http" and not "https")
                {
                    continue;
                }

                var newType = type;

                if (!uri.IsBaseOf(entry.Url) && !entry.Url.IsBaseOf(uri))
                {
                    newType = EntryType.External;
                }

                if (!Globals.QueueEntries.Any(n => n.Url.AbsolutePath.Equals(uri.AbsolutePath)))
                {
                    Globals.QueueEntries.Add(new(uri, newType));
                }
            }
        }
    }
    
    /// <summary>
    /// Parse Playwright page for new URLs.
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    /// <param name="page">Playwright page.</param>
    private async Task ParseContentForNewUrls(QueueEntry entry, IPage page)
    {
        var selectors = new List<Tuple<string, string, EntryType>>
        {
            new("a", "href", EntryType.HtmlDocument),
            new("img", "src", EntryType.Asset),
            new("link", "href", EntryType.Asset),
            new("script", "src", EntryType.Asset)
        };

        foreach (var (tag, attr, type) in selectors)
        {
            var hrefs = page.Locator($"//{tag}[@{attr}]");
            var count = await hrefs.CountAsync();

            for (var i = 0; i < count; i++)
            {
                var url = await hrefs.Nth(i).GetAttributeAsync(attr);
                
                if (!Uri.TryCreate(entry.Url, url, out var uri) ||
                    !uri.IsAbsoluteUri ||
                    string.IsNullOrWhiteSpace(uri.DnsSafeHost) ||
                    Globals.QueueEntries.Any(n => n.Url.ToString() == uri.ToString()) ||
                    uri.Scheme is not "http" and not "https")
                {
                    continue;
                }

                var newType = type;

                if (!uri.IsBaseOf(entry.Url) && !entry.Url.IsBaseOf(uri))
                {
                    newType = EntryType.External;
                }

                if (!Globals.QueueEntries.Any(n => n.Url.AbsolutePath.Equals(uri.AbsolutePath)))
                {
                    Globals.QueueEntries.Add(new(uri, newType));
                }
            }
        }
    }

    /// <summary>
    /// Run Axe accessibility scan.
    /// </summary>
    /// <param name="response">Response wrapper.</param>
    /// <param name="page">Playwright page.</param>
    private async Task RunAxeAccessibilityScan(QueueResponse response, IPage page)
    {
        try
        {
            var results = await page.RunAxe();

            response.AccessibilityResult = new(results);
        }
        catch
        {
            // Do nothing.
        }
    }
}