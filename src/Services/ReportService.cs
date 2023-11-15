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
            await this.GenerateHtmlReports();
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error while generating and writing reports.");
        }
    }
    
    #endregion
    
    #region Helper functions

    /// <summary>
    /// Add a list of accessibility issues on the main report.
    /// </summary>
    /// <param name="html">HTML.</param>
    private void AddAccessibilityIssuesToHtmlReport(ref string html)
    {
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
        
        var sb = new StringBuilder();

        foreach (var severity in severities)
        {
            var count = Program.Queue
                .Sum(n => n.AccessibilityResults?.Violations?
                    .Count(m => m.Impact == severity));
            
            sb.Append("<tr><td class=\"capitalize\">");
            sb.Append(severity);
            sb.Append("</td><td>");
            sb.Append(count);
            sb.Append("</td><td>");
            sb.Append($"<a target=\"_blank\" href=\"issues-{severity}.html\">Details</a>");
            sb.Append("</td></tr>");
        }
        
        html = html.Replace("{AccessibilityIssuesRows}", sb.ToString());
    }

    /// <summary>
    /// Add found accessibility issues to entry report.
    /// </summary>
    /// <param name="html">HTML.</param>
    /// <param name="entry">Queue entry.</param>
    private void AddAccessibilityIssuesToHtmlEntryReport(ref string html, IQueueEntry entry)
    {
        if (entry.AccessibilityResults?.Violations is null ||
            entry.AccessibilityResults.Violations.Length == 0)
        {
            html = html.Replace("{AccessibilityIssues}", string.Empty);
            return;
        }

        var sb = new StringBuilder();

        sb.AppendLine("<h2>Accessibility Issues</h2>");

        foreach (var violation in entry.AccessibilityResults.Violations)
        {
            var message = violation.Nodes?.FirstOrDefault()?.Message;
            
            sb.AppendLine($"<h3>{violation.Id![..1].ToUpper()}{violation.Id[1..].ToLower()}</h3>");
            sb.AppendLine("<table><tbody>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Description</td><td>{violation.Description}</td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Message</td><td>{message}</td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Help</td><td><a target=\"_blank\" href=\"{violation.HelpUrl}\">{violation.Help}</a></td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Tags</td><td>{string.Join(", ", violation.Tags ?? Array.Empty<string>())}</td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Impact</td><td>{violation.Impact}</td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Count</td><td>{violation.Nodes?.Length}</td></tr>");
            sb.AppendLine("</tbody></table>");

            if (!(violation.Nodes?.Length > 0))
            {
                continue;
            }

            foreach (var node in violation.Nodes)
            {
                sb.AppendLine("<div class=\"violation-node\">");
                sb.AppendLine($"<span>Selector: <code class=\"success\">{node.Target?.Selector}</code></span>");
                sb.AppendLine($"<span>HTML: <code>{node.Html?.Replace("<", "&lt;").Replace(">", "&gt;")}</code></span>");
                sb.AppendLine("</div>");
            }
        }
        
        html = html.Replace("{AccessibilityIssues}", sb.ToString());
    }

    /// <summary>
    /// Add error data to entry report.
    /// </summary>
    /// <param name="html">HTML.</param>
    /// <param name="entry">Queue entry.</param>
    private void AddErrorToHTmlEntryReport(ref string html, IQueueEntry entry)
    {
        var error = entry.Error is null
            ? string.Empty
            : $"<p class=\"error\">{entry.ErrorType}<br>{entry.Error}</p>";

        html = html.Replace("{Error}", error);
    }
    
    /// <summary>
    /// Add metadata to HTML report.
    /// </summary>
    /// <param name="html">HTML.</param>
    private void AddMetadataToHtmlReport(ref string html)
    {
        html = html.Replace("{Host}", Program.Queue.First().Url.Host);
        html = html.Replace("{GeneratedAt}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        html = html.Replace("{ProgramVersion}", Program.Version.ToString());
    }
    
    /// <summary>
    /// Add metadata to entry report.
    /// </summary>
    /// <param name="html">HTML.</param>
    /// <param name="entry">Queue entry.</param>
    private void AddMetadataToHtmlEntryReport(ref string html, IQueueEntry entry)
    {
        html = html.Replace("{Url}", entry.Url.ToString());
        html = html.Replace("{GeneratedAt}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        html = html.Replace("{ProgramVersion}", Program.Version.ToString());
    }

    /// <summary>
    /// Add list of linked-from to entry report.
    /// </summary>
    /// <param name="html">HTML.</param>
    /// <param name="entry">Queue entry.</param>
    private void AddLinkedFromToHtmlEntryReport(ref string html, IQueueEntry entry)
    {
        if (entry.LinkedFrom.Count == 0)
        {
            html = html.Replace("{LinkedFrom}", "<tr><td><em>None</em></td></tr>");
            return;
        }

        var sb = new StringBuilder();

        foreach (var url in entry.LinkedFrom)
        {
            sb.AppendLine($"<tr><td><a href=\"{url}\">{url}</a></td></tr>");
        }
        
        html = html.Replace("{LinkedFrom}", sb.ToString());
    }
    
    /// <summary>
    /// Add links to HTML report.
    /// </summary>
    /// <param name="html">HTML.</param>
    /// <param name="urlType">Link type.</param>
    private void AddQueueEntriesToHtmlReport(ref string html, UrlType urlType)
    {
        var sb = new StringBuilder();

        var entries = Program.Queue
            .Where(n => n.UrlType == urlType)
            .OrderBy(n => n.Url.ToString())
            .ToList();

        if (entries.Count == 0)
        {
            html = html.Replace("{Links" + urlType + "}", "<tr><td><em>None Found</em></td></tr>");
            html = html.Replace("{Hidden" + urlType + "}", "hidden");
        }

        foreach (var entry in entries)
        {
            string cssClass;
            
            // URL.
            sb.Append($"<tr><td class=\"url{(entry.UrlType == UrlType.InternalWebpage ? "-875" : "-700")}\"><a href=\"{entry.Url}\">{entry.Url}</a></td>");
            
            // Accessibility issues.
            if (entry.UrlType == UrlType.InternalWebpage)
            {
                var count = entry.AccessibilityResults?.Violations?.Length ?? 0;

                cssClass = count > 0
                    ? "error"
                    : "success";
                
                sb.Append($"<td class=\"{cssClass} info-cell\">{count}</td>");
            }
            
            // Error?
            if (entry.Error is not null)
            {
                sb.Append($"<td class=\"error\" colspan=\"3\">{entry.Error}</td>");
            }
            else if (entry.Skipped)
            {
                sb.Append($"<td class=\"warning\" colspan=\"3\">Skipped</td>");
            }
            else
            {
                // Status code.
                if (entry.Response?.StatusCode > 0)
                {
                    cssClass = entry.Response.StatusCode switch
                    {
                        >= 200 and <= 299 => "success",
                        <= 399 => "warning",
                        _ => "error"
                    };

                    sb.Append($"<td class=\"{cssClass} info-cell\">{entry.Response.GetStatusFormatted()}</td>");
                }
                else
                {
                    sb.Append("<td class=\"info-cell\">-</td>");
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
                    
                    sb.Append($"<td class=\"{cssClass} info-cell\">{entry.Response.GetTimeFormatted()}</td>");
                }
                else
                {
                    sb.Append("<td class=\"info-cell\">-</td>");
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
                    
                    sb.Append($"<td class=\"{cssClass} info-cell\">{entry.Response.GetSizeFormatted()}</td>");
                }
                else
                {
                    sb.Append("<td class=\"info-cell\">-</td>");
                }
            }
            
            // Details.
            sb.AppendLine($"<td class=\"info-cell\"><a target=\"_blank\" href=\"entries/entry-{entry.Id}.html\">Details</a></td></tr>");
        }

        html = html.Replace("{Links" + urlType + "}", sb.ToString());
        html = html.Replace("{Hidden" + urlType + "}", string.Empty);
    }

    /// <summary>
    /// Add response body data to entry report.
    /// </summary>
    /// <param name="html">HTML.</param>
    /// <param name="entry">Queue entry.</param>
    private void AddResponseBodyDataToHtmlEntryReport(ref string html, IQueueEntry entry)
    {
        // Document title.
        var title = entry.Response?.DocumentTitle is not null
            ? $"<strong>{entry.Response.DocumentTitle}</strong><br>"
            : string.Empty;

        html = html.Replace("{DocumentTitle}", title);

        // Headers.
        if (entry.Response?.Headers?.Count > 0)
        {
            var sb = new StringBuilder();

            foreach (var (key, value) in entry.Response.Headers)
            {
                sb.AppendLine($"<tr><td>{key}</td><td>{value}</td></tr>");
            }
            
            html = html.Replace("{HiddenHeaders}", string.Empty);
            html = html.Replace("{Headers}", sb.ToString()); 
        }
        else
        {
            html = html.Replace("{HiddenHeaders}", "hidden");
            html = html.Replace("{Headers}", "<tr><td>None</td></tr>");
        }

        // Meta tags.
        if (entry.Response?.MetaTags?.Count > 0)
        {
            var sb = new StringBuilder();

            foreach (var tag in entry.Response.MetaTags)
            {
                sb.AppendLine(
                    "<tr>" +
                    $"<td>{tag.Charset ?? "&nbsp;"}</td>" +
                    $"<td>{tag.HttpEquiv ?? "&nbsp;"}</td>" +
                    $"<td>{tag.Name ?? "&nbsp;"}</td>" +
                    $"<td>{tag.Property ?? "&nbsp;"}</td>" +
                    $"<td>{tag.Content ?? "&nbsp;"}</td>" +
                    "</tr>");
            }
            
            html = html.Replace("{HiddenMetaTags}", string.Empty);
            html = html.Replace("{MetaTags}", sb.ToString());
        }
        else
        {
            html = html.Replace("{HiddenMetaTags}", "hidden");
            html = html.Replace("{MetaTags}", "<tr><td>None</td></tr>");
        }
    }

    /// <summary>
    /// Add response data to entry report.
    /// </summary>
    /// <param name="html">HTML.</param>
    /// <param name="entry">Queue entry.</param>
    private void AddResponseDataToHtmlEntryReport(ref string html, IQueueEntry entry)
    {
        html = html.Replace("{Skipped}", entry.Skipped ? "Yes" : "No");
        html = html.Replace("{StatusCode}", entry.Response?.GetStatusFormatted() ?? "-");
        html = html.Replace("{ResponseTime}", entry.Response?.GetTimeFormatted() ?? "-");
        html = html.Replace("{DocumentSize}", entry.Response?.GetSizeFormatted() ?? "-");
    }

    /// <summary>
    /// Add clickable preview for screenshot.
    /// </summary>
    /// <param name="html">HTML.</param>
    /// <param name="entry">Queue entry.</param>
    private void AddScreenshotToHtmlEntryReport(ref string html, IQueueEntry entry)
    {
        if (!entry.ScreenshotSaved)
        {
            html = html.Replace("{Screenshot}", string.Empty);
            return;
        }

        var url = $"../screenshots/screenshot-{entry.Id}.png";
        var sb = new StringBuilder();

        sb.Append("<div class=\"screenshot\">");
        sb.Append($"<a href=\"{url}\">");
        sb.Append($"<img src=\"{url}\" alt=\"Screenshot for {entry.Url}\">");
        sb.Append("</a></div>");
        
        html = html.Replace("{Screenshot}", sb.ToString());
    }
    
    /// <summary>
    /// Add stats to HTML report.
    /// </summary>
    /// <param name="html">HTML.</param>
    private void AddStatsToHtmlReport(ref string html)
    {
        var took = Program.Finished - Program.Started;

        html = html.Replace("{ScanStarted}", Program.Started.ToString("yyyy-MM-dd HH:mm:ss"));
        html = html.Replace("{ScanFinished}", Program.Finished.ToString("yyyy-MM-dd HH:mm:ss"));
        html = html.Replace("{ScanTook}", $"<span title='{took}'>{took.ToHumanReadable()}</span>");

        var sb = new StringBuilder();

        foreach (var urlType in Enum.GetValues<UrlType>())
        {
            var title = urlType switch
            {
                UrlType.InternalWebpage => "Internal Pages",
                UrlType.InternalAsset => "Internal Assets",
                UrlType.ExternalWebpage => "External Pages",
                UrlType.ExternalAsset => "External Assets",
                _ => throw new Exception($"Unknown UrlType {urlType}")
            };
            
            var filename = urlType switch
            {
                UrlType.InternalWebpage => "type-internal-pages.html",
                UrlType.InternalAsset => "type-internal-assets.html",
                UrlType.ExternalWebpage => "type-external-pages.html",
                UrlType.ExternalAsset => "type-external-assets.html",
                _ => throw new Exception($"Unknown UrlType {urlType}")
            };
            
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td class=\"info-cell\">{title}</td>");
            sb.AppendLine($"<td class=\"info-cell\">{Program.Queue.Count(n => n.UrlType == urlType)}</td>");
            sb.AppendLine($"<td class=\"info-cell\"><a href=\"{filename}\" target=\"_blank\">Details</a></td>");
            sb.AppendLine("</tr>");
        }

        html = html.Replace("{TypeRows}", sb.ToString());
    }
    
    /// <summary>
    /// Add status codes to HTML report.
    /// </summary>
    /// <param name="html">HTML.</param>
    private void AddStatusCodesToHtmlReport(ref string html)
    {
        var statusCodes = Program.Queue
            .Select(n => n.Response?.StatusCode ?? 0)
            .Where(n => n > 0)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var dict = statusCodes
            .ToDictionary(
                n => n.ToString(),
                n => Program.Queue.Count(m => m.Response?.StatusCode == n).ToString());

        var failed = Program.Queue.Count(n => n.Response is null && !n.Skipped);
        var skipped = Program.Queue.Count(n => n.Skipped);

        if (failed > 0)
        {
            dict.Add("Failed", $"<span class=\"error\">{failed}</span>");
        }

        if (skipped > 0)
        {
            dict.Add("Skipped", $"<span class=\"warning\">{skipped}</span>");
        }
        
        var sb = new StringBuilder();

        foreach (var (code, count) in dict)
        {
            sb.Append("<tr><td>");
            sb.Append(code);
            sb.Append("</td><td>");
            sb.Append(count);
            sb.Append("</td><td>");
            sb.Append($"<a href=\"statuscode-{code.ToLower()}.html\" target=\"_blank\">Details</a>");
            sb.Append("</td></tr>");
        }

        html = html.Replace("{StatusCodesRows}", sb.ToString());
    }

    /// <summary>
    /// Generate HTML report for a specific accessibility issue severity.
    /// </summary>
    /// <param name="severity">Severity.</param>
    private async Task GenerateAccessibilityIssuesReport(string severity)
    {
        var violations = new List<AccessibilityResultItem>();

        foreach (var entry in Program.Queue.Where(n => n.AccessibilityResults?.Violations?.Length > 0))
        {
            var query =
                from n in entry.AccessibilityResults!.Violations
                where n.Impact == severity
                select n;

            violations.AddRange(query);
        }

        if (violations.Count == 0)
        {
            return;
        }
        
        var html = await this.GetReportTemplateContent("severity-template.html");
        
        //
        html = html.Replace("{Severity}", $"{severity[..1].ToUpper()}{severity[1..].ToLower()}");
        html = html.Replace("{GeneratedAt}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        html = html.Replace("{ProgramVersion}", Program.Version.ToString());
        
        //
        var sb = new StringBuilder();

        var ids = violations
            .Select(n => n.Id)
            .Distinct()
            .OrderBy(n => n)
            .ToArray();

        foreach (var id in ids)
        {
            var details = violations
                .First(n => n.Id == id);

            var temp = violations.FirstOrDefault(n => n.Id == id);
            var message = temp?.Nodes?.FirstOrDefault()?.Message;
            
            sb.AppendLine($"<h2>{id![..1].ToUpper()}{id[1..].ToLower()}</h2>");
            sb.AppendLine("<table><tbody>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Description</td><td>{details.Description}</td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Message</td><td>{message}</td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Help</td><td><a target=\"_blank\" href=\"{details.HelpUrl}\">{details.Help}</a></td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Tags</td><td>{string.Join(", ", details.Tags ?? Array.Empty<string>())}</td></tr>");
            sb.AppendLine("</tbody></table>");

            var urls = new List<string>();
            var guids = violations
                .Where(n => n.Id == id)
                .Select(n => n.Guid)
                .ToArray();

            foreach (var entry in Program.Queue)
            {
                if (entry.AccessibilityResults?.Violations is null ||
                    entry.AccessibilityResults.Violations.All(n => !guids.Contains(n.Guid)))
                {
                    continue;
                }

                var url = entry.Url.ToString();

                if (!urls.Contains(url))
                {
                    urls.Add(url);
                }
            }

            urls = urls
                .OrderBy(n => n)
                .ToList();

            sb.AppendLine("<h3>Affected URLs</h3>");
            sb.AppendLine("<table><tbody>");

            foreach (var url in urls)
            {
                sb.AppendLine($"<tr><td><a href=\"{url}\">{url}</a></td></tr>");    
            }
            
            sb.AppendLine("</tbody></table>");
            sb.AppendLine("<h3>Some of the Violations</h3>");

            var count = 0;

            foreach (var violation in violations.Where(n => n.Id == id))
            {
                if (!(violation.Nodes?.Length > 0))
                {
                    continue;
                }

                foreach (var node in violation.Nodes)
                {
                    count++;

                    if (count == 11)
                    {
                        break;
                    }
                    
                    sb.AppendLine("<div class=\"violation-node\">");
                    sb.AppendLine($"<span>Selector: <code class=\"success\">{node.Target?.Selector}</code></span>");
                    sb.AppendLine($"<span>HTML: <code>{node.Html?.Replace("<", "&lt;").Replace(">", "&gt;")}</code></span>");
                    sb.AppendLine("</div>");
                }

                if (count == 11)
                {
                    break;
                }
            }
        }

        html = html.Replace("{AccessibilityIssues}", sb.ToString());
        
        var path = Path.Combine(
            Program.Options.ReportPath!,
            $"issues-{severity}.html");
        
        await File.WriteAllTextAsync(path, html, Encoding.UTF8);
    }

    /// <summary>
    /// Generate HTML report for each severity level of accessibility issues.
    /// </summary>
    private async Task GenerateAccessibilityIssuesReports()
    {
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

        foreach (var severity in severities)
        {
            await this.GenerateAccessibilityIssuesReport(severity);
        }
    }
    
    /// <summary>
    /// Generate HTML reports from queue and write to disk.
    /// </summary>
    private async Task GenerateHtmlReports()
    {
        if (Program.Queue.IsEmpty)
        {
            return;
        }

        await this.GenerateOverviewReport();
        await this.GenerateAccessibilityIssuesReports();
        await this.GenerateStatusCodeReports();
        await this.GenerateTypeReports();

        foreach (var entry in Program.Queue)
        {
            await this.GenerateQueueEntryReport(entry);
        }
    }
    
    /// <summary>
    /// Generate the main HTML report.
    /// </summary>
    private async Task GenerateOverviewReport()
    {
        var html = await this.GetReportTemplateContent("report-template.html");

        this.AddMetadataToHtmlReport(ref html);
        this.AddStatsToHtmlReport(ref html);
        this.AddAccessibilityIssuesToHtmlReport(ref html);
        this.AddStatusCodesToHtmlReport(ref html);
        
        this.AddQueueEntriesToHtmlReport(ref html, UrlType.InternalWebpage);
        this.AddQueueEntriesToHtmlReport(ref html, UrlType.InternalAsset);
        this.AddQueueEntriesToHtmlReport(ref html, UrlType.ExternalWebpage);
        this.AddQueueEntriesToHtmlReport(ref html, UrlType.ExternalAsset);

        var path = Path.Combine(
            Program.Options.ReportPath!,
            "report.html");
        
        await File.WriteAllTextAsync(path, html, Encoding.UTF8);
    }
    
    /// <summary>
    /// Generate a HTML report for the given queue entry.
    /// </summary>
    /// <param name="entry">Queue entry.</param>
    private async Task GenerateQueueEntryReport(IQueueEntry entry)
    {
        var html = await this.GetReportTemplateContent("entry-template.html");

        this.AddMetadataToHtmlEntryReport(ref html, entry);
        this.AddErrorToHTmlEntryReport(ref html, entry);
        this.AddScreenshotToHtmlEntryReport(ref html, entry);
        this.AddResponseDataToHtmlEntryReport(ref html, entry);
        this.AddResponseBodyDataToHtmlEntryReport(ref html, entry);
        this.AddLinkedFromToHtmlEntryReport(ref html, entry);
        this.AddAccessibilityIssuesToHtmlEntryReport(ref html, entry);

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

    /// <summary>
    /// Generate HTML reports for each status code.
    /// </summary>
    private async Task GenerateStatusCodeReports()
    {
        var codes = Program.Queue
            .Select(n => n.Response?.StatusCode ?? 0)
            .Where(n => n > 0)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        foreach (var code in codes)
        {
            await this.GenerateStatusCodeReport(
                $"{code} {ScannerService.GetStatusDescription(code)}",
                $"statuscode-{code}.html",
                Program.Queue
                    .Where(n => n.Response?.StatusCode == code)
                    .Select(n => (IQueueEntry)n)
                    .ToList());
        }

        await this.GenerateStatusCodeReport(
            "Failed",
            "statuscode-failed.html",
            Program.Queue
                .Where(n => n.Response is null && !n.Skipped)
                .Select(n => (IQueueEntry)n)
                .ToList());
        
        await this.GenerateStatusCodeReport(
            "Skipped",
            "statuscode-skipped.html",
            Program.Queue
                .Where(n => n.Skipped)
                .Select(n => (IQueueEntry)n)
                .ToList());
    }

    /// <summary>
    /// Generate a status-code HTML report with given entries.
    /// </summary>
    /// <param name="title">Report title.</param>
    /// <param name="filename">Filename.</param>
    /// <param name="entries">Queue entries to write.</param>
    private async Task GenerateStatusCodeReport(string title, string filename, IReadOnlyList<IQueueEntry> entries)
    {
        var html = await this.GetReportTemplateContent("custom-template.html");

        html = html.Replace("{Title}", title);
        html = html.Replace("{GeneratedAt}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        html = html.Replace("{ProgramVersion}", Program.Version.ToString());

        var sb = new StringBuilder();
        var count = 0;

        foreach (var urlType in Enum.GetValues<UrlType>())
        {
            var urlTypeTitle = urlType switch
            {
                UrlType.InternalWebpage => "Internal Pages",
                UrlType.InternalAsset => "Internal Assets",
                UrlType.ExternalWebpage => "External Pages",
                UrlType.ExternalAsset => "External Assets",
                _ => throw new Exception($"Unknown UrlType {urlType}")
            };
            
            sb.AppendLine($"<h2>{urlTypeTitle}</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tbody>");

            var urlTypeCount = 0;
            
            foreach (var entry in entries.Where(n => n.UrlType == urlType))
            {
                sb.AppendLine($"<tr><td><a href=\"{entry.Url}\">{entry.Url}</a></td></tr>");
                count++;
                urlTypeCount++;
            }

            if (urlTypeCount == 0)
            {
                sb.AppendLine("<tr><td>None</td></tr>");
            }
            
            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
        }

        if (count == 0)
        {
            return;
        }

        html = html.Replace("{CustomContent}", sb.ToString());
        
        var path = Path.Combine(
            Program.Options.ReportPath!,
            filename);
        
        await File.WriteAllTextAsync(path, html, Encoding.UTF8);
    }

    /// <summary>
    /// Generate HTML reports for each URL type.
    /// </summary>
    private async Task GenerateTypeReports()
    {
        foreach (var urlType in Enum.GetValues<UrlType>())
        {
            var title = urlType switch
            {
                UrlType.InternalWebpage => "Internal Pages",
                UrlType.InternalAsset => "Internal Assets",
                UrlType.ExternalWebpage => "External Pages",
                UrlType.ExternalAsset => "External Assets",
                _ => throw new Exception($"Unknown UrlType {urlType}")
            };

            var slug = urlType switch
            {
                UrlType.InternalWebpage => "internal-pages",
                UrlType.InternalAsset => "internal-assets",
                UrlType.ExternalWebpage => "external-pages",
                UrlType.ExternalAsset => "external-assets",
                _ => throw new Exception($"Unknown UrlType {urlType}")
            };
            
            await this.GenerateTypeReport(
                title,
                $"type-{slug}.html",
                Program.Queue.Where(n => n.UrlType == urlType));
        }
    }

    /// <summary>
    /// Generate a type report.
    /// </summary>
    /// <param name="title">Title.</param>
    /// <param name="filename">Filename.</param>
    /// <param name="entries">Entries.</param>
    private async Task GenerateTypeReport(string title, string filename, IEnumerable<IQueueEntry> entries)
    {
        var html = await this.GetReportTemplateContent("custom-template.html");

        html = html.Replace("{Title}", title);
        html = html.Replace("{GeneratedAt}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        html = html.Replace("{ProgramVersion}", Program.Version.ToString());

        var sb = new StringBuilder();

        sb.AppendLine("<table>");
        sb.AppendLine("<tbody>");

        foreach (var entry in entries)
        {
            sb.AppendLine($"<tr><td><a href=\"{entry.Url}\">{entry.Url}</a></td></tr>");
        }
        
        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        
        html = html.Replace("{CustomContent}", sb.ToString());
        
        var path = Path.Combine(
            Program.Options.ReportPath!,
            filename);
        
        await File.WriteAllTextAsync(path, html, Encoding.UTF8);
    }

    /// <summary>
    /// Get the file content of the given report template.
    /// </summary>
    /// <param name="filename">Filename.</param>
    /// <returns>HTML.</returns>
    private async Task<string> GetReportTemplateContent(string filename)
    {
        var path = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location);

        if (path is null)
        {
            throw new Exception("Unable to locate report template folder.");
        }
        
        var file = Path.Combine(path, filename);
        
        if (!File.Exists(file))
        {
            throw new Exception("Unable to locate report template file.");
        }
        
        var html = await File.ReadAllTextAsync(file);

        return html;
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
            Program.Queue,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
    }
    
    #endregion
}