namespace Mockery.Extensions;

using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Provides extension methods for configuring OpenTelemetry observability in ASP.NET Core applications.
/// Uses standard OpenTelemetry environment variables for configuration:
/// - OTEL_EXPORTER_OTLP_ENDPOINT: The OTLP endpoint URL (e.g., http://aspire-dashboard:18889)
/// - OTEL_EXPORTER_OTLP_PROTOCOL: The protocol to use (grpc or http/protobuf, defaults to grpc)
/// - OTEL_SERVICE_NAME: The service name (defaults to application name)
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Configures OpenTelemetry observability (logging, metrics, tracing) for ASP.NET Core applications.
    /// Configuration is controlled via standard OTEL environment variables.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder to configure</param>
    /// <returns>The configured WebApplicationBuilder</returns>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
            ?? builder.Environment.ApplicationName;

        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        var otlpProtocol = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL") ?? "grpc";
        var otlpHeaders = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");

        // Configure logging
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;

            // OTLP exporter will automatically use OTEL_EXPORTER_OTLP_ENDPOINT and
            // OTEL_EXPORTER_OTLP_PROTOCOL environment variables
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                try
                {
                    options.AddOtlpExporter();
                }
                catch (Exception)
                {
                    // Fallback to console exporter if OTLP fails
                    options.AddConsoleExporter();
                }
            }
            else
            {
                // Fallback to console if no OTLP endpoint configured
                options.AddConsoleExporter();
            }
        });

        // Configure OpenTelemetry metrics and tracing
        var otel = builder.Services.AddOpenTelemetry();

        // Configure OpenTelemetry Resources with the service name and additional attributes
        otel.ConfigureResource(resource => resource
            .AddService(
                serviceName: serviceName,
                serviceVersion: "1.0.0",
                serviceInstanceId: Environment.MachineName)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName,
                ["service.namespace"] = "mockery"
            }));

        // Configure Metrics
        otel.WithMetrics(metrics =>
        {
            // Add application-specific meter
            metrics.AddMeter(serviceName);

            // Add built-in instrumentations
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddHttpClientInstrumentation();

            // Add ASP.NET Core built-in meters (.NET 9)
            metrics.AddMeter("Microsoft.AspNetCore.Hosting");
            metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");

            // Add System.Net meters
            metrics.AddMeter("System.Net.Http");
            metrics.AddMeter("System.Net.NameResolution");

            // Add exporters
            metrics.AddPrometheusExporter();

            // OTLP exporter will automatically use OTEL_EXPORTER_OTLP_ENDPOINT and
            // OTEL_EXPORTER_OTLP_PROTOCOL environment variables
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                try
                {
                    metrics.AddOtlpExporter();
                }
                catch (Exception)
                {
                    // Fallback to console exporter if OTLP fails
                    metrics.AddConsoleExporter();
                }
            }
            else
            {
                // Fallback to console if no OTLP endpoint configured
                metrics.AddConsoleExporter();
            }
        });

        // Configure Tracing
        otel.WithTracing(tracing =>
        {
            // Add built-in instrumentations
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();

            // Add application-specific activity source
            tracing.AddSource(serviceName);

            // OTLP exporter will automatically use OTEL_EXPORTER_OTLP_ENDPOINT and
            // OTEL_EXPORTER_OTLP_PROTOCOL environment variables
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                try
                {
                    tracing.AddOtlpExporter();
                }
                catch (Exception)
                {
                    // Fallback to console exporter if OTLP fails
                    tracing.AddConsoleExporter();
                }
            }
            else
            {
                // Fallback to console if no OTLP endpoint configured
                tracing.AddConsoleExporter();
            }
        });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry middleware and endpoints for ASP.NET Core applications.
    /// </summary>
    public static WebApplication UseObservability(this WebApplication app)
    {
        // Configure the Prometheus scraping endpoint
        app.MapPrometheusScrapingEndpoint();

        return app;
    }
}
