﻿using System.Reflection;

namespace Slap
{
    public static class Program
    {
        /// <summary>
        /// Analyzed app options.
        /// </summary>
        public static CmdArgs AppOptions { get; set; } = null!;

        /// <summary>
        /// Queue of items to scan.
        /// </summary>
        public static List<QueueEntry> QueueEntries { get; set; } = new();

        /// <summary>
        /// Init all the things..
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        private static async Task Main(string[] args)
        {
            // Analyze the command-line arguments.
            try
            {
                AppOptions = new CmdArgs(args);
            }
            catch (ConsoleObjectsException ex)
            {
                ConsoleEx.WriteException(ex);
                return;
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteException(ex);
                return;
            }

            // Do we need to show app options?
            if (AppOptions.ShowAppOptions)
            {
                ShowAppOptions();
                return;
            }

            // Make sure Playwright is setup correctly.
            if (!SetupPlaywright())
            {
                return;
            }

            // Init the scanner.
            await Scanner.Init();

            // Write JSON report.
            await JsonReport.Create();

            // Write HTML report.
            await HtmlReport.Create();
        }

        /// <summary>
        /// Get the full path for the current report.
        /// </summary>
        /// <returns>Path.</returns>
        public static string GetReportPath()
        {
            var path = Path.Combine(
                AppOptions.ReportPath,
                "reports",
                AppOptions.BaseUri.Host,
                Scanner.ScanStarted?.ToString("yyyy-MM-dd-HH-mm-ss") ?? DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        /// Get the assembly version.
        /// </summary>
        /// <returns>Version.</returns>
        public static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;

            if (version == null)
            {
                throw new Exception(
                    "Panic. Unable to get assembly version.");
            }

            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        /// <summary>
        /// Make sure Playwright is setup correctly by running its installer.
        /// If it's already installed, it will be omitted.
        /// </summary>
        private static bool SetupPlaywright()
        {
            try
            {
                Console.WriteLine("Setting up Playwright..");
                Console.WriteLine();

                Microsoft.Playwright.Program.Main(
                    new string[]
                    {
                        "install"
                    });

                return true;
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteException(ex);
                return false;
            }
        }

        /// <summary>
        /// Show the app options.
        /// </summary>
        private static void ShowAppOptions()
        {
            ConsoleEx.WriteObjects(
                "Slap v",
                GetVersion(),
                Environment.NewLine,
                Environment.NewLine,
                "Usage:",
                Environment.NewLine,
                "  slap <url> [<options>]",
                Environment.NewLine,
                Environment.NewLine,
                "Options:",
                Environment.NewLine);

            // Timeout, in milliseconds, to use for each request.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -t",
                ConsoleColor.Green,
                " <milliseconds>   ",
                (byte) 0x00,
                "Timeout to use for each request. Pass 0 to disable timeout.",
                Environment.NewLine);

            // Set the referer for each request.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -r",
                ConsoleColor.Green,
                " <url>            ",
                (byte) 0x00,
                "Set the referer for each request. If used with the ",
                ConsoleColor.Blue,
                "-rp",
                (byte) 0x00,
                " param, this will only be used for the first request.",
                Environment.NewLine);

            // Enable to set referer for each request to the parent the link was found on.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -rp                 ",
                (byte) 0x00,
                "Enable to set referer for each request to the parent the link was found on.",
                Environment.NewLine);

            // Set the report path.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -p",
                ConsoleColor.Green,
                " <path>           ",
                (byte) 0x00,
                "Set the report path. Defaults to working directory.",
                Environment.NewLine);

            // Set Firefox as the rendering engine.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -ff                 ",
                (byte) 0x00,
                "Set Firefox as the rendering engine. Defaults to Chromium.",
                Environment.NewLine);

            // Set Webkit as the rendering engine.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -wk                 ",
                (byte)0x00,
                "Set Webkit as the rendering engine. Defaults to Chromium.",
                Environment.NewLine);

            // Specify the user agent to use for all requests.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -ua",
                ConsoleColor.Green,
                " <agent>         ",
                (byte)0x00,
                "Specify the user agent to use for all requests.",
                Environment.NewLine);

            // Verify that a header exists.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -vh",
                ConsoleColor.Green,
                " <header>        ",
                (byte)0x00,
                "Verify that a header exists.",
                Environment.NewLine);

            // Verify that a header and value exists.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -vh",
                ConsoleColor.Green,
                " <header:value>  ",
                (byte)0x00,
                "Verify that a header and value exists. Value can be regex.",
                Environment.NewLine);

            // Add request header and value.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -rh",
                ConsoleColor.Green,
                " <header:value>  ",
                (byte)0x00,
                "Add request header and value.",
                Environment.NewLine);

            // Add HTTP authentication username and password credentials.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -hac",
                ConsoleColor.Green,
                " <user:pwd>     ",
                (byte)0x00,
                "Add HTTP authentication username and password credentials.",
                Environment.NewLine);

            // Warn if HTML title tag is missing or empty.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -wht                ",
                (byte)0x00,
                "Warn if HTML title tag is missing or empty.",
                Environment.NewLine);

            // Warn if HTML meta keywords tag is missing or empty.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -whk                ",
                (byte)0x00,
                "Warn if HTML meta keywords tag is missing or empty.",
                Environment.NewLine);

            // Warn if HTML meta description tag is missing or empty.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -whd                ",
                (byte)0x00,
                "Warn if HTML meta description tag is missing or empty.",
                Environment.NewLine);

            // Bypass Content-Security-Policy.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -bcsp               ",
                (byte)0x00,
                "Bypass Content-Security-Policy.",
                Environment.NewLine);

            // When to consider the request operation succeeded.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -wu",
                ConsoleColor.Green,
                " <state>         ",
                (byte)0x00,
                "When to consider the request operation succeeded. Defaults to 'load'. Possible states are:",
                Environment.NewLine,
                "                       * ",
                ConsoleColor.Green,
                "domcontentloaded",
                (byte)0x00,
                " - When the DOMContentLoaded event is fired.",
                Environment.NewLine,
                "                       * ",
                ConsoleColor.Green,
                "load",
                (byte)0x00,
                " - When the load event is fired.",
                Environment.NewLine,
                "                       * ",
                ConsoleColor.Green,
                "networkidle",
                (byte)0x00,
                " - When there are no network connections for at least 500 ms.",
                Environment.NewLine,
                "                       * ",
                ConsoleColor.Green,
                "commit",
                (byte)0x00,
                " - When network response is received and the document started loading.",
                Environment.NewLine);
        }
    }
}