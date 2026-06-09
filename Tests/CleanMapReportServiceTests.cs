using Core.CleanMap;
using Core.Services;
using Xunit;

namespace Tests;

public sealed class CleanMapReportServiceTests
{
    private class FakeAiDetectionService : IAiDetectionService
    {
        public List<TrashDetection> DetectionsToReturn { get; set; } = new();
        public bool WasCalled { get; private set; }

        public Task<List<TrashDetection>> DetectTrashAsync(Stream imageStream, string fileName)
        {
            WasCalled = true;
            return Task.FromResult(DetectionsToReturn);
        }
    }

    private class FakeCleanMapReportRepository : ICleanMapReportRepository
    {
        public List<CleanMapReport> Reports { get; } = new();

        public Task<CleanMapReport> CreateAsync(CreateCleanMapReportRequest request, CancellationToken cancellationToken)
        {
            var report = new CleanMapReport
            {
                Id = Guid.NewGuid().ToString(),
                Lat = request.Lat,
                Lng = request.Lng,
                PhotoBefore = request.PhotoBefore,
                Tags = request.Tags ?? new List<string>()
            };
            Reports.Add(report);
            return Task.FromResult(report);
        }

        public Task<CleanMapReport?> MarkCleanAsync(string id, MarkCleanRequest request, CancellationToken cancellationToken) => Task.FromResult<CleanMapReport?>(null);
        public Task<IReadOnlyList<CleanMapReport>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult((IReadOnlyList<CleanMapReport>)Reports.AsReadOnly());
        public Task<CleanMapReport?> GetByIdAsync(string id, CancellationToken cancellationToken) => Task.FromResult(Reports.FirstOrDefault(r => r.Id == id));
    }

    [Fact]
    public async Task CreateAsync_CallsAiDetection_WhenPhotoBeforeProvided()
    {
        var repo = new FakeCleanMapReportRepository();
        var aiService = new FakeAiDetectionService
        {
            DetectionsToReturn = new List<TrashDetection>
            {
                new() { label = "plastic_bottle", confidence = 0.95f }
            }
        };
        var service = new CleanMapReportService(repo, aiService);

        var request = new CreateCleanMapReportRequest
        {
            Lat = 10,
            Lng = 20,
            PhotoBefore = "data:image/jpeg;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=",
            Tags = new List<string> { "existing_tag" }
        };

        var created = await service.CreateAsync(request, CancellationToken.None);

        Assert.True(aiService.WasCalled);
        Assert.NotNull(created.Report);
        Assert.Contains("plastic_bottle", created.Report.Tags);
        Assert.Contains("existing_tag", created.Report.Tags);
    }

    [Fact]
    public async Task CreateAsync_DoesNotCallAiDetection_WhenNoPhotoBefore()
    {
        var repo = new FakeCleanMapReportRepository();
        var aiService = new FakeAiDetectionService();
        var service = new CleanMapReportService(repo, aiService);

        var request = new CreateCleanMapReportRequest
        {
            Lat = 10,
            Lng = 20,
            Tags = new List<string> { "existing_tag" }
        };

        var created = await service.CreateAsync(request, CancellationToken.None);

        Assert.False(aiService.WasCalled);
        Assert.NotNull(created.Report);
        Assert.Single(created.Report.Tags);
    }
}
