using System.Diagnostics;
using Core.Observability;

using Core.CleanMap.Interfaces;

namespace Core.CleanMap;

public sealed class CleanMapReportService(
    ICleanMapReportRepository repository,
    IAiDetectionService aiDetectionService) : ICleanMapReportService
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
        activity?.SetTag("report.has_photo_before", !string.IsNullOrWhiteSpace(request.PhotoBefore));
        activity?.SetTag("report.has_address", !string.IsNullOrWhiteSpace(request.Address));

        var validationError = ValidateCreate(request);
        if (validationError is not null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, validationError);
            CleanMapObservability.ReportsCreateFailed.Add(1);
            return CreateCleanMapReportResult.ValidationError(validationError);
        }

        IReadOnlyList<TrashDetection> detections = [];
        if (!string.IsNullOrWhiteSpace(request.PhotoBefore))
        {
            try
            {
                detections = await aiDetectionService.DetectTrashAsync(request.PhotoBefore, cancellationToken);
                if (detections.Count == 0)
                {
                    const string noTrashError = "Image does not actually contain trash";
                    activity?.SetStatus(ActivityStatusCode.Error, noTrashError);
                    CleanMapObservability.ReportsCreateFailed.Add(1);
                    return CreateCleanMapReportResult.ValidationError(noTrashError);
                }
            }
            catch (Exception)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "AI Detection Failed");
                CleanMapObservability.ReportsCreateFailed.Add(1);
                return CreateCleanMapReportResult.ValidationError("An error occurred");
            }
        }

        var report = await repository.CreateAsync(request, detections, cancellationToken);
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



        return null;
    }
}
