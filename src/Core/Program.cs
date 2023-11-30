using System.Collections.Concurrent;
using System.Text.Json;
using Serilog;
using Slap.Extenders;
using Slap.Models;
using Slap.Services;

namespace Slap.Core;

public static class Program
{
    /// <summary>
    /// When the scan finished.
    /// </summary>
    public static DateTimeOffset Finished { get; private set; }
    
    /// <summary>
    /// Parsed options.
    /// </summary>
    public static Options Options { get; } = new();
    
    /// <summary>
    /// Scanning queue.
    /// </summary>
    public static ConcurrentBag<QueueEntry> Queue { get; } = new();
    
    /// <summary>
    /// When the scan started.
    /// </summary>
    public static DateTimeOffset Started { get; private set; }

    /// <summary>
    /// Program version.
    /// </summary>
    public static Version Version { get; } = new(1, 5);
    
    /// <summary>
    /// Init all the things..
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    private static async Task Main(string[] args)
    {
        if (args.Length == 0 ||
            args.Any(n => n is "-h" or "--help"))
        {
            ShowProgramUsage();
            return;
        }

        if (!ParseCmdArgs(args) ||
            !SetDefaultOptionsValues())
        {
            return;
        }
        
        var tokenSource = new CancellationTokenSource();

        try
        {
            Console.CancelKeyPress += (_, e) =>
            {
                Log.Error("Aborted by user!");
                tokenSource.Cancel();
                e.Cancel = true;
            };
        }
        catch
        {
            //
        }

        if (Options.LogLevel != LogLevel.Silent)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
        }
        
        Started = DateTimeOffset.Now;

        var queueService = new QueueService();
        await queueService.ProcessQueue(tokenSource.Token);
        
        Finished = DateTimeOffset.Now;
        
        Log.Information(
            "Run started at {started} and ran till {finished} which took {took}",
            Started.ToString("yyyy-MM-dd HH:mm:ss"),
            Finished.ToString("yyyy-MM-dd HH:mm:ss"),
            (Finished - Started).ToHumanReadable());
        
