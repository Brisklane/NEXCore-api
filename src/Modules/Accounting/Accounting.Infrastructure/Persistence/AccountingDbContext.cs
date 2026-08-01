using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence.Seeders;
using Nexcore.SharedKernel.Enums;

namespace Accounting.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Accounting module (schema <c>accounting</c>): chart of accounts,
/// journals, balances, fiscal calendar, financial dimensions, budgets and approvals.
/// Chart-of-accounts seeding is done at runtime per company by AccountingInitializationService
/// (with real company/branch IDs), not at migration time.
/// </summary>
public class AccountingDbContext : DbContext
{
    private const string DefaultSchema = "accounting";

    public AccountingDbContext(DbContextOptions<AccountingDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Ledger> Ledgers { get; set; } = null!;
    public DbSet<LedgerAccount> LedgerAccounts { get; set; } = null!;
    public DbSet<AccountCategory> AccountCategories { get; set; } = null!;
    public DbSet<JournalEntry> JournalEntries { get; set; } = null!;
    public DbSet<JournalLine> JournalLines { get; set; } = null!;
    public DbSet<AccountBalance> AccountBalances { get; set; } = null!;
    public DbSet<FiscalCalendar> FiscalCalendars { get; set; } = null!;
    public DbSet<FiscalPeriod> FiscalPeriods { get; set; } = null!;
    public DbSet<Dimension> Dimensions { get; set; } = null!;
    public DbSet<DimensionValue> DimensionValues { get; set; } = null!;
    public DbSet<DimensionSet> DimensionSets { get; set; } = null!;
    public DbSet<DimensionSetItem> DimensionSetItems { get; set; } = null!;
    public DbSet<PostingProfile> PostingProfiles { get; set; } = null!;
    public DbSet<TaxCode> TaxCodes { get; set; } = null!;
    public DbSet<JournalAudit> JournalAudits { get; set; } = null!;

    /// <summary>
    /// Budget amounts by account and period
    /// Enables variance analysis (Actual vs Budget)
    /// </summary>
    public DbSet<Budget> Budgets { get; set; } = null!;

    /// <summary>
    /// Approval workflow tracking for journal entries
    /// Records all approval steps and decisions
    /// </summary>
    public DbSet<JournalEntryApproval> JournalEntryApprovals { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Suppress pending model changes warning during migration
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(DefaultSchema);

        // Ledger configuration
        modelBuilder.Entity<Ledger>(entity =>
        {
            entity.ToTable("Ledgers", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.BaseCurrencyCode).IsRequired().HasMaxLength(3);
            entity.HasMany(e => e.Accounts).WithOne(a => a.Ledger).HasForeignKey(a => a.LedgerId);
            entity.HasMany(e => e.JournalEntries).WithOne(j => j.Ledger).HasForeignKey(j => j.LedgerId);
        });

        // LedgerAccount configuration
        modelBuilder.Entity<LedgerAccount>(entity =>
        {
            entity.ToTable("LedgerAccounts", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AccountName).IsRequired().HasMaxLength(255);
            // Configure SubledgerType enum to be stored as string
            entity.Property(e => e.SubledgerType)
                .HasConversion<string?>()
                .HasMaxLength(50);
            entity.HasOne(e => e.Ledger)
                .WithMany(l => l.Accounts)
                .HasForeignKey(e => e.LedgerId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Accounts)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne(e => e.ParentAccount)
                .WithMany(p => p.ChildAccounts)
                .HasForeignKey(e => e.ParentAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasMany(e => e.JournalLines)
                .WithOne(j => j.LedgerAccount)
                .HasForeignKey(j => j.LedgerAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.SubledgerMasterAccount)
                .WithMany(m => m.SubledgerAccounts)
                .HasForeignKey(e => e.SubledgerMasterAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            // Account number is unique per company; supports fast chart-of-accounts lookups.
            entity.HasIndex(e => new { e.CompanyId, e.AccountNumber })
                .IsUnique()
                .HasDatabaseName("IX_LedgerAccount_Company_AccountNumber");
        });

        // AccountCategory configuration
        modelBuilder.Entity<AccountCategory>(entity =>
        {
            entity.ToTable("AccountCategories", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasMany(e => e.Accounts).WithOne(a => a.Category).HasForeignKey(a => a.CategoryId);
        });

        // JournalEntry configuration
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.ToTable("JournalEntries", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JournalNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6);
            entity.Property(e => e.TotalDebit).HasPrecision(18, 2);
            entity.Property(e => e.TotalCredit).HasPrecision(18, 2);
            entity.HasOne(e => e.Ledger).WithMany(l => l.JournalEntries).HasForeignKey(e => e.LedgerId);
            entity.HasMany(e => e.Lines).WithOne(j => j.JournalEntry).HasForeignKey(j => j.JournalEntryId);
            entity.HasMany(e => e.AuditTrail).WithOne(a => a.JournalEntry).HasForeignKey(a => a.JournalEntryId);

            // Journal number is unique per company.
            entity.HasIndex(e => new { e.CompanyId, e.JournalNumber })
                .IsUnique()
                .HasDatabaseName("IX_JournalEntry_Company_JournalNumber");
            // Ledger + posting date drives period/statement reporting.
            entity.HasIndex(e => new { e.LedgerId, e.PostingDate })
                .HasDatabaseName("IX_JournalEntry_Ledger_PostingDate");
            // Approval-queue lookups by status within a company.
            entity.HasIndex(e => new { e.CompanyId, e.Status })
                .HasDatabaseName("IX_JournalEntry_Company_Status");
        });

        // JournalLine configuration
        modelBuilder.Entity<JournalLine>(entity =>
        {
            entity.ToTable("JournalLines", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);
            // Configure ModuleType enum to be stored as string
            entity.Property(e => e.SourceModuleType)
                .HasConversion<string?>()
                .HasMaxLength(50);
            // Configure EntityType enum to be stored as string
            entity.Property(e => e.SourceEntityType)
                .HasConversion<string?>()
                .HasMaxLength(50);
            // Configure PaymentTerms enum to be stored as string
            entity.Property(e => e.PaymentTermsCode)
                .HasConversion<string?>()
                .HasMaxLength(50);

            // Monetary values at (18,2); exchange rate at (18,6) so small rates aren't truncated;
            // quantity/unit price at (18,4) for finer line detail.
            entity.Property(e => e.DebitAmount).HasPrecision(18, 2);
            entity.Property(e => e.CreditAmount).HasPrecision(18, 2);
            entity.Property(e => e.BaseDebitAmount).HasPrecision(18, 2);
            entity.Property(e => e.BaseCreditAmount).HasPrecision(18, 2);
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 4);

            entity.HasOne(e => e.JournalEntry)
                .WithMany(j => j.Lines)
                .HasForeignKey(e => e.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LedgerAccount)
                .WithMany(a => a.JournalLines)
                .HasForeignKey(e => e.LedgerAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            // Account-ledger / trial-balance detail: scan all lines hitting an account.
            entity.HasIndex(e => e.LedgerAccountId)
                .HasDatabaseName("IX_JournalLine_LedgerAccount");
            // Drill-back from a GL line to the source document that produced it.
            entity.HasIndex(e => new { e.SourceEntityType, e.SourceEntityId })
                .HasDatabaseName("IX_JournalLine_Source");
            // AR/AP aging: only index rows that actually carry a customer/vendor.
            entity.HasIndex(e => e.CustomerId)
                .HasFilter("customer_id IS NOT NULL")
                .HasDatabaseName("IX_JournalLine_Customer");
            entity.HasIndex(e => e.VendorId)
                .HasFilter("vendor_id IS NOT NULL")
                .HasDatabaseName("IX_JournalLine_Vendor");
        });

        // AccountBalance configuration
        modelBuilder.Entity<AccountBalance>(entity =>
        {
            entity.ToTable("AccountBalances", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BalanceType).HasMaxLength(50).HasDefaultValue("Actual");
            entity.Property(e => e.OpeningBalance).HasPrecision(18, 2);
            entity.Property(e => e.DebitTotal).HasPrecision(18, 2);
            entity.Property(e => e.CreditTotal).HasPrecision(18, 2);
            entity.Property(e => e.ClosingBalance).HasPrecision(18, 2);
            entity.HasOne(e => e.LedgerAccount)
                .WithMany(a => a.Balances)
                .HasForeignKey(e => e.LedgerAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // One balance row per account/period/type; the trial-balance hot path.
            entity.HasIndex(e => new { e.CompanyId, e.LedgerAccountId, e.FiscalPeriodId, e.BalanceType })
                .IsUnique()
                .HasDatabaseName("IX_AccountBalance_Account_Period_Type");
        });

        // FiscalCalendar configuration
        modelBuilder.Entity<FiscalCalendar>(entity =>
        {
            entity.ToTable("FiscalCalendars", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasMany(e => e.Periods).WithOne(p => p.FiscalCalendar).HasForeignKey(p => p.FiscalCalendarId);
        });

        // FiscalPeriod configuration
        modelBuilder.Entity<FiscalPeriod>(entity =>
        {
            entity.ToTable("FiscalPeriods", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PeriodName).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.FiscalCalendar).WithMany(c => c.Periods).HasForeignKey(e => e.FiscalCalendarId);
            // Resolve a posting date to its period within a calendar.
            entity.HasIndex(e => new { e.FiscalCalendarId, e.StartDate, e.EndDate })
                .HasDatabaseName("IX_FiscalPeriod_Calendar_Dates");
        });

        // Dimension configuration
        modelBuilder.Entity<Dimension>(entity =>
        {
            entity.ToTable("Dimensions", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasMany(e => e.Values).WithOne(v => v.Dimension).HasForeignKey(v => v.DimensionId);
            entity.HasIndex(e => new { e.CompanyId, e.Code })
                .IsUnique()
                .HasDatabaseName("IX_Dimension_Company_Code");
        });

        // DimensionValue configuration
        modelBuilder.Entity<DimensionValue>(entity =>
        {
            entity.ToTable("DimensionValues", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ValueCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ValueName).IsRequired().HasMaxLength(255);
            entity.HasOne(e => e.Dimension)
                .WithMany(d => d.Values)
                .HasForeignKey(e => e.DimensionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.DimensionId, e.ValueCode })
                .IsUnique()
                .HasDatabaseName("IX_DimensionValue_Dimension_ValueCode");
        });

        // DimensionSet configuration
        modelBuilder.Entity<DimensionSet>(entity =>
        {
            entity.ToTable("DimensionSets", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HashCode).IsRequired().HasMaxLength(256);
            entity.HasMany(e => e.Items).WithOne(i => i.DimensionSet).HasForeignKey(i => i.DimensionSetId);
            // The hash exists to dedupe/look up identical dimension combinations - index it.
            entity.HasIndex(e => new { e.CompanyId, e.HashCode })
                .HasDatabaseName("IX_DimensionSet_Company_Hash");
        });

        // DimensionSetItem configuration
        modelBuilder.Entity<DimensionSetItem>(entity =>
        {
            entity.ToTable("DimensionSetItems", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.DimensionSet)
                .WithMany(s => s.Items)
                .HasForeignKey(e => e.DimensionSetId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Dimension)
                .WithMany()
                .HasForeignKey(e => e.DimensionId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne(e => e.DimensionValue)
                .WithMany()
                .HasForeignKey(e => e.DimensionValueId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // PostingProfile configuration
        modelBuilder.Entity<PostingProfile>(entity =>
        {
            entity.ToTable("PostingProfiles", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModuleName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.DebitAccount)
                .WithMany()
                .HasForeignKey(e => e.DebitAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne(e => e.CreditAccount)
                .WithMany()
                .HasForeignKey(e => e.CreditAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne(e => e.TaxAccount)
                .WithMany()
                .HasForeignKey(e => e.TaxAccountId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // TaxCode configuration
        modelBuilder.Entity<TaxCode>(entity =>
        {
            entity.ToTable("TaxCodes", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Percentage).HasPrecision(7, 4);
            entity.HasOne(e => e.LedgerAccount)
                .WithMany()
                .HasForeignKey(e => e.LedgerAccountId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(e => new { e.CompanyId, e.Code })
                .IsUnique()
                .HasDatabaseName("IX_TaxCode_Company_Code");
        });

        // JournalAudit configuration
        modelBuilder.Entity<JournalAudit>(entity =>
        {
            entity.ToTable("JournalAudits", DefaultSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.JournalEntry).WithMany(j => j.AuditTrail).HasForeignKey(e => e.JournalEntryId);
        });

        // Budget configuration
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable("Budgets", DefaultSchema);
            entity.HasKey(e => e.Id);
            // Configure BudgetType enum to be stored as string (optional, default is int)
            entity.Property(e => e.BudgetType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(BudgetType.Annual);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.VarianceThresholdPercentage).HasPrecision(5, 2);
            entity.HasOne(e => e.LedgerAccount)
                .WithMany()
                .HasForeignKey(e => e.LedgerAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            
            entity.HasOne(e => e.FiscalPeriod)
                .WithMany()
                .HasForeignKey(e => e.FiscalPeriodId)
                .OnDelete(DeleteBehavior.NoAction);
            
            // Index for common queries
            entity.HasIndex(e => new { e.LedgerAccountId, e.FiscalPeriodId, e.BudgetType })
                .HasDatabaseName("IX_Budget_Account_Period_Type");
        });

        // JournalEntryApproval configuration
        modelBuilder.Entity<JournalEntryApproval>(entity =>
        {
            entity.ToTable("JournalEntryApprovals", DefaultSchema);
            entity.HasKey(e => e.Id);
            // Configure ApprovalDecision enum to be stored as string
            entity.Property(e => e.ApprovalDecision)
                .HasConversion<string>()
                .HasMaxLength(50);
            // Configure ApprovalRole enum to be stored as string
            entity.Property(e => e.RequiredApprovalRole)
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.HasOne(e => e.JournalEntry)
                .WithMany()
                .HasForeignKey(e => e.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Index for approval queries
            entity.HasIndex(e => new { e.JournalEntryId, e.ApprovalSequence })
                .HasDatabaseName("IX_JournalApproval_Entry_Sequence");
        });

        // Chart of accounts is seeded per company at runtime (AccountingInitializationService),
        // not here, so real company/branch IDs are used. To switch to migration-time seeding
        // instead, call AccountingSeeder.SeedAll(modelBuilder) at this point.

        // Cross-cutting rules shared by every module: UTC normalisation for all
        // DateTime properties and the xmin optimistic-concurrency token. Must stay
        // last so it sees owned-type and DbSet-less properties configured above.
        modelBuilder.ApplyNexcoreConventions();
    }
}
