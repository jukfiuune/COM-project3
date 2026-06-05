namespace Core.CleanMap;

public sealed record MarkCleanMapReportResult(CleanMapReport? Report, string? Error)
{
    public bool HasValidationError => Error is not null;

    public static MarkCleanMapReportResult Success(CleanMapReport? report)
    {
        return new MarkCleanMapReportResult(report, null);
    }

    public static MarkCleanMapReportResult ValidationError(string error)
    {
        return new MarkCleanMapReportResult(null, error);
    }
}
