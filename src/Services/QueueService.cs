using System.Text.RegularExpressions;
using Serilog;
using Slap.Core;
using Slap.Services.Interfaces;

namespace Slap.Services;

public class QueueService : IQueueService
{
    #region Constructor, fields, and properties
    
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
    
    #endregion
    
    #region Implementation functions

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

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken
        };

        if (Program.Options.Parallelism.HasValue)
        {
            parallelOptions.MaxDegreeOfParallelism = Program.Options.Parallelism.Value;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var entries = Program.Queue
                .Where(n => !n.Processed)
                .ToList();

            if (entries.Count == 0)
            {
                break;
            }

            var skipping = 0;

            foreach (var entry in entries)
            {
                var skip = Program.Options.UrlTypesToSkip.Contains(entry.UrlType) ||
                           Program.Options.DomainsToSkip.Contains(entry.Url.Host.ToLower()) ||
                           Program.Options.RegExMatchesToSkip.Any(n => Regex.IsMatch(entry.Url.ToString(), n));

                if (!skip)
                {
                    continue;
                }

                index++;
                skipping++;

                if (Program.Options.LogLevel == LogLevel.Verbose)
                {
                    Log.Warning(
                        "Skipping {index} of {total} : {url}",
                        index,
                        Program.Queue.Count,
                        entry.Url.ToString().Replace(" ", "%20"));    
                }

                entry.Processed = true;
                entry.Skipped = true;
            }

            if (Program.Options.LogLevel == LogLevel.Normal && skipping > 0)
            {
                Log.Warning(
                    "Skipping {count} entries.",
                    skipping);
            }

            entries = entries
                .Where(n => !n.Skipped)
                .ToList();

            if (Program.Options.LogLevel == LogLevel.Normal)
            {
                Log.Information(
                    "Processing {count} {entries}",
                    entries.Count,
                    entries.Count == 1 ? "entry" : "entries");
            }

            await Parallel.ForEachAsync(
                entries,
                parallelOptions,
                async (entry, token) =>
                {
                    if (Program.Options.LogLevel == LogLevel.Verbose)
                    {
                        Log.Information(
                            "Processing {index} of {total} : {url}",
                            ++index,
                            Program.Queue.Count,
                            entry.Url.ToString().Replace(" ", "%20"));    
                    }
                    
                    await this._scanner.PerformRequest(entry, token);
                });
        }

        await this._scanner.DisposePlaywright();
    }
    
    #endregion
}