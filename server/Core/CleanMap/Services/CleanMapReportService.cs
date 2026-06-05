using System.Diagnostics;
using Core.Observability;

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
        using var activity = CleanMapObservability.ActivitySource.StartActivity("cleanmap.report.create");
        activity?.SetTag("report.tags.count", request.Tags?.Count ?? 0);
        activity?.SetTag("report.has_photo_before", !string.IsNullOrWhiteSpace(request.PhotoBefore));
        activity?.SetTag("report.has_address", !string.IsNullOrWhiteSpace(request.Address));

        var validationError = ValidateCreate(request);
        if (validationError is not null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, validationError);
            CleanMapObservability.ReportsCreateFailed.Add(1);
            return CreateCleanMapReportResult.ValidationError(validationError);
        }

        var report = await repository.CreateAsync(request, cancellationToken);
        CleanMapObservability.ReportsCreated.Add(1);
        return CreateCleanMapReportResult.Success(report);
    }

    public async Task<MarkCleanMapReportResult> MarkCleanAsync(
        string id,
        MarkCleanRequest request,
        CancellationToken cancellationToken)
    {
        using var activity = CleanMapObservability.ActivitySource.StartActivity("cleanmap.report.clean");
        activity?.SetTag("report.id", id);

        if (string.IsNullOrWhiteSpace(id))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Report id is required.");
            CleanMapObservability.ReportsCleanFailed.Add(1);
            return MarkCleanMapReportResult.ValidationError("Report id is required.");
        }

        var report = await repository.MarkCleanAsync(id, request, cancellationToken);
        if (report is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Report not found.");
            CleanMapObservability.ReportsCleanFailed.Add(1);
        }
        else
        {
            CleanMapObservability.ReportsCleaned.Add(1);
        }
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
