namespace Core.CleanMap;

public static class CleanMapReportStatus
{
    public const string Dirty = "dirty";
    public const string Cleaned = "cleaned";

    public static bool IsValid(string? status)
    {
        return status is Dirty or Cleaned;
    }
}
