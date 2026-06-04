using App.Abstractions;
using App.Parts;
using Infra.RecordStore;

namespace Infra.Queries;

public class PsqlPartQueries(ISqlQueryExecutor executor) : IPartsQueries
{
    public Task<IEnumerable<PartLog>> GetPartLogsAsync(string partId, CancellationToken ct = default)
    {
        return executor.QueryManyAsync<PartLog>(
            """
            SELECT
                machine_id,
                timestamp,
                status,
                part_status,
                temperature::float8 ,
                vibration::float8 ,
                spindle_load::float8 ,
                cycle_time_sec::float8 ,
                quality_score::float8
            FROM machine_telemetry_packets
            WHERE part_id = @partId
            ORDER BY timestamp DESC
            """,
            new { partId },
            ct
        );
    }
}