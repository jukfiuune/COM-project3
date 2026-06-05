namespace Core.CleanMap;

public interface ICleanMapReportService
{
    Task<IReadOnlyList<CleanMapReport>> GetAllAsync(CancellationToken cancellationToken);

    Task<CleanMapReport?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<CreateCleanMapReportResult> CreateAsync(
        CreateCleanMapReportRequest request,
        CancellationToken cancellationToken);

    Task<MarkCleanMapReportResult> MarkCleanAsync(
        string id,
        MarkCleanRequest request,
        CancellationToken cancellationToken);
}
