using Common.Correlation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Common.Extensions;

public static class OpenTelemetryExtensions
{
    public static IHostApplicationBuilder AddPlatformOpenTelemetry(
        this IHostApplicationBuilder builder,
        string serviceName)
    {
        var configuration = builder.Configuration;
        var enabled = configuration.GetValue("OpenTelemetry:Enabled", true);

        if (!enabled)
        {
            return builder;
        }

        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"]
            ?? configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? "http://localhost:4317";

        var protocolName = configuration["OpenTelemetry:OtlpProtocol"]
            ?? configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]
            ?? "grpc";

        var resolvedServiceName = configuration["OpenTelemetry:ServiceName"]
            ?? configuration["OTEL_SERVICE_NAME"]
            ?? serviceName;

        if (!Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            endpoint = new Uri("http://localhost:4317");
        }

        var protocol = protocolName.Contains("grpc", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.Grpc
            : OtlpExportProtocol.HttpProtobuf;

        if (protocol == OtlpExportProtocol.Grpc && endpoint.Port == 4318)
        {
            endpoint = new UriBuilder(endpoint) { Port = 4317 }.Uri;
        }

        builder.Services.AddSingleton(new OpenTelemetrySettings(resolvedServiceName, endpoint, protocol));
        builder.Services.AddHostedService<TracerProviderHostedService>();

        return builder;
    }

    private sealed record OpenTelemetrySettings(
        string ServiceName,
        Uri OtlpEndpoint,
        OtlpExportProtocol Protocol);

    private sealed class TracerProviderHostedService(OpenTelemetrySettings settings) : IHostedService
    {
        private TracerProvider? tracerProvider;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            tracerProvider = Sdk.CreateTracerProviderBuilder()
                .ConfigureResource(resource =>
                    resource.AddService(
                        serviceName: settings.ServiceName,
                        serviceVersion: typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
                .SetSampler(new AlwaysOnSampler())
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = httpContext =>
                        !httpContext.Request.Path.StartsWithSegments("/health");
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        var correlationId = request.Headers[CorrelationIdConstants.HeaderName].FirstOrDefault();
                        if (string.IsNullOrWhiteSpace(correlationId)
                            && request.HttpContext.TryGetCorrelationId(out var contextCorrelationId))
                        {
                            correlationId = contextCorrelationId;
                        }

                        if (!string.IsNullOrWhiteSpace(correlationId))
                        {
                            activity.SetTag(CorrelationIdConstants.OtelTagName, correlationId);
                            activity.SetBaggage(CorrelationIdConstants.BaggageName, correlationId);
                        }
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        if (request.Headers.Contains(CorrelationIdConstants.HeaderName))
                        {
                            return;
                        }

                        var correlationId = activity.GetBaggageItem(CorrelationIdConstants.BaggageName)
                            ?? activity.TraceId.ToString();

                        request.Headers.TryAddWithoutValidation(
                            CorrelationIdConstants.HeaderName,
                            correlationId);
                        activity.SetTag(CorrelationIdConstants.OtelTagName, correlationId);
                    };
                })
                .AddSqlClientInstrumentation(options => options.RecordException = true)
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = settings.OtlpEndpoint;
                    options.Protocol = settings.Protocol;
                })
                .Build();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            tracerProvider?.ForceFlush(timeoutMilliseconds: 15_000);
            tracerProvider?.Dispose();
            return Task.CompletedTask;
        }
    }
}
