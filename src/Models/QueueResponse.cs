using System.Globalization;
using Slap.Models.Interfaces;

namespace Slap.Models;

public class QueueResponse : IQueueResponse
{
    #region Fields
    
    /// <summary>
    /// Culture, for formatting.
    /// </summary>
    private readonly CultureInfo _culture;
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// <inheritdoc cref="IQueueResponse.ContentType"/>
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.DocumentTitle"/>
    /// </summary>
    public string? DocumentTitle { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.Headers"/>
    /// </summary>
    public Dictionary<string, string?>? Headers { get; init; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.MetaTags"/>
    /// </summary>
    public List<MetaTag>? MetaTags { get; set; }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.Size"/>
    /// </summary>
    public int? Size { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IQueueResponse.StatusCode"/>
    /// </summary>
    public int? StatusCode { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IQueueResponse.StatusDescription"/>
    /// </summary>
    public string? StatusDescription { get; init; }
    
    /// <summary>
    /// <inheritdoc cref="IQueueResponse.Time"/>
    /// </summary>
    public long? Time { get; init; }
    
    #endregion
    
    #region Constructor

    /// <summary>
    /// Initialize a new instance of a <see cref="QueueResponse"/> class.
    /// </summary>
    public QueueResponse()
    {
        this._culture = new CultureInfo("en-US");
    }
    
    #endregion
    
    #region Implementation functions

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.GetSizeFormatted"/>
    /// </summary>
    public string? GetSizeFormatted()
    {
        if (!this.Size.HasValue)
        {
            return null;
        }

        var text = this.Size switch
        {
            > 1000000 => $"{(this.Size.Value / 1000000M).ToString("#.##", this._culture)} MB",
            > 1000 => $"{(this.Size.Value / 1000M).ToString("#.##", this._culture)} KB",
            _ => $"{this.Size.Value} B"
        };

        return text;
    }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.GetStatusFormatted"/>
    /// </summary>
    public string? GetStatusFormatted()
    {
        var text = this.StatusCode is not null
            ? $"{this.StatusCode} {this.StatusDescription}".Trim()
            : null;

        return text;
    }

    /// <summary>
    /// <inheritdoc cref="IQueueResponse.GetTimeFormatted"/>
    /// </summary>
    public string? GetTimeFormatted()
    {
        if (!this.Time.HasValue)
        {
            return null;
        }

        var text = this.Time switch
        {
            > 60 * 1000 => $"{(this.Time.Value / (60M * 1000M)).ToString(this._culture)} m",
            > 1000 => $"{(this.Time.Value / 1000M).ToString(this._culture)} s",
            _ => $"{this.Time.Value} ms"
        };

        return text;
    }
    
    #endregion
}