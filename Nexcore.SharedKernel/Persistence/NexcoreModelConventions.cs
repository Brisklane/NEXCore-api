using System.Globalization;
using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Nexcore.SharedKernel.Persistence;

/// <summary>
/// Cross-cutting model rules applied identically by every module's <c>OnModelCreating</c>.
/// Previously each context carried its own copy of the UTC converter logic — and Auth,
/// Procurement and Manufacturing were missing it altogether — so behaviour differed by module.
/// </summary>
public static class NexcoreModelConventions
{
    /// <summary>
    /// Call this as the <em>last</em> statement of <c>OnModelCreating</c>, after all entity
    /// configuration, so it sees every property the model builder produced.
    /// </summary>
    public static void ApplyNexcoreConventions(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        ApplySnakeCaseToExplicitNames(modelBuilder);
        ApplyUtcDateTimeConventions(modelBuilder);
        ApplyConcurrencyTokenConventions(modelBuilder);
    }

    // ── Naming ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reuses EFCore.NamingConventions' own rewriter rather than reimplementing snake-casing.
    /// A second implementation would eventually disagree with the convention on acronym
    /// boundaries (RFQLine, BOMItem, POSTerminal) and produce a schema where some names were
    /// rewritten one way and some another.
    /// </summary>
    private static readonly SnakeCaseNameRewriter Rewriter = new(CultureInfo.InvariantCulture);

    /// <summary>PostgreSQL truncates any identifier longer than this, silently.</summary>
    private const int MaxIdentifierLength = 63;

    /// <summary>
    /// Snake-cases a name and, if the result would overflow PostgreSQL's identifier limit,
    /// shortens it deterministically with a hash suffix instead of letting the server
    /// truncate it. Plain truncation is what makes two distinct constraints collapse onto
    /// one name — the failure mode that surfaced on hr.interview_notifications.
    /// </summary>
    private static string Rewrite(string name)
    {
        var snake = Rewriter.RewriteName(name);
        if (snake.Length <= MaxIdentifierLength)
            return snake;

        // 8 hex chars of a stable hash of the full name keeps distinct inputs distinct
        // and produces the same result on every machine and every run.
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(snake));
        var suffix = Convert.ToHexString(hash, 0, 4).ToLowerInvariant();
        return string.Concat(snake.AsSpan(0, MaxIdentifierLength - suffix.Length - 1), "_", suffix);
    }

    /// <summary>
    /// Snake-cases the names that <c>UseSnakeCaseNamingConvention()</c> deliberately leaves
    /// alone: anything a module configured explicitly via <c>ToTable</c>,
    /// <c>HasColumnName</c> or <c>HasDatabaseName</c>.
    /// <para>
    /// Without this the schema comes out half-converted — snake_case columns inside
    /// PascalCase tables — which reintroduces the mandatory double-quoting that choosing
    /// snake_case was meant to eliminate.
    /// </para>
    /// </summary>
    private static void ApplySnakeCaseToExplicitNames(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var table = entityType.GetTableName();
            if (table is not null)
                entityType.SetTableName(Rewrite(table));

            foreach (var property in entityType.GetProperties())
            {
                var column = property.GetColumnName();
                if (column is not null)
                    property.SetColumnName(Rewrite(column));
            }

            foreach (var key in entityType.GetKeys())
                key.SetName(Rewrite(key.GetName()!));

            foreach (var foreignKey in entityType.GetForeignKeys())
                foreignKey.SetConstraintName(Rewrite(foreignKey.GetConstraintName()!));

            foreach (var index in entityType.GetIndexes())
                index.SetDatabaseName(Rewrite(index.GetDatabaseName()!));
        }
    }

    // ── UTC date/time ─────────────────────────────────────────────────────────

    /// <summary>
    /// Normalises every <see cref="DateTime"/> to UTC on the way in and stamps
    /// <see cref="DateTimeKind.Utc"/> on the way out.
    /// <para>
    /// On the write side this is not cosmetic: Npgsql maps <see cref="DateTime"/> to
    /// <c>timestamp with time zone</c> and throws on any value whose <c>Kind</c> is not
    /// <see cref="DateTimeKind.Utc"/>. Values deserialised from JSON arrive as
    /// <see cref="DateTimeKind.Unspecified"/>, which SQL Server accepted silently and
    /// PostgreSQL would reject at runtime.
    /// </para>
    /// </summary>
    private static void ApplyUtcDateTimeConventions(ModelBuilder modelBuilder)
    {
        var utc = new ValueConverter<DateTime, DateTime>(
            v => ToUtc(v),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var utcNullable = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? ToUtc(v.Value) : null,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                // Never clobber a converter a module set deliberately.
                if (property.GetValueConverter() is not null)
                    continue;

                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(utc);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(utcNullable);
            }
        }
    }

    /// <summary>
    /// Treats an <see cref="DateTimeKind.Unspecified"/> value as already-UTC rather than
    /// converting it, matching the assumption the read-side converter has always made:
    /// everything in the database is stored in UTC.
    /// </summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    // ── Optimistic concurrency ────────────────────────────────────────────────

    /// <summary>
    /// Binds the <c>RowVersion</c> property of every entity that has one to PostgreSQL's
    /// <c>xmin</c> system column. <c>xmin</c> already holds the id of the transaction that
    /// last wrote the row, so this replaces SQL Server's <c>rowversion</c> with no stored
    /// column, no trigger and no application bookkeeping.
    /// </summary>
    private static void ApplyConcurrencyTokenConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var rowVersion = entityType.FindProperty(nameof(BaseEntity.RowVersion));

            // Guard on the CLR type so a business column that happens to be called
            // RowVersion could never be silently repurposed as the concurrency token.
            if (rowVersion is null || rowVersion.ClrType != typeof(uint))
                continue;

            rowVersion.SetColumnName("xmin");
            rowVersion.SetColumnType("xid");
            rowVersion.IsConcurrencyToken = true;
            rowVersion.ValueGenerated = ValueGenerated.OnAddOrUpdate;

            // xmin is maintained entirely by PostgreSQL; EF must never write it.
            rowVersion.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            rowVersion.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        }
    }
}
