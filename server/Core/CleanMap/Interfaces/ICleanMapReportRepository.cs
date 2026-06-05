namespace Core.CleanMap;

public interface ICleanMapReportRepository
{
    Task<IReadOnlyList<CleanMapReport>> GetAllAsync(CancellationToken cancellationToken);

    Task<CleanMapReport?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<CleanMapReport> CreateAsync(CreateCleanMapReportRequest request, CancellationToken cancellationToken);

    Task<CleanMapReport?> MarkCleanAsync(
        string id,
        MarkCleanRequest request,
        CancellationToken cancellationToken);
}
