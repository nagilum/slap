namespace Slap.Models;

public class QueueEntry(Uri url, EntryType entryType) : IQueueEntry
{
    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Finished"/>
    /// </summary>
    public DateTime? Finished { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Id"/>
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Started"/>
    /// </summary>
    public DateTime? Started { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Type"/>
    /// </summary>
    public EntryType Type { get; } = entryType;

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Responses"/>
    /// </summary>
    public List<IQueueResponse> Responses { get; } = [];

    /// <summary>
    /// <inheritdoc cref="IQueueEntry.Url"/>
    /// </summary>
    public Uri Url { get; } = url;
}