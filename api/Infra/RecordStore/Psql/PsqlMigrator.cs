using System.Reflection;
using DbUp;

namespace Infra.RecordStore.Psql;

public static class PsqlMigrator
{
    public static void MigrateDatabase(string connectionString)
    {
        DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                s => s.EndsWith(".psql", StringComparison.OrdinalIgnoreCase)
            )
            .WithTransactionPerScript()
            .LogToConsole()
            .Build()
            .PerformUpgrade();
    }
}