using System.Text;
using System.Text.Json;
using Serilog;
using Slap.Core;
using Slap.Extenders;
using Slap.Models;
using Slap.Models.Interfaces;
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

                var title = $"{severity[..1].ToUpper()}{severity[1..].ToLower()}";
                var link = title;

                if (items.Any())
                {
                    await this.GenerateIssuesReport(
                        title,
                        severity,
                        $"All pages that has accessibility issues marked as {severity}",
                        $"issues{Path.DirectorySeparatorChar}{severity}.html",
                        items);

                    link = $"<a href=\"issues/{severity}.html\" target=\"_blank\">{title}</a>";
                }

                var cssClass = severity switch
                {
                    "critical" => "error",
                    "serious" => "error",
                    "moderate" => "warning",
                    _ => string.Empty
                };

                sb.AppendLine($"<tr class=\"{cssClass}\"><td>{link}</td><td>{items.Count}</td></tr>");
            }
        }

        sb.AppendLine("</tbody></table>");
    }

    /// <summary>
    /// Add a row with entry and response info.
    /// </summary>
    private void AddQueueEntryResponseRow(StringBuilder sb, IQueueEntry entry)
    {
        var cssClass = string.Empty;

        if (entry.Error is not null)
        {
            cssClass = "error";
        }

        if (entry.Skipped)
        {
            cssClass = "warning";
        }

        sb.AppendLine(
            $"<tr class=\"{cssClass}\"><td><a href=\"../entries/entry-{entry.Id}.html\" target=\"_blank\">{entry.Url}</a></td>");

        // Error!
        if (entry.Error is not null)
        {
            sb.AppendLine($"<td colspan=\"3\" class=\"error\">{entry.Error.Message}</td></tr>");
            return;
        }

        // Skipped!
        if (entry.Skipped)
        {
            sb.AppendLine($"<td colspan=\"3\">Skipped!</td></tr>");
            return;
        }

        // Status code.
        if (entry.Response?.StatusCode > 0)
        {
            cssClass = entry.Response.StatusCode switch
            {
                >= 200 and <= 299 => "success",
                <= 399 => "warning",
                _ => "error"
            };

            sb.AppendLine($"<td class=\"{cssClass}\">{entry.Response.GetStatusFormatted()}</td>");
        }
        else
        {
            sb.AppendLine("<td>-</td>");
        }

        // Response time.
        if (entry.Response?.Time > 0)
        {
            cssClass = entry.Response.Time switch
            {
                > 1000 => "error",
                > 300 => "warning",
                _ => "success"
            };

            sb.AppendLine($"<td class=\"{cssClass}\">{entry.Response.GetTimeFormatted()}</td>");
        }
        else
        {
            sb.AppendLine("<td>-</td>");
        }

        // Document size.
        if (entry.Response?.Size > 0)
        {
            cssClass = entry.Response.Size switch
            {
                > 1000000 => "error",
                > 500000 => "warning",
                _ => "success"
            };

            sb.AppendLine($"<td class=\"{cssClass}\">{entry.Response.GetSizeFormatted()}</td></tr>");
        }
        else
        {
            sb.AppendLine("<td>-</td></tr>");
        }
    }

    /// <summary>
    /// Add the response time table.
    /// </summary>
    private async Task AddResponseTimeTable(StringBuilder sb)
    {
        sb.AppendLine("<h2>Response Times</h2>");
        sb.AppendLine("<table><tbody>");

        string link;
        string cssClass;

        {
            var items = Program.Queue
                .Where(n => n.Response?.Time is >= 0 and < 300)
                .ToList();

            if (items.Any())
            {
                await this.GenerateSubReport(
                    "&lt; 300 ms",
                    "All pages and assets that had a response of less than 300 milliseconds.",
                    $"response-times{Path.DirectorySeparatorChar}less-than-300-ms.html",
                    items);

                link = "<a href=\"response-times/less-than-300-ms.html\" target=\"_blank\">&lt; 300 ms</a>";

                sb.AppendLine($"<tr><td>{link}</td><td>{items.Count}</td></tr>");
            }
        }

        {
            var items = Program.Queue
                .Where(n => n.Response?.Time is >= 300 and < 600)
                .ToList();

            if (items.Any())
            {
                await this.GenerateSubReport(
                    "&gt; 300 &amp; &lt; 600 ms",
                    "All pages and assets that had a response time of less than 600 and greater than 300 milliseconds.",
                    $"response-times{Path.DirectorySeparatorChar}less-than-600-greater-than-300-ms.html",
                    items);

                link =
                    "<a href=\"response-times/less-than-600-greater-than-300-ms.html\" target=\"_blank\">&gt; 300 &amp; &lt; 600 ms</a>";

                sb.AppendLine($"<tr><td>{link}</td><td>{items.Count}</td></tr>");
            }
        }

        {
            var items = Program.Queue
                .Where(n => n.Response?.Time is >= 600 and < 900)
                .ToList();

            if (items.Any())
            {
                await this.GenerateSubReport(
                    "&gt; 600 &amp; &lt; 900 ms",
                    "All pages and assets that had a response time of less than 900 and greater than 600 milliseconds.",
                    $"response-times{Path.DirectorySeparatorChar}less-than-900-greater-than-600-ms.html",
                    items);

                link =
                    "<a href=\"response-times/less-than-900-greater-than-600-ms.html\" target=\"_blank\">&gt; 600 &amp; &lt; 900 ms</a>";
                cssClass = items.Any() ? "warning" : "";

                sb.AppendLine($"<tr class=\"{cssClass}\"><td>{link}</td><td>{items.Count}</td></tr>");
            }
        }

        {
            var items = Program.Queue
                .Where(n => n.Response?.Time is >= 900)
                .ToList();

            if (items.Any())
            {
                await this.GenerateSubReport(
                    "&gt; 900 ms",
                    "All pages and assets that had a response time of greater than 900 milliseconds.",
                    $"response-times{Path.DirectorySeparatorChar}greater-than-900-ms.html",
                    items);

                link = "<a href=\"response-times/greater-than-900-ms.html\" target=\"_blank\">&gt; 900 ms</a>";
                cssClass = items.Any() ? "error" : "";

                sb.AppendLine($"<tr class=\"{cssClass}\"><td>{link}</td><td>{items.Count}</td></tr>");
            }
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

        string link;
        string cssClass;

        foreach (var code in statusCodes)
        {
            var items = Program.Queue
                .Where(n => n.Response?.StatusCode == code)
                .ToList();

            if (!items.Any())
            {
                continue;
            }

            await this.GenerateSubReport(
                $"{code} {ScannerService.GetStatusDescription(code)}",
                $"All pages and assets matching the response status {code} {ScannerService.GetStatusDescription(code)}",
                $"statuses{Path.DirectorySeparatorChar}{code}.html",
                items);

            link =
                $"<a href=\"statuses/{code}.html\" target=\"_blank\">{code} {ScannerService.GetStatusDescription(code)}</a>";

            cssClass = code switch
            {
                >= 200 and < 300 => "",
                >= 300 and < 400 => "warning",
                _ => "error"
            };

            sb.AppendLine($"<tr class=\"{cssClass}\"><td>{link}</td>");
            sb.AppendLine($"<td>{items.Count}</td></tr>");
        }

        {
            var items = Program.Queue
                .Where(n => n.Response is null && !n.Skipped)
                .ToList();

            if (items.Any())
            {
                await this.GenerateSubReport(
                    "Failed",
                    "All pages and assets that failed.",
                    $"statuses{Path.DirectorySeparatorChar}failed.html",
                    items);

                link = "<a href=\"statuses/failed.html\" target=\"_blank\">Failed</a>";
                cssClass = items.Any() ? "error" : "";

                sb.AppendLine($"<tr class=\"{cssClass}\"><td>{link}</td>");
                sb.AppendLine($"<td>{items.Count}</td></tr>");
            }
        }

        {
            var items = Program.Queue
                .Where(n => n.Skipped)
                .ToList();

            if (items.Any())
            {
                await this.GenerateSubReport(
                    "Skipped",
                    "All pages and assets that were skipped.",
                    $"statuses{Path.DirectorySeparatorChar}skipped.html",
                    items);

                link = "<a href=\"statuses/skipped.html\" target=\"_blank\">Skipped</a>";
                cssClass = items.Any() ? "warning" : "";

                sb.AppendLine($"<tr class=\"{cssClass}\"><td>{link}</td>");
                sb.AppendLine($"<td>{items.Count}</td></tr>");
            }
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
                UrlType.InternalPage =>
                    "HTML pages matching the list of internal domains. This always includes the hostname of the initial URL to be scanned.",
                UrlType.InternalAsset =>
                    "All pages and assets apart from HTML pages matching the list of internal domains. This always includes the hostname of the initial URL to be scanned.",
                UrlType.ExternalPage => "HTML pages that doesn't match the list of internal domains.",
                UrlType.ExternalAsset =>
                    "All pages and assets apart from HTML pages that doesn't match the list of internal domains.",
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
                    items,
                    extraCssClass: "report-padding");

                title = $"<a href=\"types/{urlType.ToString().ToLower()}.html\" target=\"_blank\">{title}</a>";
            }

            sb.AppendLine($"<tr><td>{title}</td>");
            sb.AppendLine($"<td>{items.Count}</td></tr>");
        }

        {
            sb.AppendLine(
                $"<tr><td><a href=\"types{Path.DirectorySeparatorChar}all.html\" target=\"_blank\">Total</a></td>");
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
    /// Generate a accessibility issues report for a specific severity.
    /// </summary>
    /// <param name="title">Title.</param>
    /// <param name="severity">Severity.</param>
    /// <param name="description">Description.</param>
    /// <param name="filename">Filename.</param>
    /// <param name="entries">List of queue entries.</param>
    private async Task GenerateIssuesReport(
        string title,
        string severity,
        string description,
        string filename,
        IReadOnlyCollection<QueueEntry> entries)
    {
        var sb = new StringBuilder();
        var violations = new List<AccessibilityResultItem>();

        foreach (var entry in entries.Where(n => n.AccessibilityResults?.Violations?.Length > 0))
        {
            var query = entry.AccessibilityResults!.Violations!
                .Where(n => n.Impact!.Equals(severity, StringComparison.InvariantCultureIgnoreCase) &&
                            n.Id is not null);

            foreach (var violation in query)
            {
                if (violations.Any(n => n.Id == violation.Id))
                {
                    continue;
                }

                violations.Add(violation);
            }
        }

        violations = violations
            .OrderBy(n => n.Id)
            .ToList();

        foreach (var violation in violations)
        {
            sb.AppendLine($"<h2 class=\"no-bottom-padding\">{violation.Id}</h2>");
            sb.AppendLine($"<p class=\"violation-summary\">{violation.Help ?? violation.Description}");

            if (violation.HelpUrl is not null)
            {
                sb.AppendLine($" (<a href=\"{violation.HelpUrl}\" target=\"_blank\">read more</a>)");
            }

            if (violation.Tags?.Length > 0)
            {
                sb.AppendLine($"<br>Tags: {string.Join(", ", violation.Tags)}");
            }

            sb.AppendLine("</p>");

            var query =
                from n in entries
                where n.AccessibilityResults?.Violations?.Any(m => m.Id == violation.Id) == true
                select n;

            foreach (var entry in query)
            {
                var temp = entry.AccessibilityResults?.Violations?
                    .FirstOrDefault(n => n.Id == violation.Id);

                if (temp?.Nodes is null)
                {
                    continue;
                }

                sb.AppendLine("<table><tbody>");
                this.AddQueueEntryResponseRow(sb, entry);
                sb.AppendLine("</tbody></table>");

                foreach (var node in temp.Nodes!)
                {
                    var lines = new List<string>();

                    if (node.Html is not null)
                    {
                        lines.Add($"<div class=\"html\" title=\"Affected HTML code\">{node.Html?
                            .Replace("<", "&lt;").Replace(">", "&gt;")}</div>");
                    }

                    if (node.Target?.Selector is not null)
                    {
                        lines.Add($"<div class=\"selector\" title=\"DOM selector\">{node.Target?.Selector
                            .Replace("<", "&lt;").Replace(">", "&gt;")}</div>");
                    }

                    if (node.XPath?.Selector is not null)
                    {
                        lines.Add($"<div class=\"selector\" title=\"DOM selector\">{node.XPath?.Selector
                            .Replace("<", "&lt;").Replace(">", "&gt;")}</div>");
                    }

                    sb.AppendLine($"<div class=\"violation\">{string.Join(string.Empty, lines)}</div>");
                }
            }
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
    /// Generate a report for each 
    /// </summary>
    private async Task GenerateQueueEntryReports()
    {
        foreach (var entry in Program.Queue)
        {
            var sb = new StringBuilder();

            // Request and document info.
            sb.AppendLine("<table><tbody>");
            sb.AppendLine($"<tr><td>URL</td><td><a href=\"{entry.Url}\" target=\"_blank\">{entry.Url}</a></td></tr>");
            sb.AppendLine($"<tr><td>Skipped</td><td>{(entry.Skipped ? "Yes" : "No")}</td></tr>");

            if (entry.Response is not null)
            {
                sb.AppendLine($"<tr><td>Status Code</td><td>{entry.Response?.GetStatusFormatted()}</td></tr>");
                sb.AppendLine($"<tr><td>Response Time</td><td>{entry.Response?.GetTimeFormatted()}</td></tr>");
                sb.AppendLine($"<tr><td>Document Size</td><td>{entry.Response?.GetSizeFormatted()}</td></tr>");
                sb.AppendLine($"<tr><td>Document Title</td><td>{entry.Response?.DocumentTitle}</td></tr>");    
            }
            
            sb.AppendLine("</tbody></table>");
            
            // Error.
            if (entry.Error is not null)
            {
                sb.AppendLine("<h2>Error</h2>");
                sb.AppendLine("<table><tbody>");
                sb.AppendLine($"<tr><td>Message</td><td>{entry.Error.Message}</td></tr>");

                if (entry.Error.Namespace is not null)
                {
                    sb.AppendLine($"<tr><td>Namespace</td><td>{entry.Error.Namespace}</td></tr>");
                }

                if (entry.Error.Data?.Count > 0)
                {
                    foreach (var (key, value) in entry.Error.Data)
                    {
                        sb.AppendLine($"<tr><td>{key}</td><td>{value}</td></tr>");
                    }
                }
                
                sb.AppendLine("</tbody></table>");
            }

            // Show screenshot.
            if (entry.ScreenshotSaved)
            {
                var url = $"../screenshots/screenshot-{entry.Id}.png";

                sb.AppendLine("<h2>Screenshot</h2>");
                sb.AppendLine($"<div class=\"screenshot\"><a href=\"{url}\" target=\"_blank\"><img src=\"{url}\" alt=\"Screenshot for {url}\"></a></div>");
            }

            // Response headers.
            if (entry.Response?.Headers?.Count > 0)
            {
                sb.AppendLine("<h2>Headers</h2>");
                sb.AppendLine("<table><tbody>");

                foreach (var (key, value) in entry.Response.Headers)
                {
                    sb.AppendLine($"<tr><td>{key}</td><td>{value}</td></tr>");
                }

                sb.AppendLine("</tbody></table>");
            }

            // Meta tags.
            if (entry.Response?.MetaTags?.Count > 0)
            {
                sb.AppendLine("<h2>Meta Tags</h2>");
                sb.AppendLine("<table><tbody>");

                foreach (var tag in entry.Response.MetaTags)
                {
                    sb.AppendLine($"<tr><td>{tag.Name ?? tag.Property ?? tag.HttpEquiv ?? "&nbsp;"}</td>");
                    sb.AppendLine($"<td>{tag.Content ?? $"Charset {tag.Charset}"}</td></tr>");
                }

                sb.AppendLine("</tbody></table>");
            }

            // Linked from.
            if (entry.LinkedFrom.Count > 0)
            {
                sb.AppendLine("<h2>Linked From</h2>");
                sb.AppendLine("<table class=\"links\"><tbody>");

                foreach (var url in entry.LinkedFrom)
                {
                    var item = Program.Queue
                        .FirstOrDefault(n => n.Url == url);

                    this.AddQueueEntryResponseRow(sb, item ?? new QueueEntry(url));
                }

                sb.AppendLine("</tbody></table>");
            }

            // Accessibility issues.
            if (entry.AccessibilityResults?.Violations?.Length > 0)
            {
                var severities = entry.AccessibilityResults?.Violations?
                    .Select(n => n.Impact)
                    .Where(n => !string.IsNullOrWhiteSpace(n?.Trim()))
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();

                if (severities is not null)
                {
                    foreach (var severity in severities.Where(n => n is not null))
                    {
                        sb.AppendLine($"<h2>{severity![..1].ToUpper()}{severity[1..].ToLower()} Accessibility Issues</h2>");
                        sb.AppendLine("<table><tbody>");

                        var query =
                            from n in entry.AccessibilityResults?.Violations
                            where n.Impact == severity
                            select n;

                        foreach (var violation in query)
                        {
                            if (violation.Nodes is null ||
                                violation.Nodes.Length == 0)
                            {
                                continue;
                            }

                            sb.AppendLine($"<h3 class=\"no-bottom-padding\">{violation.Id}</h3>");
                            sb.AppendLine($"<p class=\"violation-summary\">{violation.Help ?? violation.Description}");
                            sb.AppendLine($"{(violation.HelpUrl is not null ? $" (<a href=\"{violation.HelpUrl}\" target=\"_blank\">read more</a>)" : string.Empty)}</p>");

                            foreach (var node in violation.Nodes)
                            {
                                var lines = new List<string>();

                                if (node.Html is not null)
                                {
                                    lines.Add($"<div class=\"html\" title=\"Affected HTML code\">{node.Html?
                                        .Replace("<", "&lt;").Replace(">", "&gt;")}</div>");
                                }

                                if (node.Target?.Selector is not null)
                                {
                                    lines.Add($"<div class=\"selector\" title=\"DOM selector\">{node.Target?.Selector
                                        .Replace("<", "&lt;").Replace(">", "&gt;")}</div>");
                                }

                                if (node.XPath?.Selector is not null)
                                {
                                    lines.Add($"<div class=\"selector\" title=\"DOM selector\">{node.XPath?.Selector
                                        .Replace("<", "&lt;").Replace(">", "&gt;")}</div>");
                                }

                                sb.AppendLine($"<div class=\"violation\">{string.Join(string.Empty, lines)}</div>");
                            }
                        }

                        sb.AppendLine("</tbody></table>");
                    }
                }
            }

            // Done.
            var html = this.GetBaseReport()
                .Replace("{BodyOverride}", "entry")
                .Replace("{HtmlTitle}", entry.Url.ToString())
                .Replace("{ReportTitle}", "Details")
                .Replace("{ReportDescription}", string.Empty)
                .Replace("{ReportContent}", sb.ToString());

            var path = Path.Combine(
                Program.Options.ReportPath!,
                "entries");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(
                path,
                $"entry-{entry.Id}.html");

            await File.WriteAllTextAsync(path, html, Encoding.UTF8);
        }
    }

    /// <summary>
    /// Generate a generic sub-report for a given dataset.
    /// </summary>
    /// <param name="title">Title.</param>
    /// <param name="description">Description.</param>
    /// <param name="filename">Filename.</param>
    /// <param name="entries">List of queue entries.</param>
    /// <param name="includeEmptyUrlTypes">Whether to include empty URL types.</param>
    /// <param name="extraCssClass">Add an extra CSS class to the body.</param>
    private async Task GenerateSubReport(
        string title,
        string description,
        string filename,
        IReadOnlyCollection<QueueEntry> entries,
        bool includeEmptyUrlTypes = false,
        string? extraCssClass = null)
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
                this.AddQueueEntryResponseRow(sb, item);
            }

            sb.AppendLine("</tbody></table>");
        }

        // Done.
        var html = this.GetBaseReport()
            .Replace("{BodyOverride}", $"report {extraCssClass}")
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
                            text-decoration: underline;
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
                            font-weight: normal;
                            margin: 0;
                            padding: 0;
                            text-transform: uppercase;
                        }
                        
                        h2 {
                            color: #d0cdd6;
                            font-weight: normal;
                            margin: 0;
                            padding: 30px 0 10px 0;
                            text-transform: uppercase;
                        }
                        
                        h3 {
                            color: #d0cdd6;
                            font-weight: normal;
                            margin: 0;
                            padding: 0;
                            text-transform: uppercase;
                        }
                        
                        h2.no-bottom-padding {
                            padding-bottom: 0;
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
                        
                        p.error,
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
                        
                        body.report-padding article {
                            padding-top: 50px;
                        }
                        
                        body.entry tbody tr td:nth-child(1) {
                            width: 200px;
                        }
                        
                        body.entry tbody tr td:nth-child(2) {
                            text-align: left;
                            width: auto;
                        }
                        
                        body.entry table.links tbody tr td:nth-child(1) {
                            width: auto;
                        }
                        
                        body.entry table.links tbody tr td:nth-child(2) {
                            text-align: right;
                            width: 200px;
                        }
                        
                        body.entry table.links tbody tr td:nth-child(3),
                        body.entry table.links tbody tr td:nth-child(4) {
                            text-align: right;
                            width: 100px;
                        }
                        
                        div.screenshot {
                            margin: 0 0 30px;
                        }
                        
                        div.screenshot a img {
                            max-height: 400px;
                            max-width: 400px;
                        }
                        
                        div.violation {
                            border-left: solid 3px #c0bdc6;
                            font-family: monospace;
                            margin: 10px 0;
                            padding: 0 0 0 10px;
                            transition: .25s;
                        }
                        
                        p.violation-summary {
                            margin-top: 0;
                        }
                        
                        div.violation div.html::before {
                            color: rgba(102, 102, 255, 1);
                            content: '<>';
                            display: inline-block;
                            padding-right: 8px;
                            text-align: center;
                            transition: .25s;
                            width: 20px;
                        }
                        
                        div.violation div.selector::before {
                            color: rgba(102, 102, 255, 1);
                            content: '#';
                            display: inline-block;
                            padding-right: 8px;
                            text-align: center;
                            transition: .25s;
                            width: 20px;
                        }
                        
                        div.violation:hover {
                            border-left-color: rgba(153, 153, 0, 1);
                            color: #e0dde6;
                        }
                        
                        div.violation:hover div.html::before {
                            color: rgba(153, 153, 255, 1);
                        }
                        
                        div.violation:hover div.selector::before {
                            color: rgba(153, 153, 255, 1);
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