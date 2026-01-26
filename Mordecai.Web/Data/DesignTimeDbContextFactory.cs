using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mordecai.Web.Data;

/// <summary>
/// Factory for creating ApplicationDbContext instances at design time for EF Core migrations.
/// This allows 'dotnet ef migrations add' to work without requiring database credentials.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Use a dummy connection string for design-time migrations
        // The actual connection string is configured at runtime
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=mordecai_migrations;Username=postgres;Password=design_time_only");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
