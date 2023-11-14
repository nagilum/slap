using System.Text.RegularExpressions;
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
        if (await this._scanner.SetupPlaywright() == false)
        {
            return;
        }

        var index = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var entries = Program.Queue
                .Where(n => !n.Processed)
                .Take(20)
                .ToList();

            if (entries.Count == 0)
            {
                break;
            }

            foreach (var entry in entries)
            {
                index++;
                
                var skip = Program.Options.UrlTypesToSkip.Contains(entry.UrlType) ||
                           Program.Options.DomainsToSkip.Contains(entry.Url.Host.ToLower()) ||
                           Program.Options.RegExMatchesToSkip.Any(n => Regex.IsMatch(entry.Url.ToString(), n));

                if (skip)
                {
                    Log.Warning(
                        "Skipping {index} of {total} : {url}",
                        index,
                        Program.Queue.Count,
                        entry.Url.ToString().Replace(" ", "%20"));

                    entry.Processed = true;
                    entry.Skipped = true;
                }
                else
                {
                    Log.Information(
                        "Processing {index} of {total} : {url}",
                        index,
                        Program.Queue.Count,
                        entry.Url.ToString().Replace(" ", "%20"));
                }
            }

            await Parallel.ForEachAsync(
                entries,
                cancellationToken,
                async (entry, token) =>
                {
                    await this._scanner.PerformRequest(entry, token);
                });
        }

        await this._scanner.DisposePlaywright();
    }
}