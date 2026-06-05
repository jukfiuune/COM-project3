namespace Core.CleanMap;

public sealed class CleanMapReport
{
    public string Id { get; init; } = string.Empty;
    public double Lat { get; init; }
    public double Lng { get; init; }
    public string? Address { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? Notes { get; init; }
    public string Status { get; init; } = CleanMapReportStatus.Dirty;
    public string? PhotoBefore { get; init; }
    public string? PhotoAfter { get; init; }
    public long CreatedAt { get; init; }
    public long? CleanedAt { get; init; }
}
