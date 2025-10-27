using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Tabligo.Clickhouse.Models;
using Tabligo.Domain.Enums;

namespace Tabligo.Clickhouse.Handlers;

public class PutIndicatorValuesHandler(IOptions<ClickhouseOptions> optionsAccessor)
{
    private readonly ClickhouseOptions _options = optionsAccessor.Value;

    public async Task PutIndicatorValuesAsync(
        int indicatorId,
        int companyId,
        FillmentPeriodEnum fillmentPeriod,
        List<IndicatorValue> values,
        CancellationToken ct = default)
    {
        if (values == null || !values.Any())
            return;

        // Use the same key format as FetchCompanyMetricsHandler
        var key = $"company-metric-{indicatorId}";
        
        // Map FillmentPeriodEnum to PeriodTypeEnum for ClickHouse
        var periodType = MapFillmentPeriodToPeriodType(fillmentPeriod);

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

        // Удаляем старые значения для этого индикатора по датам
        // Собираем все unique даты из новых значений
        var dates = values.Select(v => v.Date).Distinct().ToList();

        if (dates.Any())
        {
            var datesList = string.Join(", ", dates.Select(d => $"toDate('{d:yyyy-MM-dd}')"));
            
            // Читаем существующие plan_value для сохранения
            var selectSql = $"""
                SELECT upsert_date, plan_value
                FROM {table}
                WHERE metric_key = '{key}' AND company_id = {companyId} AND period_type = '{periodType}' AND upsert_date IN ({datesList})
            """;
            
            var existingPlanValues = new Dictionary<DateTime, int>();
            await using (var selectCmd = connection.CreateCommand())
            {
                selectCmd.CommandText = selectSql;
                await using var reader = await selectCmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var date = reader.GetDateTime(0);
                    var planVal = reader.GetInt32(1);
                    existingPlanValues[date] = planVal;
                }
            }
            
            var deleteSql = $"""
                ALTER TABLE {table}
                DELETE WHERE metric_key = '{key}' AND company_id = {companyId} AND period_type = '{periodType}' AND upsert_date IN ({datesList})
            """;

            await using (var deleteCmd = connection.CreateCommand())
            {
                deleteCmd.CommandText = deleteSql;
                await deleteCmd.ExecuteNonQueryAsync(ct);
            }
            
            // Batch insert новых значений с сохранением существующих plan_value
            var valuesSql = string.Join(", ", values.Select((v, i) => 
            {
                var existingPlan = existingPlanValues.TryGetValue(v.Date, out var plan) ? plan : 0;
                return $"({companyId}, '{key}', '{periodType}', toDate('{v.Date:yyyy-MM-dd}'), {existingPlan}, {v.PaidAmount:0.00}, '{v.Status}', {v.PaidAmount:0.00}, {v.TotalAmount:0.00}, '{v.ExternalId}')";
            }));

            var insertSql = $"""
                INSERT INTO {table} (company_id, metric_key, period_type, upsert_date, plan_value, fact_value, status, paid_amount, total_amount, external_id)
                VALUES {valuesSql}
            """;

            await using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.CommandText = insertSql;
                await insertCmd.ExecuteNonQueryAsync(ct);
            }
        }
    }

    private static PeriodTypeEnum MapFillmentPeriodToPeriodType(FillmentPeriodEnum fillmentPeriod)
    {
        return fillmentPeriod switch
        {
            FillmentPeriodEnum.Daily => PeriodTypeEnum.Day,
            FillmentPeriodEnum.Weekly => PeriodTypeEnum.Week,
            FillmentPeriodEnum.Monthly => PeriodTypeEnum.Month,
            _ => PeriodTypeEnum.Day
        };
    }
}
