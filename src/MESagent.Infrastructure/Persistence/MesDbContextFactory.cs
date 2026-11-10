using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MESagent.Infrastructure.Persistence;

public sealed class MesDbContextFactory : IDesignTimeDbContextFactory<MesDbContext>
{
    public MesDbContext CreateDbContext(string[] args)
    {
        // The fallback is only for offline migration generation; it contains no credentials.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Mes")
            ?? "Host=127.0.0.1;Port=55432;Database=mesagent;Username=mesagent_owner";
        return Create(connection);
    }

    public static MesDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.SetPostgresVersion(18, 0);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public");
            })
            .Options;
        return new MesDbContext(options);
    }
}
