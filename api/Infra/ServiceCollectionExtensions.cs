using App.Abstractions;
using Dapper;
using Infra.Queries;
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
        DefaultTypeMap.MatchNamesWithUnderscores = true; // dapper

        services.AddScoped(sp => new PsqlRecordStore(recordStoreConfig));
        services.AddScoped<IRecordStore>(sp => sp.GetRequiredService<PsqlRecordStore>());
        services.AddScoped<ISqlQueryExecutor>(sp => sp.GetRequiredService<PsqlRecordStore>());
        services.AddScoped<IFactoryQueries, PsqlFactoryQueries>();
        return services;
    }
}