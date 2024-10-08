using System.Collections.Concurrent;
using Slap.Models;

namespace Slap;

public static class Globals
{
    /// <summary>
    /// Queue entries.
    /// </summary>
    public static readonly ConcurrentBag<QueueEntry> QueueEntries = [];

    /// <summary>
    /// Response type counts.
    /// </summary>
    public static readonly ConcurrentDictionary<string, int> ResponseTypeCounts = [];
    
    /// <summary>
    /// When scanning started.
    /// </summary>
    public static DateTime Started { get; } = DateTime.Now;
    
    /// <summary>
    /// When scanning finished.
    /// </summary>
    public static DateTime? Finished { get; set; }
}