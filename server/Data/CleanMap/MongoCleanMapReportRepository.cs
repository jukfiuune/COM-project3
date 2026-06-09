using Core.CleanMap;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Data.CleanMap;

public sealed class MongoCleanMapReportRepository : ICleanMapReportRepository, ICleanMapHealthCheck
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<CleanMapReportDocument> _reports;
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private bool _indexesCreated;

    public MongoCleanMapReportRepository(IOptions<CleanMapMongoOptions> options)
    {
        var settings = options.Value;
        var url = MongoUrl.Create(settings.ConnectionString);
        var mongoSettings = MongoClientSettings.FromUrl(url);
        mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);

        var client = new MongoClient(mongoSettings);

        _database = client.GetDatabase(settings.DatabaseName);
        _reports = _database.GetCollection<CleanMapReportDocument>(settings.ReportsCollectionName);
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            var command = new BsonDocument("ping", 1);
            await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
            await EnsureIndexesAsync(cancellationToken);
            return true;
        }
        catch (MongoException)
        {
            return false;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<CleanMapReport>> GetAllAsync(CancellationToken cancellationToken)
    {
        await EnsureIndexesAsync(cancellationToken);

        var documents = await _reports
            .Find(Builders<CleanMapReportDocument>.Filter.Empty)
            .SortByDescending(report => report.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<CleanMapReport?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        await EnsureIndexesAsync(cancellationToken);

        var document = await _reports
            .Find(report => report.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<CleanMapReport> CreateAsync(
        CreateCleanMapReportRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureIndexesAsync(cancellationToken);

        var document = CleanMapReportMapper.FromCreate(request);
        await _reports.InsertOneAsync(document, cancellationToken: cancellationToken);

        return document.ToDomain();
    }

    public async Task<CleanMapReport?> MarkCleanAsync(
        string id,
        MarkCleanRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureIndexesAsync(cancellationToken);

        var update = Builders<CleanMapReportDocument>.Update
            .Set(report => report.Status, CleanMapReportStatus.Cleaned)
            .Set(report => report.PhotoAfter, Normalize(request.PhotoAfter))
            .Set(report => report.CleanedAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        var options = new FindOneAndUpdateOptions<CleanMapReportDocument>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updated = await _reports.FindOneAndUpdateAsync(
            report => report.Id == id,
            update,
            options,
            cancellationToken);

        return updated?.ToDomain();
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        if (_indexesCreated) return;

        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (_indexesCreated) return;

            var indexes = new[]
            {
                new CreateIndexModel<CleanMapReportDocument>(
                    Builders<CleanMapReportDocument>.IndexKeys.Descending(report => report.CreatedAt)),
                new CreateIndexModel<CleanMapReportDocument>(
                    Builders<CleanMapReportDocument>.IndexKeys.Ascending(report => report.Status)),
                new CreateIndexModel<CleanMapReportDocument>(
                    Builders<CleanMapReportDocument>.IndexKeys.Geo2DSphere("location"))
            };

            await _reports.Indexes.CreateManyAsync(indexes, cancellationToken);
            _indexesCreated = true;
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
