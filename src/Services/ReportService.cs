using System.Text;
using System.Text.Json;
using Serilog;
using Slap.Core;
using Slap.Extenders;
using Slap.Models;
using Slap.Services.Interfaces;

namespace Slap.Services;

public class ReportService : IReportService
{
    #region Implementation functions
    
    /// <summary>
    /// <inheritdoc cref="IReportService.GenerateReports"/>
    /// </summary>
    public async Task GenerateReports()
    {
        if (Program.Queue.IsEmpty)
        {
            return;
        }
        
        var path = Path.GetRelativePath(
            Directory.GetCurrentDirectory(), 
            Program.Options.ReportPath!);
        
        Log.Information(
            "Writing reports to .{separator}{path}",
            Path.DirectorySeparatorChar.ToString(),
            path);

        try
        {
            await this.WriteQueueToDisk();
            await this.GenerateSummaryReport();
            await this.GenerateQueueEntryReports();
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error while generating and writing reports to disk.");
        }
    }
    
    #endregion
    
    #region Report generating functions

    /// <summary>
    /// Add the accessibility issues table.
    /// </summary>
    private async Task AddAccessibilityIssuesTable(StringBuilder sb)
    {
        sb.AppendLine("<h2>Accessibility Issues</h2>");
        sb.AppendLine("<table><tbody>");
        
        var severities = new List<string>();

        foreach (var entry in Program.Queue)
        {
            severities.AddRange(
                from violation in entry.AccessibilityResults?.Violations ?? Array.Empty<AccessibilityResultItem>()
                select violation.Impact ?? string.Empty);
        }

        severities = severities
            .Where(n => !string.IsNullOrWhiteSpace(n.Trim()))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        if (severities.Count == 0)
        {
            sb.AppendLine("<tr><td>None</td></tr>");
        }
        else
        {
            foreach (var severity in severities)
            {
                var items = Program.Queue
                    .Where(n => n.AccessibilityResults?.Violations?.Any(m => m.Impact == severity) == true)
                    .ToList();

                var str = $"{severity[..1].ToUpper()}{severity[1..].ToLower()}";

                if (items.Any())
                {
                    await this.GenerateSubReport(
                        $"{severity[..1].ToUpper()}{severity[1..].ToLower()}",
                        $"All pages that has accessibility issues marked as {severity}",
                        $"issues{Path.DirectorySeparatorChar}{severity}.html", 
                        items);
                    
                    str = $"<a href=\"issues/{severity}.html\" target=\"_blank\">{str}</a>";
                }
                
                var cssClass = severity switch
                {
                    "critical" => "error",
                    "serious" => "error",
                    "moderate" => "warning",
                    _ => string.Empty
                };
                
                sb.AppendLine($"<tr class=\"{cssClass}\"><td>{str}</td><td>{items.Count}</td></tr>");
            }
        }

        sb.AppendLine("</tbody></table>");
    }

