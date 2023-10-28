﻿using Serilog;
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
    public static List<QueueEntry> Queue { get; } = new();
    
    /// <summary>
    /// When the scan started.
    /// </summary>
    public static DateTimeOffset Started { get; } = DateTimeOffset.Now;

    /// <summary>
    /// Program version.
    /// </summary>
    public static Version Version { get; } = new(1, 4);
    
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

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        var queueService = new QueueService();
        var reportService = new ReportService();
        
        await queueService.ProcessQueue(tokenSource.Token);
        Finished = DateTimeOffset.Now;
        
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
                            skips.Add(UrlType.ExternalWebpage);
                            break;
                        
                        case "external-assets":
                            skips.Add(UrlType.ExternalAsset);
                            break;
                        
                        case "external-webpages":
                            skips.Add(UrlType.ExternalWebpage);
                            break;
                        
                        case "internal-assets":
                            skips.Add(UrlType.InternalAsset);
                            break;
                        
                        default:
                            Console.WriteLine($"ERROR: {args[i + 1]} is not a valid link type. Valid options are: assets, external, external-assets, external-webpages, internal-assets");
                            return false;
                    }

                    foreach (var urlType in skips.Where(urlType => !Options.UrlTypesToSkip.Contains(urlType)))
                    {
                        Options.UrlTypesToSkip.Add(urlType);
                    }

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
                
                // Parse as URL.
                default:
                    if (!Uri.TryCreate(args[i], UriKind.Absolute, out var url))
                    {
                        Console.WriteLine($"ERROR: {args[i]} is an invalid URL.");
                        return false;
                    }

                    if (Queue.All(n => n.Url != url))
                    {
                        var host = url.Host;
                        
                        Queue.Add(new(new Uri($"{url.Scheme}://{host}/")));

                        if (!Options.InternalDomains.Contains(host))
                        {
                            Options.InternalDomains.Add(host);
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
            "  --timeout <seconds>       Set the timeout for each request. Defaults to 30 seconds.",
            "  --screenshots             Save a screenshot for every internal webpage scan.",
            "  --full-page               Capture full page instead of just the viewport size.",
            "  --size <width>x<height>   Set the viewport size, for the screenshots and accessibility checks. Defaults to 1920x1080.",
            "  --load <file>             Load a queue file, but only process the entries that failed.", // TODO 
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
            Queue[0].Url.Host,
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