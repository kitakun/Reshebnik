using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models;
using System.Collections.Concurrent;

namespace Tabligo.Clickhouse.Handlers;

public class FetchCompanyMetricsHandler(IOptions<ClickhouseOptions> optionsAccessor)
{
    private readonly ClickhouseOptions _options = optionsAccessor.Value;

    public record MetricsDataResponse(int[] PlanData, int[] FactData);

    public record MetricRequest(int MetricId, PeriodTypeEnum ExpectedValues, PeriodTypeEnum SourcePeriod);

    public async Task<MetricsDataResponse> HandleAsync(
        DateRange range,
        int metricId,
        PeriodTypeEnum expectedValues,
        PeriodTypeEnum sourcePeriod,
        CancellationToken ct = default)
    {
        var map = await HandleBulkAsync(
            range,
            [new MetricRequest(metricId, expectedValues, sourcePeriod)],
            ct);

        return map.TryGetValue(metricId, out var resp)
            ? resp
            : new MetricsDataResponse(new int[GetPeriodLength(expectedValues, range)], new int[GetPeriodLength(expectedValues, range)]);
    }

    /// <summary>
    /// BULK: один запрос в CH для множества метрик и period_type.
    /// Возвращает словарь { metricId -> MetricsDataResponse }.
    /// </summary>
    public async Task<Dictionary<int, MetricsDataResponse>> HandleBulkAsync(
        DateRange range,
        IEnumerable<MetricRequest> requests,
        CancellationToken ct = default)
    {
        // Нормализуем вход
        var reqList = requests?.ToList() ?? [];
        if (reqList.Count == 0)
            return new Dictionary<int, MetricsDataResponse>();

        // Подготовка ключей и буферов
        var unionFrom = range.From.AddYears(-1);
        var totalTo = range.To;

        // key -> metricId
        string Key(int metricId) => $"company-metric-{metricId}";

        var metricIds = reqList.Select(r => r.MetricId).Distinct().ToList();
        var metricKeyToId = metricIds.ToDictionary(id => Key(id), id => id);

        // period_type, которые понадобятся
        var sourcePeriods = reqList.Select(r => r.SourcePeriod).Distinct().ToList();

        // Буферы под ответы, и сопоставление metricId -> expectedValues
        var expectedByMetric = reqList.ToDictionary(r => r.MetricId, r => r.ExpectedValues);
        var buffers = new ConcurrentDictionary<int, (int[] plan, int[] fact)>();

        foreach (var r in reqList)
        {
            var len = GetPeriodLength(r.ExpectedValues, range);
            buffers.TryAdd(r.MetricId, (new int[len], new int[len]));
        }

        // --- Подключение и одиночный SELECT ---
        var builder = new ClickHouseConnectionStringBuilder
        {
            Host = _options.Host,
            Database = _options.DbName,
            Username = _options.Username,
            Password = _options.Password,
            Protocol = "http",
        };
        await using var connection = new ClickHouseConnection(builder.ToString());
        await connection.OpenAsync(ct);

        var table = $"{_options.Prefix}_company_metrics";

        // Сформируем IN-списки (безопасно — только ints/enums, без пользовательского ввода).
        var inKeys = string.Join(", ", metricKeyToId.Keys.Select(k => $"'{k}'"));
        var inPeriods = string.Join(", ", sourcePeriods.Select(p => $"'{p}'"));

        var sql = $"""
            SELECT
                metric_key,
                upsert_date,
                plan_value,
                fact_value,
                period_type
            FROM {table}
            WHERE metric_key IN ({inKeys})
              AND period_type IN ({inPeriods})
              AND upsert_date BETWEEN toDate('{unionFrom:yyyy-MM-dd}')
                                 AND toDate('{totalTo:yyyy-MM-dd}')
            ORDER BY metric_key, upsert_date
        """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var metricKey = reader.GetString(0);
            if (!metricKeyToId.TryGetValue(metricKey, out var metricId))
                continue;

            var date = reader.GetDateTime(1);
            var planVal = reader.GetInt32(2);
            var factVal = reader.GetInt32(3);
            // var periodType = Enum.Parse<PeriodTypeEnum>(reader.GetString(4)); // при необходимости

            var expected = expectedByMetric[metricId];
            var (plan, fact) = buffers[metricId];

            var idx = GetIndex(date, range.From.Date, expected);
            if (idx is >= 0 && idx < plan.Length)
            {
                plan[idx] += planVal;
                fact[idx] += factVal;
            }
        }

