using App.Parts;

namespace App.Abstractions;

public interface IPartsQueries
{
    Task<IEnumerable<PartLog>> GetPartLogsAsync(string partId, CancellationToken ct = default);
}