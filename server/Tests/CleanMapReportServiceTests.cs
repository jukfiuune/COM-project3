using Core.CleanMap.Entities;
using Core.CleanMap.Repositories;
using Core.CleanMap.Services;
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

        public Task<CleanMapReport> CreateAsync(CleanMapReport report)
        {
            report.Id = Guid.NewGuid().ToString();
            Reports.Add(report);
            return Task.FromResult(report);
        }

        public Task<bool> DeleteAsync(string id) => Task.FromResult(true);
        public Task<IReadOnlyList<CleanMapReport>> GetAllAsync() => Task.FromResult((IReadOnlyList<CleanMapReport>)Reports.AsReadOnly());
        public Task<CleanMapReport?> GetByIdAsync(string id) => Task.FromResult(Reports.FirstOrDefault(r => r.Id == id));
        public Task<bool> UpdateAsync(string id, CleanMapReport report) => Task.FromResult(true);
    }

    [Fact]
    public async Task CreateAsync_CallsAiDetection_WhenPhotoBeforeProvided()
    {
        var repo = new FakeCleanMapReportRepository();
        var aiService = new FakeAiDetectionService
        {
            DetectionsToReturn = new List<TrashDetection>
            {
                new() { Label = "plastic_bottle", Confidence = 0.95f }
            }
        };
        var service = new CleanMapReportService(repo, aiService);

        var report = new CleanMapReport
        {
            Lat = 10,
            Lng = 20,
            // valid base64 image data dummy
            PhotoBefore = "data:image/jpeg;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=",
            Tags = new List<string> { "existing_tag" }
        };

        var created = await service.CreateAsync(report);

        Assert.True(aiService.WasCalled);
        Assert.Contains("plastic_bottle", created.Tags);
        Assert.Contains("existing_tag", created.Tags);
    }

    [Fact]
    public async Task CreateAsync_DoesNotCallAiDetection_WhenNoPhotoBefore()
    {
        var repo = new FakeCleanMapReportRepository();
        var aiService = new FakeAiDetectionService();
        var service = new CleanMapReportService(repo, aiService);

        var report = new CleanMapReport
        {
            Lat = 10,
            Lng = 20,
            Tags = new List<string> { "existing_tag" }
        };

        var created = await service.CreateAsync(report);

        Assert.False(aiService.WasCalled);
        Assert.Single(created.Tags);
    }
}
