using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Slap.Exceptions;
using Slap.Models;
using Slap.Tools;

namespace Slap.Core;

internal static class Program
{
    /// <summary>
    /// Program version.
    /// </summary>
    public static Version ProgramVersion { get; } = new(1, 3);

    /// <summary>
    /// Report path.
    /// </summary>
    public static string ReportPath { get; set; } = null!;

    /// <summary>
    /// Generate the report path for this run.
    /// </summary>
    /// <param name="optionsReportPath">Report path, from options, if specified.</param>
    /// <param name="hostname">Hostname of the initial URL.</param>
    /// <returns>Full report path.</returns>
    private static string GenerateReportPath(string? optionsReportPath, string hostname)
    {
        var path = optionsReportPath;

        if (path is null)
        {
            var location = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                           ?? Directory.GetCurrentDirectory();

            path = Path.Combine(location, "reports");
        }

        path = Path.Combine(
            path,
            hostname,
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
            // Do nothing.
        }

        return path;
    }

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

        if (!ParseCmdArgs(args, out var url, out var options))
        {
            return;
        }

        ReportPath = GenerateReportPath(options.ReportPath, url.Host);

        var scanner = new Scanner(url, options);

        if (!await scanner.SetupPlaywright())
        {
            return;
        }

        var tokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            ConsoleEx.Write(
                Environment.NewLine,
                ConsoleColor.Red,
                "Aborted bu user!",
                Environment.NewLine,
                Environment.NewLine);
            
            try
            {
                tokenSource.Cancel();
            }
            catch
            {
                // Do nothing.
            }
            
