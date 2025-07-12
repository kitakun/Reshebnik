using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;

namespace Reshebnik.Clickhouse.Handlers;

public class FetchCompanyMetricsHandler(IOptions<ClickhouseOptions> optionsAccessor)
{
    private readonly ClickhouseOptions _options = optionsAccessor.Value;

    public record MetricsDataResponse(int[] PlanData, int[] FactData, int[] TotalPlanData, int[] TotalFactData);

    public async Task<MetricsDataResponse> HandleAsync(
        DateRange range,
        int metricId,
        PeriodTypeEnum expectedValues,
        PeriodTypeEnum sourcePeriod,
        CancellationToken cancellationToken)
    {
        var length = GetPeriodLength(expectedValues, range);
        var plan = new int[length];
        var fact = new int[length];
        var totalPlan = new int[12];
        var totalFact = new int[12];

        var totalRange = new DateRange(range.From.AddYears(-1), range.To);

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
                plan_value,
                fact_value
            FROM {table}
            WHERE metric_key = '{key}'
              AND period_type = '{sourcePeriod}'
              AND upsert_date BETWEEN toDate('{totalRange.From:yyyy-MM-dd}') AND toDate('{totalRange.To:yyyy-MM-dd}')
            ORDER BY upsert_date
        """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var monthsCount = GetMonthDiff(totalRange.From.Date, totalRange.To.Date);
        var step = (int)Math.Ceiling(monthsCount / 12.0);
        while (await reader.ReadAsync(cancellationToken))
        {
            var date = reader.GetDateTime(0);
            var planVal = reader.GetInt32(1);
            var factVal = reader.GetInt32(2);

            var idx = GetIndex(date, range.From.Date, expectedValues);
            if (idx is >= 0 && idx < length)
            {
                fact[idx] += factVal;
                plan[idx] += planVal;
            }

            var totalIdxRaw = GetIndex(date, totalRange.From.Date, PeriodTypeEnum.Month);
            var totalIdx = totalIdxRaw >= 0 ? Math.Min(11, totalIdxRaw / step) : -1;
            if (totalIdx is >= 0 && totalIdx < 12)
            {
                totalPlan[totalIdx] += planVal;
                totalFact[totalIdx] += factVal;
            }
        }
        return new MetricsDataResponse(plan, fact, totalPlan, totalFact);
    }

    public async Task PutAsync(
        int metricId,
        MetricValueTypeEnum valueType,
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

        var selectSql = $"""
            SELECT plan_value, fact_value
            FROM {table}
            WHERE metric_key = '{key}' AND company_id = {companyId} AND upsert_date = toDate('{upsertDate:yyyy-MM-dd}') AND period_type = '{periodType}'
            LIMIT 1
        """;

        int planVal = 0;
        int factVal = 0;
        await using (var selectCmd = connection.CreateCommand())
        {
            selectCmd.CommandText = selectSql;
            await using var reader = await selectCmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                planVal = reader.GetInt32(0);
                factVal = reader.GetInt32(1);
            }
        }

        if (valueType == MetricValueTypeEnum.Plan)
            planVal = value;
        else
            factVal = value;

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
            INSERT INTO {table} (company_id, metric_key, period_type, upsert_date, plan_value, fact_value)
            VALUES ({companyId}, '{key}', '{periodType}', toDate('{upsertDate:yyyy-MM-dd}'), {planVal}, {factVal})
        """;
        await using (var insertCmd = connection.CreateCommand())
        {
            insertCmd.CommandText = insertSql;
            await insertCmd.ExecuteNonQueryAsync(ct);
        }
    }

    private static int GetIndex(DateTime date, DateTime start, PeriodTypeEnum expected)
    {
        start = NormalizeStart(start, expected);
        return expected switch
        {
            PeriodTypeEnum.Day => (int)(date.Date - start).TotalDays,
            PeriodTypeEnum.Week => (int)(date.Date - start).TotalDays % 7,
            PeriodTypeEnum.Month => (date.Year - start.Year) * 12 + date.Month - start.Month,
            PeriodTypeEnum.Quartal => ((date.Year - start.Year) * 12 + date.Month - start.Month) / 3,
            PeriodTypeEnum.Year => date.Year - start.Year,
            _ => -1
        };
    }

    private static DateTime NormalizeStart(DateTime start, PeriodTypeEnum expected)
    {
        return expected switch
        {
            PeriodTypeEnum.Week => StartOfWeek(start, DayOfWeek.Monday),
            PeriodTypeEnum.Month => new DateTime(start.Year, start.Month, 1),
            PeriodTypeEnum.Quartal => new DateTime(start.Year, ((start.Month - 1) / 3) * 3 + 1, 1),
            PeriodTypeEnum.Year => new DateTime(start.Year, 1, 1),
            _ => start.Date
        };
    }

    private static int GetPeriodLength(PeriodTypeEnum expected, DateRange range)
    {
        return expected switch
        {
            PeriodTypeEnum.Day => (int)(range.To.Date - range.From.Date).TotalDays + 1,
            PeriodTypeEnum.Week => 7,
            PeriodTypeEnum.Month => 12,
            PeriodTypeEnum.Quartal => 1,
            PeriodTypeEnum.Year => 1,
            _ => 1
        };
    }

    private static int GetMonthDiff(DateTime from, DateTime to)
    {
        return (to.Year - from.Year) * 12 + to.Month - from.Month + 1;
    }

    private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-1 * diff);
    }
}