        // Сборка результата
        var result = new Dictionary<int, MetricsDataResponse>(buffers.Count);
        foreach (var kv in buffers)
        {
            var p = kv.Value.plan;
            var f = kv.Value.fact;
            result[kv.Key] = new MetricsDataResponse(p, f);
        }
        return result;
    }

    /// <summary>
    /// Updates both plan and fact values atomically in a single operation
    /// </summary>
    public async Task PutAsync(
        int metricId,
        int companyId,
        PeriodTypeEnum periodType,
        DateTime upsertDate,
        int? planValue = null,
        int? factValue = null,
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
        await using var connection = new ClickHouseConnection(builder.ToString());
        await connection.OpenAsync(ct);

        var table = $"{_options.Prefix}_company_metrics";

        // Чтение текущего значения
        // Используем FINAL чтобы получить актуальную (после всех мержей) версию строки
        var selectSql = $"""
            SELECT plan_value, fact_value, status, paid_amount, total_amount, external_id
            FROM {table} FINAL
            WHERE metric_key = '{key}'
              AND company_id = {companyId}
              AND upsert_date = toDate('{upsertDate:yyyy-MM-dd}')
              AND period_type = '{periodType}'
            LIMIT 1
        """;

        int planVal = 0, factVal = 0;
        string status = string.Empty;
        decimal paidAmount = 0;
        decimal totalAmount = 0;
        string externalId = string.Empty;
        
        await using (var selectCmd = connection.CreateCommand())
        {
            selectCmd.CommandText = selectSql;
            await using var reader = await selectCmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                planVal = reader.GetInt32(0);
                factVal = reader.GetInt32(1);
                status = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                paidAmount = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3);
                totalAmount = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4);
                externalId = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
            }
        }

        // Update only provided values, keep existing for null values
        if (planValue.HasValue) planVal = planValue.Value;
        if (factValue.HasValue) factVal = factValue.Value;

        // Delete existing rows before inserting new ones
        var deleteSql = $"""
            ALTER TABLE {table}
            DELETE WHERE metric_key = '{key}'
              AND company_id = {companyId}
              AND upsert_date = toDate('{upsertDate:yyyy-MM-dd}')
              AND period_type = '{periodType}'
        """;
        await using (var deleteCmd = connection.CreateCommand())
        {
            deleteCmd.CommandText = deleteSql;
            await deleteCmd.ExecuteNonQueryAsync(ct);
        }

        // Insert updated row
        var insertSql = $"""
            INSERT INTO {table} (company_id, metric_key, period_type, upsert_date, plan_value, fact_value, status, paid_amount, total_amount, external_id)
            VALUES ({companyId}, '{key}', '{periodType}', toDate('{upsertDate:yyyy-MM-dd}'), {planVal}, {factVal}, '{status}', {paidAmount:0.00}, {totalAmount:0.00}, '{externalId}')
        """;
        await using (var insertCmd = connection.CreateCommand())
        {
            insertCmd.CommandText = insertSql;
            await insertCmd.ExecuteNonQueryAsync(ct);
        }
    }

    // --- Вспомогательные функции (как у тебя) ---
    private static int GetIndex(DateTime date, DateTime start, PeriodTypeEnum expected)
    {
        start = NormalizeStart(start, expected);
        return expected switch
        {
            PeriodTypeEnum.Day => (int)(date.Date - start).TotalDays,
            PeriodTypeEnum.Custom => (int)(date.Date - start).TotalDays,
            PeriodTypeEnum.Week => (int)((date.Date - start).TotalDays / 7),
            PeriodTypeEnum.Month => (date.Year - start.Year) * 12 + date.Month - start.Month,
            PeriodTypeEnum.Quartal => ((date.Year - start.Year) * 12 + date.Month - start.Month) / 3,
            PeriodTypeEnum.Year => date.Year - start.Year,
            _ => -1
        };
    }

    private static DateTime NormalizeStart(DateTime start, PeriodTypeEnum expected) => expected switch
    {
        PeriodTypeEnum.Week => StartOfWeek(start, DayOfWeek.Monday),
        PeriodTypeEnum.Month => new DateTime(start.Year, start.Month, 1),
        PeriodTypeEnum.Quartal => new DateTime(start.Year, ((start.Month - 1) / 3) * 3 + 1, 1),
        PeriodTypeEnum.Year => new DateTime(start.Year, 1, 1),
        _ => start.Date
    };

    private static int GetPeriodLength(PeriodTypeEnum expected, DateRange range) => expected switch
    {
        PeriodTypeEnum.Day => (int)(range.To.Date - range.From.Date).TotalDays + 1,
        PeriodTypeEnum.Custom => (int)(range.To.Date - range.From.Date).TotalDays + 1,
        PeriodTypeEnum.Week => GetWeekDiff(range.From.Date, range.To.Date),
        PeriodTypeEnum.Month => 12,
        PeriodTypeEnum.Quartal => GetQuartalDiff(range.From.Date, range.To.Date),
        PeriodTypeEnum.Year => GetYearDiff(range.From.Date, range.To.Date),
        _ => 1
    };

    private static int GetYearDiff(DateTime from, DateTime to) => to.Year - from.Year + 1;
    private static int GetQuartalDiff(DateTime from, DateTime to) => ((to.Year - from.Year) * 12 + to.Month - from.Month) / 3 + 1;

    private static int GetWeekDiff(DateTime from, DateTime to)
    {
        from = StartOfWeek(from, DayOfWeek.Monday);
        to = StartOfWeek(to, DayOfWeek.Monday);
        return (int)((to - from).TotalDays / 7) + 1;
    }

    private static int GetMonthDiff(DateTime from, DateTime to) => (to.Year - from.Year) * 12 + to.Month - from.Month + 1;

    private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-1 * diff);
    }
}