using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Enums;

namespace Accounting.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seed data generator for Accounting module
/// Creates comprehensive test data including:
/// - Chart of Accounts (COA)
/// - Ledgers
/// - Fiscal Calendars & Periods
/// - Account Categories
/// - Tax Codes
/// - Dimensions
/// - Posting Profiles
/// - Sample Journal Entries (all statuses)
/// - Budget data
/// </summary>
public static class AccountingSeeder
{
    // Test Company ID (should match a real company from Core module)
    private static readonly Guid TestCompanyId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    private static readonly Guid TestBranchId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
    private static readonly Guid TestBusinessUnitId = Guid.Parse("550e8400-e29b-41d4-a716-446655440002");
    private static readonly Guid TestUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440003");

    /// <summary>
    /// Seed all accounting data
    /// </summary>
    public static void SeedAll(ModelBuilder modelBuilder)
    {
        SeedAccountCategories(modelBuilder);
        SeedLedgers(modelBuilder);
        SeedLedgerAccounts(modelBuilder);
        SeedFiscalCalendars(modelBuilder);
        SeedFiscalPeriods(modelBuilder);
        SeedTaxCodes(modelBuilder);
        SeedDimensions(modelBuilder);
        SeedDimensionValues(modelBuilder);
        SeedPostingProfiles(modelBuilder);
        SeedBudgets(modelBuilder);
        SeedJournalEntries(modelBuilder);
        SeedJournalLines(modelBuilder);
    }

    #region Account Categories

    private static void SeedAccountCategories(ModelBuilder modelBuilder)
    {
        var categories = new[]
        {
            // Assets
            new AccountCategory
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                CompanyId = TestCompanyId,
                Name = "Current Assets",
                Type = AccountType.Asset,
                NormalBalance = NormalBalance.Debit,
                Description = "Short-term assets expected to be converted to cash within one year",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new AccountCategory
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                CompanyId = TestCompanyId,
                Name = "Fixed Assets",
                Type = AccountType.Asset,
                NormalBalance = NormalBalance.Debit,
                Description = "Long-term assets with useful life greater than one year",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Liabilities
            new AccountCategory
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                CompanyId = TestCompanyId,
                Name = "Current Liabilities",
                Type = AccountType.Liability,
                NormalBalance = NormalBalance.Credit,
                Description = "Obligations due within one year",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new AccountCategory
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                CompanyId = TestCompanyId,
                Name = "Long-term Liabilities",
                Type = AccountType.Liability,
                NormalBalance = NormalBalance.Credit,
                Description = "Obligations due after one year",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Equity
            new AccountCategory
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                CompanyId = TestCompanyId,
                Name = "Shareholders' Equity",
                Type = AccountType.Equity,
                NormalBalance = NormalBalance.Credit,
                Description = "Owner's investment and retained earnings",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Revenue
            new AccountCategory
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
                CompanyId = TestCompanyId,
                Name = "Sales Revenue",
                Type = AccountType.Revenue,
                NormalBalance = NormalBalance.Credit,
                Description = "Income from product/service sales",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Expenses
            new AccountCategory
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
                CompanyId = TestCompanyId,
                Name = "Cost of Goods Sold",
                Type = AccountType.Expense,
                NormalBalance = NormalBalance.Debit,
                Description = "Direct costs of producing goods sold",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new AccountCategory
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000008"),
                CompanyId = TestCompanyId,
                Name = "Operating Expenses",
                Type = AccountType.Expense,
                NormalBalance = NormalBalance.Debit,
                Description = "Expenses incurred in normal business operations",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            }
        };

