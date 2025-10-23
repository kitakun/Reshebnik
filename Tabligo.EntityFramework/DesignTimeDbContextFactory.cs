using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tabligo.EntityFramework;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TabligoContext>
{
    public TabligoContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TabligoContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=reshebnik;Username=migrator;Password=migrator");

        return new TabligoContext(optionsBuilder.Options);
    }
}