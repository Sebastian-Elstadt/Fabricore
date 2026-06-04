using App.Abstractions;
using Domain.Parts;
using Infra.RecordStore;

namespace Infra.Repositories;

public class PsqlPartRepository(ISqlQueryExecutor executor) : IPartRepository
{
    public async Task AddAsync(Part part, CancellationToken ct = default)
    {
        await executor.ExecuteAsync(
            """
            INSERT INTO parts (id, started_on, finished_on) VALUES (@Id, @StartedOn, @FinishedOn::TIMESTAMPTZ);
            """,
            new
            {
                part.Id,
                part.StartedOn,
                part.FinishedOn
            },
            ct
        );
    }

    public Task<Part?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return executor.QuerySingleAsync<Part>("SELECT * FROM parts WHERE id = @id", new { id }, ct);
    }

    public Task<IEnumerable<Part>> GetTopAsync(int count, CancellationToken ct = default)
    {
        return executor.QueryManyAsync<Part>("SELECT * FROM parts ORDER BY started_on DESC LIMIT @count", new { count }, ct);
    }

    public async Task UpdateAsync(Part part, CancellationToken ct = default)
    {
        int updated = await executor.ExecuteAsync(
            """
            UPDATE parts
            SET started_on = @StartedOn,
                finished_on = @FinishedOn::TIMESTAMPTZ
            WHERE id = @Id
            """,
            new
            {
                part.Id,
                part.StartedOn,
                part.FinishedOn
            },
            ct
        );

        if (updated < 1) throw new InvalidOperationException($"No part record could be updated by Id {part.Id}");
    }
}