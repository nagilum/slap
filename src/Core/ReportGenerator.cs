using System.Globalization;
using System.Text;
using System.Text.Json;
using Slap.Extensions;
using Slap.Models;
using Slap.Tools;

namespace Slap.Core;

internal class ReportGenerator
{
    /// <summary>
    /// Options.
    /// </summary>
    private readonly Options _options;

    /// <summary>
    /// Queue.
    /// </summary>
    private readonly List<QueueEntry> _queue;

    /// <summary>
    /// Stats.
    /// </summary>
    private readonly ScanStats _stats;
    
    /// <summary>
    /// Initialize a new instance of a <see cref="ReportGenerator"/> class.
    /// </summary>
    /// <param name="options">Options.</param>
    /// <param name="queue">Queue.</param>
    /// <param name="stats">Stats.</param>
    public ReportGenerator(
        Options options,
        List<QueueEntry> queue,
        ScanStats stats)
    {
        this._options = options;
        this._queue = queue;
        this._stats = stats;
    }

    /// <summary>
    /// Write JSON reports to disk.
    /// </summary>
    public async Task WriteJsonReports()
    {
        var path = Path.GetRelativePath(
            Directory.GetCurrentDirectory(),
            Program.ReportPath!);
        
        ConsoleEx.Write(
            "Writing reports to ",
            ConsoleColor.Yellow,
            ".",
            Path.DirectorySeparatorChar,
            path,
            Environment.NewLine);
        
        await this.WriteJsonReport(Program.ReportPath!, "options.json", this._options);
        await this.WriteJsonReport(Program.ReportPath!, "queue.json", this._queue);
        await this.WriteJsonReport(Program.ReportPath!, "stats.json", this._stats);
    }
    