        var reportService = new ReportService();
        await reportService.GenerateReports();
    }
    
    /// <summary>
    /// Parse the command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Success.</returns>
    private static bool ParseCmdArgs(IReadOnlyList<string> args)
    {
        var skip = false;

        for (var i = 0; i < args.Count; i++)
        {
            if (skip)
            {
                skip = false;
                continue;
            }
            
            switch (args[i])
            {
                // Set rendering engine to use.
                case "--engine":
                case "-e":
                    if (i == args.Count - 1)
                    {
                        Console.WriteLine($"ERROR: {args[i]} must be followed by a rendering engine name.");
                        return false;
                    }

                    switch (args[i + 1].ToLower())
                    {
                        case "chromium":
                            Options.RenderingEngine = RenderingEngine.Chromium;
                            break;
                        
                        case "firefox":
                            Options.RenderingEngine = RenderingEngine.Firefox;
                            break;
                        
                        case "webkit":
                            Options.RenderingEngine = RenderingEngine.Webkit;
                            break;
                        
                        default:
                            Console.WriteLine($"ERROR: Invalid value for {args[i]}");
                            return false;
                    }

                    skip = true;
                    break;
                
                // Add a domain to be treated as another internal domain.
                case "--add":
                case "-a":
                    if (i == args.Count - 1)
                    {
                        Console.WriteLine($"ERROR: {args[i]} must be followed by a domain name.");
                        return false;
                    }

                    if (!Options.InternalDomains.Contains(args[i + 1].ToLower()))
                    {
                        Options.InternalDomains.Add(args[i + 1].ToLower());
                    }

                    skip = true;
                    break;
                
                // Set report path.
                case "--path":
                case "-p":
                    if (i == args.Count - 1)
                    {
                        Console.WriteLine($"ERROR: {args[i]} must be followed by a valid folder path.");
                        return false;
                    }

                    if (!Directory.Exists(args[i + 1]))
                    {
                        Console.WriteLine($"ERROR: {args[i + 1]} is not a valid folder path.");
                        return false;
                    }

                    Options.ReportPath = args[i + 1];
                    skip = true;
                    
                    break;
                
                // Skip scanning of certain types of links.
                case "--skip":
                case "-s":
                    if (i == args.Count - 1)
                    {
                        Console.WriteLine($"ERROR: {args[i]} must be followed by a valid link type.");
                        return false;
                    }

                    var skips = new List<UrlType>();

                    switch (args[i + 1])
                    {
                        case "assets":
                            skips.Add(UrlType.ExternalAsset);
                            skips.Add(UrlType.InternalAsset);
                            break;
                        
                        case "external":
                            skips.Add(UrlType.ExternalAsset);
                            skips.Add(UrlType.ExternalPage);
                            break;
                        
                        case "external-assets":
                            skips.Add(UrlType.ExternalAsset);
                            break;
                        
                        case "external-webpages":
                            skips.Add(UrlType.ExternalPage);
                            break;
                        
                        case "internal-assets":
                            skips.Add(UrlType.InternalAsset);
                            break;
                        
                        default:
                            if (!Uri.TryCreate($"https://{args[i + 1]}", UriKind.Absolute, out var uri))
                            {
                                Console.WriteLine($"ERROR: {args[i + 1]} is not a valid link type or domain name. Valid options are: assets, external, external-assets, external-webpages, internal-assets");
                                return false;
                            }

                            if (!Options.DomainsToSkip.Contains(uri.Host.ToLower()))
                            {
                                Options.DomainsToSkip.Add(uri.Host.ToLower());
                            }

                            break;
                    }

                    foreach (var urlType in skips.Where(urlType => !Options.UrlTypesToSkip.Contains(urlType)))
                    {
                        Options.UrlTypesToSkip.Add(urlType);
                    }

                    skip = true;
                    break;
                
                // Skip any URL that matches the given regular expression.
                case "--skip-re":
                    if (i == args.Count - 1)
                    {
                        Console.WriteLine($"ERROR: {args[i]} must be followed by a reg-ex string.");
                        return false;
                    }

                    Options.RegExMatchesToSkip.Add(args[i + 1]);
                    skip = true;
                    
                    break;
                
                // Set the timeout for each request.
                case "--timeout":
                case "-t":
                    if (i == args.Count - 1)
                    {
                        Console.WriteLine($"ERROR: {args[i]} must be followed by a number of seconds.");
                        return false;
                    }

                    if (!int.TryParse(args[i + 1], out var seconds))
                    {
                        Console.WriteLine($"ERROR: {args[i+1]} is an invalid value for number of seconds.");
                        return false;
                    }

                    Options.Timeout = seconds;
                    skip = true;
                    
                    break;
                
                // Save a screenshot for every internal webpage scan.
                case "--screenshots":
                case "-ss":
                    Options.SaveScreenshots = true;
                    break;
                
                // Capture full page instead of just the viewport size.
                case "--full-page":
                case "-fp":
                    Options.CaptureFullPage = true;
                    break;
                
                // Set the viewport size, for the screenshots and accessibility checks.
                case "--size":
                    if (i == args.Count - 1)
                    {
                        Console.WriteLine($"ERROR: {args[i]} must be followed by a width and height.");
                        return false;
                    }

                    var size = args[i + 1].Split('x');

                    if (size.Length != 2 ||
                        !int.TryParse(size[0], out var width) ||
                        !int.TryParse(size[1], out var height))
                    {
                        Console.WriteLine($"ERROR: {args[i + 1]} is not a valid width and height. Must be in the format of 1920x1080.");
                        return false;
                    }

                    Options.ViewportHeight = height;
                    Options.ViewportWidth = width;

                    skip = true;
                    break;
                
                // Load a queue file, but only process the entries that failed.
                case "--load":
                    if (i == args.Count - 1)
                    {
                        Console.WriteLine($"ERROR: {args[i]} must be followed by a file path.");
                        return false;
                    }

                    try
                    {
                        var json = File.ReadAllText(args[i + 1]);
                        var list = JsonSerializer.Deserialize<List<QueueEntry>>(
                            json,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        if (list is null ||
                            list.Count == 0 ||
                            list.First().Url == null!)
                        {
                            throw new Exception("Invalid deserialization");
                        }

                        foreach (var entry in list.Where(n => n.Error is not null))
                        {
                            entry.AccessibilityResults = null;
                            entry.Error = null;
                            entry.ErrorType = null;
                            entry.Processed = false;
                            entry.Response = null;
                            entry.ScreenshotSaved = false;
                        }

                        foreach (var item in list)
                        {
                            Queue.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR: Unable to load queue from file: {ex.Message}");
                        return false;
                    }

                    skip = true;
                    break;
                
                // Sets the maximum number of concurrent tasks URL scans.
                case "--parallelism":
                    if (i == args.Count - 1)
                    {
                        Console.WriteLine($"ERROR: {args[i]} must be followed by a file path.");
                        return false;
                    }

                    if (!int.TryParse(args[i + 1], out var parallelism) ||
                        parallelism == 0 ||
                        parallelism < -1)
                    {
                        Console.WriteLine($"ERROR: {args[i + 1]} is not a valid parallelism number. Must be -1 or a positive number.");
                        return false;
                    }

                    Options.Parallelism = parallelism;
                    skip = true;
                    
                    break;
                
                // Allows the program to follow redirection responses.
                case "--allow-redirects":
                    Options.AllowAutoRedirect = true;
                    break;
                
                // Display more detailed info while logging to console.
                case "--verbose":
                    Options.LogLevel = LogLevel.Verbose;
                    break;
                
                // Do not display any logs in console.
                case "--silent":
                    Options.LogLevel = LogLevel.Silent;
                    break;
                
                // Parse as URL.
                default:
                    if (!Uri.TryCreate(args[i], UriKind.Absolute, out var url))
                    {
                        Console.WriteLine($"ERROR: {args[i]} is an invalid URL.");
                        return false;
                    }

                    if (Queue.All(n => n.Url != url))
                    {
                        Queue.Add(new(new Uri($"{url.Scheme}://{url.Host}/")));

                        if (!Options.InternalDomains.Contains(url.Host))
                        {
                            Options.InternalDomains.Add(url.Host);
                        }
                    }
                    
                    break;
            }
        }

        return true;
    }

    /// <summary>
    /// Show program usage and options.
    /// </summary>
    private static void ShowProgramUsage()
    {
        var lines = new[]
        {
            $"Slap v{Version}",
            "Slap a site and see what falls out. A simple CLI to assist in QA checking of a site.",
            "",
            "If you slap https://example.com it will crawl all URLs found, both internal and external, but not move beyond the initial domain. After it is done, it will generate a report.",
            "",
            "Usage:",
            "  slap <url> [<options>]",
            "",
            "Options:",
            "  --engine <engine>         Set the rendering engine to use. Defaults to Chromium.",
            "  --add <domain>            Add a domain to be treated as another internal domain.",
            "  --path <folder>           Set report path. Defaults to current directory.",
            "  --skip <type>             Skip scanning of certain types of links.",
            "  --skip <domain>           Add a domain to be skipped while scanning.",
            "  --skip-re <regex>         Skip any URL that matches the given regular expression.",
            "  --timeout <seconds>       Set the timeout for each request. Defaults to 10 seconds.",
            "  --screenshots             Save a screenshot for every internal webpage scan.",
            "  --full-page               Capture full page instead of just the viewport size.",
            "  --size <width>x<height>   Set the viewport size, for the screenshots and accessibility checks. Defaults to 1920x1080.",
            "  --load <file>             Load a queue file, but only process the entries that failed.",
            "  --parallelism <count>     Sets the maximum number of concurrent URL scans.",
            "  --allow-redirects         Allows the program to follow redirection responses.",
            "  --verbose                 Display more detailed info while logging to console.",
            "  --silent                  Do not display any logs in console.",
            "",
            "Source and documentation available at https://github.com/nagilum/slap"
        };

        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }
    }

    /// <summary>
    /// Set default value for all options not specified by user.
    /// </summary>
    private static bool SetDefaultOptionsValues()
    {
        // Report path.
        var path = Options.ReportPath
            ??= Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                ?? Directory.GetCurrentDirectory();

        path = Path.Combine(
            path,
            "reports",
            Queue.First().Url.Host,
            DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch
        {
            Console.WriteLine($"ERROR: Unable to create report path {path}");
            return false;
        }

        Options.ReportPath = path;

        // Done.
        return true;
    }
}