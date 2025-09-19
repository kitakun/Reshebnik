using Microsoft.EntityFrameworkCore;
using Reshebnik.EntityFramework;
using Reshebnik.Clickhouse.Handlers;

namespace Reshebnik.Web.ProgramExtensions;

public static class MigrationRunners
{
    public static async Task RunMigrationsAsync(this WebApplication app, IConfiguration configuration)
    {
        var runMigrationsOnStart = configuration.GetValue<bool>("RunMigrationsOnStart", true);

        if (!runMigrationsOnStart)
        {
            var logger = app.Services.GetRequiredService<ILogger<ReshebnikContext>>();
            logger.LogInformation("Migrations are disabled by configuration (RunMigrationsOnStart = false)");
            return;
        }

        // Run Entity Framework migrations
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ReshebnikContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReshebnikContext>>();
            logger.LogInformation($"before database migration env={app.Environment.EnvironmentName}");
            await db.Database.MigrateAsync(); // ⬅️ Applies any pending migrations
            logger.LogInformation("migrated");
        }

        // Run Clickhouse migrations
        using (var scope = app.Services.CreateScope())
        {
            var clickhouse = scope.ServiceProvider.GetRequiredService<MigrateClickhouseDatabase>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReshebnikContext>>();
            logger.LogInformation("before clickhouse migration");
            await clickhouse.HandleAsync(); // ⬅️ Applies any pending migrations
            logger.LogInformation("migrated");
        }
    }
}
