using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Service for initializing accounting data for newly created companies
/// Automatically seeds chart of accounts, categories, posting profiles, etc.
/// Only works in non-production environments
/// </summary>
public class AccountingInitializationService : IAccountingInitializationService
{
    private readonly AccountingDbContext _accountingContext;
    private readonly ILogger<AccountingInitializationService> _logger;

    public AccountingInitializationService(
        AccountingDbContext accountingContext,
        ILogger<AccountingInitializationService> logger)
    {
        _accountingContext = accountingContext;
        _logger = logger;
    }

    /// <summary>
    /// Initialize accounting data for a newly created company
    /// </summary>
    public async Task<Result> InitializeAccountingForNewCompanyAsync(
        Guid companyId,
        Guid branchId,
        Guid businessUnitId,
        Guid userId)
    {
        try
        {
            _logger.LogInformation("Initializing accounting data for Company: {CompanyId}, Branch: {BranchId}, BU: {BusinessUnitId}",
                companyId, branchId, businessUnitId);

            // Check if accounting data already exists
            var dataExists = await AccountingDataExistsAsync(companyId);
            if (dataExists)
            {
                _logger.LogWarning("Accounting data already exists for Company: {CompanyId}", companyId);
                return Result.Ok("Accounting data already initialized for this company");
            }

            // Begin transaction
            await using var transaction = await _accountingContext.Database.BeginTransactionAsync();

            try
            {
                // Create account categories
                var categories = CreateAccountCategories(companyId, userId);
                _accountingContext.Set<AccountCategory>().AddRange(categories);
                await _accountingContext.SaveChangesAsync();
                _logger.LogInformation("Created {Count} account categories for Company: {CompanyId}", categories.Length, companyId);

                // Create fiscal calendar
                var fiscalCalendar = CreateFiscalCalendar(companyId, branchId, businessUnitId, userId);
                _accountingContext.Set<FiscalCalendar>().Add(fiscalCalendar);
                await _accountingContext.SaveChangesAsync();
                _logger.LogInformation("Created fiscal calendar for Company: {CompanyId}", companyId);

                // Create fiscal periods
                var periods = CreateFiscalPeriods(companyId, branchId, businessUnitId, fiscalCalendar.Id, userId);
                _accountingContext.Set<FiscalPeriod>().AddRange(periods);
                await _accountingContext.SaveChangesAsync();
                _logger.LogInformation("Created {Count} fiscal periods for Company: {CompanyId}", periods.Count, companyId);

                // Create ledgers
                var ledgers = CreateLedgers(companyId, branchId, businessUnitId, fiscalCalendar.Id, userId);
                _accountingContext.Set<Ledger>().AddRange(ledgers);
                await _accountingContext.SaveChangesAsync();
                _logger.LogInformation("Created {Count} ledgers for Company: {CompanyId}", ledgers.Length, companyId);

                // Create ledger accounts (Chart of Accounts)
                var accounts = CreateLedgerAccounts(companyId, branchId, businessUnitId, categories, ledgers, userId);
                _accountingContext.Set<LedgerAccount>().AddRange(accounts);
                await _accountingContext.SaveChangesAsync();
                _logger.LogInformation("Created {Count} ledger accounts for Company: {CompanyId}", accounts.Count, companyId);

                // Create tax codes
                var taxCodes = CreateTaxCodes(companyId, branchId, businessUnitId, accounts, userId);
                _accountingContext.Set<TaxCode>().AddRange(taxCodes);
                await _accountingContext.SaveChangesAsync();
                _logger.LogInformation("Created {Count} tax codes for Company: {CompanyId}", taxCodes.Length, companyId);

                // Create dimensions
                var dimensions = CreateDimensions(companyId, branchId, businessUnitId, userId);
                _accountingContext.Set<Dimension>().AddRange(dimensions);
                await _accountingContext.SaveChangesAsync();
                _logger.LogInformation("Created {Count} dimensions for Company: {CompanyId}", dimensions.Length, companyId);

                // Create dimension values
                var dimensionValues = CreateDimensionValues(companyId, branchId, businessUnitId, dimensions, userId);
                _accountingContext.Set<DimensionValue>().AddRange(dimensionValues);
                await _accountingContext.SaveChangesAsync();
                _logger.LogInformation("Created {Count} dimension values for Company: {CompanyId}", dimensionValues.Length, companyId);

                // Create posting profiles
                var postingProfiles = CreatePostingProfiles(companyId, branchId, businessUnitId, accounts, userId);
                _accountingContext.Set<PostingProfile>().AddRange(postingProfiles);
                await _accountingContext.SaveChangesAsync();
                _logger.LogInformation("Created {Count} posting profiles for Company: {CompanyId}", postingProfiles.Length, companyId);

                // Commit transaction
                await transaction.CommitAsync();
                _logger.LogInformation("Successfully initialized accounting data for Company: {CompanyId}", companyId);

                return Result.Ok("Accounting data initialized successfully for new company");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during accounting data initialization for Company: {CompanyId}", companyId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize accounting data for Company: {CompanyId}", companyId);
            return Result.Fail($"Failed to initialize accounting data: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if accounting data already exists for a company
    /// </summary>
    public async Task<bool> AccountingDataExistsAsync(Guid companyId)
    {
        return await _accountingContext.Set<Ledger>()
            .AnyAsync(l => l.CompanyId == companyId);
    }

    #region Seeding Methods

    private AccountCategory[] CreateAccountCategories(Guid companyId, Guid userId)
    {
        var now = DateTime.UtcNow;

        return new[]
        {
            new AccountCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Current Assets",
                Type = AccountType.Asset,
                NormalBalance = NormalBalance.Debit,
                Description = "Short-term assets expected to be converted to cash within one year",
                IsActive = true,
                CreatedAt = now,
                CreatedByUserId = userId
            },
            new AccountCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Fixed Assets",
                Type = AccountType.Asset,
                NormalBalance = NormalBalance.Debit,
                Description = "Long-term assets with useful life greater than one year",
                IsActive = true,
                CreatedAt = now,
                CreatedByUserId = userId
            },
            new AccountCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Current Liabilities",
                Type = AccountType.Liability,
                NormalBalance = NormalBalance.Credit,
                Description = "Obligations due within one year",
                IsActive = true,
                CreatedAt = now,
                CreatedByUserId = userId
            },
            new AccountCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Long-term Liabilities",
                Type = AccountType.Liability,
                NormalBalance = NormalBalance.Credit,
                Description = "Obligations due after one year",
                IsActive = true,
                CreatedAt = now,
                CreatedByUserId = userId
            },
            new AccountCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Shareholders' Equity",
                Type = AccountType.Equity,
                NormalBalance = NormalBalance.Credit,
                Description = "Owner's investment and retained earnings",
                IsActive = true,
                CreatedAt = now,
                CreatedByUserId = userId
            },
            new AccountCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Sales Revenue",
                Type = AccountType.Revenue,
                NormalBalance = NormalBalance.Credit,
                Description = "Income from product/service sales",
                IsActive = true,
                CreatedAt = now,
                CreatedByUserId = userId
            },
            new AccountCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Cost of Goods Sold",
                Type = AccountType.Expense,
                NormalBalance = NormalBalance.Debit,
                Description = "Direct costs of producing goods sold",
                IsActive = true,
                CreatedAt = now,
                CreatedByUserId = userId
            },
            new AccountCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Operating Expenses",
                Type = AccountType.Expense,
                NormalBalance = NormalBalance.Debit,
                Description = "Expenses incurred in normal business operations",
                IsActive = true,
                CreatedAt = now,
                CreatedByUserId = userId
            }
        };
    }

    private FiscalCalendar CreateFiscalCalendar(Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
    {
        var now = DateTime.UtcNow;
        var currentYear = now.Year;

        return new FiscalCalendar
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            BusinessUnitId = businessUnitId,
            Name = $"Calendar Year {currentYear}",
            StartDate = new DateTime(currentYear, 1, 1),
            EndDate = new DateTime(currentYear, 12, 31),
            IsActive = true,
            Description = $"Calendar year fiscal period {currentYear}",
            CreatedAt = now,
            CreatedByUserId = userId
        };
    }

    private List<FiscalPeriod> CreateFiscalPeriods(Guid companyId, Guid branchId, Guid businessUnitId, Guid fiscalCalendarId, Guid userId)
    {
        var periods = new List<FiscalPeriod>();
        var now = DateTime.UtcNow;
        var currentYear = now.Year;

        for (int month = 1; month <= 12; month++)
        {
            var startDate = new DateTime(currentYear, month, 1);
            var endDate = month == 12 ? new DateTime(currentYear, 12, 31) : new DateTime(currentYear, month + 1, 1).AddDays(-1);

            periods.Add(new FiscalPeriod
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                FiscalCalendarId = fiscalCalendarId,
                PeriodName = $"{startDate:MMMM} {currentYear}",
                StartDate = startDate,
                EndDate = endDate,
                IsClosed = month < now.Month,
                Description = $"Fiscal period for {startDate:MMMM yyyy}",
                CreatedAt = now,
                CreatedByUserId = userId
            });
        }

        return periods;
    }

    private Ledger[] CreateLedgers(Guid companyId, Guid branchId, Guid businessUnitId, Guid fiscalCalendarId, Guid userId)
    {
        var now = DateTime.UtcNow;

        return new[]
        {
            new Ledger
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                Name = "Main Ledger",
                BaseCurrencyCode = "USD",
                FiscalCalendarId = fiscalCalendarId,
                IsDefault = true,
                IsActive = true,
                Description = "Primary general ledger for the company",
                CreatedAt = now,
                CreatedByUserId = userId
            }
        };
    }

    private List<LedgerAccount> CreateLedgerAccounts(
        Guid companyId, Guid branchId, Guid businessUnitId,
        AccountCategory[] categories, Ledger[] ledgers, Guid userId)
    {
        var now = DateTime.UtcNow;
        var ledgerId = ledgers[0].Id;
        var accounts = new List<LedgerAccount>();

        // Get categories by type
        var assetCategory = categories.First(c => c.Type == AccountType.Asset);
        var liabilityCategory = categories.First(c => c.Type == AccountType.Liability);
        var equityCategory = categories.First(c => c.Type == AccountType.Equity);
        var revenueCategory = categories.First(c => c.Type == AccountType.Revenue);
        var expenseCategory = categories.First(c => c.Type == AccountType.Expense);

        #region LEVEL 1 - ACCOUNT TYPES (Non-Posting)

        // 1000 - Assets
        var level1Assets = CreateAccount("1000", "Assets", assetCategory.Id, null, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Balance Sheet Asset section");
        accounts.Add(level1Assets);

        // 2000 - Liabilities
        var level1Liabilities = CreateAccount("2000", "Liabilities", liabilityCategory.Id, null, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Balance Sheet Liability section");
        accounts.Add(level1Liabilities);

        // 3000 - Equity
        var level1Equity = CreateAccount("3000", "Equity", equityCategory.Id, null, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Balance Sheet Equity section");
        accounts.Add(level1Equity);

        // 4000 - Revenue
        var level1Revenue = CreateAccount("4000", "Revenue", revenueCategory.Id, null, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Income Statement Revenue section");
        accounts.Add(level1Revenue);

        // 5000 - Expenses
        var level1Expenses = CreateAccount("5000", "Expenses", expenseCategory.Id, null, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Income Statement Expense section");
        accounts.Add(level1Expenses);

        #endregion

        #region LEVEL 2 - ASSET CATEGORIES (Non-Posting)

        // 1100 - Current Assets
        var level2CurrentAssets = CreateAccount("1100", "Current Assets", assetCategory.Id, level1Assets.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Short-term assets");
        accounts.Add(level2CurrentAssets);

        // 1200 - Fixed Assets
        var level2FixedAssets = CreateAccount("1200", "Fixed Assets", assetCategory.Id, level1Assets.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Long-term assets");
        accounts.Add(level2FixedAssets);

        // 1300 - Intangible Assets
        var level2IntangibleAssets = CreateAccount("1300", "Intangible Assets", assetCategory.Id, level1Assets.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Intangible assets and goodwill");
        accounts.Add(level2IntangibleAssets);

        #endregion

        #region LEVEL 3 - CURRENT ASSETS GROUPS (Non-Posting)

        // 1110 - Cash & Bank Accounts
        var level3CashBank = CreateAccount("1110", "Cash & Bank", assetCategory.Id, level2CurrentAssets.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Liquid cash positions");
        accounts.Add(level3CashBank);

        // 1120 - Receivables
        var level3Receivables = CreateAccount("1120", "Receivables", assetCategory.Id, level2CurrentAssets.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Customer receivables");
        accounts.Add(level3Receivables);

        // 1130 - Prepaid Expenses
        var level3Prepaid = CreateAccount("1130", "Prepaid Expenses", assetCategory.Id, level2CurrentAssets.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Prepaid items");
        accounts.Add(level3Prepaid);

        // 1140 - Inventory
        var level3Inventory = CreateAccount("1140", "Inventory", assetCategory.Id, level2CurrentAssets.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Stock of goods");
        accounts.Add(level3Inventory);

        #endregion

        #region LEVEL 4 - CASH & BANK ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("1111", "Cash on Hand", assetCategory.Id, level3CashBank.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Physical cash in register"));
        accounts.Add(CreateAccount("1112", "Bank Account - Primary", assetCategory.Id, level3CashBank.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Main operating bank account"));
        accounts.Add(CreateAccount("1113", "Bank Account - Savings", assetCategory.Id, level3CashBank.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Savings/reserve bank account"));
        accounts.Add(CreateAccount("1114", "Bank Account - USD", assetCategory.Id, level3CashBank.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "USD currency bank account"));

        #endregion

        #region LEVEL 4 - RECEIVABLES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("1121", "Accounts Receivable - Trade", assetCategory.Id, level3Receivables.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Trade customer invoices"));
        accounts.Add(CreateAccount("1122", "Allowance for Doubtful Debts", assetCategory.Id, level3Receivables.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Contra-asset: Provision for bad debts"));
        accounts.Add(CreateAccount("1123", "Other Receivables", assetCategory.Id, level3Receivables.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Non-trade receivables"));
        accounts.Add(CreateAccount("1124", "Employee Advances", assetCategory.Id, level3Receivables.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Advances given to employees"));

        #endregion

        #region LEVEL 4 - PREPAID EXPENSES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("1131", "Prepaid Insurance", assetCategory.Id, level3Prepaid.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Insurance paid in advance"));
        accounts.Add(CreateAccount("1132", "Prepaid Rent", assetCategory.Id, level3Prepaid.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Rent paid in advance"));
        accounts.Add(CreateAccount("1133", "Prepaid Subscriptions", assetCategory.Id, level3Prepaid.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Software/service subscriptions"));
        accounts.Add(CreateAccount("1134", "Other Prepaid", assetCategory.Id, level3Prepaid.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Other prepaid expenses"));

        #endregion

        #region LEVEL 4 - INVENTORY ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("1141", "Raw Materials", assetCategory.Id, level3Inventory.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Raw materials inventory"));
        accounts.Add(CreateAccount("1142", "Work in Progress", assetCategory.Id, level3Inventory.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Partially completed goods"));
        accounts.Add(CreateAccount("1143", "Finished Goods", assetCategory.Id, level3Inventory.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Completed products ready for sale"));
        accounts.Add(CreateAccount("1144", "Inventory - Warehouse A", assetCategory.Id, level3Inventory.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Warehouse A stock"));

        #endregion

        #region LEVEL 3 - FIXED ASSETS GROUPS (Non-Posting)

        // 1210 - Property & Equipment
        var level3PPE = CreateAccount("1210", "Property, Plant & Equipment", assetCategory.Id, level2FixedAssets.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Tangible fixed assets");
        accounts.Add(level3PPE);

        // 1220 - Accumulated Depreciation
        var level3AccumDepr = CreateAccount("1220", "Accumulated Depreciation", assetCategory.Id, level2FixedAssets.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Contra-asset: Depreciation");
        accounts.Add(level3AccumDepr);

        // 1230 - Leasehold Improvements
        var level3Leasehold = CreateAccount("1230", "Leasehold Improvements", assetCategory.Id, level2FixedAssets.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Improvements to leased property");
        accounts.Add(level3Leasehold);

        #endregion

        #region LEVEL 4 - PPE ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("1211", "Land", assetCategory.Id, level3PPE.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Land holdings"));
        accounts.Add(CreateAccount("1212", "Buildings", assetCategory.Id, level3PPE.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Building structures"));
        accounts.Add(CreateAccount("1213", "Machinery & Equipment", assetCategory.Id, level3PPE.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Production machinery"));
        accounts.Add(CreateAccount("1214", "Vehicles", assetCategory.Id, level3PPE.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Company vehicles"));
        accounts.Add(CreateAccount("1215", "Furniture & Fixtures", assetCategory.Id, level3PPE.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Office furniture"));
        accounts.Add(CreateAccount("1216", "Computer Equipment", assetCategory.Id, level3PPE.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Computers and servers"));

        #endregion

        #region LEVEL 4 - ACCUMULATED DEPRECIATION ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("1221", "Accumulated Depreciation - Buildings", assetCategory.Id, level3AccumDepr.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Depreciation reserve for buildings"));
        accounts.Add(CreateAccount("1222", "Accumulated Depreciation - Equipment", assetCategory.Id, level3AccumDepr.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Depreciation reserve for equipment"));
        accounts.Add(CreateAccount("1223", "Accumulated Depreciation - Vehicles", assetCategory.Id, level3AccumDepr.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Depreciation reserve for vehicles"));
        accounts.Add(CreateAccount("1224", "Accumulated Depreciation - Furniture", assetCategory.Id, level3AccumDepr.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Depreciation reserve for furniture"));

        #endregion

        #region LEVEL 4 - LEASEHOLD IMPROVEMENTS ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("1231", "Leasehold Improvements - Renovations", assetCategory.Id, level3Leasehold.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Renovations and improvements"));
        accounts.Add(CreateAccount("1232", "Accumulated Amortization - Leasehold", assetCategory.Id, level3Leasehold.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Amortization of leasehold"));

        #endregion

        #region LEVEL 3 - INTANGIBLE ASSETS GROUPS (Non-Posting)

        // 1310 - Goodwill & Intangibles
        var level3Intangible = CreateAccount("1310", "Goodwill & Intangibles", assetCategory.Id, level2IntangibleAssets.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Intangible asset items");
        accounts.Add(level3Intangible);

        #endregion

        #region LEVEL 4 - INTANGIBLE ASSETS ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("1311", "Goodwill", assetCategory.Id, level3Intangible.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Goodwill from acquisition"));
        accounts.Add(CreateAccount("1312", "Trademarks & Patents", assetCategory.Id, level3Intangible.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Intellectual property"));
        accounts.Add(CreateAccount("1313", "Software Licenses", assetCategory.Id, level3Intangible.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Software and licenses"));

        #endregion

        #region LEVEL 2 - LIABILITY CATEGORIES (Non-Posting)

        // 2100 - Current Liabilities
        var level2CurrentLiab = CreateAccount("2100", "Current Liabilities", liabilityCategory.Id, level1Liabilities.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Short-term obligations");
        accounts.Add(level2CurrentLiab);

        // 2200 - Long-term Liabilities
        var level2LongTermLiab = CreateAccount("2200", "Long-term Liabilities", liabilityCategory.Id, level1Liabilities.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Long-term obligations");
        accounts.Add(level2LongTermLiab);

        #endregion

        #region LEVEL 3 - CURRENT LIABILITIES GROUPS (Non-Posting)

        // 2110 - Payables
        var level3Payables = CreateAccount("2110", "Payables", liabilityCategory.Id, level2CurrentLiab.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Vendor payables");
        accounts.Add(level3Payables);

        // 2120 - Accrued Expenses
        var level3Accrued = CreateAccount("2120", "Accrued Expenses", liabilityCategory.Id, level2CurrentLiab.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Accrued liabilities");
        accounts.Add(level3Accrued);

        // 2130 - Short-term Debt
        var level3ShortDebt = CreateAccount("2130", "Short-term Debt", liabilityCategory.Id, level2CurrentLiab.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Current portion of debt");
        accounts.Add(level3ShortDebt);

        // 2140 - Tax Payables
        var level3TaxPayable = CreateAccount("2140", "Tax Payables", liabilityCategory.Id, level2CurrentLiab.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Tax obligations");
        accounts.Add(level3TaxPayable);

        #endregion

        #region LEVEL 4 - PAYABLES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("2111", "Accounts Payable - Trade", liabilityCategory.Id, level3Payables.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Trade vendor invoices"));
        accounts.Add(CreateAccount("2112", "Supplier Advances Received", liabilityCategory.Id, level3Payables.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Advance payments from suppliers"));

        #endregion

        #region LEVEL 4 - ACCRUED EXPENSES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("2121", "Accrued Salaries & Wages", liabilityCategory.Id, level3Accrued.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Accrued employee compensation"));
        accounts.Add(CreateAccount("2122", "Accrued Interest", liabilityCategory.Id, level3Accrued.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Interest accrued on debt"));
        accounts.Add(CreateAccount("2123", "Accrued Utilities", liabilityCategory.Id, level3Accrued.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Accrued utility expenses"));
        accounts.Add(CreateAccount("2124", "Accrued Rent", liabilityCategory.Id, level3Accrued.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Accrued rent expense"));

        #endregion

        #region LEVEL 4 - SHORT-TERM DEBT ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("2131", "Current Portion - Bank Loan", liabilityCategory.Id, level3ShortDebt.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Current year bank loan payment"));
        accounts.Add(CreateAccount("2132", "Line of Credit", liabilityCategory.Id, level3ShortDebt.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Short-term credit facility"));

        #endregion

        #region LEVEL 4 - TAX PAYABLES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("2141", "Sales Tax Payable", liabilityCategory.Id, level3TaxPayable.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Sales/VAT tax collected"));
        accounts.Add(CreateAccount("2142", "Income Tax Payable", liabilityCategory.Id, level3TaxPayable.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Income tax liability"));
        accounts.Add(CreateAccount("2143", "Payroll Tax Payable", liabilityCategory.Id, level3TaxPayable.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Withholding taxes"));

        #endregion

        #region LEVEL 3 - LONG-TERM LIABILITIES GROUPS (Non-Posting)

        // 2210 - Long-term Debt
        var level3LongDebt = CreateAccount("2210", "Long-term Debt", liabilityCategory.Id, level2LongTermLiab.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Debt due after 1 year");
        accounts.Add(level3LongDebt);

        // 2220 - Deferred Liabilities
        var level3Deferred = CreateAccount("2220", "Deferred Liabilities", liabilityCategory.Id, level2LongTermLiab.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Deferred revenue/income");
        accounts.Add(level3Deferred);

        #endregion

        #region LEVEL 4 - LONG-TERM DEBT ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("2211", "Bank Loan - Long-term", liabilityCategory.Id, level3LongDebt.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Long-term bank financing"));
        accounts.Add(CreateAccount("2212", "Bonds Payable", liabilityCategory.Id, level3LongDebt.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Issued bonds"));
        accounts.Add(CreateAccount("2213", "Finance Lease Liability", liabilityCategory.Id, level3LongDebt.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Lease obligations"));

        #endregion

        #region LEVEL 4 - DEFERRED LIABILITIES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("2221", "Deferred Revenue", liabilityCategory.Id, level3Deferred.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Advance customer payments"));
        accounts.Add(CreateAccount("2222", "Deferred Tax Liability", liabilityCategory.Id, level3Deferred.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Tax liability deferred"));

        #endregion

        #region LEVEL 2 - EQUITY CATEGORIES (Non-Posting)

        // 3100 - Share Capital
        var level2ShareCapital = CreateAccount("3100", "Share Capital", equityCategory.Id, level1Equity.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Shareholder investment");
        accounts.Add(level2ShareCapital);

        // 3200 - Retained Earnings
        var level2RetainedEarnings = CreateAccount("3200", "Retained Earnings", equityCategory.Id, level1Equity.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Accumulated profit");
        accounts.Add(level2RetainedEarnings);

        // 3300 - Other Equity
        var level2OtherEquity = CreateAccount("3300", "Other Equity", equityCategory.Id, level1Equity.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Other equity items");
        accounts.Add(level2OtherEquity);

        #endregion

        #region LEVEL 4 - SHARE CAPITAL ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("3101", "Common Stock", equityCategory.Id, level2ShareCapital.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Common shares issued"));
        accounts.Add(CreateAccount("3102", "Preferred Stock", equityCategory.Id, level2ShareCapital.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Preferred shares issued"));
        accounts.Add(CreateAccount("3103", "Share Premium", equityCategory.Id, level2ShareCapital.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Premium on share issuance"));
        accounts.Add(CreateAccount("3104", "Treasury Stock", equityCategory.Id, level2ShareCapital.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Repurchased shares"));

        #endregion

        #region LEVEL 4 - RETAINED EARNINGS ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("3201", "Beginning Retained Earnings", equityCategory.Id, level2RetainedEarnings.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Prior period earnings"));
        accounts.Add(CreateAccount("3202", "Current Period Earnings", equityCategory.Id, level2RetainedEarnings.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Current net income"));
        accounts.Add(CreateAccount("3203", "Dividend Paid", equityCategory.Id, level2RetainedEarnings.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Dividends distributed"));

        #endregion

        #region LEVEL 4 - OTHER EQUITY ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("3301", "Revaluation Reserve", equityCategory.Id, level2OtherEquity.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Asset revaluation gain/loss"));
        accounts.Add(CreateAccount("3302", "Other Comprehensive Income", equityCategory.Id, level2OtherEquity.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "OCI items"));

        #endregion

        #region LEVEL 2 - REVENUE CATEGORIES (Non-Posting)

        // 4100 - Product Revenue
        var level2ProductRevenue = CreateAccount("4100", "Product Revenue", revenueCategory.Id, level1Revenue.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Revenue from goods sold");
        accounts.Add(level2ProductRevenue);

        // 4200 - Service Revenue
        var level2ServiceRevenue = CreateAccount("4200", "Service Revenue", revenueCategory.Id, level1Revenue.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Revenue from services");
        accounts.Add(level2ServiceRevenue);

        // 4300 - Other Revenue
        var level2OtherRevenue = CreateAccount("4300", "Other Revenue", revenueCategory.Id, level1Revenue.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Miscellaneous revenue");
        accounts.Add(level2OtherRevenue);

        #endregion

        #region LEVEL 3 - PRODUCT REVENUE GROUPS (Non-Posting)

        // 4110 - Domestic Sales
        var level3DomesticSales = CreateAccount("4110", "Domestic Sales", revenueCategory.Id, level2ProductRevenue.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Local market sales");
        accounts.Add(level3DomesticSales);

        // 4120 - Export Sales
        var level3ExportSales = CreateAccount("4120", "Export Sales", revenueCategory.Id, level2ProductRevenue.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "International market sales");
        accounts.Add(level3ExportSales);

        #endregion

        #region LEVEL 4 - DOMESTIC SALES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("4111", "Product Sales - Retail", revenueCategory.Id, level3DomesticSales.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Retail product sales"));
        accounts.Add(CreateAccount("4112", "Product Sales - Wholesale", revenueCategory.Id, level3DomesticSales.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Wholesale product sales"));
        accounts.Add(CreateAccount("4113", "Product Sales - Online", revenueCategory.Id, level3DomesticSales.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "E-commerce sales"));

        #endregion

        #region LEVEL 4 - EXPORT SALES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("4121", "Export Sales - Europe", revenueCategory.Id, level3ExportSales.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "European market sales"));
        accounts.Add(CreateAccount("4122", "Export Sales - Asia", revenueCategory.Id, level3ExportSales.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Asian market sales"));
        accounts.Add(CreateAccount("4123", "Export Sales - Americas", revenueCategory.Id, level3ExportSales.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Americas market sales"));

        #endregion

        #region LEVEL 3 - SERVICE REVENUE GROUPS (Non-Posting)

        // 4210 - Professional Services
        var level3Professional = CreateAccount("4210", "Professional Services", revenueCategory.Id, level2ServiceRevenue.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Consulting and professional fees");
        accounts.Add(level3Professional);

        // 4220 - Support Services
        var level3Support = CreateAccount("4220", "Support Services", revenueCategory.Id, level2ServiceRevenue.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Technical and customer support");
        accounts.Add(level3Support);

        #endregion

        #region LEVEL 4 - SERVICE REVENUE ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("4211", "Consulting Revenue", revenueCategory.Id, level3Professional.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Consulting services"));
        accounts.Add(CreateAccount("4212", "Maintenance Revenue", revenueCategory.Id, level3Professional.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Equipment maintenance"));
        accounts.Add(CreateAccount("4213", "Technical Support Revenue", revenueCategory.Id, level3Support.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Technical assistance"));
        accounts.Add(CreateAccount("4214", "Training Revenue", revenueCategory.Id, level3Support.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Training services"));

        #endregion

        #region LEVEL 4 - OTHER REVENUE ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("4301", "Interest Income", revenueCategory.Id, level2OtherRevenue.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Interest earned"));
        accounts.Add(CreateAccount("4302", "Dividend Income", revenueCategory.Id, level2OtherRevenue.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Dividend received"));
        accounts.Add(CreateAccount("4303", "Rental Income", revenueCategory.Id, level2OtherRevenue.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Property rental income"));
        accounts.Add(CreateAccount("4304", "Gain on Sale of Assets", revenueCategory.Id, level2OtherRevenue.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Profit on asset disposal"));
        accounts.Add(CreateAccount("4305", "Delivery / Freight Income", revenueCategory.Id, level2OtherRevenue.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Shipping / freight charged to customers"));

        #endregion

        #region LEVEL 2 - EXPENSE CATEGORIES (Non-Posting)

        // 5100 - Cost of Goods Sold
        var level2COGS = CreateAccount("5100", "Cost of Goods Sold", expenseCategory.Id, level1Expenses.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Direct product costs");
        accounts.Add(level2COGS);

        // 5200 - Employee Expenses
        var level2EmployeeExp = CreateAccount("5200", "Employee Expenses", expenseCategory.Id, level1Expenses.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Payroll and benefits");
        accounts.Add(level2EmployeeExp);

        // 5300 - Occupancy Expenses
        var level2Occupancy = CreateAccount("5300", "Occupancy Expenses", expenseCategory.Id, level1Expenses.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Facility costs");
        accounts.Add(level2Occupancy);

        // 5400 - Depreciation & Amortization
        var level2Depreciation = CreateAccount("5400", "Depreciation & Amortization", expenseCategory.Id, level1Expenses.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Asset use allocation");
        accounts.Add(level2Depreciation);

        // 5500 - Administrative Expenses
        var level2Admin = CreateAccount("5500", "Administrative Expenses", expenseCategory.Id, level1Expenses.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Office operations");
        accounts.Add(level2Admin);

        // 5600 - Marketing & Sales
        var level2Marketing = CreateAccount("5600", "Marketing & Sales", expenseCategory.Id, level1Expenses.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Sales promotion");
        accounts.Add(level2Marketing);

        // 5700 - Finance Costs
        var level2Finance = CreateAccount("5700", "Finance Costs", expenseCategory.Id, level1Expenses.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Interest and bank charges");
        accounts.Add(level2Finance);

        // 5800 - Other Expenses
        var level2OtherExp = CreateAccount("5800", "Other Expenses", expenseCategory.Id, level1Expenses.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Miscellaneous expenses");
        accounts.Add(level2OtherExp);

        #endregion

        #region LEVEL 4 - COGS ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5101", "Raw Materials Used", expenseCategory.Id, level2COGS.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Raw material consumption"));
        accounts.Add(CreateAccount("5102", "Direct Labor", expenseCategory.Id, level2COGS.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Production labor"));
        accounts.Add(CreateAccount("5103", "Manufacturing Overhead", expenseCategory.Id, level2COGS.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Factory overhead"));
        accounts.Add(CreateAccount("5104", "Cost of Inventory Sold", expenseCategory.Id, level2COGS.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Product COGS"));

        #endregion

        #region LEVEL 3 - EMPLOYEE EXPENSES GROUPS (Non-Posting)

        // 5210 - Salaries & Wages
        var level3Salaries = CreateAccount("5210", "Salaries & Wages", expenseCategory.Id, level2EmployeeExp.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Employee compensation");
        accounts.Add(level3Salaries);

        // 5220 - Benefits & Payroll Taxes
        var level3Benefits = CreateAccount("5220", "Benefits & Payroll Taxes", expenseCategory.Id, level2EmployeeExp.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Employee benefits");
        accounts.Add(level3Benefits);

        // 5230 - Training & Development
        var level3Training = CreateAccount("5230", "Training & Development", expenseCategory.Id, level2EmployeeExp.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Employee training");
        accounts.Add(level3Training);

        #endregion

        #region LEVEL 4 - SALARIES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5211", "Executive Salaries", expenseCategory.Id, level3Salaries.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Executive compensation"));
        accounts.Add(CreateAccount("5212", "Staff Salaries", expenseCategory.Id, level3Salaries.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Employee salaries"));
        accounts.Add(CreateAccount("5213", "Wages - Hourly", expenseCategory.Id, level3Salaries.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Hourly wages"));
        accounts.Add(CreateAccount("5214", "Bonuses & Commissions", expenseCategory.Id, level3Salaries.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Performance bonuses"));

        #endregion

        #region LEVEL 4 - BENEFITS ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5221", "Health Insurance", expenseCategory.Id, level3Benefits.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Health insurance costs"));
        accounts.Add(CreateAccount("5222", "Retirement Contribution", expenseCategory.Id, level3Benefits.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Pension contributions"));
        accounts.Add(CreateAccount("5223", "Payroll Taxes", expenseCategory.Id, level3Benefits.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Employer taxes"));
        accounts.Add(CreateAccount("5224", "Workers Compensation", expenseCategory.Id, level3Benefits.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Workers comp insurance"));

        #endregion

        #region LEVEL 4 - TRAINING ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5231", "Staff Training", expenseCategory.Id, level3Training.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Employee training programs"));
        accounts.Add(CreateAccount("5232", "Conference & Seminars", expenseCategory.Id, level3Training.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Conference attendance"));

        #endregion

        #region LEVEL 3 - OCCUPANCY EXPENSES GROUPS (Non-Posting)

        // 5310 - Rent & Lease
        var level3Rent = CreateAccount("5310", "Rent & Lease", expenseCategory.Id, level2Occupancy.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Facility rental");
        accounts.Add(level3Rent);

        // 5320 - Utilities
        var level3Utilities = CreateAccount("5320", "Utilities", expenseCategory.Id, level2Occupancy.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Facility utilities");
        accounts.Add(level3Utilities);

        // 5330 - Maintenance & Repairs
        var level3Maintenance = CreateAccount("5330", "Maintenance & Repairs", expenseCategory.Id, level2Occupancy.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Building maintenance");
        accounts.Add(level3Maintenance);

        #endregion

        #region LEVEL 4 - RENT ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5311", "Office Rent", expenseCategory.Id, level3Rent.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Office space rental"));
        accounts.Add(CreateAccount("5312", "Warehouse Rent", expenseCategory.Id, level3Rent.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Warehouse rental"));
        accounts.Add(CreateAccount("5313", "Parking & Storage Rent", expenseCategory.Id, level3Rent.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Parking/storage rental"));

        #endregion

        #region LEVEL 4 - UTILITIES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5321", "Electricity", expenseCategory.Id, level3Utilities.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Electricity costs"));
        accounts.Add(CreateAccount("5322", "Water & Sewage", expenseCategory.Id, level3Utilities.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Water and sewage"));
        accounts.Add(CreateAccount("5323", "Gas & Heating", expenseCategory.Id, level3Utilities.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Gas and heating"));
        accounts.Add(CreateAccount("5324", "Internet & Telecom", expenseCategory.Id, level3Utilities.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Communications"));

        #endregion

        #region LEVEL 4 - MAINTENANCE ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5331", "Building Maintenance", expenseCategory.Id, level3Maintenance.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Building upkeep"));
        accounts.Add(CreateAccount("5332", "Equipment Maintenance", expenseCategory.Id, level3Maintenance.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Equipment service"));
        accounts.Add(CreateAccount("5333", "Cleaning & Janitorial", expenseCategory.Id, level3Maintenance.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Cleaning services"));

        #endregion

        #region LEVEL 4 - DEPRECIATION ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5401", "Depreciation - Buildings", expenseCategory.Id, level2Depreciation.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Building depreciation"));
        accounts.Add(CreateAccount("5402", "Depreciation - Equipment", expenseCategory.Id, level2Depreciation.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Equipment depreciation"));
        accounts.Add(CreateAccount("5403", "Depreciation - Vehicles", expenseCategory.Id, level2Depreciation.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Vehicle depreciation"));
        accounts.Add(CreateAccount("5404", "Amortization - Intangibles", expenseCategory.Id, level2Depreciation.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Intangible asset amortization"));

        #endregion

        #region LEVEL 3 - ADMINISTRATIVE EXPENSES GROUPS (Non-Posting)

        // 5510 - Office Operations
        var level3Office = CreateAccount("5510", "Office Operations", expenseCategory.Id, level2Admin.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Office supplies and services");
        accounts.Add(level3Office);

        // 5520 - Professional Services
        var level3ProfServices = CreateAccount("5520", "Professional Services", expenseCategory.Id, level2Admin.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Outsourced professional services");
        accounts.Add(level3ProfServices);

        // 5530 - Insurance
        var level3Insurance = CreateAccount("5530", "Insurance", expenseCategory.Id, level2Admin.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Insurance premiums");
        accounts.Add(level3Insurance);

        #endregion

        #region LEVEL 4 - OFFICE OPERATIONS ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5511", "Office Supplies", expenseCategory.Id, level3Office.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Stationery and supplies"));
        accounts.Add(CreateAccount("5512", "Printing & Copying", expenseCategory.Id, level3Office.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Printing services"));
        accounts.Add(CreateAccount("5513", "Postage & Shipping", expenseCategory.Id, level3Office.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Mailing and shipping"));
        accounts.Add(CreateAccount("5514", "Office Software & IT", expenseCategory.Id, level3Office.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Software and IT expenses"));

        #endregion

        #region LEVEL 4 - PROFESSIONAL SERVICES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5521", "Audit & Accounting Fees", expenseCategory.Id, level3ProfServices.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "External audit"));
        accounts.Add(CreateAccount("5522", "Legal Fees", expenseCategory.Id, level3ProfServices.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Legal consultation"));
        accounts.Add(CreateAccount("5523", "Consulting Fees", expenseCategory.Id, level3ProfServices.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Consulting services"));
        accounts.Add(CreateAccount("5524", "IT Support Services", expenseCategory.Id, level3ProfServices.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "IT support contracts"));

        #endregion

        #region LEVEL 4 - INSURANCE ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5531", "General Liability Insurance", expenseCategory.Id, level3Insurance.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "General liability premiums"));
        accounts.Add(CreateAccount("5532", "Property Insurance", expenseCategory.Id, level3Insurance.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Building/property insurance"));
        accounts.Add(CreateAccount("5533", "Vehicle Insurance", expenseCategory.Id, level3Insurance.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Fleet vehicle insurance"));
        accounts.Add(CreateAccount("5534", "Professional Liability Insurance", expenseCategory.Id, level3Insurance.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Errors & omissions insurance"));

        #endregion

        #region LEVEL 3 - MARKETING EXPENSES GROUPS (Non-Posting)

        // 5610 - Advertising & Promotion
        var level3Advertising = CreateAccount("5610", "Advertising & Promotion", expenseCategory.Id, level2Marketing.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Ad spending");
        accounts.Add(level3Advertising);

        // 5620 - Sales Travel
        var level3Travel = CreateAccount("5620", "Sales Travel", expenseCategory.Id, level2Marketing.Id, false, false, ledgerId, companyId, branchId, businessUnitId, userId, now, "Travel for sales");
        accounts.Add(level3Travel);

        #endregion

        #region LEVEL 4 - ADVERTISING ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5611", "Digital Marketing", expenseCategory.Id, level3Advertising.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Online advertising"));
        accounts.Add(CreateAccount("5612", "Social Media Marketing", expenseCategory.Id, level3Advertising.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Social media campaigns"));
        accounts.Add(CreateAccount("5613", "Print Advertising", expenseCategory.Id, level3Advertising.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Print media"));
        accounts.Add(CreateAccount("5614", "Trade Shows & Events", expenseCategory.Id, level3Advertising.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Event sponsorship"));

        #endregion

        #region LEVEL 4 - TRAVEL ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5621", "Airfare & Transportation", expenseCategory.Id, level3Travel.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Travel transportation"));
        accounts.Add(CreateAccount("5622", "Hotel & Accommodation", expenseCategory.Id, level3Travel.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Hotel stays"));
        accounts.Add(CreateAccount("5623", "Meals & Entertainment", expenseCategory.Id, level3Travel.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Business meals"));

        #endregion

        #region LEVEL 4 - FINANCE COSTS ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5701", "Interest Expense", expenseCategory.Id, level2Finance.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Interest on debt"));
        accounts.Add(CreateAccount("5702", "Bank Fees & Charges", expenseCategory.Id, level2Finance.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Bank service charges"));
        accounts.Add(CreateAccount("5703", "Exchange Loss", expenseCategory.Id, level2Finance.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Currency exchange loss"));

        #endregion

        #region LEVEL 4 - OTHER EXPENSES ACCOUNTS (POSTING)

        accounts.Add(CreateAccount("5801", "Donation & Charity", expenseCategory.Id, level2OtherExp.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Charitable contributions"));
        accounts.Add(CreateAccount("5802", "Loss on Sale of Assets", expenseCategory.Id, level2OtherExp.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Loss on asset disposal"));
        accounts.Add(CreateAccount("5803", "Penalties & Fines", expenseCategory.Id, level2OtherExp.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Regulatory penalties"));
        accounts.Add(CreateAccount("5804", "Contingency Reserve", expenseCategory.Id, level2OtherExp.Id, true, true, ledgerId, companyId, branchId, businessUnitId, userId, now, "Contingency allocation"));

        #endregion

        return accounts;
    }

    /// <summary>
    /// Helper method to create a ledger account with consistent properties
    /// </summary>
    private LedgerAccount CreateAccount(
        string accountNumber, string accountName, Guid categoryId, Guid? parentAccountId,
        bool isPostingAllowed, bool isSubledger, Guid ledgerId, Guid companyId, Guid branchId,
        Guid businessUnitId, Guid userId, DateTime createdAt, string description)
    {
        return new LedgerAccount
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            LedgerId = ledgerId,
            BranchId = branchId,
            BusinessUnitId = businessUnitId,
            AccountNumber = accountNumber,
            AccountName = accountName,
            CategoryId = categoryId,
            ParentAccountId = parentAccountId,
            IsSubledgerAccount = isSubledger,
            IsPostingAllowed = isPostingAllowed,
            IsControlAccount = !isPostingAllowed, // Control accounts are non-posting parent accounts
            CurrencyCode = "USD",
            AllowManualEntry = isPostingAllowed, // Only posting accounts allow manual entry
            IsActive = true,
            Description = description,
            CreatedAt = createdAt,
            CreatedByUserId = userId
        };
    }

    private TaxCode[] CreateTaxCodes(Guid companyId, Guid branchId, Guid businessUnitId, List<LedgerAccount> accounts, Guid userId)
    {
        var now = DateTime.UtcNow;
        var taxPayableAccount = accounts.FirstOrDefault(a => a.AccountName.Contains("Tax"));

        return new[]
        {
            new TaxCode
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                Code = "SALES_TAX",
                Name = "Sales Tax 10%",
                Percentage = 10.00m,
                LedgerAccountId = taxPayableAccount?.Id ?? Guid.Empty,
                IsRecoverable = false,
                IsActive = true,
                Description = "Standard sales tax rate",
                CreatedAt = now,
                CreatedByUserId = userId
            },
            new TaxCode
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                Code = "INPUT_TAX",
                Name = "Input VAT 15%",
                Percentage = 15.00m,
                LedgerAccountId = accounts.FirstOrDefault(a => a.AccountName.Contains("Receivable"))?.Id ?? Guid.Empty,
                IsRecoverable = true,
                IsActive = true,
                Description = "Recoverable input tax",
                CreatedAt = now,
                CreatedByUserId = userId
            }
        };
    }

    private Dimension[] CreateDimensions(Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
    {
        var now = DateTime.UtcNow;

        return new[]
        {
            new Dimension
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                Code = "DEPT",
                Name = "Department",
                IsActive = true,
                Description = "Organizational department",
                CreatedAt = now,
                CreatedByUserId = userId
            },
            new Dimension
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                Code = "LOCATION",
                Name = "Location",
                IsActive = true,
                Description = "Geographic location or branch",
                CreatedAt = now,
                CreatedByUserId = userId
            }
        };
    }

    private DimensionValue[] CreateDimensionValues(
        Guid companyId, Guid branchId, Guid businessUnitId,
        Dimension[] dimensions, Guid userId)
    {
        var now = DateTime.UtcNow;

        return new[]
        {
            new DimensionValue
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                DimensionId = dimensions[0].Id, // Department
                ValueCode = "SALES",
                ValueName = "Sales Department",
                IsActive = true,
                Description = "Sales and marketing",
                CreatedAt = now,
                CreatedByUserId = userId
            },
            new DimensionValue
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                DimensionId = dimensions[0].Id, // Department
                ValueCode = "OPS",
                ValueName = "Operations Department",
                IsActive = true,
                Description = "Operations and production",
                CreatedAt = now,
                CreatedByUserId = userId
            },
            new DimensionValue
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                DimensionId = dimensions[1].Id, // Location
                ValueCode = "HQ",
                ValueName = "Headquarters",
                IsActive = true,
                Description = "Main office location",
                CreatedAt = now,
                CreatedByUserId = userId
            }
        };
    }

    private PostingProfile[] CreatePostingProfiles(
        Guid companyId, Guid branchId, Guid businessUnitId,
        List<LedgerAccount> accounts, Guid userId)
    {
        var now = DateTime.UtcNow;

        var arAccount = accounts.FirstOrDefault(a => a.AccountName.Contains("Receivable"));
        var apAccount = accounts.FirstOrDefault(a => a.AccountName.Contains("Payable") && a.AccountNumber.StartsWith("21"));
        var salesAccount = accounts.FirstOrDefault(a => a.AccountName.Contains("Product Sales"));
        var inventoryAccount = accounts.FirstOrDefault(a => a.AccountName.Contains("Inventory"));
        var cashAccount = accounts.FirstOrDefault(a => a.AccountName.Contains("Cash"));

        var profiles = new List<PostingProfile>();

        if (arAccount != null && salesAccount != null)
        {
            profiles.Add(new PostingProfile
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                ModuleName = "Sales",
                TransactionType = "SalesInvoice",
                DebitAccountId = arAccount.Id,
                CreditAccountId = salesAccount.Id,
                TaxAccountId = accounts.FirstOrDefault(a => a.AccountNumber == "2141")?.Id,
                IsActive = true,
                Description = "Sales Invoice: DR Accounts Receivable / CR Product Sales Revenue / CR Tax Payable",
                CreatedAt = now,
                CreatedByUserId = userId
            });

            // POS immediate sale - cash/card collected at point of sale
            var cashAccount2 = accounts.FirstOrDefault(a => a.AccountNumber == "1111"); // Cash on Hand
            if (cashAccount2 != null)
            {
                profiles.Add(new PostingProfile
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    BranchId = branchId,
                    BusinessUnitId = businessUnitId,
                    ModuleName = "Sales",
                    TransactionType = "POSSale",
                    DebitAccountId = cashAccount2.Id,
                    CreditAccountId = salesAccount.Id,
                    TaxAccountId = accounts.FirstOrDefault(a => a.AccountNumber == "2141")?.Id,
                    IsActive = true,
                    Description = "POS Sale: DR Cash on Hand / CR Product Sales Revenue",
                    CreatedAt = now,
                    CreatedByUserId = userId
                });
            }

            // Payment received against outstanding invoice - AR clearing
            if (cashAccount != null)
            {
                profiles.Add(new PostingProfile
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    BranchId = branchId,
                    BusinessUnitId = businessUnitId,
                    ModuleName = "Sales",
                    TransactionType = "SalesPayment",
                    DebitAccountId = cashAccount.Id,
                    CreditAccountId = arAccount.Id,
                    IsActive = true,
                    Description = "Sales Payment: DR Cash/Bank / CR Accounts Receivable",
                    CreatedAt = now,
                    CreatedByUserId = userId
                });
            }
        }

        if (inventoryAccount != null && apAccount != null)
        {
            profiles.Add(new PostingProfile
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                ModuleName = "Purchasing",
                TransactionType = "PurchaseInvoice",
                DebitAccountId = inventoryAccount.Id,
                CreditAccountId = apAccount.Id,
                TaxAccountId = null,
                IsActive = true,
                Description = "Posting profile for purchase invoices",
                CreatedAt = now,
                CreatedByUserId = userId
            });
        }

        if (apAccount != null && cashAccount != null)
        {
            profiles.Add(new PostingProfile
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                BusinessUnitId = businessUnitId,
                ModuleName = "Accounting",
                TransactionType = "CashPayment",
                DebitAccountId = apAccount.Id,
                CreditAccountId = cashAccount.Id,
                TaxAccountId = null,
                IsActive = true,
                Description = "Posting profile for cash payments",
                CreatedAt = now,
                CreatedByUserId = userId
            });
        }

        return profiles.ToArray();
    }

    #endregion
}
