using Npgsql;
using Dapper;

namespace Infra.RecordStore.Psql;

public class PsqlQueryExecutor : ISqlQueryExecutor
{
    protected readonly NpgsqlConnection connection;
    protected NpgsqlTransaction? transaction = null;

    public PsqlQueryExecutor(string connectionString)
    {
        connection = new(connectionString);
    }

    public Task<int> ExecuteAsync(string query, object? parameters = null, CancellationToken ct = default)
    {
        var command = new CommandDefinition(query, parameters, transaction, cancellationToken: ct);
        return connection.ExecuteAsync(command);
    }

    public Task<IEnumerable<T>> QueryManyAsync<T>(string query, object? parameters = null, CancellationToken ct = default)
    {
        var command = new CommandDefinition(query, parameters, transaction, cancellationToken: ct);
        return connection.QueryAsync<T>(command);
    }

    public Task<T?> QuerySingleAsync<T>(string query, object? parameters = null, CancellationToken ct = default)
    {
        var command = new CommandDefinition(query, parameters, transaction, cancellationToken: ct);
        return connection.QuerySingleOrDefaultAsync<T>(command);
    }
}