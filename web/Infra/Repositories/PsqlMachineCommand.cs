using System.Text.Json;
using App.Abstractions;
using Domain.Machines;
using Infra.RecordStore;

namespace Infra.Repositories;

public class PsqlMachineCommandRepository(ISqlQueryExecutor executor) : IMachineCommandRepository
{
    public Task AddAsync(MachineCommand cmd, CancellationToken ct = default)
    {
        return executor.ExecuteAsync(
            """
            INSERT INTO machine_commands (
                id,
                created_on,
                machine_id,
                type,
                parameters
            )
            VALUES (
                @Id,
                @CreatedOn,
                @MachineId,
                @Type,
                @Parameters::jsonb
            );
            """,
            new {
                cmd.Id,
                cmd.CreatedOn,
                cmd.MachineId,
                Type = (short)cmd.Type,
                Parameters = JsonSerializer.Serialize(cmd.Parameters)
            },
            ct
        );
    }

    public Task MarkExecutedAsync(Guid id, DateTime executedOn, CancellationToken ct = default)
    {
        return executor.ExecuteAsync(
            """
            UPDATE machine_commands
            SET executed_on = @ExecutedOn
            WHERE id = @Id;
            """,
            new {
                Id = id,
                ExecutedOn = executedOn
            },
            ct
        );
    }
}