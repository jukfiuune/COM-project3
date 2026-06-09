using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Core.Observability;

public static class CleanMapObservability
{
    public const string ActivitySourceName = "cleanmap-api";
    public const string MeterName = "cleanmap-api";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> ReportsCreated =
        Meter.CreateCounter<long>("cleanmap.reports.created");

    public static readonly Counter<long> ReportsCreateFailed =
        Meter.CreateCounter<long>("cleanmap.reports.create_failed");

    public static readonly Counter<long> ReportsCleaned =
        Meter.CreateCounter<long>("cleanmap.reports.cleaned");

    public static readonly Counter<long> ReportsCleanFailed =
        Meter.CreateCounter<long>("cleanmap.reports.clean_failed");
}
