using Microsoft.EntityFrameworkCore;

using System;
using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Reshebnik.Clickhouse;
using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Handlers.Auth;

namespace Reshebnik.Handlers.Metric;

public class MetricArchiveHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext,
    UserContextHandler userContext,
    IOptions<ClickhouseOptions> optionsAccessor)
{
    private readonly ClickhouseOptions _options = optionsAccessor.Value;

    public async Task HandleAsync(int id, MetricArchiveDto dto, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;

        var (first, last) = dto.MetricType switch
        {
            ArchiveMetricTypeEnum.Employee => await LoadDatesAsync($"user-metric-{id}", $"{_options.Prefix}_user_metrics", $"has(company_ids, {companyId})", ct),
            ArchiveMetricTypeEnum.Company => await LoadDatesAsync($"company-metric-{id}", $"{_options.Prefix}_company_metrics", $"company_id = {companyId}", ct),
            _ => throw new ArgumentOutOfRangeException(nameof(dto.MetricType))
        };

        var metric = await db.Metrics.FirstAsync(m => m.CompanyId == companyId && m.Id == id, ct);

        var archived = new ArchivedMetricEntity
        {
            CompanyId = companyId,
            MetricId = metric.Id,
            MetricType = metric.Type,
            FirstDate = first,
            LastDate = last,
            ArchivedAt = DateTime.UtcNow,
            ArchivedByUserId = userContext.CurrentUserId
        };

        metric.IsArchived = true;
        metric.ArchivedMetric = archived;

        db.ArchivedMetrics.Add(archived);
        await db.SaveChangesAsync(ct);
    }

    private async Task<(DateTime FirstDate, DateTime LastDate)> LoadDatesAsync(
        string key,
        string table,
        string companyFilter,
        CancellationToken ct)
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

        var sql = $"""
            SELECT
                min(upsert_date) AS min_date,
                max(upsert_date) AS max_date
            FROM {table}
            WHERE metric_key = '{key}' AND {companyFilter}
        """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        DateTime first = DateTime.UtcNow;
        DateTime last = DateTime.UtcNow;
        if (await reader.ReadAsync(ct))
        {
            if (!reader.IsDBNull(0))
                first = reader.GetDateTime(0);
            if (!reader.IsDBNull(1))
                last = reader.GetDateTime(1);
        }

        return (first, last);
    }
}

