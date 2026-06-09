namespace Core.CleanMap;

public sealed record CreateCleanMapReportResult(CleanMapReport? Report, string? Error)
{
    public bool IsSuccess => Report is not null;

    public static CreateCleanMapReportResult Success(CleanMapReport report)
    {
        return new CreateCleanMapReportResult(report, null);
    }

    public static CreateCleanMapReportResult ValidationError(string error)
    {
        return new CreateCleanMapReportResult(null, error);
    }
}
