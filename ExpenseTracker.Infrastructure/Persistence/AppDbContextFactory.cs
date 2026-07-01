using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the EF Core CLI (<c>dotnet ef migrations add ...</c>) so migrations
/// can be generated against this project without spinning up the API host. The connection string is
/// only used for tooling/migration scaffolding, never at runtime.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=ExpenseTrackerApi;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