        modelBuilder.Entity<AccountCategory>().HasData(categories);
    }

    #endregion

    #region Ledgers

    private static void SeedLedgers(ModelBuilder modelBuilder)
    {
        var ledgers = new[]
        {
            new Ledger
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                Name = "Main Ledger",
                BaseCurrencyCode = "USD",
                FiscalCalendarId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                IsDefault = true,
                IsActive = true,
                Description = "Primary general ledger for the company",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new Ledger
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                Name = "Subsidiary Ledger",
                BaseCurrencyCode = "USD",
                FiscalCalendarId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                IsDefault = false,
                IsActive = true,
                Description = "Secondary ledger for specific transactions",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            }
        };

        modelBuilder.Entity<Ledger>().HasData(ledgers);
    }

    #endregion

    #region Chart of Accounts (Ledger Accounts)

    private static void SeedLedgerAccounts(ModelBuilder modelBuilder)
    {
        var accounts = new List<LedgerAccount>
        {
            // ASSETS (1000-1999)
            // Current Assets
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "1000",
                AccountName = "Cash and Cash Equivalents",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = true,
                CurrencyCode = "USD",
                AllowManualEntry = true,
                IsActive = true,
                Description = "Company cash accounts including checking and savings",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000002"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "1010",
                AccountName = "Checking Account",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                ParentAccountId = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                IsSubledgerAccount = true,
                SubledgerType = SubledgerType.Bank,
                SubledgerMasterAccountId = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Main checking account at XYZ Bank",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000003"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "1020",
                AccountName = "Savings Account",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                ParentAccountId = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                IsSubledgerAccount = true,
                SubledgerType = SubledgerType.Bank,
                SubledgerMasterAccountId = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Savings account for emergency reserves",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Accounts Receivable
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "1200",
                AccountName = "Accounts Receivable",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                IsSubledgerAccount = false,
                IsPostingAllowed = false,
                IsControlAccount = true,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Master account for customer receivables",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000005"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "1201",
                AccountName = "Customer: ABC Corp",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                ParentAccountId = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                IsSubledgerAccount = true,
                SubledgerType = SubledgerType.AccountsReceivable,
                SubledgerMasterAccountId = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Receivable from ABC Corporation",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000006"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "1202",
                AccountName = "Customer: XYZ Inc",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                ParentAccountId = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                IsSubledgerAccount = true,
                SubledgerType = SubledgerType.AccountsReceivable,
                SubledgerMasterAccountId = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Receivable from XYZ Inc",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Inventory
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000007"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "1300",
                AccountName = "Inventory",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Finished goods inventory",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Fixed Assets
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000008"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "1500",
                AccountName = "Property, Plant & Equipment",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = true,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Buildings, equipment, and machinery",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000009"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "1501",
                AccountName = "Office Equipment",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                ParentAccountId = Guid.Parse("40000000-0000-0000-0000-000000000008"),
                IsSubledgerAccount = true,
                SubledgerType = SubledgerType.FixedAssets,
                SubledgerMasterAccountId = Guid.Parse("40000000-0000-0000-0000-000000000008"),
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Computer equipment and furniture",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000010"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "1999",
                AccountName = "Accumulated Depreciation",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Contra-asset account for depreciation",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // LIABILITIES (2000-2999)
            // Accounts Payable
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000011"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "2100",
                AccountName = "Accounts Payable",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                IsSubledgerAccount = false,
                IsPostingAllowed = false,
                IsControlAccount = true,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Master account for vendor payables",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000012"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "2101",
                AccountName = "Vendor: ABC Supplies",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                ParentAccountId = Guid.Parse("40000000-0000-0000-0000-000000000011"),
                IsSubledgerAccount = true,
                SubledgerType = SubledgerType.AccountsPayable,
                SubledgerMasterAccountId = Guid.Parse("40000000-0000-0000-0000-000000000011"),
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Payable to ABC Supplies vendor",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Sales Tax Payable
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000013"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "2200",
                AccountName = "Sales Tax Payable",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Sales tax collected and payable to government",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // EQUITY (3000-3999)
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000014"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "3100",
                AccountName = "Common Stock",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                IsSubledgerAccount = false,
                IsPostingAllowed = false,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Common stock issued",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000015"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "3200",
                AccountName = "Retained Earnings",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                IsSubledgerAccount = false,
                IsPostingAllowed = false,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Accumulated earnings retained in business",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // REVENUE (4000-4999)
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000016"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "4100",
                AccountName = "Product Sales",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000006"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = true,
                IsActive = true,
                Description = "Revenue from product sales",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000017"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "4200",
                AccountName = "Service Revenue",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000006"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = true,
                IsActive = true,
                Description = "Revenue from services rendered",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // EXPENSES (5000-5999)
            // COGS
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000018"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "5100",
                AccountName = "Cost of Goods Sold",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000007"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Direct cost of producing goods",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Operating Expenses
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000019"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "5200",
                AccountName = "Salaries & Wages",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000008"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = true,
                IsActive = true,
                Description = "Employee salaries and wages",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000020"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "5300",
                AccountName = "Rent Expense",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000008"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = true,
                IsActive = true,
                Description = "Office and warehouse rent",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000021"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "5400",
                AccountName = "Depreciation Expense",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000008"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = false,
                IsActive = true,
                Description = "Depreciation of fixed assets",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new LedgerAccount
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000022"),
                CompanyId = TestCompanyId,
                LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                AccountNumber = "5500",
                AccountName = "Office Supplies",
                CategoryId = Guid.Parse("10000000-0000-0000-0000-000000000008"),
                IsSubledgerAccount = false,
                IsPostingAllowed = true,
                IsControlAccount = false,
                CurrencyCode = "USD",
                AllowManualEntry = true,
                IsActive = true,
                Description = "Office supplies and materials",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            }
        };

        modelBuilder.Entity<LedgerAccount>().HasData(accounts);
    }

    #endregion

    #region Fiscal Calendars & Periods

    private static void SeedFiscalCalendars(ModelBuilder modelBuilder)
    {
        var calendars = new[]
        {
            new FiscalCalendar
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                Name = "Calendar Year 2024",
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31),
                IsActive = true,
                Description = "Calendar year fiscal period 2024",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            }
        };

        modelBuilder.Entity<FiscalCalendar>().HasData(calendars);
    }

    private static void SeedFiscalPeriods(ModelBuilder modelBuilder)
    {
        var periods = new List<FiscalPeriod>();

        // Create 12 months for 2024
        for (int month = 1; month <= 12; month++)
        {
            var startDate = new DateTime(2024, month, 1);
            var endDate = month == 12 ? new DateTime(2024, 12, 31) : new DateTime(2024, month + 1, 1).AddDays(-1);

            periods.Add(new FiscalPeriod
            {
                Id = Guid.Parse($"31000000-0000-0000-0000-{month:D12}"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                FiscalCalendarId = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                PeriodName = $"{startDate:MMMM} 2024",
                StartDate = startDate,
                EndDate = endDate,
                IsClosed = month < DateTime.Now.Month, // Close past periods
                Description = $"Fiscal period for {startDate:MMMM 'yyyy}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            });
        }

        modelBuilder.Entity<FiscalPeriod>().HasData(periods);
    }

    #endregion

    #region Tax Codes

    private static void SeedTaxCodes(ModelBuilder modelBuilder)
    {
        var taxCodes = new[]
        {
            new TaxCode
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000001"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                Code = "SALES_TAX_10",
                Name = "Sales Tax 10%",
                Percentage = 10.00m,
                LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000013"),
                IsRecoverable = false,
                IsActive = true,
                Description = "Standard sales tax rate",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new TaxCode
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000002"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                Code = "INPUT_TAX",
                Name = "Input VAT 15%",
                Percentage = 15.00m,
                LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                IsRecoverable = true,
                IsActive = true,
                Description = "Recoverable input tax",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            }
        };

        modelBuilder.Entity<TaxCode>().HasData(taxCodes);
    }

    #endregion

    #region Dimensions

    private static void SeedDimensions(ModelBuilder modelBuilder)
    {
        var dimensions = new[]
        {
            new Dimension
            {
                Id = Guid.Parse("60000000-0000-0000-0000-000000000001"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                Code = "DEPT",
                Name = "Department",
                IsActive = true,
                Description = "Organizational department",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new Dimension
            {
                Id = Guid.Parse("60000000-0000-0000-0000-000000000002"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                Code = "LOCATION",
                Name = "Location",
                IsActive = true,
                Description = "Geographic location or branch",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new Dimension
            {
                Id = Guid.Parse("60000000-0000-0000-0000-000000000003"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                Code = "PROJECT",
                Name = "Project",
                IsActive = true,
                Description = "Project tracking",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            }
        };

        modelBuilder.Entity<Dimension>().HasData(dimensions);
    }

    private static void SeedDimensionValues(ModelBuilder modelBuilder)
    {
        var dimensionValues = new[]
        {
            // Department values
            new DimensionValue
            {
                Id = Guid.Parse("61000000-0000-0000-0000-000000000001"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                DimensionId = Guid.Parse("60000000-0000-0000-0000-000000000001"),
                ValueCode = "SALES",
                ValueName = "Sales Department",
                IsActive = true,
                Description = "Sales and marketing",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new DimensionValue
            {
                Id = Guid.Parse("61000000-0000-0000-0000-000000000002"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                DimensionId = Guid.Parse("60000000-0000-0000-0000-000000000001"),
                ValueCode = "OPS",
                ValueName = "Operations Department",
                IsActive = true,
                Description = "Operations and production",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new DimensionValue
            {
                Id = Guid.Parse("61000000-0000-0000-0000-000000000003"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                DimensionId = Guid.Parse("60000000-0000-0000-0000-000000000001"),
                ValueCode = "ADMIN",
                ValueName = "Administration",
                IsActive = true,
                Description = "Administrative functions",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Location values
            new DimensionValue
            {
                Id = Guid.Parse("61000000-0000-0000-0000-000000000004"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                DimensionId = Guid.Parse("60000000-0000-0000-0000-000000000002"),
                ValueCode = "NYC",
                ValueName = "New York",
                IsActive = true,
                Description = "New York office",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },
            new DimensionValue
            {
                Id = Guid.Parse("61000000-0000-0000-0000-000000000005"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                DimensionId = Guid.Parse("60000000-0000-0000-0000-000000000002"),
                ValueCode = "LA",
                ValueName = "Los Angeles",
                IsActive = true,
                Description = "Los Angeles office",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Project values
            new DimensionValue
            {
                Id = Guid.Parse("61000000-0000-0000-0000-000000000006"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                DimensionId = Guid.Parse("60000000-0000-0000-0000-000000000003"),
                ValueCode = "PROJ001",
                ValueName = "Project Alpha",
                IsActive = true,
                Description = "Internal project Alpha",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            }
        };

        modelBuilder.Entity<DimensionValue>().HasData(dimensionValues);
    }

    #endregion

    #region Posting Profiles

    private static void SeedPostingProfiles(ModelBuilder modelBuilder)
    {
        var postingProfiles = new[]
        {
            // Sales transactions
            new PostingProfile
            {
                Id = Guid.Parse("70000000-0000-0000-0000-000000000001"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                ModuleName = "Sales",
                TransactionType = "SalesInvoice",
                DebitAccountId = Guid.Parse("40000000-0000-0000-0000-000000000005"), // AR - ABC Corp
                CreditAccountId = Guid.Parse("40000000-0000-0000-0000-000000000016"), // Product Sales
                TaxAccountId = Guid.Parse("40000000-0000-0000-0000-000000000013"), // Sales Tax Payable
                IsActive = true,
                Description = "Posting profile for sales invoices",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Purchase transactions
            new PostingProfile
            {
                Id = Guid.Parse("70000000-0000-0000-0000-000000000002"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                ModuleName = "Purchasing",
                TransactionType = "PurchaseInvoice",
                DebitAccountId = Guid.Parse("40000000-0000-0000-0000-000000000007"), // Inventory
                CreditAccountId = Guid.Parse("40000000-0000-0000-0000-000000000012"), // AP - ABC Supplies
                TaxAccountId = null,
                IsActive = true,
                Description = "Posting profile for purchase invoices",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            },

            // Payment transactions
            new PostingProfile
            {
                Id = Guid.Parse("70000000-0000-0000-0000-000000000003"),
                CompanyId = TestCompanyId,
                BranchId = TestBranchId,
                BusinessUnitId = TestBusinessUnitId,
                ModuleName = "Accounting",
                TransactionType = "CashPayment",
                DebitAccountId = Guid.Parse("40000000-0000-0000-0000-000000000011"), // AP
                CreditAccountId = Guid.Parse("40000000-0000-0000-0000-000000000001"), // Cash
                TaxAccountId = null,
                IsActive = true,
                Description = "Posting profile for cash payments",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = TestUserId
            }
        };

        modelBuilder.Entity<PostingProfile>().HasData(postingProfiles);
    }

    #endregion

    #region Budgets

    private static void SeedBudgets(ModelBuilder modelBuilder)
    {
        var budgets = new List<Budget>();

        // Create budgets for major expense accounts for 2024
        var expenseAccounts = new[]
        {
            (Guid.Parse("40000000-0000-0000-0000-000000000019"), "Salaries", 50000m),
            (Guid.Parse("40000000-0000-0000-0000-000000000020"), "Rent", 10000m),
            (Guid.Parse("40000000-0000-0000-0000-000000000021"), "Depreciation", 5000m),
            (Guid.Parse("40000000-0000-0000-0000-000000000022"), "Office Supplies", 2000m)
        };

        for (int month = 1; month <= 12; month++)
        {
            foreach (var (accountId, accountName, amount) in expenseAccounts)
            {
                budgets.Add(new Budget
                {
                    Id = Guid.NewGuid(),
                    CompanyId = TestCompanyId,
                    BranchId = TestBranchId,
                    BusinessUnitId = TestBusinessUnitId,
                    LedgerAccountId = accountId,
                    FiscalPeriodId = Guid.Parse($"31000000-0000-0000-0000-{month:D12}"),
                    Amount = amount,
                    BudgetType = BudgetType.Annual,
                    Description = $"{accountName} budget for {month:MMMM} 2024",
                    IsActive = true,
                    VarianceThresholdPercentage = 0.10m, // 10% variance threshold
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = TestUserId
                });
            }
        }

        modelBuilder.Entity<Budget>().HasData(budgets);
    }

    #endregion

    #region Journal Entries & Lines

    private static void SeedJournalEntries(ModelBuilder modelBuilder)
    {
        var entries = new List<JournalEntry>();
        var baseDate = new DateTime(2024, 1, 15);

        // Entry 1: Sales Transaction (Invoice from ABC Corp) - POSTED
        entries.Add(new JournalEntry
        {
            Id = Guid.Parse("80000000-0000-0000-0000-000000000001"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            JournalNumber = "JE-20240115-001",
            DocumentType = "SalesInvoice",
            DocumentDate = baseDate,
            PostingDate = baseDate,
            Description = "Sale to ABC Corp - Invoice #INV001",
            CurrencyCode = "USD",
            ReferenceNumber = "INV001",
            Status = JournalEntryStatus.Posted,
            TotalDebit = 5500m,
            TotalCredit = 5500m,
            PostedAt = baseDate.AddDays(2),
            PostedByUserId = TestUserId,
            CreatedAt = baseDate,
            CreatedByUserId = TestUserId
        });

        // Entry 2: Purchase Transaction (from ABC Supplies) - POSTED
        entries.Add(new JournalEntry
        {
            Id = Guid.Parse("80000000-0000-0000-0000-000000000002"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            JournalNumber = "JE-20240116-001",
            DocumentType = "PurchaseInvoice",
            DocumentDate = baseDate.AddDays(1),
            PostingDate = baseDate.AddDays(1),
            Description = "Purchase from ABC Supplies - PO #PO001",
            CurrencyCode = "USD",
            ReferenceNumber = "PO001",
            Status = JournalEntryStatus.Posted,
            TotalDebit = 2000m,
            TotalCredit = 2000m,
            PostedAt = baseDate.AddDays(3),
            PostedByUserId = TestUserId,
            CreatedAt = baseDate.AddDays(1),
            CreatedByUserId = TestUserId
        });

        // Entry 3: Manual Expense Entry - SUBMITTED (waiting approval)
        entries.Add(new JournalEntry
        {
            Id = Guid.Parse("80000000-0000-0000-0000-000000000003"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            JournalNumber = "JE-20240120-001",
            DocumentType = "ManualEntry",
            DocumentDate = baseDate.AddDays(5),
            PostingDate = baseDate.AddDays(5),
            Description = "Monthly salary accrual",
            CurrencyCode = "USD",
            ReferenceNumber = "SAL-JAN",
            Status = JournalEntryStatus.Submitted,
            TotalDebit = 50000m,
            TotalCredit = 50000m,
            CreatedAt = baseDate.AddDays(5),
            CreatedByUserId = TestUserId
        });

        // Entry 4: Depreciation Entry - DRAFT (not yet submitted)
        entries.Add(new JournalEntry
        {
            Id = Guid.Parse("80000000-0000-0000-0000-000000000004"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            JournalNumber = "JE-20240122-001",
            DocumentType = "Depreciation",
            DocumentDate = baseDate.AddDays(7),
            PostingDate = baseDate.AddDays(7),
            Description = "Monthly depreciation accrual",
            CurrencyCode = "USD",
            ReferenceNumber = "DEP-JAN",
            Status = JournalEntryStatus.Draft,
            TotalDebit = 5000m,
            TotalCredit = 5000m,
            CreatedAt = baseDate.AddDays(7),
            CreatedByUserId = TestUserId
        });

        // Entry 5: Payment Receipt - POSTED
        entries.Add(new JournalEntry
        {
            Id = Guid.Parse("80000000-0000-0000-0000-000000000005"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            LedgerId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            JournalNumber = "JE-20240125-001",
            DocumentType = "PaymentReceipt",
            DocumentDate = baseDate.AddDays(10),
            PostingDate = baseDate.AddDays(10),
            Description = "Payment from ABC Corp - Check #CHK001",
            CurrencyCode = "USD",
            ReferenceNumber = "CHK001",
            Status = JournalEntryStatus.Posted,
            TotalDebit = 3000m,
            TotalCredit = 3000m,
            PostedAt = baseDate.AddDays(11),
            PostedByUserId = TestUserId,
            CreatedAt = baseDate.AddDays(10),
            CreatedByUserId = TestUserId
        });

        modelBuilder.Entity<JournalEntry>().HasData(entries);
    }

    private static void SeedJournalLines(ModelBuilder modelBuilder)
    {
        var lines = new List<JournalLine>();
        var baseDate = new DateTime(2024, 1, 15);

        // Entry 1: Sales Transaction Lines
        lines.Add(new JournalLine
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000001"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            JournalEntryId = Guid.Parse("80000000-0000-0000-0000-000000000001"),
            LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000005"), // AR - ABC Corp
            DebitAmount = 5000m,
            CreditAmount = 0m,
            CurrencyCode = "USD",
            LineNumber = 1,
            Description = "Accounts Receivable - ABC Corp",
            SourceModuleType = ModuleType.Sales,
            SourceEntityType = EntityType.SalesInvoice,
            CustomerId = Guid.NewGuid(),
            DueDate = baseDate.AddDays(30),
            PaymentTermsCode = PaymentTerms.Net30,
            CreatedAt = baseDate,
            CreatedByUserId = TestUserId
        });
        lines.Add(new JournalLine
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000002"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            JournalEntryId = Guid.Parse("80000000-0000-0000-0000-000000000001"),
            LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000013"), // Sales Tax Payable
            DebitAmount = 0m,
            CreditAmount = 500m,
            CurrencyCode = "USD",
            LineNumber = 2,
            Description = "Sales Tax Payable",
            SourceModuleType = ModuleType.Sales,
            SourceEntityType = EntityType.SalesInvoice,
            CreatedAt = baseDate,
            CreatedByUserId = TestUserId
        });
        lines.Add(new JournalLine
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000003"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            JournalEntryId = Guid.Parse("80000000-0000-0000-0000-000000000001"),
            LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000016"), // Product Sales
            DebitAmount = 0m,
            CreditAmount = 5000m,
            CurrencyCode = "USD",
            LineNumber = 3,
            Description = "Product Sales Revenue",
            SourceModuleType = ModuleType.Sales,
            SourceEntityType = EntityType.SalesInvoice,
            CreatedAt = baseDate,
            CreatedByUserId = TestUserId
        });

        // Entry 2: Purchase Transaction Lines
        lines.Add(new JournalLine
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000004"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            JournalEntryId = Guid.Parse("80000000-0000-0000-0000-000000000002"),
            LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000007"), // Inventory
            DebitAmount = 2000m,
            CreditAmount = 0m,
            CurrencyCode = "USD",
            LineNumber = 1,
            Description = "Inventory Purchase",
            SourceModuleType = ModuleType.Purchasing,
            SourceEntityType = EntityType.PurchaseOrder,
            VendorId = Guid.NewGuid(),
            DueDate = baseDate.AddDays(30),
            PaymentTermsCode = PaymentTerms.Net30,
            CreatedAt = baseDate.AddDays(1),
            CreatedByUserId = TestUserId
        });
        lines.Add(new JournalLine
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000005"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            JournalEntryId = Guid.Parse("80000000-0000-0000-0000-000000000002"),
            LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000012"), // AP - ABC Supplies
            DebitAmount = 0m,
            CreditAmount = 2000m,
            CurrencyCode = "USD",
            LineNumber = 2,
            Description = "Accounts Payable - ABC Supplies",
            SourceModuleType = ModuleType.Purchasing,
            SourceEntityType = EntityType.PurchaseOrder,
            VendorId = Guid.NewGuid(),
            DueDate = baseDate.AddDays(30),
            PaymentTermsCode = PaymentTerms.Net30,
            CreatedAt = baseDate.AddDays(1),
            CreatedByUserId = TestUserId
        });

        // Entry 3: Salary Accrual Lines
        lines.Add(new JournalLine
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000006"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            JournalEntryId = Guid.Parse("80000000-0000-0000-0000-000000000003"),
            LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000019"), // Salaries & Wages
            DebitAmount = 50000m,
            CreditAmount = 0m,
            CurrencyCode = "USD",
            LineNumber = 1,
            Description = "Monthly Salary Accrual",
            SourceModuleType = ModuleType.HR,
            SourceEntityType = EntityType.PayrollEntry,
            CreatedAt = baseDate.AddDays(5),
            CreatedByUserId = TestUserId
        });
        lines.Add(new JournalLine
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000007"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            JournalEntryId = Guid.Parse("80000000-0000-0000-0000-000000000003"),
            LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000003"), // Savings Account
            DebitAmount = 0m,
            CreditAmount = 50000m,
            CurrencyCode = "USD",
            LineNumber = 2,
            Description = "Salary Payment Liability",
            SourceModuleType = ModuleType.HR,
            SourceEntityType = EntityType.PayrollEntry,
            CreatedAt = baseDate.AddDays(5),
            CreatedByUserId = TestUserId
        });

        // Entry 4: Depreciation Lines
        lines.Add(new JournalLine
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000008"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            JournalEntryId = Guid.Parse("80000000-0000-0000-0000-000000000004"),
            LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000021"), // Depreciation Expense
            DebitAmount = 5000m,
            CreditAmount = 0m,
            CurrencyCode = "USD",
            LineNumber = 1,
            Description = "Monthly Depreciation Expense",
            SourceModuleType = ModuleType.FixedAssets,
            SourceEntityType = EntityType.DepreciationSchedule,
            CreatedAt = baseDate.AddDays(7),
            CreatedByUserId = TestUserId
        });
        lines.Add(new JournalLine
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000009"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            JournalEntryId = Guid.Parse("80000000-0000-0000-0000-000000000004"),
            LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000010"), // Accumulated Depreciation
            DebitAmount = 0m,
            CreditAmount = 5000m,
            CurrencyCode = "USD",
            LineNumber = 2,
            Description = "Accumulated Depreciation",
            SourceModuleType = ModuleType.FixedAssets,
            SourceEntityType = EntityType.DepreciationSchedule,
            CreatedAt = baseDate.AddDays(7),
            CreatedByUserId = TestUserId
        });

        // Entry 5: Payment Receipt Lines
        lines.Add(new JournalLine
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000010"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            JournalEntryId = Guid.Parse("80000000-0000-0000-0000-000000000005"),
            LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000002"), // Checking Account
            DebitAmount = 3000m,
            CreditAmount = 0m,
            CurrencyCode = "USD",
            LineNumber = 1,
            Description = "Cash Receipt from Customer",
            SourceModuleType = ModuleType.Sales,
            SourceEntityType = EntityType.PaymentReceipt,
            CustomerId = Guid.NewGuid(),
            CreatedAt = baseDate.AddDays(10),
            CreatedByUserId = TestUserId
        });
        lines.Add(new JournalLine
        {
            Id = Guid.Parse("90000000-0000-0000-0000-000000000011"),
            CompanyId = TestCompanyId,
            BranchId = TestBranchId,
            BusinessUnitId = TestBusinessUnitId,
            JournalEntryId = Guid.Parse("80000000-0000-0000-0000-000000000005"),
            LedgerAccountId = Guid.Parse("40000000-0000-0000-0000-000000000005"), // AR - ABC Corp
            DebitAmount = 0m,
            CreditAmount = 3000m,
            CurrencyCode = "USD",
            LineNumber = 2,
            Description = "Accounts Receivable Collection",
            SourceModuleType = ModuleType.Sales,
            SourceEntityType = EntityType.PaymentReceipt,
            CustomerId = Guid.NewGuid(),
            CreatedAt = baseDate.AddDays(10),
            CreatedByUserId = TestUserId
        });

        modelBuilder.Entity<JournalLine>().HasData(lines);
    }

    #endregion
}
