using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Otm.Server.OTel.Activities;

namespace Otm.Server.OTel;

public static class TelemetryService
{
    public static IServiceCollection AddOTel(
        this IServiceCollection services,
        string serviceName,
        string[] customTracings = null,
        string[] customMetrics = null
    )
    {
        var otel = services.AddOpenTelemetry();

        Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
            [
                new TraceContextPropagator(),
                new BaggagePropagator()
            ]));

        otel.ConfigureResource(resource =>
            resource.AddService(serviceName));

        otel.WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter(PalantirActivity.Name)
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            .AddMeter("System.Net.Http")
            .AddMeter("System.Net.NameResolution")
            .AddMeter(customMetrics ?? [])
            .AddOtlpExporter(o =>
            {
                o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            }));

        otel.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
            tracing.AddSource(PalantirActivity.Name);
            tracing.AddSource(customTracings ?? []);
            tracing.AddOtlpExporter(o =>
            {
                o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            });
        });

        return services;
    }
}