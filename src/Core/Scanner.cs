using Slap.Models;

namespace Slap.Core;

internal class Scanner
{
    #region Properties and constructor
    
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
        throw new NotImplementedException();
        
        /*
          * 2xx status code
          * Favicon
          * Console logs
          * Verify HTML title tag.
          * Verify HTML meta description.
          * Verify HTML meta keywords.
          * Asset links
          * External links
        */
    }

    /// <summary>
    /// Install and setup Playwright.
    /// </summary>
    /// <returns>Success.</returns>
    public async Task<bool> SetupPlaywright()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Write logs to disk.
    /// </summary>
    public async Task WriteLogs()
    {
        throw new NotImplementedException();
    }
    
    #endregion
    
    #region Helper functions
    #endregion
}