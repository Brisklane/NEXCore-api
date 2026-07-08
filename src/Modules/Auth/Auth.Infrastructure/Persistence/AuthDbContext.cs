using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
            entity.ToTable("Users", "auth");
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
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
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
            entity.Property(e => e.RowVersion).IsRowVersion();
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
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles", "auth");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", "auth");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.RowVersion).IsRowVersion();
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
            entity.Property(e => e.RowVersion).IsRowVersion();
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
    }

    /// <summary>Seeds the fixed baseline permissions (stable GUIDs) for company/user/role management.</summary>
    private void SeedDefaultPermissions(ModelBuilder modelBuilder)
    {
        var permissions = new List<Permission>
        {
            new Permission { Id = new Guid("7a0162fd-cafb-42b8-bd11-1f5990be353d"), Code = "COMPANY_CREATE", Name = "Create Company", Module = "Company", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
            new Permission { Id = new Guid("27ed5123-d396-4dfa-9e33-38cd05c50cc2"), Code = "COMPANY_EDIT", Name = "Edit Company", Module = "Company", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
            new Permission { Id = new Guid("d286502a-955b-45ef-a8a8-13648c9049f4"), Code = "COMPANY_DELETE", Name = "Delete Company", Module = "Company", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
            new Permission { Id = new Guid("e5cc617f-0cf9-408f-a8dd-378dafbfea61"), Code = "COMPANY_VIEW", Name = "View Company", Module = "Company", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
            new Permission { Id = new Guid("929b9d87-dee0-48f6-ad5c-7b35a28f4d5a"), Code = "USER_CREATE", Name = "Create User", Module = "User", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
            new Permission { Id = new Guid("12cccfb2-e17e-4019-9ca6-9c18b9d50348"), Code = "USER_EDIT", Name = "Edit User", Module = "User", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
            new Permission { Id = new Guid("e5379386-7a5d-4248-ba87-c1e4dd6c2183"), Code = "USER_DELETE", Name = "Delete User", Module = "User", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
            new Permission { Id = new Guid("f9e1e66f-b5b0-4922-8139-747d3f805fe1"), Code = "USER_VIEW", Name = "View User", Module = "User", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
            new Permission { Id = new Guid("77dff666-1eca-44d2-b4c6-e8168546283e"), Code = "ROLE_CREATE", Name = "Create Role", Module = "Role", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
            new Permission { Id = new Guid("2c585047-844d-47a5-b429-5951a439bc56"), Code = "ROLE_EDIT", Name = "Edit Role", Module = "Role", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
            new Permission { Id = new Guid("b62d4f77-21a2-4e27-9df8-120c207408dc"), Code = "ROLE_DELETE", Name = "Delete Role", Module = "Role", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
            new Permission { Id = new Guid("d33cdf79-f498-414f-9f58-17c81aa024be"), Code = "ROLE_VIEW", Name = "View Role", Module = "Role", IsActive = true, CreatedByUserId = Guid.Empty, CreatedAt = DateTime.UtcNow },
        };

        modelBuilder.Entity<Permission>().HasData(permissions);
    }
}
