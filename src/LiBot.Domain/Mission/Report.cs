using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class Report : Entity
{
    public string ReportType { get; private set; }
    public string Summary { get; private set; }

    public Report(string reportType, string summary)
    {
        if (string.IsNullOrWhiteSpace(reportType))
            throw new ArgumentException("Report type is required.", nameof(reportType));
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Report summary is required.", nameof(summary));

        ReportType = reportType.Trim();
        Summary = summary.Trim();
    }
}