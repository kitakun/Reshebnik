using Octonica.ClickHouseClient;
using Microsoft.Extensions.Options;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using System.Data;

namespace Reshebnik.Clickhouse.Handlers;

public class FetchUserMetricsHandler(IOptions<ClickhouseOptions> optionsAccessor)
{
    public record MetricsDataResponse(int[] PlanData, int[] FactData);

    private readonly ClickhouseOptions _options = optionsAccessor.Value;

    public async Task<MetricsDataResponse> HandleAsync(
        DateRange range,
        string key,
        PeriodTypeEnum expectedValues,
        PeriodTypeEnum sourcePeriod,
        CancellationToken cancellationToken)
    {
        var fact = new int[13];
        var plan = new int[13];

        var connectionString = $"Host={_options.Host};Port={_options.Port};Database={_options.DbName};User={_options.Username};Password={_options.Password}";
        await using var connection = new ClickHouseConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var table = $"{_options.Prefix}_user_metrics";
        var cmd = connection.CreateCommand($"""
                                          SELECT
                                              upsert_date,
                                              value
                                          FROM {table}
                                          WHERE
                                               metric_key=@key AND period_type=@ptype
                                               AND upsert_date BETWEEN @from AND @to
                                          ORDER BY upsert_date
                                          """);
        cmd.Parameters.AddWithValue("key", key);
        cmd.Parameters.AddWithValue("ptype", sourcePeriod.ToString());
        cmd.Parameters.AddWithValue("from", range.From.Date);
        cmd.Parameters.AddWithValue("to", range.To.Date);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var date = reader.GetDateTime(0);
            var value = reader.GetInt32(1);
            var idx = GetIndex(date, range.From.Date, expectedValues);
            if (idx is >= 0 and < 13)
            {
                fact[idx] += value;
                plan[idx] += value;
            }
        }

        return new MetricsDataResponse(plan, fact);
    }

    public async Task PutAsync(
        string key,
        int employeeId,
        int companyId,
        int? departmentId,
        PeriodTypeEnum periodType,
        DateTime upsertDate,
        int value,
        CancellationToken ct = default)
    {
        var connectionString = $"Host={_options.Host};Port={_options.Port};Database={_options.DbName};User={_options.Username};Password={_options.Password}";
        await using var connection = new ClickHouseConnection(connectionString);
        await connection.OpenAsync(ct);

        var table = $"{_options.Prefix}_user_metrics";
        var delete = connection.CreateCommand($"ALTER TABLE {table} DELETE WHERE metric_key=@key AND employee_id=@eid AND company_id=@cid");
        delete.Parameters.AddWithValue("key", key);
        delete.Parameters.AddWithValue("eid", employeeId);
        delete.Parameters.AddWithValue("cid", companyId);
        await delete.ExecuteNonQueryAsync(ct);

        var insert = connection.CreateCommand($"INSERT INTO {table} (employee_id, company_id, department_id, metric_key, period_type, upsert_date, value) VALUES (@eid,@cid,@did,@key,@ptype,@date,@val)");
        insert.Parameters.AddWithValue("eid", employeeId);
        insert.Parameters.AddWithValue("cid", companyId);
        insert.Parameters.AddWithValue("did", (object?)departmentId ?? DBNull.Value);
        insert.Parameters.AddWithValue("key", key);
        insert.Parameters.AddWithValue("ptype", periodType.ToString());
        insert.Parameters.AddWithValue("date", upsertDate.Date);
        insert.Parameters.AddWithValue("val", value);
        await insert.ExecuteNonQueryAsync(ct);
    }

    private static int GetIndex(DateTime date, DateTime start, PeriodTypeEnum expected)
    {
        return expected switch
        {
            PeriodTypeEnum.Day => (int)(date.Date - start.Date).TotalDays,
            PeriodTypeEnum.Week => (int)((date.Date - start.Date).TotalDays / 7),
            PeriodTypeEnum.Month => (date.Year - start.Year) * 12 + date.Month - start.Month,
            PeriodTypeEnum.Quartal => ((date.Year - start.Year) * 12 + date.Month - start.Month) / 3,
            PeriodTypeEnum.Year => date.Year - start.Year,
            _ => -1
        };
    }
}
