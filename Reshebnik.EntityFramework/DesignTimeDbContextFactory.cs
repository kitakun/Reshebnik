using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reshebnik.EntityFramework;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ReshebnikContext>
{
    public ReshebnikContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReshebnikContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=reshebnik;Username=migrator;Password=migrator");

        return new ReshebnikContext(optionsBuilder.Options);
    }
}