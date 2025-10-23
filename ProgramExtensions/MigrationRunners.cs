using Microsoft.EntityFrameworkCore;
using Tabligo.EntityFramework;
using Tabligo.Clickhouse.Handlers;

namespace Tabligo.Web.ProgramExtensions;

public static class MigrationRunners
{
    public static async Task RunMigrationsAsync(this WebApplication app, IConfiguration configuration)
    {
        var runMigrationsOnStart = configuration.GetValue<bool>("RunMigrationsOnStart", true);

        if (!runMigrationsOnStart)
        {
            var logger = app.Services.GetRequiredService<ILogger<TabligoContext>>();
            logger.LogInformation("Migrations are disabled by configuration (RunMigrationsOnStart = false)");
            return;
        }

        // Run Entity Framework migrations
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TabligoContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TabligoContext>>();
            logger.LogInformation($"before database migration env={app.Environment.EnvironmentName}");
            await db.Database.MigrateAsync(); // ⬅️ Applies any pending migrations
            logger.LogInformation("migrated");
        }

        // Run Clickhouse migrations
        using (var scope = app.Services.CreateScope())
        {
            var clickhouse = scope.ServiceProvider.GetRequiredService<MigrateClickhouseDatabase>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TabligoContext>>();
            logger.LogInformation("before clickhouse migration");
            await clickhouse.HandleAsync(); // ⬅️ Applies any pending migrations
            logger.LogInformation("migrated");
        }
    }
}
