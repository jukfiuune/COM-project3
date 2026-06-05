namespace Data.CleanMap;

public sealed class CleanMapMongoOptions
{
    public const string SectionName = "CleanMapDatabase";

    public string ConnectionString { get; init; } = "mongodb://localhost:27017";
    public string DatabaseName { get; init; } = "nexora";
    public string ReportsCollectionName { get; init; } = "reports";
}
