using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;

namespace Reshebnik.Clickhouse.Handlers;

public class FetchUserMetricsHandler(IOptions<ClickhouseOptions> optionsAccessor)
{
    public record MetricsDataResponse(int[] PlanData, int[] FactData, int[] TotalPlanData, int[] TotalFactData);

    private readonly ClickhouseOptions _options = optionsAccessor.Value;

    public async Task<MetricsDataResponse> HandleAsync(
        DateRange range,
        int metricId,
        PeriodTypeEnum expectedValues,
        PeriodTypeEnum sourcePeriod,
        CancellationToken cancellationToken)
    {
        var length = GetPeriodLength(expectedValues, range);
        var fact = new int[length];
        var plan = new int[length];
        var totalPlan = new int[12];
        var totalFact = new int[12];

        var totalRange = new DateRange(range.From.AddYears(-1), range.To);
        var monthsCount = GetMonthDiff(totalRange.From.Date, totalRange.To.Date);
        var step = (int)Math.Ceiling(monthsCount / 12.0);

        var key = $"user-metric-{metricId}";

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
                value,
                value_type
            FROM {table}
            WHERE metric_key = '{key}'
              AND period_type = '{sourcePeriod}'
              AND upsert_date BETWEEN toDate('{range.From:yyyy-MM-dd}') AND toDate('{range.To:yyyy-MM-dd}')
            ORDER BY upsert_date, value_type
        """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var date = reader.GetDateTime(0);
            var value = reader.GetInt32(1);
            var type = reader.GetString(2);
            var idx = GetIndex(date, range.From.Date, expectedValues);
            if (idx is >= 0 && idx < length && Enum.TryParse<MetricValueTypeEnum>(type, out var vt))
            {
                if (vt == MetricValueTypeEnum.Fact)
                    fact[idx] += value;
                else if (vt == MetricValueTypeEnum.Plan)
                    plan[idx] += value;
            }

            var totalIdxRaw = GetIndex(date, totalRange.From.Date, PeriodTypeEnum.Month);
            var totalIdx = totalIdxRaw >= 0 ? Math.Min(11, totalIdxRaw / step) : -1;
            if (totalIdx is >= 0 && totalIdx < 12 && Enum.TryParse<MetricValueTypeEnum>(type, out var vtTotal))
            {
                if (vtTotal == MetricValueTypeEnum.Fact)
                    totalFact[totalIdx] += value;
                else if (vtTotal == MetricValueTypeEnum.Plan)
                    totalPlan[totalIdx] += value;
            }
        }

        return new MetricsDataResponse(plan, fact, totalPlan, totalFact);
    }

    public async Task PutAsync(
        int metricId,
        MetricValueTypeEnum valueType,
        int employeeId,
        int companyId,
        int? departmentId,
        PeriodTypeEnum periodType,
        DateTime upsertDate,
        int value,
        CancellationToken ct = default)
    {
        var key = $"user-metric-{metricId}";

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

        var table = $"{_options.Prefix}_user_metrics";

        var deleteSql = $"""
            ALTER TABLE {table}
            DELETE WHERE metric_key = '{key}' AND value_type = '{valueType}' AND employee_id = {employeeId} AND company_id = {companyId} AND upsert_date = toDate('{upsertDate:yyyy-MM-dd}')
            AND value_type = '{valueType}' AND period_type = '{periodType}'
        """;

        await using (var deleteCmd = connection.CreateCommand())
        {
            deleteCmd.CommandText = deleteSql;
            await deleteCmd.ExecuteNonQueryAsync(ct);
        }

        var insertSql = $"""
            INSERT INTO {table} (employee_id, company_id, department_id, metric_key, value_type, period_type, upsert_date, value)
            VALUES ({employeeId}, {companyId}, {departmentId?.ToString() ?? "NULL"}, '{key}', '{valueType}', '{periodType}', toDate('{upsertDate:yyyy-MM-dd}'), {value})
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
