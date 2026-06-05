namespace Core.CleanMap;

public sealed class CleanMapReportService(ICleanMapReportRepository repository) : ICleanMapReportService
{
    public Task<IReadOnlyList<CleanMapReport>> GetAllAsync(CancellationToken cancellationToken)
    {
        return repository.GetAllAsync(cancellationToken);
    }

    public Task<CleanMapReport?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<CreateCleanMapReportResult> CreateAsync(
        CreateCleanMapReportRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCreate(request);
        if (validationError is not null)
        {
            return CreateCleanMapReportResult.ValidationError(validationError);
        }

        var report = await repository.CreateAsync(request, cancellationToken);
        return CreateCleanMapReportResult.Success(report);
    }

    public async Task<MarkCleanMapReportResult> MarkCleanAsync(
        string id,
        MarkCleanRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return MarkCleanMapReportResult.ValidationError("Report id is required.");
        }

        var report = await repository.MarkCleanAsync(id, request, cancellationToken);
        return MarkCleanMapReportResult.Success(report);
    }

    private static string? ValidateCreate(CreateCleanMapReportRequest input)
    {
        if (input.Lat is < -90 or > 90)
        {
            return "Latitude must be between -90 and 90.";
        }

        if (input.Lng is < -180 or > 180)
        {
            return "Longitude must be between -180 and 180.";
        }

        if (input.Tags is null || input.Tags.Count == 0)
        {
            return "At least one waste tag is required.";
        }

        return null;
    }
}
