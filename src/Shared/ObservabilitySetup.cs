using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared;

public sealed class ObservabilitySetup : IDisposable
{
    public const string SourceName = "Observability";
    private const string ServiceName = "AgentObservability";
    private const string OtlpEndpoint = "http://localhost:4317";

    public TracerProvider? TracerProvider { get; }
    public MeterProvider? MeterProvider { get; }

    public ObservabilitySetup(bool consoleExporter = true)
    {
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(ServiceName);

        var tracerBuilder = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource(SourceName)
            .AddOtlpExporter(o => o.Endpoint = new Uri(OtlpEndpoint));

        if (consoleExporter)
            tracerBuilder.AddConsoleExporter();

        TracerProvider = tracerBuilder.Build();

        var meterBuilder = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddMeter(SourceName)
            .AddOtlpExporter(o => o.Endpoint = new Uri(OtlpEndpoint));

        if (consoleExporter)
            meterBuilder.AddConsoleExporter();

        MeterProvider = meterBuilder.Build();
    }

    public void Dispose()
    {
        TracerProvider?.Dispose();
        MeterProvider?.Dispose();
    }
}
