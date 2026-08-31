using HcbeApi.Data;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Infrastructure;

public static class DatabaseConfiguration
{
    public const string PostgreSql = "PostgreSQL";
    public const string Sqlite = "SQLite";

    public static bool IsPostgreSql(string? provider) =>
        string.Equals(provider, PostgreSql, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase);

    public static void Configure(
        DbContextOptionsBuilder options,
        string? provider,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
        }

        if (IsPostgreSql(provider))
        {
            options.UseNpgsql(connectionString, postgres =>
            {
                postgres.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                postgres.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
            return;
        }

        if (!string.IsNullOrWhiteSpace(provider) &&
            !string.Equals(provider, Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported database provider '{provider}'. Use PostgreSQL or SQLite.");
        }

        options.UseSqlite(connectionString);
    }
}
