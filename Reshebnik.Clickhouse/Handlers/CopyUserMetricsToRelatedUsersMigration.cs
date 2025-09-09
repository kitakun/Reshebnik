using ClickHouse.Client.ADO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reshebnik.EntityFramework;
using Reshebnik.Clickhouse;

namespace Reshebnik.Clickhouse.Handlers;

public class CopyUserMetricsToRelatedUsersMigration(
    ReshebnikContext db,
    IOptions<ClickhouseOptions> optionsAccessor,
    ILogger<CopyUserMetricsToRelatedUsersMigration> logger)
{
    private readonly ClickhouseOptions _options = optionsAccessor.Value;

    public const string Name = "0006_copy_user_metrics_to_related_users.cs";
    private const uint Version = 6;

    private record MetricRow(
        int[] CompanyIds,
        int? DepartmentId,
        string ValueType,
        string PeriodType,
        DateTime UpsertDate,
        int Value
    );

    public async Task ApplyAsync(ClickHouseConnection connection, HashSet<string> applied, CancellationToken ct = default)
    {
        if (applied.Contains(Name))
        {
            logger.LogDebug($"[SKIP] {Name}");
            return;
        }

        logger.LogDebug($"[APPLY] {Name}");

        var metricLinks = await db.MetricEmployeeLinks
            .AsNoTracking()
            .GroupBy(l => l.MetricId)
            .Select(g => new { MetricId = g.Key, Employees = g.Select(e => e.EmployeeId).ToArray() })
            .ToListAsync(ct);

        foreach (var link in metricLinks)
        {
            var key = $"user-metric-{link.MetricId}";

            await using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = $@"
                SELECT employee_ids, company_ids, department_id, value_type, period_type, upsert_date, value
                FROM {_options.Prefix}_user_metrics
                WHERE metric_key = '{key}' AND length(employee_ids) = 1";

            var rows = new List<MetricRow>();
            int? sourceEmployee = null;

            await using (var reader = await selectCmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    var employees = (int[])reader[0];
                    sourceEmployee ??= employees.FirstOrDefault();
                    var companies = (int[])reader[1];
                    var dept = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                    var vt = reader.GetString(3);
                    var pt = reader.GetString(4);
                    var date = reader.GetDateTime(5);
                    var val = reader.GetInt32(6);
                    rows.Add(new MetricRow(companies, dept, vt, pt, date, val));
                }
            }

            if (rows.Count == 0 || sourceEmployee is null)
                continue;

            foreach (var employeeId in link.Employees.Where(e => e != sourceEmployee))
            {
                await using var existsCmd = connection.CreateCommand();
                existsCmd.CommandText = $"SELECT count() FROM {_options.Prefix}_user_metrics WHERE metric_key = '{key}' AND has(employee_ids, {employeeId})";
                var exists = Convert.ToInt64(await existsCmd.ExecuteScalarAsync(ct)) > 0;
                if (exists)
                    continue;

                foreach (var row in rows)
                {
                    var insertSql = $@"
                        INSERT INTO {_options.Prefix}_user_metrics
                        (employee_ids, company_ids, department_id, metric_key, value_type, period_type, upsert_date, value)
                        VALUES ([{employeeId}], [{string.Join(',', row.CompanyIds)}], {row.DepartmentId?.ToString() ?? "NULL"}, '{key}', '{row.ValueType}', '{row.PeriodType}', toDate('{row.UpsertDate:yyyy-MM-dd}'), {row.Value})";
                    await using var insertCmd = connection.CreateCommand();
                    insertCmd.CommandText = insertSql;
                    await insertCmd.ExecuteNonQueryAsync(ct);
                }
            }
        }

        await using var recordCmd = connection.CreateCommand();
        recordCmd.CommandText = $"INSERT INTO {_options.Prefix}_schema_migrations VALUES ({Version}, '{Name}', now())";
        await recordCmd.ExecuteNonQueryAsync(ct);
    }
}

