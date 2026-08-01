using Microsoft.EntityFrameworkCore;

namespace Nexcore.SharedKernel.Persistence;

/// <summary>
/// A binary file held in the database rather than an external object store — item images,
/// POS receipt logos, and similar small assets.
/// <para>
/// Intended for local and self-hosted deployments that have no blob storage. Bytes are served
/// back over HTTP by each module's image controller, and that URL is what gets persisted on the
/// owning record (for example <c>ItemImage.Url</c> or a receipt template's <c>LogoUrl</c>).
/// </para>
/// <para>
/// Defined once here, but each module still maps it to its <em>own</em> table
/// (<c>inventory.stored_files</c>, <c>sales.stored_files</c>) via
/// <see cref="StoredFileConfiguration.ConfigureStoredFile"/>. That mirrors how
/// <see cref="Audit.AuditLogEntry"/> is handled: one definition, no shared write path, and each
/// module's migrations stay independent so a module can still be extracted later.
/// </para>
/// </summary>
public class StoredFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Original file name, used only for reference and download naming.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME type, returned as the Content-Type when the file is served.</summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>Raw file bytes (PostgreSQL <c>bytea</c>).</summary>
    public byte[] Content { get; set; } = [];

    public long SizeBytes { get; set; }

    // ── Tenant scope ─────────────────────────────────────────────────────────
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? BusinessUnitId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Shared mapping for <see cref="StoredFile"/>, so the two module tables cannot drift apart in
/// column limits or indexing the way two hand-maintained copies would.
/// </summary>
public static class StoredFileConfiguration
{
    /// <summary>Maps <see cref="StoredFile"/> into <paramref name="schema"/>. Call from <c>OnModelCreating</c>.</summary>
    public static ModelBuilder ConfigureStoredFile(this ModelBuilder modelBuilder, string schema)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("StoredFiles", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Content).IsRequired().HasColumnType("bytea");
            entity.HasIndex(e => new { e.CompanyId, e.BranchId, e.BusinessUnitId })
                  .HasDatabaseName("IX_StoredFile_Tenant");
        });

        return modelBuilder;
    }
}
