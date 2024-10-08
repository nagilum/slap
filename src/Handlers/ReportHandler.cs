using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Slap.Extenders;
using Slap.Models;

namespace Slap.Handlers;

public class ReportHandler(IOptions options) : IReportHandler
{
    /// <summary>
    /// Culture, for formatting.
    /// </summary>
    private readonly CultureInfo _culture = new("en-US");
    
    /// <summary>
    /// JSON serializer options.
    /// </summary>
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    /// <summary>
    /// Executable path.
    /// </summary>
    private string? ExecPath { get; set; }
    
    /// <summary>
    /// <inheritdoc cref="IReportHandler.ReportPath"/>
    /// </summary>
    public string? ReportPath { get; set; }
    
    /// <summary>
    /// <inheritdoc cref="IReportHandler.Setup"/>
    /// </summary>
    public bool Setup()
    {
        try
        {
            this.ExecPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                            ?? throw new Exception("Unable to determine executing assembly location.");
        }
        catch (Exception ex)
        {
            this.WriteError(ex, "Error while figuring out executable directory!");
            return false;
        }
        
        try
        {
            this.ReportPath = Path.Combine(
                options.ReportPath,
                "reports",
                options.InitialUrls.First().Authority.ToLower().Replace(":", "-"),
                DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

            if (!Directory.Exists(this.ReportPath))
            {
                Directory.CreateDirectory(this.ReportPath);
            }
        }
        catch (Exception ex)
        {
            this.WriteError(ex, "Error while creating report directory!");
            return false;
        }

        return true;
    }

    /// <summary>
    /// <inheritdoc cref="IReportHandler.WriteReports"/>
    /// </summary>
    public async Task WriteReports()
    {
        Console.ResetColor();
        Console.CursorTop = 7 + Globals.ResponseTypeCounts.Count;
        Console.CursorLeft = 0;
        
        Console.Write("Writing reports to ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(this.ReportPath);

        this.CopyIcons();
        
        await this.WriteQueueToJson();
        await this.WriteHtmlReport();

        Console.WriteLine();
    }
    
    #region Helper functions
    
    /// <summary>
    /// Write error and exception info to console.
    /// </summary>
    /// <param name="ex">Exception.</param>
    /// <param name="message">Error message.</param>
    private void WriteError(Exception ex, string message)
    {
        Console.ResetColor();
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
    
    #endregion
    
    #region IO functions

    /// <summary>
    /// Copy icons to report path.
    /// </summary>
    private void CopyIcons()
    {
        try
        {
            var files = new[]
            {
                "chromium.png",
                "firefox.png",
                "http.png",
                "webkit.png"
            };

            foreach (var file in files)
            {
                var path = Path.Combine(
                    this.ReportPath!,
                    file);

                File.Copy(file, path);
            }
        }
        catch (Exception ex)
        {
            this.WriteError(ex, "Error while copying icons to report path!");
        }
    }
    
    /// <summary>
    /// Create an overview HTML document report.
    /// </summary>
    private async Task WriteHtmlReport()
    {
        try
        {
            const string filename = "report.html";
            
            Console.ResetColor();
            Console.Write("> ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(filename);
            
            var path = Path.Combine(
                this.ExecPath!,
                filename);

            var html = await File.ReadAllTextAsync(
                path,
                Encoding.UTF8);

            this.CompileReport(ref html);

            path = Path.Combine(
                this.ReportPath!,
                filename);

            await File.WriteAllTextAsync(
                path, 
                html,
                Encoding.UTF8);
        }
        catch (Exception ex)
        {
            this.WriteError(ex, "Error while writing HTML report!");
        }
    }
    
    /// <summary>
    /// Write queue to JSON.
    /// </summary>
    private async Task WriteQueueToJson()
    {
        try
        {
            const string filename = "queue.json";
            
            Console.ResetColor();
            Console.Write("> ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(filename);
            
            var path = Path.Combine(
                this.ReportPath!,
                filename);

            await using var stream = File.OpenWrite(path);
            await JsonSerializer.SerializeAsync(
                stream,
                Globals.QueueEntries.OrderBy(n => n.Started).ToList(),
                _jsonSerializerOptions,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            this.WriteError(ex, "Error while writing JSON queue!");
        }
    }
    
    #endregion
    
    #region Report generating functions

    /// <summary>
    /// Compile and replace all fields in the report.
    /// </summary>
    /// <param name="html">HTML source.</param>
    private void CompileReport(ref string html)
    {
        var dict = new Dictionary<string, string>
        {
            {"Host", options.InitialUrls.First().Authority},
            {"Started", Globals.Started.ToString("yyyy-MM-dd HH:mm:ss")},
            {"Finished", Globals.Finished?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-"},
            {"TotalUrls", Globals.QueueEntries.Count.ToString()},
        };
        
        // Took.
        var took = Globals.Finished is null
            ? null
            : Globals.Finished - Globals.Started;
        
        dict.Add("Took", $"<abbr title=\"{took?.ToString()}\">{took?.ToHumanReadable() ?? "-"}</abbr>");
        
        // Response types.
        var list = new List<string>();
        var responseTypeCounts = Globals.ResponseTypeCounts
            .OrderBy(n => n.Key)
            .ToDictionary(n => n.Key, n => n.Value);

        foreach (var (type, count) in responseTypeCounts)
        {
            list.Add($"<tr><td>{type}</td><td>{count}</td></tr>");
        }
        
        dict.Add("ResponseTypes", string.Join("</tr><tr>", list));
        
        // HTML documents.
        var entries = Globals.QueueEntries
            .Where(n => n.Type is EntryType.HtmlDocument)
            .OrderBy(n => n.Started)
            .ToList();
        
        var table = this.CompileQueueEntryTable(entries);
        
        dict.Add("HtmlDocuments", table ?? "<i>None</i>");
        
        // Assets.
        entries = Globals.QueueEntries
            .Where(n => n.Type is EntryType.Asset)
            .OrderBy(n => n.Started)
            .ToList();

        table = this.CompileQueueEntryTable(entries);

        dict.Add("Assets", table ?? "<i>None</i>");
        
        // External links.
        entries = Globals.QueueEntries
            .Where(n => n.Type is EntryType.External)
            .OrderBy(n => n.Started)
            .ToList();

        table = this.CompileQueueEntryTable(entries);

        dict.Add("ExternalLinks", table ?? "<i>None</i>");
        
        // Replace elements.
        foreach (var (key, value) in dict)
        {
            html = html.Replace("{{" + key + "}}", value);
        }
    }

    /// <summary>
    /// Compile HTML table for queue entry rows.
    /// </summary>
    /// <param name="entries">Queue entries.</param>
    /// <returns>HTML.</returns>
    private string? CompileQueueEntryTable(List<QueueEntry> entries)
    {
        if (entries.Count is 0)
        {
            return default;
        }
        
        var rows = new List<string>();
        const string template =
            "<tr><td>{{Url}}</td>" +
            "<td>{{Responses}}</td>" +
            "<td>{{Issues}}</td></tr>";

        foreach (var entry in entries)
        {
            var responseIcons = new List<string>();

            foreach (var response in entry.Responses)
            {
                var filename = response.BrowserType switch
                {
                    BrowserType.HttpClient => "http.png",
                    BrowserType.Chromium => "chromium.png",
                    BrowserType.Firefox => "firefox.png",
                    BrowserType.Webkit => "webkit.png",
                    _ => throw new Exception("Invalid browser type.")
                };

                var parts = new List<string>
                {
                    response.BrowserType switch
                    {
                        BrowserType.HttpClient => "HTTP GET",
                        _ => response.BrowserType.ToString()
                    }
                };

                if (response.StatusCode.HasValue)
                {
                    parts.Add($"{response.StatusCode} {response.StatusDescription}".Trim());
                }
                
                var time = this.GetTimeFormatted(response);
                var size = this.GetSizeFormatted(response);
                var error = response.Timeout ? "Connection Timed Out" : response.ErrorCode ?? response.Error;
                var contentType = response.GetContentType();

                if (contentType is not null)
                {
                    parts.Add(contentType);
                }

                if (time is not null)
                {
                    parts.Add(time);
                }

                if (size is not null)
                {
                    parts.Add(size);
                }

                if (error is not null)
                {
                    parts.Add(error);
                }

                var tooltip = string.Join(" - ", parts);
                
                var html = $"<img class=\"browser-type\" src=\"{filename}\" alt=\"{tooltip}\" title=\"{tooltip}\" />";

                responseIcons.Add(html);
            }

            var url = entry.Type is not EntryType.External &&
                      options.InternalDomains.Count is 1
                ? entry.Url.ToString()[$"{entry.Url.Scheme}://{entry.Url.Authority}".Length..]
                : entry.Url.ToString();
            
            rows.Add(
                template
                    .Replace("{{Url}}", $"<a href=\"{entry.Url}\">{url}</a>")
                    .Replace("{{Responses}}", responseIcons.Count is 0 ? "-" : string.Join(string.Empty, responseIcons))
                    .Replace("{{Issues}}", "-"));
        }

        return
            "<table class=\"queue-entries\"><thead><tr>" +
            "</tr></thead><tbody>" +
            string.Join(string.Empty, rows) +
            "</tbody></table>";
    }

    /// <summary>
    /// Get size formatted.
    /// </summary>
    /// <param name="response">Queue response entry.</param>
    /// <returns>Size formatted.</returns>
    private string? GetSizeFormatted(IQueueResponse response)
    {
        if (response.Size is null)
        {
            return default;
        }
        
        var text = response.Size switch
        {
            > 1000000 => $"{(response.Size.Value / 1000000M).ToString("#.##", _culture)} MB",
            > 1000 => $"{(response.Size.Value / 1000M).ToString("#.##", _culture)} KB",
            _ => $"{response.Size.Value} B"
        };

        return text;
    }

    /// <summary>
    /// Get time formatted.
    /// </summary>
    /// <param name="response">Queue response entry.</param>
    /// <returns>Time formatted.</returns>
    private string? GetTimeFormatted(IQueueResponse response)
    {
        if (response.Time is null)
        {
            return default;
        }
        
        var text = response.Time switch
        {
            > 60 * 1000 => $"{(response.Time.Value / (60M * 1000M)).ToString(_culture)} mins",
            > 1000 => $"{(response.Time.Value / 1000M).ToString(_culture)} secs",
            _ => $"{response.Time.Value} ms"
        };

        return text;
    }
    
    #endregion
}