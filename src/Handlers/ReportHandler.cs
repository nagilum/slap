using System.Reflection;
using System.Text;
using System.Text.Json;
using Slap.Extenders;
using Slap.Models;

namespace Slap.Handlers;

public class ReportHandler(IOptions options) : IReportHandler
{
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
        
        Console.WriteLine("Writing reports to:");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(this.ReportPath);

        this.CopyIcons();
        
        await this.WriteQueueToJson();
        await this.WriteHtmlEntryReports();
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
                "warning.png",
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
    /// Write an HTML report for each queue entry.
    /// </summary>
    private async Task WriteHtmlEntryReports()
    {
        try
        {
            Console.ResetColor();
            Console.Write("· ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Queue entry reports to HTML");

            var path = Path.Combine(this.ExecPath!, "entry.html");
            var html = await File.ReadAllTextAsync(path, Encoding.UTF8);

            path = Path.Combine(this.ReportPath!, "entries");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            foreach (var entry in Globals.QueueEntries)
            {
                var reportHtml = this.CompileEntryReport(entry, html);
                var file = Path.Combine(path, $"entry-{entry.Id}.html");

                await File.WriteAllTextAsync(
                    file,
                    reportHtml,
                    Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            this.WriteError(ex, "Error while writing HTML report!");
        }
    }
    
    /// <summary>
    /// Write an overview HTML report.
    /// </summary>
    private async Task WriteHtmlReport()
    {
        try
        {
            Console.ResetColor();
            Console.Write("· ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Main report to HTML");
            
            const string filename = "report.html";
            
            var path = Path.Combine(this.ExecPath!, filename);
            var html = await File.ReadAllTextAsync(path, Encoding.UTF8);

            this.CompileReport(ref html);

            path = Path.Combine(this.ReportPath!, filename);

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
            Console.ResetColor();
            Console.Write("· ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Queue entries to JSON");
            
            var path = Path.Combine(this.ReportPath!, "queue.json");

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
    /// Compile and replace all fields in the overview report.
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
    /// Compile and replace all fields in the entry report.
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    /// <param name="html">HTML source.</param>
    /// <returns>New HTML source.</returns>
    private string CompileEntryReport(QueueEntry entry, string html)
    {
        var dict = new Dictionary<string, string>
        {
            {"Url", entry.Url.ToString()},
            {"Started", entry.Started?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-"},
            {"Finished", entry.Finished?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-"}
        };

        foreach (var response in entry.Responses)
        {
            dict.Add($"{response.BrowserType}Response", this.CompileResponseEntry(response));
        }

        var output = html;

        foreach (var (key, value) in dict)
        {
            output = output.Replace("{{" + key + "}}", value);
        }

        output = output.Replace("{{HttpClientResponse}}", string.Empty);
        output = output.Replace("{{ChromiumResponse}}", string.Empty);
        output = output.Replace("{{FirefoxResponse}}", string.Empty);
        output = output.Replace("{{WebkitResponse}}", string.Empty);
        
        return output;
    }

    /// <summary>
    /// Compile HTML for queue response.
    /// </summary>
    /// <param name="response">Queue response.</param>
    /// <returns>HTML.</returns>
    private string CompileResponseEntry(IQueueResponse response)
    {
        var dict = new Dictionary<string, string>
        {
            {"BrowserType", response.BrowserType.ToString()},
            {"BrowserTypeLowerCase", response.BrowserType.ToString().ToLower()},
            {"Size", response.GetSizeFormatted() ?? "-"},
            {"Time", response.GetTimeFormatted() ?? "-"},
            {"ContentType", response.GetContentType() ?? "-"}
        };

        var html =
            "<h2 class=\"icon {{BrowserTypeLowerCase}}\">{{BrowserType}}</h2>" +
            "<table class=\"stats\"></tbody>" +
            "<tr><td>Status:</td><td>{{Status}}</td></tr>" +
            "<tr><td>Size:</td><td>{{Size}}</td></tr>" +
            "<tr><td>Time:</td><td>{{Time}}</td></tr>" +
            "<tr><td>Content Type:</td><td>{{ContentType}}</td></tr>" +
            "</tbody></table>" +
            "<h3>Headers</h3>" +
            "{{HeadersTable}}";

        dict.Add(
            "Status",
            response.StatusCode.HasValue
                ? $"{response.StatusCode} {response.StatusDescription}".Trim()
                : "-");

        if (response.Headers?.Count > 0)
        {
            dict.Add(
                "HeadersTable",
                "<table class=\"headers\"><tbody><tr>" +
                string.Join("</tr><tr>", response.Headers.Select(n => $"<td>{n.Key.ToLower()}</td><td>{n.Value}</td>")) +
                "</tr></tbody></table>");
        }
        else
        {
            dict.Add("HeadersTable", "<i>None</i>");
        }

        foreach (var (key, value) in dict)
        {
            html = html.Replace("{{" + key + "}}", value);
        }

        return html;
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
            "<td>{{Responses}}</td></tr>";

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

                var parts = new List<string?>();
                var failed = !response.StatusCode.HasValue ||
                             response.Error is not null;

                if (response.StatusCode.HasValue)
                {
                    parts.Add($"{response.StatusCode} {response.StatusDescription}".Trim());
                }

                parts.Add(response.GetContentType());
                parts.Add(response.GetTimeFormatted());
                parts.Add(response.GetSizeFormatted());
                parts.Add(response.Error?.Type);

                var tooltip = response.Error is not null ? response.Error.Message : string.Join(" - ", parts.Where(n => n is not null));
                var html = $"<a href=\"entries/entry-{entry.Id}.html\" target=\"_blank\" class=\"response-icon {(failed ? "failed" : string.Empty)}\" style=\"background-image: url('{filename}');\" title=\"{tooltip}\"></div>";

                responseIcons.Add(html);
            }
            
            var hasError = entry.Responses.Any(n => n.Error is not null);
            var warnings = entry.Responses.Sum(n => n.AccessibilityResult?.Incomplete.Length + n.AccessibilityResult?.Violations.Length);

            if (hasError || warnings > 0)
            {
                var tooltip = $"Errors: {(hasError ? 1 : 0)} - Warnings: {warnings}";
                var html = $"<a href=\"entries/entry-{entry.Id}.html\" target=\"_blank\" class=\"response-icon\" style=\"background-image: url('warning.png');\" title=\"{tooltip}\"></div>";
                
                responseIcons.Add(html);
            }

            var url = entry.Type is not EntryType.External && options.InternalDomains.Count is 1
                ? entry.Url.ToString()[$"{entry.Url.Scheme}://{entry.Url.Authority}".Length..]
                : entry.Url.ToString();

            rows.Add(
                template
                    .Replace("{{Url}}", $"<a href=\"{entry.Url}\">{url}</a>")
                    .Replace("{{Responses}}", responseIcons.Count is 0 ? "-" : string.Join(string.Empty, responseIcons)));
        }

        return
            "<table class=\"queue-entries\"><tbody>" +
            string.Join(string.Empty, rows) +
            "</tbody></table>";
    }
    
    #endregion
}