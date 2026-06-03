namespace Infra.RecordStore;

public interface ISqlQueryExecutor
{
    Task<IEnumerable<T>> QueryManyAsync<T>(string query, object? parameters = null, CancellationToken ct = default);
    Task<T?> QuerySingleAsync<T>(string query, object? parameters = null, CancellationToken ct = default);
    Task ExecuteAsync(string query, object? parameters = null, CancellationToken ct = default);
}