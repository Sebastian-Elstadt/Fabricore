using App.Abstractions;
using App.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace App;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApp(this IServiceCollection services)
    {
        services.AddScoped<ITelemetryService, TelemetryService>();

        return services;
    }
}