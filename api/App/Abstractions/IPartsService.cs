namespace App.Abstractions;

public interface IPartsService {
    Task AddRecordAsync(string partId, DateTime startedOn, CancellationToken ct = default);
    Task MarkRecordFinishedAsync(string partId, DateTime finishedOn, CancellationToken ct = default);
}