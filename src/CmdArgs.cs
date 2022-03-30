namespace Slap
{
    public class CmdArgs
    {
        /// <summary>
        /// Rendering engines supported by Playwright.
        /// </summary>
        public enum RenderingEngineType
        {
            Chromium,
            Firefox,
            Webkit
        }

        /// <summary>
        /// Base URL to start scan.
        /// </summary>
        public Uri BaseUri { get; set; } = null!;

        /// <summary>
        /// Timeout, in milliseconds, to use for each request.
        /// </summary>
        public float ConnectionTimeout { get; set; } = 10000;

        /// <summary>
        /// Set to use parent as referer.
        /// </summary>
        public bool UseParentAsReferer { get; set; }

        /// <summary>
        /// The base report path.
        /// </summary>
        public string ReportPath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// Rendering engine to use.
        /// </summary>
        public RenderingEngineType RenderingEngine { get; set; } = RenderingEngineType.Chromium;

        /// <summary>
        /// Headers to verify.
        /// </summary>
        public Dictionary<string, string?> HeadersToVerify { get; set; } = new();

        /// <summary>
        /// Whether to show the app options.
        /// </summary>
        public bool ShowAppOptions { get; set; } = true;

        /// <summary>
        /// Analyze the command-line arguments.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        public CmdArgs(string[] args)
        {
            if (args == null ||
                args.Length == 0 ||
                args.Any(n => n == "-h"))
            {
                return;
            }

            // Base URL.
            try
            {
                this.BaseUri = new Uri(args[0]);
                this.ShowAppOptions = false;
            }
            catch (Exception ex)
            {
                throw new ConsoleObjectsException(
                    "Unable to parse ",
                    ConsoleColor.Blue,
                    args[0],
                    (byte) 0x00,
                    " to a proper URL.",
                    Environment.NewLine,
                    ex.Message);
            }

            // Remaining params.
            for (var i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    // Timeout, in milliseconds, to use for each request.
                    case "-t":
                        if (i == args.Length - 1)
                        {
                            throw new ConsoleObjectsException(
                                "Argument ",
                                ConsoleColor.Blue,
                                "-t ",
                                (byte) 0x00,
                                "Must be followed by a number of milliseconds.");
                        }

                        if (!float.TryParse(args[i + 1], out float timeout))
                        {
                            throw new ConsoleObjectsException(
                                "Argument ",
                                ConsoleColor.Blue,
                                args[i + 1],
                                (byte) 0x00,
                                " cannot be parsed.");
                        }

                        this.ConnectionTimeout = timeout;
                        break;

                    // Enable to set referer for each request to the parent the link was found on.
                    case "-rp":
                        this.UseParentAsReferer = true;
                        break;

                    // Set the report path.
                    case "-p":
                        if (i == args.Length - 1)
                        {
                            throw new ConsoleObjectsException(
                                "Argument ",
                                ConsoleColor.Blue,
                                "-p ",
                                (byte) 0x00,
                                "Must be followed by a valid path.");
                        }

                        if (!Directory.Exists(args[i + 1]))
                        {
                            throw new ConsoleObjectsException(
                                "The path ",
                                ConsoleColor.Blue,
                                args[i + 1],
                                (byte) 0x00,
                                " does not exist.");
                        }

                        this.ReportPath = args[i + 1];
                        break;

                    // Set Firefox as the rendering engine.
                    case "-ff":
                        this.RenderingEngine = RenderingEngineType.Firefox;
                        break;

                    // Set Webkit as the rendering engine.
                    case "-wk":
                        this.RenderingEngine = RenderingEngineType.Webkit;
                        break;

                    // Verify that a header exists.
                    // Verify that a header and value exists.
                    case "-vh":
                        if (i == args.Length - 1)
                        {
                            throw new ConsoleObjectsException(
                                "Argument ",
                                ConsoleColor.Blue,
                                "-vh ",
                                (byte) 0x00,
                                "Must be followed by a header name and optional value.");
                        }

                        var value = args[i + 1];
                        var sp = value.IndexOf(':');

                        var key = sp == -1
                            ? value
                            : value.Substring(0, sp);

                        value = sp == -1
                            ? null
                            : value.Substring(sp + 1);

                        this.HeadersToVerify[key] = value;
                        break;
                }
            }
        }
    }
}