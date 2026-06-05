namespace Core.CleanMap;

public sealed class CreateCleanMapReportRequest
{
    public double Lat { get; init; }
    public double Lng { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public string? PhotoBefore { get; init; }
}