    /// <summary>
    /// Add the response time table.
    /// </summary>
    private async Task AddResponseTimeTable(StringBuilder sb)
    {
        sb.AppendLine("<h2>Response Times</h2>");
        sb.AppendLine("<table><tbody>");

        {
            var items = Program.Queue
                .Where(n => n.Response?.Time is >= 0 and < 300)
                .ToList();

            var str = "&lt; 300 ms";

            if (items.Any())
            {
                await this.GenerateSubReport(
                    "&lt; 300 ms",
                    "All pages and assets that had a response of less than 300 milliseconds.",
                    $"response-times{Path.DirectorySeparatorChar}less-than-300-ms.html", 
                    items);

                str = $"<a href=\"response-times/less-than-300-ms.html\" target=\"_blank\">{str}</a>";
            }
            
            sb.AppendLine($"<tr><td>{str}</td><td>{items.Count}</td></tr>");
        }

        {
            var items = Program.Queue
                .Where(n => n.Response?.Time is >= 300 and < 600)
                .ToList();
            
            var str = "&gt; 300 &amp; &lt; 600 ms";

            if (items.Any())
            {
                await this.GenerateSubReport(
                    "&gt; 300 &amp; &lt; 600 ms",
                    "All pages and assets that had a response time of less than 600 and greater than 300 milliseconds.",
                    $"response-times{Path.DirectorySeparatorChar}less-than-600-greater-than-300-ms.html", 
                    items);

                str = $"<a href=\"response-times/less-than-600-greater-than-300-ms.html\" target=\"_blank\">{str}</a>";
            }
            
            sb.AppendLine($"<tr><td>{str}</td><td>{items.Count}</td></tr>");
        }
        
        {
            var items = Program.Queue
                .Where(n => n.Response?.Time is >= 600 and < 900)
                .ToList();

            var str = "&gt; 600 &amp; &lt; 900 ms";

            if (items.Any())
            {
                await this.GenerateSubReport(
                    "&gt; 600 &amp; &lt; 900 ms",
                    "All pages and assets that had a response time of less than 900 and greater than 600 milliseconds.",
                    $"response-times{Path.DirectorySeparatorChar}less-than-900-greater-than-600-ms.html", 
                    items);
                
                str = $"<a href=\"response-times/less-than-900-greater-than-600-ms.html\" target=\"_blank\">{str}</a>";
            }
            
            var cssClass = items.Any()
                ? "warning"
                : "";
            
            sb.AppendLine($"<tr class=\"{cssClass}\"><td>{str}</td><td>{items.Count}</td></tr>");
        }
        
        {
            var items = Program.Queue
                .Where(n => n.Response?.Time is >= 900)
                .ToList();
            
            var str = "&gt; 900 ms";

            if (items.Any())
            {
                await this.GenerateSubReport(
                    "&gt; 900 ms",
                    "All pages and assets that had a response time of greater than 900 milliseconds.",
                    $"response-times{Path.DirectorySeparatorChar}greater-than-900-ms.html", 
                    items);
                
                str = $"<a href=\"response-times/greater-than-900-ms.html\" target=\"_blank\">{str}</a>";
            }
            
            var cssClass = items.Any()
                ? "error"
                : "";
            
            sb.AppendLine($"<tr class=\"{cssClass}\"><td>{str}</td><td>{items.Count}</td></tr>");
        }
        
        sb.AppendLine("</tbody></table>");
    }

    /// <summary>
    /// Add the status code table.
    /// </summary>
    private async Task AddStatusCodeTable(StringBuilder sb)
    {
        sb.AppendLine("<h2>Status Codes</h2>");
        sb.AppendLine("<table><tbody>");
        
        var statusCodes = Program.Queue
            .Select(n => n.Response?.StatusCode ?? 0)
            .Where(n => n > 0)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        foreach (var code in statusCodes)
        {
            var items = Program.Queue
                .Where(n => n.Response?.StatusCode == code)
                .ToList();
            
            var str = $"{code} {ScannerService.GetStatusDescription(code)}";

            if (items.Any())
            {
                await this.GenerateSubReport(
                    $"{code} {ScannerService.GetStatusDescription(code)}",
                    $"All pages and assets matching the response status {code} {ScannerService.GetStatusDescription(code)}",
                    $"statuses{Path.DirectorySeparatorChar}{code}.html", 
                    items);

                str = $"<a href=\"statuses/{code}.html\" target=\"_blank\">{str}</a>";
            }

            var cssClass = code switch
            {
                >= 200 and < 300 => "",
                >= 300 and < 400 => "warning",
                _ => "error"
            };

            sb.AppendLine($"<tr class=\"{cssClass}\"><td>{str}</td>");
            sb.AppendLine($"<td>{items.Count}</td></tr>");
        }

        {
            var items = Program.Queue
                .Where(n => n.Response is null && !n.Skipped)
                .ToList();

            var str = "Failed";
            
            if (items.Any())
            {
                await this.GenerateSubReport(
                    "Failed",
                    "All pages and assets that failed.",
                    $"statuses{Path.DirectorySeparatorChar}failed.html", 
                    items);

                str = "<a href=\"statuses/failed.html\" target=\"_blank\">Failed</a>";
            }
            
            var cssClass = items.Any()
                ? "error"
                : "";
            
            sb.AppendLine($"<tr class=\"{cssClass}\"><td>{str}</td>");
            sb.AppendLine($"<td>{items.Count}</td></tr>");
        }

        {
            var items = Program.Queue
                .Where(n => n.Skipped)
                .ToList();
            
            var str = "Skipped";
            
            if (items.Any())
            {
                await this.GenerateSubReport(
                    "Skipped",
                    "All pages and assets that were skipped.",
                    $"statuses{Path.DirectorySeparatorChar}skipped.html", 
                    items);

                str = "<a href=\"statuses/failed.html\" target=\"_blank\">Skipped</a>";
            }
            
            var cssClass = items.Any()
                ? "warning"
                : "";
            
            sb.AppendLine($"<tr class=\"{cssClass}\"><td>{str}</td>");
            sb.AppendLine($"<td>{items.Count}</td></tr>");
        }
        
        sb.AppendLine("</tbody></table>");
    }

