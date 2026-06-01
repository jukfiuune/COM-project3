using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("cleanmap", policy =>
    {
        policy.WithOrigins("http://localhost:8000", "http://127.0.0.1:8000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSingleton(new CleanMapStore(builder.Environment.ContentRootPath));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("cleanmap");

app.MapGet("/api/cleanmap/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/cleanmap/reports", (CleanMapStore store) => Results.Ok(store.GetAll()));

app.MapPost("/api/cleanmap/reports", (CleanMapReportCreate input, CleanMapStore store) =>
{
    var report = CleanMapReport.FromCreate(input);
    store.Add(report);
    return Results.Created($"/api/cleanmap/reports/{report.Id}", report);
});

app.MapPost("/api/cleanmap/reports/{id}/clean", (string id, CleanMapCleanRequest input, CleanMapStore store) =>
{
    var report = store.MarkClean(id, input);
    return report is null ? Results.NotFound() : Results.Ok(report);
});

app.Run();

sealed class CleanMapStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly object _lock = new();
    private readonly string _path;
    private List<CleanMapReport> _reports = new();

    public CleanMapStore(string rootPath)
    {
        _path = Path.Combine(rootPath, "cleanmap-db.json");
        Load();
    }

    public List<CleanMapReport> GetAll()
    {
        lock (_lock)
        {
            return _reports.Select(report => report).ToList();
        }
    }

    public void Add(CleanMapReport report)
    {
        lock (_lock)
        {
            _reports.Add(report);
            Save();
        }
    }

    public CleanMapReport? MarkClean(string id, CleanMapCleanRequest input)
    {
        lock (_lock)
        {
            var report = _reports.FirstOrDefault(item => item.Id == id);
            if (report is null) return null;

            report.Status = "cleaned";
            report.PhotoAfter = input.PhotoAfter;
            report.CleanedAt = input.CleanedAt ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Save();
            return report;
        }
    }

    private void Load()
    {
        if (!File.Exists(_path)) return;
        var json = File.ReadAllText(_path);
        var data = JsonSerializer.Deserialize<List<CleanMapReport>>(json, JsonOptions);
        if (data is not null)
        {
            _reports = data;
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_reports, JsonOptions);
        File.WriteAllText(_path, json);
    }
}

sealed class CleanMapReport
{
    public string Id { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? Address { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Notes { get; set; }
    public string Status { get; set; } = "dirty";
    public string? PhotoBefore { get; set; }
    public string? PhotoAfter { get; set; }
    public long CreatedAt { get; set; }
    public long? CleanedAt { get; set; }

    public static CleanMapReport FromCreate(CleanMapReportCreate input)
    {
        var id = string.IsNullOrWhiteSpace(input.Id)
            ? $"rep_{Guid.NewGuid():N}".Substring(0, 12)
            : input.Id;
        var createdAt = input.CreatedAt > 0
            ? input.CreatedAt
            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return new CleanMapReport
        {
            Id = id,
            Lat = input.Lat,
            Lng = input.Lng,
            Address = input.Address,
            Tags = input.Tags ?? new List<string>(),
            Notes = input.Notes,
            Status = string.IsNullOrWhiteSpace(input.Status) ? "dirty" : input.Status,
            PhotoBefore = input.PhotoBefore,
            PhotoAfter = input.PhotoAfter,
            CreatedAt = createdAt,
            CleanedAt = input.CleanedAt
        };
    }
}

sealed class CleanMapReportCreate
{
    public string? Id { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? Address { get; set; }
    public List<string>? Tags { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
    public string? PhotoBefore { get; set; }
    public string? PhotoAfter { get; set; }
    public long CreatedAt { get; set; }
    public long? CleanedAt { get; set; }
}

sealed class CleanMapCleanRequest
{
    public string? PhotoAfter { get; set; }
    public long? CleanedAt { get; set; }
}
