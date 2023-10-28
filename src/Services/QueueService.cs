using Serilog;
using Slap.Core;
using Slap.Services.Interfaces;

namespace Slap.Services;

public class QueueService : IQueueService
{
    /// <summary>
    /// Scanner service.
    /// </summary>
    private readonly IScannerService _scanner;

    /// <summary>
    /// Initialize a new instance of a <see cref="QueueService"/> class.
    /// </summary>
    public QueueService()
    {
        this._scanner = new ScannerService();
    }

    /// <summary>
    /// <inheritdoc cref="IQueueService.ProcessQueue"/>
    /// </summary>
    public async Task ProcessQueue(CancellationToken cancellationToken)
    {
        Log.Information("Setting up Playwright..");
        
        if (await this._scanner.SetupPlaywright() == false)
        {
            return;
        }

        var index = -1;
        var reset = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            reset++;

            if (reset == 50)
            {
                Log.Information("Refreshing Playwright..");
                
                if (await this._scanner.SetupPlaywright() == false)
                {
                    Log.Error("Playwright refresh failed. Aborting!");
                    break;
                }

                reset = 0;
            }
            
            var entry = Program.Queue
                .FirstOrDefault(n => !n.Processed);

            if (entry is null)
            {
                Log.Information("No more entries in queue. Stopping.");
                break;
            }

            index++;
            
            Log.Information(
                "Processing {index} of {total} : {url}",
                index + 1,
                Program.Queue.Count,
                entry.Url.ToString().Replace(" ", "%20"));

            await this._scanner.PerformRequest(entry, cancellationToken);
        }
    }
}