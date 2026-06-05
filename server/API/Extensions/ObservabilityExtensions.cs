using Core.Observability;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace API.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddCleanMapObservability(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        var endpoint = config["Observability:OtlpEndpoint"]
            ?? config["OTEL_EXPORTER_OTLP_ENDPOINT"];

        var enabled = config.GetValue("Observability:Enabled", !string.IsNullOrWhiteSpace(endpoint));
        if (!enabled)
        {
            return builder;
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = "http://grafana-alloy:4317";
        }

        var serviceName = config.GetValue("Observability:ServiceName", "cleanmap-api");
        var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion);

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.SetResourceBuilder(resourceBuilder);
            options.AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(endpoint);
            });
        });

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(opt =>
                    {
                        opt.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource(CleanMapObservability.ActivitySourceName)
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(endpoint);
                    });
            })
            .WithMetrics(metricsProviderBuilder =>
            {
                metricsProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(CleanMapObservability.MeterName)
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(endpoint);
                    });
            });

        return builder;
    }
}
