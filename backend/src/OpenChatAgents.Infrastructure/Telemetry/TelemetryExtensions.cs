using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenChatAgents.Infrastructure.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenChatAgents.Infrastructure.Telemetry;

public static class TelemetryExtensions
{
    /// <summary>Source name used when wrapping AIAgent/IEmbeddingGenerator instances via UseOpenTelemetry(...).</summary>
    public const string AgentSourceName = "OpenChatAgents.Agent";

    /// <summary>
    /// Registers OpenTelemetry tracing exported via OTLP/HTTP (e.g. to a self-hosted Langfuse instance).
    /// Safe to call from both the Api (<paramref name="includeAspNetCore"/> = true) and the Worker.
    /// </summary>
    public static IHostApplicationBuilder AddOpenChatAgentsTelemetry(this IHostApplicationBuilder builder, string serviceName, bool includeAspNetCore)
    {
        var appOptions = builder.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>() ?? new AppOptions();
        var telemetry = appOptions.Telemetry;

        if (!telemetry.Enabled)
            return builder;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(AppActivitySource.Name)
                    .AddSource(AgentSourceName)
                    .AddHttpClientInstrumentation();

                if (includeAspNetCore)
                    tracing.AddAspNetCoreInstrumentation();

                tracing.AddOtlpExporter(otlp =>
                {
                    // The .NET OTLP/HTTP exporter does not append the per-signal path itself,
                    // so the configured base endpoint (e.g. ".../api/public/otel") needs "/v1/traces" added explicitly.
                    otlp.Endpoint = new Uri(telemetry.OtlpEndpoint.TrimEnd('/') + "/v1/traces");
                    otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
                    otlp.Headers = BuildOtlpHeaders(telemetry);
                });
            });

        return builder;
    }

    private static string BuildOtlpHeaders(TelemetryOptions telemetry)
    {
        var headers = "x-langfuse-ingestion-version=4";

        if (!string.IsNullOrEmpty(telemetry.LangfusePublicKey) && !string.IsNullOrEmpty(telemetry.LangfuseSecretKey))
        {
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{telemetry.LangfusePublicKey}:{telemetry.LangfuseSecretKey}"));
            headers = $"Authorization=Basic {auth},{headers}";
        }

        return headers;
    }
}
