using Mockery.Configuration;
using Mockery.Services;

namespace Mockery.Extensions;

/// <summary>
/// Extension methods for registering throttling services.
/// </summary>
public static class ThrottlingExtensions
{
    /// <summary>
    /// Adds throttling services to the dependency injection container.
    /// Configures ThrottlingOptions from the "Throttling" configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddThrottling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration with IOptionsMonitor for hot reload support
        services.Configure<ThrottlingOptions>(
            configuration.GetSection(ThrottlingOptions.SectionName));

        // Register throttling service as singleton for global rate limiting
        services.AddSingleton<IThrottlingService, ThrottlingService>();

        return services;
    }
}
