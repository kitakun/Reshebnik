using ClickHouse.Client.ADO;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Text.RegularExpressions;

namespace Reshebnik.Clickhouse.Handlers;

public class MigrateClickhouseDatabase(IOptions<ClickhouseOptions> optionsAccessor, ILogger<MigrateClickhouseDatabase> logger)
{
    public async ValueTask HandleAsync()
    {
        var options = optionsAccessor.Value!;
        var migrationsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reshebnik.Clickhouse", "Migrations");
        var builder = new ClickHouseConnectionStringBuilder
        {
            Host = options.Host,
            Database = options.DbName,
            Username = options.Username,
            Password = options.Password,
            Protocol = "http",
        };
        var connStr = builder.ToString();

        await using var connection = new ClickHouseConnection(connStr);
        await connection.OpenAsync();

        // Create versioning table if not exists
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $@"CREATE TABLE IF NOT EXISTS {options.Prefix}_schema_migrations (
        version UInt32,
        name String,
        applied_at DateTime
    ) ENGINE = TinyLog";
            await cmd.ExecuteNonQueryAsync();
        }

        // Get already applied versions
        var applied = new HashSet<string>();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"SELECT name FROM {options.Prefix}_schema_migrations";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    applied.Add(reader.GetString(0));
            }
        }

        // Apply new migrations
        foreach (var file in Directory.GetFiles(migrationsDir, "*.sql").OrderBy(f => f))
        {
            var name = Path.GetFileName(file);
            if (applied.Contains(name))
            {
                logger.LogDebug($"[SKIP] {name}");
                continue;
            }

            logger.LogDebug($"[APPLY] {name}");

            var sql = await File.ReadAllTextAsync(file);
            sql = sql.Replace("{prefix}", options.Prefix);
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                await cmd.ExecuteNonQueryAsync();
            }

            // Record applied migration
            await using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.CommandText =
                    $"INSERT INTO {options.Prefix}_schema_migrations VALUES ({{version:UInt32}}, {{name:String}}, now())";

                var versionMatch = Regex.Match(name, @"^(\d+)");
                var version = versionMatch.Success ? uint.Parse(versionMatch.Groups[1].Value) : 0;

                insertCmd.Parameters.Add(new ClickHouse.Client.ADO.Parameters.ClickHouseDbParameter()
                {
                    ParameterName = "version",
                    Value = version
                });
                insertCmd.Parameters.Add(new ClickHouse.Client.ADO.Parameters.ClickHouseDbParameter()
                {
                    ParameterName = "name",
                    Value = name
                });

                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        logger.LogDebug("✅ All migrations applied");
    }
}