            eventArgs.Cancel = true;
        };
        
        await scanner.ProcessQueue(tokenSource.Token);
        await scanner.WriteReports();
    }

    /// <summary>
    /// Attempt to parse cmd-args to valid options and initial URL.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="url">Initial URL.</param>
    /// <param name="options">Parsed options.</param>
    /// <returns>Success.</returns>
    private static bool ParseCmdArgs(
        IReadOnlyList<string> args,
        [NotNullWhen(true)] out Uri? url,
        out Options options)
    {
        url = null;
        options = new();
        
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
                // Set rendering engine.
                case "-re":
                    if (i == args.Count - 1)
                    {
                        ConsoleEx.WriteException(
                            ConsoleObjectsException.From(
                                "Argument ",
                                ConsoleColor.Blue,
                                args[i],
                                ConsoleColorEx.ResetColor,
                                " must be followed by a string indicating which rendering engine to use."));

                        return false;
                    }

                    switch (args[i + 1].ToLower())
                    {
                        case "chromium":
                            options.RenderingEngine = RenderingEngine.Chromium;
                            break;
                        
                        case "firefox":
                            options.RenderingEngine = RenderingEngine.Firefox;
                            break;
                        
                        case "webkit":
                            options.RenderingEngine = RenderingEngine.Webkit;
                            break;
                        
                        default:
                            ConsoleEx.WriteException(
                                ConsoleObjectsException.From(
                                    "Invalid value ",
                                    ConsoleColor.Red,
                                    args[i + 1],
                                    ConsoleColorEx.ResetColor,
                                    " for parameter ",
                                    ConsoleColor.Blue,
                                    args[i],
                                    ConsoleColorEx.ResetColor));

                            return false;
                    }

                    skip = true;
                    break;
                
                // Add a domain to be treated as an internal domain.
                case "-ad":
                    if (i == args.Count - 1)
                    {
                        ConsoleEx.WriteException(
                            ConsoleObjectsException.From(
                                "Argument ",
                                ConsoleColor.Blue,
                                args[i],
                                ConsoleColorEx.ResetColor,
                                " must be followed by a string indicating a domain to add."));

                        return false;
                    }

                    var host = args[i + 1].ToLower();

                    if (!options.InternalDomains.Contains(host))
                    {
                        options.InternalDomains.Add(host);
                    }
                    
                    skip = true;
                    break;
                
                // Set report path.
                case "-rp":
                    if (i == args.Count - 1)
                    {
                        ConsoleEx.WriteException(
                            ConsoleObjectsException.From(
                                "Argument ",
                                ConsoleColor.Blue,
                                args[i],
                                ConsoleColorEx.ResetColor,
                                " must be followed by a valid path."));

                        return false;
                    }

                    options.ReportPath = args[i + 1];
                    skip = true;
                    
                    break;
                
                // Load Playwright config.
                case "-lc":
                    if (i == args.Count - 1)
                    {
                        ConsoleEx.WriteException(
                            ConsoleObjectsException.From(
                                "Argument ",
                                ConsoleColor.Blue,
                                args[i],
                                ConsoleColorEx.ResetColor,
                                " must be followed by a valid path to a Playwright config file."));

                        return false;
                    }

                    try
                    {
                        var json = File.ReadAllText(args[i + 1])
                                   ?? throw new Exception(
                                       $"Unable to read from {args[i + 1]}");

                        var obj = JsonSerializer.Deserialize<PlaywrightConfig>(json)
                                  ?? throw new Exception(
                                      $"Unable to read Playwright config from {args[i + 1]}");

                        options.PlaywrightConfig = obj;
                    }
                    catch (Exception ex)
                    {
                        ConsoleEx.WriteException(ex);
                        return false;
                    }

                    skip = true;
                    break;
                
                // Save screenshots.
                case "-ss":
                    options.SaveScreenshots = true;
                    break;
                
                // Attempt to parse as URL.
                default:
                    if (!Uri.TryCreate(args[i], UriKind.Absolute, out var uri))
                    {
                        ConsoleEx.WriteException(
                            ConsoleObjectsException.From(
                                "Unable to parse ",
                                ConsoleColor.Red,
                                args[i],
                                ConsoleColorEx.ResetColor,
                                " to a valid URL."));

                        return false;
                    }

                    url = uri;
                    break;
            }
        }

        return url is not null;
    }

    /// <summary>
    /// Show program usage and options.
    /// </summary>
    private static void ShowProgramUsage()
    {
        ConsoleEx.Write(
            ConsoleColor.White,
            "Slap v",
            ProgramVersion,
            ConsoleColorEx.ResetColor,
            Environment.NewLine,
            Environment.NewLine,
            "Slap a site and see what falls out. A simple command-line site check tool. Slap will start with the given URL and scan outwards till it has covered all links from the same domain/subdomain.",
            Environment.NewLine,
            Environment.NewLine);
        
        ConsoleEx.Write(
            "Usage:",
            Environment.NewLine,
            "  slap ",
            ConsoleColor.Green,
            "<url> ",
            ConsoleColor.Blue,
            "[<options>]",
            ConsoleColorEx.ResetColor,
            Environment.NewLine,
            Environment.NewLine,
            "Options:",
            Environment.NewLine);
        
        ConsoleEx.Write(
            "  -re ",
            ConsoleColor.Blue,
            "<engine>   ",
            ConsoleColorEx.ResetColor,
            "Set rendering engine. Defaults to Chromium.",
            Environment.NewLine);
        
        ConsoleEx.Write(
            "  -ad ",
            ConsoleColor.Blue,
            "<domain>   ",
            ConsoleColorEx.ResetColor,
            "Add a domain to be treated as an internal domain.",
            Environment.NewLine);
        
        ConsoleEx.Write(
            "  -rp ",
            ConsoleColor.Blue,
            "<path>     ",
            ConsoleColorEx.ResetColor,
            "Set report path. Defaults to ",
            ConsoleColor.Yellow,
            "current directory/reports",
            ConsoleColorEx.ResetColor,
            ".",
            Environment.NewLine);
        
        ConsoleEx.Write(
            "  -lc ",
            ConsoleColor.Blue,
            "<path>     ",
            ConsoleColorEx.ResetColor,
            "Load Playwright config file. See documentation for structure.",
            Environment.NewLine);
        
        ConsoleEx.Write(
            "  -ss            ",
            ConsoleColorEx.ResetColor,
            "Save screenshot of each webpage URL.",
            Environment.NewLine,
            Environment.NewLine);
        
        ConsoleEx.Write(
            "Source and documentation over at ",
            ConsoleColor.Blue,
            "https://github.com/nagilum/slap",
            Environment.NewLine,
            Environment.NewLine);
    }
}