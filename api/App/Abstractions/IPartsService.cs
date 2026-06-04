using Domain.Parts;

namespace App.Abstractions;

public interface IPartsService {
    Task<bool> TryAddRecordAsync(string partId, DateTime startedOn, CancellationToken ct = default);
    Task<Part> MarkRecordFinishedAsync(string partId, DateTime finishedOn, CancellationToken ct = default);
}