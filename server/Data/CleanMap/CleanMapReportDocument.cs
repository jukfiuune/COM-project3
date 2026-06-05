using MongoDB.Bson.Serialization.Attributes;

namespace Data.CleanMap;

internal sealed class CleanMapReportDocument
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    [BsonElement("lat")]
    public double Lat { get; init; }

    [BsonElement("lng")]
    public double Lng { get; init; }

    [BsonElement("location")]
    public GeoJsonPointDocument Location { get; init; } = new();

    [BsonElement("address")]
    public string? Address { get; init; }

    [BsonElement("tags")]
    public List<string> Tags { get; init; } = [];

    [BsonElement("notes")]
    public string? Notes { get; init; }

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("photoBefore")]
    public string? PhotoBefore { get; init; }

    [BsonElement("photoAfter")]
    public string? PhotoAfter { get; init; }

    [BsonElement("createdAt")]
    public long CreatedAt { get; init; }

    [BsonElement("cleanedAt")]
    public long? CleanedAt { get; init; }
}

internal sealed class GeoJsonPointDocument
{
    [BsonElement("type")]
    public string Type { get; init; } = "Point";

    [BsonElement("coordinates")]
    public double[] Coordinates { get; init; } = [0, 0];
}
