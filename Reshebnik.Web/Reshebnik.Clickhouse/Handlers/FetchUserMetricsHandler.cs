using Octonica.ClickHouseClient;
using Microsoft.Extensions.Options;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;

namespace Reshebnik.Clickhouse.Handlers;

public class FetchUserMetricsHandler(IOptions<ClickhouseOptions> optionsAccessor)
{
    public record MetricsDataResponse(int[] PlanData, int[] FactData);

    private readonly ClickhouseOptions _options = optionsAccessor.Value;

    public async Task<MetricsDataResponse> HandleAsync(DateRange range, string key, CancellationToken cancellationToken)
    {
        var fact = new int[13];
        var plan = new int[13];

        var connectionString = $"Host={_options.Host};Port={_options.Port};Database={_options.DbName};User={_options.Username};Password={_options.Password}";
        await using var connection = new ClickHouseConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var cmd = connection.CreateCommand($"SELECT upsert_date, value FROM user_metrics WHERE metric_key=@key AND upsert_date BETWEEN @from AND @to ORDER BY upsert_date");
        cmd.Parameters.AddWithValue("key", key);
        cmd.Parameters.AddWithValue("from", range.From.Date);
        cmd.Parameters.AddWithValue("to", range.To.Date);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var index = 0;
        while (await reader.ReadAsync(cancellationToken) && index < 13)
        {
            var value = reader.GetInt32(1);
            fact[index] = value;
            plan[index] = value;
            index++;
        }

        return new MetricsDataResponse(plan, fact);
    }

    public async Task PutAsync(string key, int employeeId, int companyId, int? departmentId, PeriodTypeEnum periodType, DateTime upsertDate, int value, CancellationToken ct = default)
    {
        var connectionString = $"Host={_options.Host};Port={_options.Port};Database={_options.DbName};User={_options.Username};Password={_options.Password}";
        await using var connection = new ClickHouseConnection(connectionString);
        await connection.OpenAsync(ct);

        var delete = connection.CreateCommand($"ALTER TABLE user_metrics DELETE WHERE metric_key=@key AND employee_id=@eid AND company_id=@cid AND {(departmentId.HasValue ? "department_id=@did" : "department_id IS NULL")} AND period_type=@ptype AND upsert_date=@date");
        delete.Parameters.AddWithValue("key", key);
        delete.Parameters.AddWithValue("eid", employeeId);
        delete.Parameters.AddWithValue("cid", companyId);
        if (departmentId.HasValue)
            delete.Parameters.AddWithValue("did", departmentId.Value);
        delete.Parameters.AddWithValue("ptype", periodType.ToString());
        delete.Parameters.AddWithValue("date", upsertDate.Date);
        await delete.ExecuteNonQueryAsync(ct);

        var insert = connection.CreateCommand("INSERT INTO user_metrics (employee_id, company_id, department_id, metric_key, period_type, upsert_date, value) VALUES (@eid,@cid,@did,@key,@ptype,@date,@val)");
        insert.Parameters.AddWithValue("eid", employeeId);
        insert.Parameters.AddWithValue("cid", companyId);
        insert.Parameters.AddWithValue("did", (object?)departmentId ?? DBNull.Value);
        insert.Parameters.AddWithValue("key", key);
        insert.Parameters.AddWithValue("ptype", periodType.ToString());
        insert.Parameters.AddWithValue("date", upsertDate.Date);
        insert.Parameters.AddWithValue("val", value);
        await insert.ExecuteNonQueryAsync(ct);
    }
}