    /// <summary>
    /// Add the types table.
    /// </summary>
    private async Task AddTypesTable(StringBuilder sb)
    {
        sb.AppendLine("<h2>Types</h2>");
        sb.AppendLine("<table><tbody>");

        foreach (var urlType in Enum.GetValues<UrlType>())
        {
            var title = urlType switch
            {
                UrlType.InternalPage => "Internal Pages",
                UrlType.InternalAsset => "Internal Assets",
                UrlType.ExternalPage => "External Pages",
                UrlType.ExternalAsset => "External Asset",
                _ => throw new Exception($"Invalid URL type: {urlType}")
            };
            
            var description = urlType switch
            {
                UrlType.InternalPage => "HTML pages matching the list of internal domains. This always includes the hostname of the initial URL to be scanned.",
                UrlType.InternalAsset => "All pages and assets apart from HTML pages matching the list of internal domains. This always includes the hostname of the initial URL to be scanned.",
                UrlType.ExternalPage => "HTML pages that doesn't match the list of internal domains.",
                UrlType.ExternalAsset => "All pages and assets apart from HTML pages that doesn't match the list of internal domains.",
                _ => throw new Exception($"Invalid URL type: {urlType}")
            };

            var items = Program.Queue
                .Where(n => n.UrlType == urlType)
                .ToList();

            if (items.Any())
            {
                await this.GenerateSubReport(
                    title,
                    description,
                    $"types{Path.DirectorySeparatorChar}{urlType.ToString().ToLower()}.html", 
                    items);
                
                title = $"<a href=\"types/{urlType.ToString().ToLower()}.html\" target=\"_blank\">{title}</a>";
            }
            
            sb.AppendLine($"<tr><td>{title}</td>");
            sb.AppendLine($"<td>{items.Count}</td></tr>");
        }

        {
            sb.AppendLine($"<tr><td><a href=\"types{Path.DirectorySeparatorChar}all.html\" target=\"_blank\">Total</a></td>");
            sb.AppendLine($"<td>{Program.Queue.Count}</td></tr>");

            await this.GenerateSubReport(
                "All",
                "All pages and assets, both internal and external.",
                $"types{Path.DirectorySeparatorChar}all.html", 
                Program.Queue.ToList(),
                true);
        }

        sb.AppendLine("</tbody></table>");
    }

    /// <summary>
    /// Generate a report for each 
    /// </summary>
    private async Task GenerateQueueEntryReports()
    {
    }

