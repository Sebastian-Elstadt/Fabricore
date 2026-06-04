using App.Factory;

namespace App.Abstractions;

public interface IFactoryQueries
{
    Task<FactoryStateSnapshot> GetFactoryStateAsync(CancellationToken ct = default);
}