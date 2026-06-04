using Domain.Parts;

namespace App.Abstractions;

public interface IPartRepository {
    Task AddAsync(Part part, CancellationToken ct = default);
    Task UpdateAsync(Part part, CancellationToken ct = default);
    Task<Part?> GetByIdAsync(string id, CancellationToken ct = default);
}