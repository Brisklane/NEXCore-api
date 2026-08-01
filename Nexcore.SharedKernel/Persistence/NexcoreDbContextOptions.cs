using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Nexcore.SharedKernel.Persistence;

/// <summary>
/// Single place where every module's <see cref="DbContext"/> is pointed at PostgreSQL.
/// Both the runtime composition root (<c>AddXInfrastructure</c>) and the design-time
/// <c>IDesignTimeDbContextFactory</c> go through here, so the provider options that shape
/// the model — naming convention above all — can never drift between the two. A drift there
/// would produce migrations that do not match the running application.
/// </summary>
public static class NexcoreDbContextOptions
{
    /// <summary>
    /// Configures Npgsql with the platform-wide conventions:
    /// <list type="bullet">
    ///   <item>snake_case tables, columns, indexes and constraints (idiomatic PostgreSQL —
    ///         no double-quoting needed in psql or hand-written SQL);</item>
    ///   <item>transient-fault retries, matching the previous SQL Server behaviour;</item>
    ///   <item>migrations pinned to the module's own assembly.</item>
    /// </list>
    /// </summary>
    /// <param name="options">The builder being configured.</param>
    /// <param name="connectionString">Npgsql connection string.</param>
    /// <param name="migrationsAssembly">Assembly holding the module's migrations.</param>
    public static DbContextOptionsBuilder UseNexcorePostgres(
        this DbContextOptionsBuilder options,
        string? connectionString,
        Assembly migrationsAssembly)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(migrationsAssembly);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "No PostgreSQL connection string was supplied. Set ConnectionStrings:DefaultConnection.");

        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);

            npgsql.MigrationsAssembly(migrationsAssembly.FullName);

            // Keep the migrations-history table in each module's own schema so the nine
            // modules stay independently migratable inside the one database.
            npgsql.MigrationsHistoryTable("__ef_migrations_history");
        });

        // Applied last so it rewrites every name the model builder produced.
        options.UseSnakeCaseNamingConvention();

        return options;
    }

    /// <summary>
    /// Generic overload, so design-time factories can keep the strongly typed builder and still
    /// hand a <see cref="DbContextOptions{TContext}"/> to the context constructor.
    /// </summary>
    public static DbContextOptionsBuilder<TContext> UseNexcorePostgres<TContext>(
        this DbContextOptionsBuilder<TContext> options,
        string? connectionString,
        Assembly migrationsAssembly)
        where TContext : DbContext
    {
        UseNexcorePostgres((DbContextOptionsBuilder)options, connectionString, migrationsAssembly);
        return options;
    }
}
