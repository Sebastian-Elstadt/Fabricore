using App.Abstractions;
using Infra.RecordStore;
using Microsoft.Extensions.DependencyInjection;

namespace Infra;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfra(this IServiceCollection services)
    {
        services.AddScoped<IRecordStore, PsqlRecordStore>();
        return services;
    }
}