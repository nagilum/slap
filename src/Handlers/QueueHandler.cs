using Slap.Models;

namespace Slap.Handlers;

public class QueueHandler(IOptions options) : IQueueHandler
{
    /// <summary>
    /// Request handler.
    /// </summary>
    private readonly RequestHandler _requestHandler = new(options);
    
    /// <summary>
    /// <inheritdoc cref="IQueueHandler.ProcessQueue"/>
    /// </summary>
    public async Task ProcessQueue(CancellationToken cancellationToken)
    {
        foreach (var uri in options.InitialUrls)
        {
            Globals.QueueEntries.Add(new(uri, EntryType.HtmlDocument));
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var entries = Globals.QueueEntries
                .Where(n => !n.Finished.HasValue)
                .ToList();

            if (entries.Count is 0)
            {
                break;
            }

            try
            {
                await Parallel.ForEachAsync(
                    entries,
                    cancellationToken,
                    async (entry, token) =>
                    {
                        entry.Started = DateTime.Now;
                        
                        await _requestHandler.PerformHttpClientRequest(entry, token);

                        if (entry.Type is EntryType.HtmlDocument)
                        {
                            await _requestHandler.PerformPlaywrightRequest(BrowserType.Chromium, entry, token);
                            await _requestHandler.PerformPlaywrightRequest(BrowserType.Firefox, entry, token);
                            await _requestHandler.PerformPlaywrightRequest(BrowserType.Webkit, entry, token);                            
                        }
                        
                        entry.Finished = DateTime.Now;
                    });
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
        
        Globals.Finished = DateTime.Now;
    }

    /// <summary>
    /// <inheritdoc cref="IQueueHandler.Setup"/>
    /// </summary>
    public async Task<bool> Setup(string? reportPath, CancellationToken cancellationToken)
    {
        if (reportPath is null)
        {
            return false;
        }
        
        try
        {
            _requestHandler.ScreenshotPath = Path.Combine(reportPath, "screenshots");
        }
        catch (Exception ex)
        {
            this.WriteError(ex, "Error while creating screenshot path.");
            return false;
        }
        
        try
        {
            _requestHandler.SetupHttpClient();
        }
        catch (Exception ex)
        {
            this.WriteError(ex, "Error while setting up HTTP client!");
            return false;
        }

        try
        {
            await _requestHandler.SetupPlaywright(cancellationToken);
        }
        catch (Exception ex)
        {
            this.WriteError(ex, "Error while setting up Playwright browser instances!");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Write error and exception info to console.
    /// </summary>
    /// <param name="ex">Exception.</param>
    /// <param name="message">Error message.</param>
    private void WriteError(Exception ex, string message)
    {
        Console.WriteLine(message);

        while (true)
        {
            Console.WriteLine($"Exception: {ex.Message}");

            if (ex.InnerException is null)
            {
                break;
            }

            ex = ex.InnerException;
        }
    }
}