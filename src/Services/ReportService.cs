using System.Text;
using System.Text.Json;
using Serilog;
using Slap.Core;
using Slap.Extenders;
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
            await this.GenerateAndWriteHtmlReport();
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
            sb.AppendLine($"<h3>{violation.Id}</h3>");
            sb.AppendLine("<table><tbody>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Description</td><td>{violation.Description}</td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Help</td><td><a href=\"{violation.HelpUrl}\">{violation.Help}</a></td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Tags</td><td>{string.Join(", ", violation.Tags ?? Array.Empty<string>())}</td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Impact</td><td>{violation.Impact}</td></tr>");
            sb.AppendLine($"<tr><td class=\"info-cell\">Count</td><td>{violation.Nodes.Length}</td></tr>");
            sb.AppendLine("</tbody></table>");

            foreach (var node in violation.Nodes)
            {
                sb.AppendLine("<div class=\"violation-node\">");
                sb.AppendLine($"<span>{node.Message}</span>");
                sb.AppendLine($"<span>Selector: <code class=\"success\">{node.Target?.Selector}</code></span>");
                sb.AppendLine($"<code>{node.Html?.Replace("<", "&lt;").Replace(">", "&gt;")}</code>");
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
        html = html.Replace("{InitialUrl}", Program.Queue.First().Url.Host);
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
            sb.Append($"<tr><td><a href=\"{entry.Url}\">{entry.Url}</a></td>");
            
            // Error?
            if (entry.Error is not null)
            {
                sb.Append($"<td class=\"error\" colspan=\"3\">{entry.Error}</td>");
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

                    sb.Append($"<td class=\"{cssClass}\">{entry.Response.GetStatusFormatted()}</td>");
                }
                else
                {
                    sb.Append("<td>-</td>");
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
                    
                    sb.Append($"<td class=\"{cssClass}\">{entry.Response.GetTimeFormatted()}</td>");
                }
                else
                {
                    sb.Append("<td>-</td>");
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
                    
                    sb.Append($"<td class=\"{cssClass}\">{entry.Response.GetSizeFormatted()}</td>");
                }
                else
                {
                    sb.Append("<td>-</td>");
                }
            }
            
            // Accessibility issues.
            if (entry.UrlType == UrlType.InternalWebpage)
            {
                var count = entry.AccessibilityResults?.Violations?.Length ?? 0;

                cssClass = count > 0
                    ? "error"
                    : "success";
                
                sb.Append($"<td class=\"{cssClass}\">{count}</td>");
            }
            else
            {
                sb.Append("<td>&nbsp;</td>");
            }
            
            // Details.
            sb.AppendLine($"<td><a target=\"_blank\" href=\"entries/entry-{entry.Id}.html\">Details</a></td></tr>");
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
    /// Add stats to HTML report.
    /// </summary>
    /// <param name="html">HTML.</param>
    private void AddStatsToHtmlReport(ref string html)
    {
        var took = Program.Finished - Program.Started;
        var issues = Program.Queue.Sum(n => n.AccessibilityResults?.Violations?.Length ?? 0);

        html = html.Replace("{ScanStarted}", Program.Started.ToString("yyyy-MM-dd HH:mm:ss"));
        html = html.Replace("{ScanFinished}", Program.Finished.ToString("yyyy-MM-dd HH:mm:ss"));
        html = html.Replace("{ScanTook}", $"<span title='{took}'>{took.ToHumanReadable()}</span>");
        html = html.Replace("{TotalAccessibilityIssues}", issues > 0 ? $"<span class=\"error\">{issues}</span>" : issues.ToString());
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
            dict.Add("FAILED", $"<span class=\"error\">{failed}</span>");
        }

        if (skipped > 0)
        {
            dict.Add("SKIPPED", $"<span class=\"warning\">{skipped}</span>");
        }
        
        var sb = new StringBuilder();

        foreach (var (code, count) in dict)
        {
            sb.Append("<tr><td>");
            sb.Append(code);
            sb.Append("</td><td>");
            sb.Append(count);
            sb.Append("</td></tr>");
        }

        html = html.Replace("{StatusCodesRows}", sb.ToString());
    }
    
    /// <summary>
    /// Generate HTML reports from queue and write to disk.
    /// </summary>
    private async Task GenerateAndWriteHtmlReport()
    {
        if (Program.Queue.Count == 0)
        {
            return;
        }

        await this.GenerateAndWriteMainHtmlReport();

        foreach (var entry in Program.Queue)
        {
            await this.GenerateAndWriteQueueEntryHtmlReport(entry);
        }
    }
    
    /// <summary>
    /// Generate the main HTML report.
    /// </summary>
    private async Task GenerateAndWriteMainHtmlReport()
    {
        var templatePath = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location);

        if (templatePath is null)
        {
            throw new Exception("Unable to locate report template folder.");
        }

        const string templateFilename = "report-template.html";
        var templateFile = Path.Combine(templatePath, templateFilename);

        if (!File.Exists(templateFile))
        {
            throw new Exception("Unable to locate report template file.");
        }
        
        var html = await File.ReadAllTextAsync(templateFile);

        this.AddMetadataToHtmlReport(ref html);
        this.AddStatsToHtmlReport(ref html);
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
    private async Task GenerateAndWriteQueueEntryHtmlReport(IQueueEntry entry)
    {
        var templatePath = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location);

        if (templatePath is null)
        {
            throw new Exception("Unable to locate report template folder.");
        }
        
        const string templateFilename = "entry-template.html";
        var templateFile = Path.Combine(templatePath, templateFilename);
        
        if (!File.Exists(templateFile))
        {
            throw new Exception("Unable to locate report template file.");
        }
        
        var html = await File.ReadAllTextAsync(templateFile);

        this.AddMetadataToHtmlEntryReport(ref html, entry);
        this.AddErrorToHTmlEntryReport(ref html, entry);
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