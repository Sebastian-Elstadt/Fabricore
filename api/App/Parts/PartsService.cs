using App.Abstractions;
using Domain.Parts;

namespace App.Parts;

public class PartsService(IRecordStore recordStore) : IPartsService
{
    public async Task<bool> TryAddRecordAsync(string partId, DateTime startedOn, CancellationToken ct = default)
    {
        try
        {
            var part = new Part(partId, startedOn);
            await recordStore.PartRepository.AddAsync(part, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task MarkRecordFinishedAsync(string partId, DateTime finishedOn, CancellationToken ct = default)
    {
        var part = await recordStore.PartRepository.GetByIdAsync(partId, ct);
        if (part is null) throw new InvalidOperationException($"No part was found with Id: {partId}");

        part.FinishedOn = finishedOn;
        await recordStore.PartRepository.UpdateAsync(part, ct);
    }
}