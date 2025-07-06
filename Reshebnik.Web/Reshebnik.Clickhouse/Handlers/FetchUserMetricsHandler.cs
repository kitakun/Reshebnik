using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;

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

        var builder = new ClickHouseConnectionStringBuilder
        {
            Host = _options.Host,
            Database = _options.DbName,
            Username = _options.Username,
            Password = _options.Password,
            Protocol = "http",
        };
        var connStr = builder.ToString();
        
        await using var connection = new ClickHouseConnection(connStr);
        await connection.OpenAsync(cancellationToken);

        var table = $"{_options.Prefix}_user_metrics";
        var sql = $"""
            SELECT
                upsert_date,
                value
            FROM {table}
            WHERE metric_key = '{key}'
              AND period_type = '{sourcePeriod}'
              AND upsert_date BETWEEN toDate('{range.From:yyyy-MM-dd}') AND toDate('{range.To:yyyy-MM-dd}')
            ORDER BY upsert_date
        """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
            
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
        var connStr =
            $"Host={_options.Host};Port={_options.Port};Protocol=http;Database={_options.DbName};User={_options.Username};Password={_options.Password}";
        await using var connection = new ClickHouseConnection(connStr);
        await connection.OpenAsync(ct);

        var table = $"{_options.Prefix}_user_metrics";

        var deleteSql = $"""
            ALTER TABLE {table}
            DELETE WHERE metric_key = '{key}' AND employee_id = {employeeId} AND company_id = {companyId}
        """;

        await using (var deleteCmd = connection.CreateCommand())
        {
            deleteCmd.CommandText = deleteSql;
            await deleteCmd.ExecuteNonQueryAsync(ct);
        }

        var insertSql = $"""
            INSERT INTO {table} (employee_id, company_id, department_id, metric_key, period_type, upsert_date, value)
            VALUES ({employeeId}, {companyId}, {departmentId?.ToString() ?? "NULL"}, '{key}', '{periodType}', toDate('{upsertDate:yyyy-MM-dd}'), {value})
        """;

        await using (var insertCmd = connection.CreateCommand())
        {
            insertCmd.CommandText = insertSql;
            await insertCmd.ExecuteNonQueryAsync(ct);
        }
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
