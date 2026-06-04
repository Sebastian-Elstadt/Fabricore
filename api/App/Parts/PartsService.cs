using App.Abstractions;
using Domain.Parts;

namespace App.Parts;

public class PartsService(IRecordStore recordStore) : IPartsService
{
    public async Task AddRecordAsync(string partId, DateTime startedOn, CancellationToken ct = default)
    {
        var part = new Part(partId, startedOn);
        await recordStore.PartRepository.AddAsync(part, ct);
    }

    public async Task MarkRecordFinishedAsync(string partId, DateTime finishedOn, CancellationToken ct = default)
    {
        var part = await recordStore.PartRepository.GetByIdAsync(partId, ct);
        if (part is null) throw new InvalidOperationException($"No part was found with Id: {partId}");

        part.FinishedOn = finishedOn;
        await recordStore.PartRepository.UpdateAsync(part, ct);
    }
}