using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.ValueObjects;

namespace Core.Infrastructure.Persistence;

/// <summary>
/// Core module database context for organizational entities.
/// ITenantContext is injected to scope Company queries to the current tenant.
/// Global query filter on Company ensures cross-tenant data leakage is impossible at the ORM level.
/// </summary>
public class CoreDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public CoreDbContext(DbContextOptions<CoreDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Tenant / Subscription
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();

    // Organizational
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();

    // Reference data
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Subdivision> Subdivisions => Set<Subdivision>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<CountryTranslation> CountryTranslations => Set<CountryTranslation>();
    public DbSet<SubdivisionTranslation> SubdivisionTranslations => Set<SubdivisionTranslation>();
    public DbSet<CityTranslation> CityTranslations => Set<CityTranslation>();
    public DbSet<CurrencyTranslation> CurrencyTranslations => Set<CurrencyTranslation>();
    public DbSet<Language> Languages => Set<Language>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("core");
        ApplyUtcDateTimeConverters(modelBuilder);

        // ── Tenant ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants", "core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.Slug).IsUnique();

            entity.HasMany(e => e.Companies)
                  .WithOne(c => c.Tenant)
                  .HasForeignKey(c => c.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Subscriptions)
                  .WithOne(s => s.Tenant)
                  .HasForeignKey(s => s.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── SubscriptionPlan ───────────────────────────────────────────────────
        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("SubscriptionPlans", "core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.PricePerMonth).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PricePerYear).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AllowedModules).HasMaxLength(500);
            entity.HasIndex(e => e.Code).IsUnique();

            entity.HasMany(e => e.TenantSubscriptions)
                  .WithOne(ts => ts.Plan)
                  .HasForeignKey(ts => ts.SubscriptionPlanId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Seed default subscription plans
            entity.HasData(SeedSubscriptionPlans());
        });

        // ── TenantSubscription ─────────────────────────────────────────────────
        modelBuilder.Entity<TenantSubscription>(entity =>
        {
            entity.ToTable("TenantSubscriptions", "core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.BillingCycle).IsRequired();
            entity.Property(e => e.LicenseKey).IsRequired().HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.LicenseKey).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status });
        });

        // ── Company ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Companies", "core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(50).IsUnicode(false);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(50).IsUnicode(false).IsRequired(false);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.LegalName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RegistrationNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BaseCurrencyCode).IsRequired().HasMaxLength(3).IsUnicode(false);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.MobileNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ContactPerson).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.WebsiteUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CompanyLogo).HasColumnType("varbinary(max)").IsRequired();
            entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)").IsRequired();
            entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)").IsRequired();
            entity.Property(e => e.RadiusInMeters).IsRequired();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
            entity.HasIndex(e => e.TenantId);
            // Company name unique within the tenant (live rows only).
            entity.HasIndex(e => new { e.TenantId, e.CompanyName }).IsUnique().HasFilter("[IsDeleted] = 0");

            entity.OwnsOne(e => e.Address, a =>
            {
                a.Property(ad => ad.StreetAddress).HasColumnName("Company_StreetAddress").HasMaxLength(500);
                a.Property(ad => ad.City).HasColumnName("Company_City").HasMaxLength(100).IsRequired();
                a.Property(ad => ad.State).HasColumnName("Company_State").HasMaxLength(100);
                a.Property(ad => ad.PostalCode).HasColumnName("Company_PostalCode").HasMaxLength(20);
                a.Property(ad => ad.Country).HasColumnName("Company_Country").HasMaxLength(100);
                a.WithOwner();
            });

            entity.HasMany(e => e.Branches).WithOne().HasForeignKey("CompanyId").OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.BusinessUnits).WithOne().HasForeignKey("CompanyId").OnDelete(DeleteBehavior.Restrict);

            // ── Global query filter: scope to current tenant when authenticated ──
            // GetValueOrDefault() avoids InvalidOperationException when TenantId is null
            // (anonymous endpoints like registration have no tenant claim).
            entity.HasQueryFilter(c =>
                _tenantContext == null || !_tenantContext.HasTenant || c.TenantId == _tenantContext.TenantId.GetValueOrDefault());
        });

        // ── Branch ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branches", "core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsUnicode(false).IsRequired(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.ManagerName).HasMaxLength(255);
            entity.Property(e => e.BranchType).HasMaxLength(50);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.BranchLogo).HasColumnType("varbinary(max)").IsRequired();
            entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)").IsRequired();
            entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)").IsRequired();
            entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique().HasFilter("[Code] IS NOT NULL");
            // Branch name unique within the company (live rows only).
            entity.HasIndex(e => new { e.CompanyId, e.Name }).IsUnique().HasFilter("[IsDeleted] = 0");

            entity.OwnsOne(e => e.Address, a =>
            {
                a.Property(ad => ad.StreetAddress).HasColumnName("Branch_StreetAddress").HasMaxLength(500);
                a.Property(ad => ad.City).HasColumnName("Branch_City").HasMaxLength(100);
                a.Property(ad => ad.State).HasColumnName("Branch_State").HasMaxLength(100);
                a.Property(ad => ad.PostalCode).HasColumnName("Branch_PostalCode").HasMaxLength(20);
                a.Property(ad => ad.Country).HasColumnName("Branch_Country").HasMaxLength(100);
                a.WithOwner();
            });

            entity.HasMany(e => e.BusinessUnits).WithOne().HasForeignKey("BranchId").OnDelete(DeleteBehavior.Restrict);
        });

        // ── BusinessUnit ───────────────────────────────────────────────────────
        modelBuilder.Entity<BusinessUnit>(entity =>
        {
            entity.ToTable("BusinessUnits", "core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsUnicode(false).IsRequired(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UnitType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.ManagerName).HasMaxLength(255);
            entity.Property(e => e.ManagerEmail).HasMaxLength(255);
            entity.Property(e => e.CostCenterCode).HasMaxLength(50);
            entity.Property(e => e.ProfitCenterCode).HasMaxLength(50);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique().HasFilter("[Code] IS NOT NULL");
            // Business unit name unique within the branch (live rows only).
            entity.HasIndex(e => new { e.BranchId, e.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // ── Reference data (unchanged) ─────────────────────────────────────────
        ConfigureCountry(modelBuilder);
        ConfigureSubdivision(modelBuilder);
        ConfigureCity(modelBuilder);
        ConfigureCurrency(modelBuilder);
        ConfigureLanguage(modelBuilder);
        ConfigureTranslations(modelBuilder);
    }

    // ── Subscription plan seed data ────────────────────────────────────────────
    private static SubscriptionPlan[] SeedSubscriptionPlans() =>
    [
        new SubscriptionPlan
        {
            Id = new Guid("00000000-0000-0000-0000-000000000001"),
            Code = "TRIAL",
            Name = "Trial",
            Description = "30-day free trial with full access",
            PricePerMonth = 0,
            PricePerYear = 0,
            MaxCompanies = 1,
            MaxUsers = 5,
            MaxBranches = 1,
            AllowedModules = "Core,HR,Accounting,Inventory,Sales",
            TrialDays = 30,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedByUserId = Guid.Empty
        },
        new SubscriptionPlan
        {
            Id = new Guid("00000000-0000-0000-0000-000000000002"),
            Code = "STARTER",
            Name = "Starter",
            Description = "For small businesses getting started",
            PricePerMonth = 49,
            PricePerYear = 490,
            MaxCompanies = 1,
            MaxUsers = 10,
            MaxBranches = 2,
            AllowedModules = "Core,HR,Accounting",
            TrialDays = 0,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedByUserId = Guid.Empty
        },
        new SubscriptionPlan
        {
            Id = new Guid("00000000-0000-0000-0000-000000000003"),
            Code = "PROFESSIONAL",
            Name = "Professional",
            Description = "For growing businesses needing full ERP capabilities",
            PricePerMonth = 149,
            PricePerYear = 1490,
            MaxCompanies = 5,
            MaxUsers = 50,
            MaxBranches = 10,
            AllowedModules = "Core,HR,Accounting,Inventory,Sales",
            TrialDays = 0,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedByUserId = Guid.Empty
        },
        new SubscriptionPlan
        {
            Id = new Guid("00000000-0000-0000-0000-000000000004"),
            Code = "ENTERPRISE",
            Name = "Enterprise",
            Description = "Unlimited scale for large organizations",
            PricePerMonth = 499,
            PricePerYear = 4990,
            MaxCompanies = -1,
            MaxUsers = -1,
            MaxBranches = -1,
            AllowedModules = null,
            TrialDays = 0,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedByUserId = Guid.Empty
        }
    ];

    // ── Reference data configuration ───────────────────────────────────────────
    private static void ConfigureCountry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("Countries", "core");
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(2).IsUnicode(false);
            entity.Property(e => e.Code3).IsRequired().HasMaxLength(3).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OfficialName).HasMaxLength(200);
            entity.Property(e => e.Region).HasMaxLength(50);
            entity.Property(e => e.SubRegion).HasMaxLength(75);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3).IsUnicode(false);
            entity.Property(e => e.DefaultCurrencyId);
            entity.Property(e => e.PhoneCode).HasMaxLength(10);
            entity.Property(e => e.TimeZone).HasMaxLength(50);
            entity.Property(e => e.FlagEmoji).HasMaxLength(10);
            entity.Property(e => e.PostalCodePattern).HasMaxLength(100);
            entity.Property(e => e.AddressFormat).HasMaxLength(20).HasDefaultValue("DEFAULT");
            entity.Property(e => e.StateLabel).HasMaxLength(50).HasDefaultValue("State / Province");
            entity.Property(e => e.PostalCodeLabel).HasMaxLength(50);

            entity.HasOne(e => e.DefaultCurrency)
                  .WithMany(c => c.Countries)
                  .HasForeignKey(e => e.DefaultCurrencyId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Subdivisions)
                  .WithOne(s => s.Country)
                  .HasForeignKey(s => s.CountryCode)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Cities)
                  .WithOne(c => c.Country)
                  .HasForeignKey(c => c.CountryCode)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSubdivision(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subdivision>(entity =>
        {
            entity.ToTable("Subdivisions", "core");
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(2).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SubdivisionType).HasMaxLength(30).HasDefaultValue("State");
            entity.HasIndex(e => e.CountryCode);

            entity.HasMany(e => e.Cities)
                  .WithOne(c => c.Subdivision)
                  .HasForeignKey(c => c.SubdivisionCode)
                  .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureCity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>(entity =>
        {
            entity.ToTable("Cities", "core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.CityCode).HasMaxLength(3).IsUnicode(false);
            entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(2).IsUnicode(false);
            entity.Property(e => e.SubdivisionCode).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)");
            entity.HasIndex(e => e.CountryCode);
            entity.HasIndex(e => e.SubdivisionCode);
            entity.HasIndex(e => new { e.CountryCode, e.Name });
        });
    }

    private static void ConfigureCurrency(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Currency>(entity =>
        {
            entity.ToTable("Currencies", "core");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(3).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Symbol).HasMaxLength(10);
            entity.HasIndex(e => e.Code).IsUnique();
        });
    }

    private static void ConfigureLanguage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Language>(entity =>
        {
            entity.ToTable("Languages", "core");
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NativeName).IsRequired().HasMaxLength(100);
        });
    }

    private static void ConfigureTranslations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CountryTranslation>(entity =>
        {
            entity.ToTable("CountryTranslations", "core");
            entity.HasKey(e => new { e.CountryCode, e.LanguageCode });
            entity.Property(e => e.CountryCode).HasMaxLength(2).IsUnicode(false);
            entity.Property(e => e.LanguageCode).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OfficialName).HasMaxLength(200);

            entity.HasOne(e => e.Country).WithMany(c => c.Translations).HasForeignKey(e => e.CountryCode).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Language).WithMany(l => l.CountryTranslations).HasForeignKey(e => e.LanguageCode).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SubdivisionTranslation>(entity =>
        {
            entity.ToTable("SubdivisionTranslations", "core");
            entity.HasKey(e => new { e.SubdivisionCode, e.LanguageCode });
            entity.Property(e => e.SubdivisionCode).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.LanguageCode).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Subdivision).WithMany(s => s.Translations).HasForeignKey(e => e.SubdivisionCode).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Language).WithMany(l => l.SubdivisionTranslations).HasForeignKey(e => e.LanguageCode).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CityTranslation>(entity =>
        {
            entity.ToTable("CityTranslations", "core");
            entity.HasKey(e => new { e.CityId, e.LanguageCode });
            entity.Property(e => e.LanguageCode).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);

            entity.HasOne(e => e.City).WithMany(c => c.Translations).HasForeignKey(e => e.CityId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Language).WithMany(l => l.CityTranslations).HasForeignKey(e => e.LanguageCode).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CurrencyTranslation>(entity =>
        {
            entity.ToTable("CurrencyTranslations", "core");
            entity.HasKey(e => new { e.CurrencyId, e.LanguageCode });
            entity.Property(e => e.LanguageCode).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Currency).WithMany(c => c.Translations).HasForeignKey(e => e.CurrencyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Language).WithMany(l => l.CurrencyTranslations).HasForeignKey(e => e.LanguageCode).OnDelete(DeleteBehavior.Restrict);
        });
    }

    protected static void ApplyUtcDateTimeConverters(ModelBuilder modelBuilder)
    {
        var utc = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var utcN = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
            v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);
        foreach (var e in modelBuilder.Model.GetEntityTypes())
            foreach (var p in e.GetProperties())
            {
                if (p.ClrType == typeof(DateTime))  p.SetValueConverter(utc);
                if (p.ClrType == typeof(DateTime?)) p.SetValueConverter(utcN);
            }
    }
}
