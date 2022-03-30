using System.Reflection;

[assembly:AssemblyVersion("1.0.*")]
namespace Slap
{
    public static class Program
    {
        /// <summary>
        /// When the app started.
        /// </summary>
        public static DateTimeOffset AppStarted { get; set; } = DateTimeOffset.Now;

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
            QueueEntries.Add(new QueueEntry(AppOptions.BaseUri));

            ConsoleEx.WriteObjects(
                "Scan started ",
                ConsoleColor.Blue,
                AppStarted,
                Environment.NewLine);

            await Scanner.Init();

            var end = DateTimeOffset.Now;

            ConsoleEx.WriteObjects(
                "Scan ended ",
                ConsoleColor.Blue,
                end,
                Environment.NewLine);

            var duration = end - AppStarted;

            ConsoleEx.WriteObjects(
                "Scan took ",
                ConsoleColor.Blue,
                duration,
                Environment.NewLine);

            // Write JSON report.
            // TODO

            // Write HTML report.
            // TODO
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
                AppStarted.ToString("yyyy-MM-dd-HH-mm-ss"));

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
        private static string GetVersion()
        {
            return Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString()
                   ?? "0.1";
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

            // Wait for CSS selector before taking screenshots.
            ConsoleEx.WriteObjects(
                ConsoleColor.Blue,
                "  -ws",
                ConsoleColor.Green,
                " <selector>      ",
                (byte) 0x00,
                "Wait for CSS selector before taking screenshots.",
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
        }
    }
}