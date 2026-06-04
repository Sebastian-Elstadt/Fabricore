using App.Abstractions;
using App.Commands;
using App.Parts;
using App.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace App;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApp(this IServiceCollection services)
    {
        services.AddScoped<ITelemetryService, TelemetryService>();
        services.AddScoped<ICommandsService, CommandsService>();
        services.AddScoped<IPartsService, PartsService>();

        return services;
    }
}