    /// <summary>
    /// Generate a generic sub-report for a given dataset.
    /// </summary>
    /// <param name="title">Title.</param>
    /// <param name="description">Description.</param>
    /// <param name="filename">Filename.</param>
    /// <param name="entries">List of queue entries.</param>
    /// <param name="includeEmptyUrlTypes">Whether to include empty URL types.</param>
    private async Task GenerateSubReport(
        string title,
        string description,
        string filename,
        IReadOnlyCollection<QueueEntry> entries,
        bool includeEmptyUrlTypes = false)
    {
        var sb = new StringBuilder();
        var urlTypesWithEntries =
            Enum.GetValues<UrlType>().Count(urlType => entries.Any(n => n.UrlType == urlType));

        foreach (var urlType in Enum.GetValues<UrlType>())
        {
            var items = entries
                .Where(n => n.UrlType == urlType)
                .ToList();

            if (!items.Any() && !includeEmptyUrlTypes)
            {
                continue;
            }
            
            var enumTitle = urlType switch
            {
                UrlType.InternalPage => "Internal Pages",
                UrlType.InternalAsset => "Internal Assets",
                UrlType.ExternalPage => "External Pages",
                UrlType.ExternalAsset => "External Asset",
                _ => throw new Exception($"Invalid URL type: {urlType}")
            };

            if (urlTypesWithEntries > 1)
            {
                sb.AppendLine($"<h2>{enumTitle}</h2>");    
            }
            
            sb.AppendLine("<table><tbody>");

            foreach (var item in items)
            {
                var trCssClass = string.Empty;

                if (item.Error is not null)
                {
                    trCssClass = "error";
                }

                if (item.Skipped)
                {
                    trCssClass = "warning";
                }
                
                sb.AppendLine($"<tr class=\"{trCssClass}\"><td><a href=\"../entries/entry-{item.Id}.html\" target=\"_blank\">{item.Url}</a></td>");

                // Error!
                if (item.Error is not null)
                {
                    sb.AppendLine($"<td colspan=\"3\" class=\"error\">{item.Error}</td></tr>");
                    continue;
                }

                // Skipped!
                if (item.Skipped)
                {
                    sb.AppendLine($"<td colspan=\"3\">Skipped!</td></tr>");
                    continue;
                }

                // Status code.
                if (item.Response?.StatusCode > 0)
                {
                    var cssClass = item.Response.StatusCode switch
                    {
                        >= 200 and <= 299 => "success",
                        <= 399 => "warning",
                        _ => "error"
                    };

                    sb.AppendLine($"<td class=\"{cssClass}\">{item.Response.GetStatusFormatted()}</td>");
                }
                else
                {
                    sb.AppendLine("<td>-</td>");
                }
                
                // Response time.
                if (item.Response?.Time > 0)
                {
                    var cssClass = item.Response.Time switch
                    {
                        > 1000 => "error",
                        > 300 => "warning",
                        _ => "success"
                    };
                    
                    sb.AppendLine($"<td class=\"{cssClass}\">{item.Response.GetTimeFormatted()}</td>");
                }
                else
                {
                    sb.AppendLine("<td>-</td>");
                }
                
                // Document size.
                if (item.Response?.Size > 0)
                {
                    var cssClass = item.Response.Size switch
                    {
                        > 1000000 => "error",
                        > 500000 => "warning",
                        _ => "success"
                    };
                    
                    sb.AppendLine($"<td class=\"{cssClass}\">{item.Response.GetSizeFormatted()}</td></tr>");
                }
                else
                {
                    sb.AppendLine("<td>-</td></tr>");
                }
            }
            
            sb.AppendLine("</tbody></table>");
        }
        
        // Done.
        var html = this.GetBaseReport()
            .Replace("{BodyOverride}", "report")
            .Replace("{HtmlTitle}", title)
            .Replace("{ReportTitle}", title)
            .Replace("{ReportDescription}", description)
            .Replace("{ReportContent}", sb.ToString());
        
        // Write to disk.
        var path = Path.Combine(
            Program.Options.ReportPath!,
            filename);

        var dir = Path.GetDirectoryName(path);

        if (dir is not null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(path, html, Encoding.UTF8);
    }

    /// <summary>
    /// Generate the summary report page.
    /// </summary>
    private async Task GenerateSummaryReport()
    {
        var host = Program.Queue
            .OrderBy(n => n.Created)
            .First().Url.Host;

        var sb = new StringBuilder();
        
        sb.AppendLine("<h2>Stats</h2>");
        sb.AppendLine("<table><tbody>");
        sb.AppendLine($"<tr><td>Started</td><td>{Program.Started.ToString("yyyy-MM-dd HH:mm:ss")}</td></tr>");
        sb.AppendLine($"<tr><td>Finished</td><td>{Program.Finished.ToString("yyyy-MM-dd HH:mm:ss")}</td></tr>");
        sb.AppendLine($"<tr><td>Took</td><td>{(Program.Finished - Program.Started).ToHumanReadable()}</td></tr>");
        sb.AppendLine("</tbody></table>");

        await this.AddTypesTable(sb);
        await this.AddStatusCodeTable(sb);
        await this.AddResponseTimeTable(sb);
        await this.AddAccessibilityIssuesTable(sb);
        
        var html = this.GetBaseReport()
            .Replace("{BodyOverride}", "summary")
            .Replace("{HtmlTitle}", host)
            .Replace("{ReportTitle}", host)
            .Replace("{ReportDescription}", string.Empty)
            .Replace("{ReportContent}", sb.ToString());
        
        var path = Path.Combine(
            Program.Options.ReportPath!,
            "report.html");

        await File.WriteAllTextAsync(path, html, Encoding.UTF8);
    }
    
    #endregion
    
    #region Helper functions

    /// <summary>
    /// Get the base HTML and CSS for the report.
    /// </summary>
    /// <returns>HTML and CSS.</returns>
    private string GetBaseReport()
    {
        const string html =
            """
            <!DOCTYPE html>
            <html lang="en">
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>Slap Report - {HtmlTitle}</title>
                    <style>
                        * {
                            box-sizing: border-box;
                        }
                        
                        a {
                            color: #ba9ffb;
                            text-decoration: none;
                        }
                        
                        a:hover {
                            text-decoration: none;
                        }
                        
                        a:visited {
                            color: #a688fa;
                            text-decoration: none;
                        }
                        
                        article {
                            margin: 0;
                            padding: 10px 0 0 0;
                        }
                        
                        body {
                            background-color: #121212;
                            color: #c0bdc6;
                            font-family: sans-serif;
                            font-size: 13px;
                            margin: 0;
                            padding: 50px;
                            transition: .25s;
                        }
                        
                        footer {
                            font-size: smaller;
                            margin: 0 auto;
                            padding: 150px 0 0 0;
                            text-align: right;
                        }
                        
                        h1 {
                            color: #e0dde6;
                            margin: 0;
                            padding: 0;
                            text-transform: uppercase;
                        }
                        
                        h2 {
                            color: #d0cdd6;
                            margin: 0;
                            padding: 30px 0 10px 0;
                        }
                        
                        header {
                            margin: 0;
                            padding: 0;
                        }
                        
                        table {
                            border-collapse: collapse;
                            width: 100%;
                        }
                        
                        thead tr th {
                            font-weight: normal;
                            margin: 0;
                            padding: 7px 10px;
                            text-align: left;;
                        }
                        
                        thead tr th:nth-child(2),
                        tbody tr td:nth-child(2) {
                            text-align: right;
                            width: 200px;
                        }
                        
                        thead tr th:nth-child(3),
                        thead tr th:nth-child(4),
                        tbody tr td:nth-child(3),
                        tbody tr td:nth-child(4) {
                            text-align: right;
                            width: 100px;
                        }
                        
                        tr,
                        tr td {
                            transition: .25s;
                        }
                        
                        tr td {
                            margin: 0;
                            padding: 7px 10px;
                        }
                        
                        tr:nth-child(even) td {
                            background-color: #171717;
                        }
                        
                        tr.error td:nth-child(2) {
                            border-right: solid 3px rgba(153, 0, 0, 0.5);
                        }
                        
                        tr.success td:nth-child(2) {
                            border-right: solid 3px rgba(0, 102, 0, 0.5);
                        }
                        
                        tr.warning td:nth-child(2) {
                            border-right: solid 3px rgba(102, 102, 0, 0.5);
                        }
                        
                        tr:hover td {
                            background-color: #272727;
                            color: #e0dde6;
                        }
                        
                        tr.error:hover td:nth-child(2) {
                            border-right: solid 3px rgba(153, 0, 0, 1);
                        }
                        
                        tr.success:hover td:nth-child(2) {
                            border-right: solid 3px rgba(0, 102, 0, 1);
                        }
                        
                        tr.warning:hover td:nth-child(2) {
                            border-right: solid 3px rgba(102, 102, 0, 1);
                        }
                        
                        td.error {
                            color: rgba(153, 0, 0, 1);
                        }
                        
                        td.success {
                            color: rgba(0, 153, 0, 1);
                        }
                        
                        td.warning {
                            color: rgba(153, 153, 0, 1);
                        }
                        
                        body.summary article,
                        body.summary footer,
                        body.summary header {
                            margin-left: auto;
                            margin-right: auto;
                            width: 400px;
                        }
                        
                        body.summary tr td:first-child {
                            width: 200px;
                        }
                        
                        body.summary tr td:nth-child(2) {
                            text-align: right;
                            width: 200px;
                        }
                        
                        body.report article {
                            padding-top: 50px;
                        }
                    </style>
                </head>
                <body class="{BodyOverride}">
                    <header>
                        <h1>{ReportTitle}</h1>
                        {ReportDescription}
                    </header>
                    
                    <article>
                        {ReportContent}
                    </article>
                    
                    <footer>
                        Generated at {GeneratedAt} by Slap v{ProgramVersion}<br>
                        <a href="https://github.com/nagilum/slap">https://github.com/nagilum/slap</a>
                    </footer>
                </body>
            </html>
            """;

        return html
            .Replace("{GeneratedAt}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{ProgramVersion}", Program.Version.ToString());
    }
    
    /// <summary>
    /// Write the queue to disk as JSON.
    /// </summary>
    private async Task WriteQueueToDisk()
    {
        var path = Path.Combine(
            Program.Options.ReportPath!,
            "queue.json");
        
        await using var stream = File.OpenWrite(path);
        await JsonSerializer.SerializeAsync(
            stream,
            Program.Queue.OrderBy(n => n.Created),
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
    }
    
    #endregion
}