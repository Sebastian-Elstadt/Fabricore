using App.Abstractions;
using Infra.RecordStore;
using Infra.RecordStore.Psql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Infra;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfra(this IServiceCollection services, IConfiguration config)
    {
        var recordStoreConfig = config.GetRequiredSection("RecordStore").Get<RecordStoreConfig>();
        if (recordStoreConfig is null) throw new InvalidOperationException("RecordStoreConfig is null");
        recordStoreConfig.EnsureValid();

        PsqlMigrator.MigrateDatabase(recordStoreConfig.ConnectionString);

        services.AddScoped<IRecordStore, PsqlRecordStore>(sp => new PsqlRecordStore(recordStoreConfig));
        return services;
    }
}