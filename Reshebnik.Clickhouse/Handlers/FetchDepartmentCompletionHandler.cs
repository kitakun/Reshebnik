using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Reshebnik.Domain.Models;

namespace Reshebnik.Clickhouse.Handlers;

public class FetchDepartmentCompletionHandler(IOptions<ClickhouseOptions> optionsAccessor)
{
    private readonly ClickhouseOptions _options = optionsAccessor.Value;

    public async Task<double> HandleAsync(
        int companyId,
        int departmentId,
        string metricKey,
        DateRange range,
        CancellationToken ct = default)
    {
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

        var table = $"{_options.Prefix}_user_metrics";
        var sql = $"""
            SELECT AVG(value)
            FROM {table}
            WHERE has(company_ids, {companyId})
              AND department_id = {departmentId}
              AND metric_key = '{metricKey}'
              AND upsert_date BETWEEN toDate('{range.From:yyyy-MM-dd}') AND toDate('{range.To:yyyy-MM-dd}')
              AND value_type = 'Fact'
        """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        var resultObj = await cmd.ExecuteScalarAsync(ct);
        if (resultObj == null || resultObj is DBNull) return 0;

        var value = Convert.ToDouble(resultObj);
        return double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;
    }
}