    /// <summary>
    /// Generate and write HTML report to disk.
    /// </summary>
    public async Task WriteHtmlReport()
    {
        if (this._queue.Count == 0)
        {
            return;
        }
        
        try
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
            
            this.AddLinksDetailsToHtmlReport(ref html);

            var path = Path.Combine(
                Program.ReportPath!,
                "report.html");

            await File.WriteAllTextAsync(path, html, Encoding.UTF8);
            
            ConsoleEx.Write(
                "Wrote HTML report to ",
                ConsoleColor.Yellow,
                ".",
                Path.DirectorySeparatorChar,
                templateFilename,
                Environment.NewLine);
        }
        catch (Exception ex)
        {
            ConsoleEx.Write(
                "Error while compiling and writing HTML report to disk. ",
                Environment.NewLine,
                ConsoleColor.Red,
                ex.Message,
                Environment.NewLine);
        }
    }
    
    #region Helper functions

    /// <summary>
    /// Add links to HTML report.
    /// </summary>
    /// <param name="html">HTML.</param>
    /// <param name="urlType">Link type.</param>
    private void AddQueueEntriesToHtmlReport(ref string html, UrlType urlType)
    {
        var culture = new CultureInfo("en-US");
        var sb = new StringBuilder();

        foreach (var entry in this._queue.Where(n => n.UrlType == urlType))
        {
            string cssClass;
            
            // Identifier.
            sb.Append("<tr class=\"clickable\" id=\"");
            sb.Append(entry.Id);
            sb.Append("\">");
            
            // URL.
            sb.Append("<td><a href=\"");
            sb.Append(entry.Url);
            sb.Append("\">");
            sb.Append(entry.Url);
            sb.Append("</a></td>");
            
            // Status code.
            if (entry.Response?.StatusCode > 0)
            {
                cssClass = entry.Response.StatusCode switch
                {
                    >= 200 and <= 299 => "success",
                    <= 399 => "warning",
                    _ => "error"
                };
                
                sb.Append("<td class=\"");
                sb.Append(cssClass);
                sb.Append("\">");
                
                sb.Append(entry.Response.StatusCode);
                sb.Append(' ');
                sb.Append(entry.Response.StatusDescription);
                
                sb.Append("</td>");
            }
            else
            {
                sb.Append("<td class=\"error\" title=\"");
                sb.Append(entry.Error);
                sb.Append("\">Error</td>");
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
                
                var timeFormatted = entry.Response.Time switch
                {
                    > 60 * 1000 => $"{(entry.Response.Time / (60M * 1000M)).ToString(culture)} m",
                    > 1000 => $"{(entry.Response.Time / 1000M).ToString(culture)} s",
                    _ => $"{entry.Response.Time} ms"
                };
                
                sb.Append("<td class=\"");
                sb.Append(cssClass);
                sb.Append("\">");
                
                sb.Append(timeFormatted);
                
                sb.Append("</td>");
            }
            else
            {
                sb.Append("<td class=\"error\">-</td>");
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
                
                var sizeFormatted = entry.Response.Size switch
                {
                    > 1000000 => $"{(entry.Response.Size / 1000000).ToString("#.##", culture)} MB",
                    > 1000 => $"{(entry.Response.Size / 1000).ToString("#.##", culture)} KB",
                    _ => $"{entry.Response.Size} B"
                };
                
                sb.Append("<td class=\"");
                sb.Append(cssClass);
                sb.Append("\">");
                
                sb.Append(sizeFormatted);
                
                sb.Append("</td>");
            }
            else
            {
                sb.Append("<td class=\"error\">-</td>");
            }
            
            // Accessibility issues.
            if (urlType == UrlType.InternalWebpage)
            {
                if (entry.AccessibilityResults is not null)
                {
                    if (entry.AccessibilityResults.Violations?.Length > 0)
                    {
                        sb.Append("<td class=\"error\">");
                        sb.Append(entry.AccessibilityResults.Violations.Length);
                        sb.Append("</td>");
                    }
                    else
                    {
                        sb.Append("<td class=\"success\">0</td>");
                    }
                }
                else
                {
                    sb.Append("<td class=\"success\">0</td>");
                }
            }
            
            // Done.
            sb.Append("</tr>");
        }

        html = html.Replace($"{{Links{urlType}}}", sb.ToString());
    }
    
    /// <summary>
    /// Add link details for each item in queue to HTML report.
    /// </summary>
    /// <param name="html">HTML.</param>
    private void AddLinksDetailsToHtmlReport(ref string html)
    {
    }

    /// <summary>
    /// Add metadata to HTML report.
    /// </summary>
    /// <param name="html">HTML.</param>
    private void AddMetadataToHtmlReport(ref string html)
    {
        html = html.Replace("{InitialUrl}", this._queue[0].Url.Host);
        html = html.Replace("{GeneratedAt}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        html = html.Replace("{ProgramVersion}", Program.ProgramVersion.ToString());
    }
    
    /// <summary>
    /// Add stats to HTML report.
    /// </summary>
    /// <param name="html">HTML.</param>
    private void AddStatsToHtmlReport(ref string html)
    {
        var stats = new StringBuilder();

        stats.Append("<tr><td>Started</td><td>");
        stats.Append(this._stats.Meta!.Started.ToString("yyyy-MM-dd HH:mm:ss"));
        stats.Append("</td></tr>");
        
        stats.Append("<tr><td>Finished</td><td>");
        stats.Append(this._stats.Meta!.Finished.ToString("yyyy-MM-dd HH:mm:ss"));
        stats.Append("</td></tr>");
        
        stats.Append("<tr><td>Took</td><td><span title=\"");
        stats.Append(this._stats.Meta!.Took.ToString());
        stats.Append("\">");
        stats.Append(this._stats.Meta!.Took.ToHumanReadable());
        stats.Append("</span></td></tr>");
        
        stats.Append("<tr><td>Accessibility Issues</td><td>");
        stats.Append(this._stats.Accessibility!.Violations);
        stats.Append("</td></tr>");

        html = html.Replace("{StatsRows}", stats.ToString());
    }
    
    /// <summary>
    /// Add status codes to HTML report.
    /// </summary>
    /// <param name="html">HTML.</param>
    private void AddStatusCodesToHtmlReport(ref string html)
    {
        var codes = new StringBuilder();

        foreach (var (code, count) in this._stats.StatusCodes!)
        {
            codes.Append("<tr><td>");
            codes.Append(code);
            codes.Append("</td><td>");
            codes.Append(count);
            codes.Append("</td></tr>");
        }

        html = html.Replace("{StatusCodesRows}", codes.ToString());
    }
    
    /// <summary>
    /// Write a single JSON report to disk.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <param name="filename">Filename.</param>
    /// <param name="data">Data to write.</param>
    /// <typeparam name="T">Data type.</typeparam>
    private async Task WriteJsonReport<T>(string path, string filename, T data)
    {
        var fullPath = Path.Combine(
            path,
            filename);

        try
        {
            await using var stream = File.OpenWrite(fullPath);
            await JsonSerializer.SerializeAsync(
                stream,
                data,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

            ConsoleEx.Write(
                "Wrote JSON report to ",
                ConsoleColor.Yellow,
                ".",
                Path.DirectorySeparatorChar,
                filename,
                Environment.NewLine);
        }
        catch (Exception ex)
        {
            ConsoleEx.Write(
                "Error while writing to file ",
                ConsoleColor.Yellow,
                ".",
                Path.DirectorySeparatorChar,
                filename,
                Environment.NewLine,
                ConsoleColor.Red,
                ex.Message,
                Environment.NewLine);
        }
    }
    
    #endregion
}