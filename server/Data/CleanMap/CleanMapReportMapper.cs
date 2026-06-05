using Core.CleanMap;

namespace Data.CleanMap;

internal static class CleanMapReportMapper
{
    public static CleanMapReport ToDomain(this CleanMapReportDocument document)
    {
        return new CleanMapReport
        {
            Id = document.Id,
            Lat = document.Lat,
            Lng = document.Lng,
            Address = document.Address,
            Tags = document.Tags,
            Notes = document.Notes,
            Status = document.Status,
            PhotoBefore = document.PhotoBefore,
            PhotoAfter = document.PhotoAfter,
            CreatedAt = document.CreatedAt,
            CleanedAt = document.CleanedAt
        };
    }

    public static CleanMapReportDocument FromCreate(CreateCleanMapReportRequest request, IReadOnlyList<TrashDetection> detections)
    {
        return new CleanMapReportDocument
        {
            Id = $"rep_{Guid.NewGuid():N}"[..16],
            Lat = request.Lat,
            Lng = request.Lng,
            Location = new GeoJsonPointDocument
            {
                Coordinates = [request.Lng, request.Lat]
            },
            Address = Normalize(request.Address),
            Tags = detections
                .Select(d => d.Label)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Select(label => label.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Notes = Normalize(request.Notes),
            Status = CleanMapReportStatus.Dirty,
            PhotoBefore = Normalize(request.PhotoBefore),
            PhotoAfter = null,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            CleanedAt = null
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
