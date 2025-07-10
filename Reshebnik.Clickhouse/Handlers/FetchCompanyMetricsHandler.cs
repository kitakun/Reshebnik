using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;

namespace Reshebnik.Clickhouse.Handlers;

public class FetchCompanyMetricsHandler(IOptions<ClickhouseOptions> optionsAccessor)
{
    private readonly ClickhouseOptions _options = optionsAccessor.Value;

    public async Task<int[]> HandleAsync(
        DateRange range,
        int metricId,
        PeriodTypeEnum expectedValues,
        PeriodTypeEnum sourcePeriod,
        CancellationToken cancellationToken)
    {
        var length = GetPeriodLength(range, expectedValues);
        var data = new int[length];

        var key = $"company-metric-{metricId}";
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

        var table = $"{_options.Prefix}_company_metrics";
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
            if (idx >= 0 && idx < length)
                data[idx] += value;
        }

        return data;
    }

    public async Task PutAsync(
        int metricId,
        int companyId,
        PeriodTypeEnum periodType,
        DateTime upsertDate,
        int value,
        CancellationToken ct = default)
    {
        var key = $"company-metric-{metricId}";

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
        await connection.OpenAsync(ct);

        var table = $"{_options.Prefix}_company_metrics";
        var deleteSql = $"""
            ALTER TABLE {table}
            DELETE WHERE metric_key = '{key}' AND company_id = {companyId} AND upsert_date = toDate('{upsertDate:yyyy-MM-dd}') AND period_type = '{periodType}'
        """;
        await using (var deleteCmd = connection.CreateCommand())
        {
            deleteCmd.CommandText = deleteSql;
            await deleteCmd.ExecuteNonQueryAsync(ct);
        }

        var insertSql = $"""
            INSERT INTO {table} (company_id, metric_key, period_type, upsert_date, value)
            VALUES ({companyId}, '{key}', '{periodType}', toDate('{upsertDate:yyyy-MM-dd}'), {value})
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

    private static int GetPeriodLength(DateRange range, PeriodTypeEnum expected)
    {
        if (range.To < range.From) return 0;
        var lastIndex = GetIndex(range.To.Date, range.From.Date, expected);
        return Math.Max(0, lastIndex + 1);
    }
}
