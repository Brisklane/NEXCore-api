using Microsoft.EntityFrameworkCore;

namespace Nexcore.SharedKernel.Audit;

/// <summary>
/// Base <see cref="DbContext"/> for modules that persist an audit trail. Subclasses expose the
/// <see cref="AuditLogs"/> set and call <see cref="ConfigureAuditLogEntry"/> from their
/// <c>OnModelCreating</c> so every module maps the audit table identically.
/// </summary>
public abstract class AuditDbContextBase : DbContext
{
    public abstract DbSet<AuditLogEntry> AuditLogs { get; set; }

    protected AuditDbContextBase(DbContextOptions options) : base(options)
    {
    }

    /// <summary>Maps <see cref="AuditLogEntry"/> — column limits plus the read-path indexes. Invoke from <c>OnModelCreating</c>.</summary>
    protected virtual void ConfigureAuditLogEntry(ModelBuilder modelBuilder)
    {
        if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ModuleName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.OldValues)
                .HasMaxLength(4000);

            entity.Property(e => e.NewValues)
                .HasMaxLength(4000);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45); // fits a full IPv6 literal

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.Property(e => e.HttpMethod)
                .HasMaxLength(10);

            entity.Property(e => e.Endpoint)
                .HasMaxLength(500);

            entity.Property(e => e.ChangedByUsername)
                .HasMaxLength(255);

            entity.Property(e => e.CorrelationId)
                .HasMaxLength(100);

            entity.Property(e => e.Category)
                .HasMaxLength(50);

            entity.Property(e => e.Metadata)
                .HasMaxLength(4000);

            entity.Property(e => e.CompanyId).IsRequired();
            entity.Property(e => e.BranchId).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            // Indexes chosen for the common audit queries: by record, by actor, by time, and the
            // module/entity/date and company/date combinations the audit screens filter on.
            entity.HasIndex(e => new { e.EntityId, e.EntityType })
                .HasDatabaseName("IX_AuditLog_EntityId_EntityType")
                .IsUnique(false);

            entity.HasIndex(e => e.ChangedByUserId)
                .HasDatabaseName("IX_AuditLog_ChangedByUserId")
                .IsUnique(false);

            entity.HasIndex(e => e.ChangedAt)
                .HasDatabaseName("IX_AuditLog_ChangedAt")
                .IsUnique(false);

            entity.HasIndex(e => e.ModuleName)
                .HasDatabaseName("IX_AuditLog_ModuleName")
                .IsUnique(false);

            entity.HasIndex(e => new { e.ModuleName, e.EntityType, e.ChangedAt })
                .HasDatabaseName("IX_AuditLog_Module_Entity_Date")
                .IsUnique(false);

            entity.HasIndex(e => new { e.CompanyId, e.ChangedAt })
                .HasDatabaseName("IX_AuditLog_Company_Date")
                .IsUnique(false);

            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_AuditLog_Category")
                .IsUnique(false);

            entity.HasIndex(e => e.Severity)
                .HasDatabaseName("IX_AuditLog_Severity")
                .IsUnique(false);

            entity.ToTable("AuditLogs");
        });
    }
}
