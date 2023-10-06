using System.Text.Json;
using Microsoft.Playwright;
using Slap.Models;
using Slap.Tools;

namespace Slap.Core;

internal class Scanner
{
    #region Properties and constructor
    
    /// <summary>
    /// Playwright browser.
    /// </summary>
    private IBrowser? _browser { get; set; }
    
    /// <summary>
    /// Playwright page.
    /// </summary>
    private IPage? _page { get; set; }

    /// <summary>
    /// Options.
    /// </summary>
    private readonly Options _options;

    /// <summary>
    /// Queue.
    /// </summary>
    private readonly List<QueueEntry> _queue;

    /// <summary>
    /// Initialize a new instance of a <see cref="Scanner"/> class.
    /// </summary>
    /// <param name="url">Initial URL.</param>
    /// <param name="options">Options.</param>
    public Scanner(Uri url, Options options)
    {
        this._options = options;
        this._queue = new List<QueueEntry>
        {
            new()
            {
                UrlType = UrlType.Webpage,
                Url = url
            }
        };
    }

    #endregion

    #region Public functions

    /// <summary>
    /// Process the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ProcessQueue(CancellationToken cancellationToken)
    {
        /*
         * 2xx status code
         * Favicon
         * Console logs
         * Verify HTML title tag.
         * Verify HTML meta description.
         * Verify HTML meta keywords.
         * Check asset links
         * Check external links
         */

        var queueIndex = -1;

        while (!cancellationToken.IsCancellationRequested)
        {
            queueIndex++;

            if (queueIndex == this._queue.Count)
            {
                break;
            }

            var queueEntry = this._queue[queueIndex];
            
            throw new NotImplementedException();            
        }
    }

    /// <summary>
    /// Install and setup Playwright.
    /// </summary>
    /// <returns>Success.</returns>
    public async Task<bool> SetupPlaywright()
    {
        try
        {
            Microsoft.Playwright.Program.Main(
                new string[]
                {
                    "install"
                });

            var instance = await Playwright.CreateAsync();
            
            this._browser = this._options.RenderingEngine switch
            {
                RenderingEngine.Chromium => await instance.Chromium.LaunchAsync(
                    this._options.PlaywrightConfig?.BrowserTypeLaunchOptions),
                
                RenderingEngine.Firefox => await instance.Firefox.LaunchAsync(
                    this._options.PlaywrightConfig?.BrowserTypeLaunchOptions),
                
                RenderingEngine.Webkit => await instance.Webkit.LaunchAsync(
                    this._options.PlaywrightConfig?.BrowserTypeLaunchOptions),
                
                _ => throw new Exception(
                    "Invalid rendering engine value.")
            };

            this._page = await this._browser.NewPageAsync(
                this._options.PlaywrightConfig?.BrowserNewPageOptions);

            return true;
        }
        catch (Exception ex)
        {
            ConsoleEx.Write(
                "Error while setting up Playwright. ",
                Environment.NewLine,
                ex.Message,
                Environment.NewLine);

            return false;
        }
    }

    /// <summary>
    /// Write logs to disk.
    /// </summary>
    public async Task WriteLogs()
    {
        var path = Path.Combine(
            this._options.LogPath,
            "logs",
            this._queue[0].Url.Host);

        // Write options to disk.
        await this.WriteLog(path, "options.json", this._options);

        // Write queue to disk.
        await this.WriteLog(path, "queue.json", this._queue);
    }

    #endregion

    #region Helper functions

    /// <summary>
    /// Write a single log to disk.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <param name="filename">Filename.</param>
    /// <param name="data">Data to write.</param>
    /// <typeparam name="T">Data-type.</typeparam>
    private async Task WriteLog<T>(
        string path,
        string filename,
        T data)
    {
        var fillPath = Path.Combine(
            path,
            filename);
        
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            await using var stream = File.OpenWrite(fillPath);
            await JsonSerializer.SerializeAsync(
                stream,
                data,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

            ConsoleEx.Write(
                "Wrote to file ",
                ConsoleColor.Yellow,
                fillPath,
                Environment.NewLine);
        }
        catch (Exception ex)
        {
            ConsoleEx.Write(
                "Error while writing to file ",
                ConsoleColor.Yellow,
                fillPath,
                Environment.NewLine,
                ConsoleColor.Red,
                ex.Message,
                Environment.NewLine);
        }
    }

    #endregion
}