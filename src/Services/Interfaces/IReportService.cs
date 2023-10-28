namespace Slap.Services.Interfaces;

public interface IReportService
{
    /// <summary>
    /// Generate JSON and HTML reports.
    /// </summary>
    Task GenerateReports();
}