using Slap.Handlers;
using Slap.Models;

namespace Slap;

public static class Program
{
    /// <summary>
    /// Program name.
    /// </summary>
    public const string Name = "Slap";

    /// <summary>
    /// Program version.
    /// </summary>
    public const string Version = "2.0-alpha";
    
    /// <summary>
    /// Init all the things...
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    private static async Task Main(string[] args)
    {
        if (args.Length is 0 ||
            args.Any(n => n is "-h" or "--help"))
        {
            ShowProgramUsage();
            return;
        }

        if (!TryParseCmdArgs(args, out var options))
        {
            return;
        }
        
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        var uiHandler = new UiHandler();
        var queueHandler = new QueueHandler(options);
        var reportHandler = new ReportHandler(options);
        
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            tokenSource.Cancel();
        };
        
        Console.CursorVisible = false;
        Console.WriteLine($"{Name} v{Version}");
        
        if (!reportHandler.Setup() ||
            !await queueHandler.Setup(reportHandler.ReportPath, token))
        {
            return;
        }
        
        uiHandler.Setup(token);
        
        await queueHandler.ProcessQueue(token);
        await reportHandler.WriteReports();
        
        if (!token.IsCancellationRequested)
        {
            await tokenSource.CancelAsync();    
        }
        
        uiHandler.UpdateUi();
        
        Console.ResetColor();
        Console.CursorTop = 10 + Globals.ResponseTypeCounts.Count;
        Console.CursorLeft = 0;
    }

    /// <summary>
    /// Show program usage and options.
    /// </summary>
    private static void ShowProgramUsage()
    {
        var lines = new[]
        {
            $"{Name} v{Version}",
            "Slap a site and see what falls out. A simple program to assist in QA checking of a site.",
            "",
            "If you slap https://example.com it will crawl all URLs found, both internal and external, but not move beyond the initial domain. After it is done, it will generate a report.",
            "",
            "Usage:",
            $"  {Name.ToLower()} <url> [<options>]",
            "",
            "Options:",
            "  --add <domain>            Add a domain to be treated as another internal domain. Such as cdn.example.com.",
            "  --path <path>             Set path to save report to. Defaults to current directory.",
            "  --skip <type>             Skip scanning of certain types of links. Options are assets and external.",
            "  --skip <regex>            Skip any URL that matches the given regular expression.",
            "  --timeout <seconds>       Set the timeout for each request. Defaults to 10 seconds.",
            "  --screenshots             Save a screenshot for every internal webpage scan.",
            "  --full-page               Capture full page screenshots instead of just the viewport.",
            "  --size <width>x<height>   Set the viewport size, for the screenshots and accessibility checks. Defaults to 1920x1080.",
            "  --follow-redirects        Allows the program to follow redirection responses, such as 301.",
            "",
            "Source and documentation available at https://github.com/nagilum/slap"
        };

        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }
    }

    /// <summary>
    /// Attempt to parse command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="options">Parsed options.</param>
    /// <returns>Success.</returns>
    private static bool TryParseCmdArgs(string[] args, out IOptions options)
    {
        options = new Options
        {
            BrowserNewPageOptions = new()
            {
                ViewportSize = new()
                {
                    Height = 1080,
                    Width = 1920
                }
            },
            BrowserLaunchOptions = new(),
            PageGotoOptions = new()
            {
                Timeout = 10000
            }
        };

        var skip = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (skip)
            {
                skip = false;
                continue;
            }

            var argv = args[i];
            var value = i < args.Length - 1 ? args[i + 1] : null;

            switch (argv.ToLower())
            {
                case "-a":
                case "--add":
                    if (i == args.Length - 1)
                    {
                        Console.WriteLine($"Error: Option {argv} must be followed by a domain name.");
                        return false;
                    }

                    if (!options.InternalDomains.Contains(value!))
                    {
                        options.InternalDomains.Add(value!);
                    }
                    
                    skip = true;
                    break;
                
                case "-p":
                case "--path":
                    if (i == args.Length - 1)
                    {
                        Console.WriteLine($"Error: Option {argv} must be followed by a valid path.");
                        return false;
                    }

                    if (!Directory.Exists(value))
                    {
                        Console.WriteLine($"Error: \"{value}\" does not exist.");
                        return false;
                    }

                    options.ReportPath = value;
                    skip = true;
                    break;
                
                case "-s":
                case "--skip":
                    if (i == args.Length - 1)
                    {
                        Console.WriteLine($"Error: Option {argv} must be followed by 'assets', 'external' or a regex string.");
                        return false;
                    }

                    switch (value!.ToLower())
                    {
                        case "assets":
                            options.SkipAssets = true;
                            break;
                        
                        case "external":
                            options.SkipExternal = true;
                            break;
                        
                        default:
                            options.SkipRegexMatches.Add(value);
                            break;
                    }

                    skip = true;
                    break;
                
                case "-t":
                case "--timeout":
                    if (i == args.Length - 1)
                    {
                        Console.WriteLine($"Error: Option {argv} must be followed by a number of seconds.");
                        return false;
                    }

                    if (!int.TryParse(value, out var seconds))
                    {
                        Console.WriteLine($"Error: \"{value}\" is not a valid number of seconds.");
                        return false;
                    }

                    options.PageGotoOptions.Timeout = seconds * 1000;
                    skip = true;
                    break;
                
                case "-ss":
                case "--screenshots":
                    options.SaveScreenshots = true;
                    break;
                
                case "-fp":
                case "--full-page":
                    options.SaveScreenshots = true;
                    options.SaveFullPageScreenshots = true;
                    break;
                
                case "-z":
                case "--size":
                    if (i == args.Length - 1)
                    {
                        Console.WriteLine($"Error: Option {argv} must be followed by a viewport size, like 1920x1080.");
                        return false;
                    }

                    var values = value!.Split('x');

                    if (values.Length is not 2 ||
                        !int.TryParse(values[0], out var width) ||
                        !int.TryParse(values[1], out var height) ||
                        width < 100 ||
                        height < 100)
                    {
                        Console.WriteLine($"Error: \"{value}\" is not a valid viewport size.");
                        return false;
                    }

                    options.ViewportHeight = height;
                    options.ViewportWidth = width;
                    skip = true;
                    break;
                
                case "-fr":
                case "--follow-redirects":
                    options.FollowRedirects = true;
                    break;
                
                default:
                    if (!Uri.TryCreate(argv, UriKind.Absolute, out var uri))
                    {
                        Console.WriteLine($"Error: \"{argv}\" is an invalid URL.");
                        return false;
                    }

                    var authority = options.InternalDomains.FirstOrDefault();

                    if (options.InternalDomains.Count > 0 && 
                        uri.Authority != authority)
                    {
                        Console.WriteLine($"Error: You can only add additional URL matching {authority}");
                        return false;
                    }

                    if (!options.InitialUrls.Contains(uri))
                    {
                        options.InitialUrls.Add(uri);                        
                    }

                    if (!options.InternalDomains.Contains(uri.Authority))
                    {
                        options.InternalDomains.Add(uri.Authority);    
                    }
                    
                    break;
            }
        }

        if (options.InitialUrls.Count is 0)
        {
            Console.WriteLine("Error: You have to provide at least one URL to scan.");
        }

        return options.InitialUrls.Count > 0;
    }
}