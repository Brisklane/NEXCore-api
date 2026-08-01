using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;

namespace Auth.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Auth module. Owns the identity/authorization tables under the
/// <c>auth</c> schema and seeds the baseline permission catalogue.
/// </summary>
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserCompany> UserCompanies => Set<UserCompany>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // The migration snapshot already reflects the current model, so silence the pending-changes warning.
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("auth");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.EmployeeId).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            // ── Case-insensitive identity ─────────────────────────────────────
            // SQL Server's case-insensitive collation made "Alice" and "alice" one account.
            // PostgreSQL compares case-sensitively, so uniqueness moves onto normalized
            // columns. The entity derives them (works under every provider, including the
            // in-memory one the unit tests use) and the CHECK constraints below make the
            // database reject any row where they disagree — including a raw-SQL write.
            entity.Property(e => e.UsernameNormalized)
                  .IsRequired().HasMaxLength(255).IsUnicode(false);

            entity.Property(e => e.EmailNormalized)
                  .IsRequired().HasMaxLength(255);

            entity.HasIndex(e => e.UsernameNormalized).IsUnique();
            entity.HasIndex(e => e.EmailNormalized).IsUnique();

            // btrim mirrors the Trim() in User.Normalize exactly; plain lower() would reject
            // a legitimately stored value that had surrounding whitespace.
            entity.ToTable("Users", "auth", t =>
            {
                t.HasCheckConstraint("ck_users_username_normalized",
                    "username_normalized = lower(btrim(username))");
                t.HasCheckConstraint("ck_users_email_normalized",
                    "email_normalized = lower(btrim(email))");
            });

            // Uniqueness now lives on the normalized columns; these still serve lookups and
            // ordering on the as-entered values.
            entity.HasIndex(e => e.Username);
            entity.HasIndex(e => e.Email);
            entity.HasMany(e => e.UserRoles).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.RefreshTokens).WithOne(rt => rt.User).HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", "auth");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasMany(e => e.Permissions).WithMany(p => p.Roles).UsingEntity("RolePermissions");
            entity.HasMany(e => e.UserRoles).WithOne(ur => ur.Role).HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions", "auth");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Module).HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles", "auth");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", "auth");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.HasIndex(e => e.Token).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs", "auth");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.OldValues).HasMaxLength(4000);
            entity.Property(e => e.NewValues).HasMaxLength(4000);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.ChangedByUsername).HasMaxLength(255);
            entity.HasIndex(e => new { e.EntityId, e.EntityType }).IsUnique(false);
            entity.HasIndex(e => e.ChangedByUserId).IsUnique(false);
            entity.HasIndex(e => e.ChangedAt).IsUnique(false);
        });

        modelBuilder.Entity<UserCompany>(entity =>
        {
            entity.ToTable("UserCompanies", "auth");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CompanySlug).IsRequired().HasMaxLength(50).IsUnicode(false);
            entity.HasIndex(e => new { e.UserId, e.CompanyId }).IsUnique();
            entity.HasIndex(e => e.CompanySlug);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        SeedDefaultPermissions(modelBuilder);

        // Cross-cutting rules shared by every module: UTC normalisation for all
        // DateTime properties and the xmin optimistic-concurrency token. Must stay
        // last so it sees owned-type and DbSet-less properties configured above.
        modelBuilder.ApplyNexcoreConventions();
    }

    /// <summary>
    /// Fixed creation stamp for seeded rows. <c>HasData</c> is part of the model, so a
    /// <c>DateTime.UtcNow</c> here made the model differ on every build: EF reported pending
    /// model changes permanently and <c>migrations add</c> produced an empty migration each
    /// time. Mirrors the constant CoreDbContext uses for its subscription-plan seed.
    /// </summary>
    private static readonly DateTime SeedTimestamp = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Seeds the fixed baseline permissions (stable GUIDs) for company/user/role management.</summary>
    private void SeedDefaultPermissions(ModelBuilder modelBuilder)
    {
        var permissions = new List<Permission>
        {
            new Permission { Id = new Guid("7a0162fd-cafb-42b8-bd11-1f5990be353d"), Code = "COMPANY_CREATE", Name = "Create Company", Module = "Company", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
            new Permission { Id = new Guid("27ed5123-d396-4dfa-9e33-38cd05c50cc2"), Code = "COMPANY_EDIT", Name = "Edit Company", Module = "Company", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
            new Permission { Id = new Guid("d286502a-955b-45ef-a8a8-13648c9049f4"), Code = "COMPANY_DELETE", Name = "Delete Company", Module = "Company", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
            new Permission { Id = new Guid("e5cc617f-0cf9-408f-a8dd-378dafbfea61"), Code = "COMPANY_VIEW", Name = "View Company", Module = "Company", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
            new Permission { Id = new Guid("929b9d87-dee0-48f6-ad5c-7b35a28f4d5a"), Code = "USER_CREATE", Name = "Create User", Module = "User", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
            new Permission { Id = new Guid("12cccfb2-e17e-4019-9ca6-9c18b9d50348"), Code = "USER_EDIT", Name = "Edit User", Module = "User", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
            new Permission { Id = new Guid("e5379386-7a5d-4248-ba87-c1e4dd6c2183"), Code = "USER_DELETE", Name = "Delete User", Module = "User", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
            new Permission { Id = new Guid("f9e1e66f-b5b0-4922-8139-747d3f805fe1"), Code = "USER_VIEW", Name = "View User", Module = "User", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
            new Permission { Id = new Guid("77dff666-1eca-44d2-b4c6-e8168546283e"), Code = "ROLE_CREATE", Name = "Create Role", Module = "Role", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
            new Permission { Id = new Guid("2c585047-844d-47a5-b429-5951a439bc56"), Code = "ROLE_EDIT", Name = "Edit Role", Module = "Role", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
            new Permission { Id = new Guid("b62d4f77-21a2-4e27-9df8-120c207408dc"), Code = "ROLE_DELETE", Name = "Delete Role", Module = "Role", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
            new Permission { Id = new Guid("d33cdf79-f498-414f-9f58-17c81aa024be"), Code = "ROLE_VIEW", Name = "View Role", Module = "Role", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = SeedTimestamp },
        };

        modelBuilder.Entity<Permission>().HasData(permissions);
    }
}
