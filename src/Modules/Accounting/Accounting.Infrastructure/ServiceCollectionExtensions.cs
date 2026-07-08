using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Repositories.Interfaces;
using Accounting.Infrastructure.Repositories.Implementations;
using Accounting.Domain.Entities;
using Nexcore.SharedKernel.Repository;
using Accounting.Infrastructure.Services;
using Accounting.Infrastructure.Events;
using Accounting.Infrastructure.Reports.Services;
using Nexcore.SharedKernel.Events;
using Accounting.Application.Services.Interfaces;

namespace Accounting.Infrastructure;

/// <summary>
/// Service collection extensions for Accounting module
/// Registers all infrastructure services, repositories, and application services
/// Follows the Clean Architecture pattern with proper layer separation:
/// - Controllers use application services
/// - Application services coordinate between controllers and repositories
/// - Repositories handle data access with tenant filtering
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Accounting infrastructure services
    /// Registers DbContext, repositories, and application services
    /// </summary>
    public static IServiceCollection AddAccountingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<AccountingDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b =>
                {
                    b.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    b.MigrationsAssembly("Accounting.Infrastructure");
                }));

        // ========== REGISTER REPOSITORIES ==========
        // Register generic repositories for direct entity access
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register specialized repositories (data access layer)
        services.AddScoped<ILedgerRepository, LedgerRepository>();
        services.AddScoped<ILedgerAccountRepository, LedgerAccountRepository>();
        services.AddScoped<IAccountCategoryRepository, AccountCategoryRepository>();
        services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
        services.AddScoped<IJournalLineRepository, JournalLineRepository>();
        services.AddScoped<IAccountBalanceRepository, AccountBalanceRepository>();
        services.AddScoped<IFiscalCalendarRepository, FiscalCalendarRepository>();
        services.AddScoped<IFiscalPeriodRepository, FiscalPeriodRepository>();
        services.AddScoped<IDimensionRepository, DimensionRepository>();
        services.AddScoped<IDimensionValueRepository, DimensionValueRepository>();
        services.AddScoped<IPostingProfileRepository, PostingProfileRepository>();
        services.AddScoped<ITaxCodeRepository, TaxCodeRepository>();
        services.AddScoped<IJournalAuditRepository, JournalAuditRepository>();

        // ========== REGISTER APPLICATION SERVICES ==========
        // These services implement business logic and coordinate between controllers and repositories
        // Controllers should use these services, NOT repositories directly
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddScoped<ILedgerAccountService, LedgerAccountService>();
        services.AddScoped<IAccountCategoryService, AccountCategoryService>();
        services.AddScoped<IFiscalCalendarService, FiscalCalendarService>();
        services.AddScoped<IJournalEntryService, JournalEntryService>();
        services.AddScoped<ITaxCodeService, TaxCodeService>();
        services.AddScoped<IDimensionService, DimensionService>();
        services.AddScoped<IFiscalPeriodService, FiscalPeriodService>();
        services.AddScoped<IPostingProfileService, PostingProfileService>();
        services.AddScoped<IAccountBalanceService, AccountBalanceService>();

        // ========== REGISTER INFRASTRUCTURE SERVICES ==========
        // Register Accounting initialization service
        // Used to seed accounting data when a new company is created (non-production only)
        services.AddScoped<IAccountingInitializationService, AccountingInitializationService>();

        // Register event handler for CompanyCreatedEvent
        // This allows Accounting module to subscribe to company creation events from Core module
        // Maintains loose coupling - modules only know about events, not each other
        services.AddScoped<IEventHandler<CompanyCreatedEvent>, CompanyCreatedEventHandler>();

        // Register handler that creates four dedicated GL sub-accounts when a new
        // inventory category is created. Inventory publishes CategoryGlAccountsRequestedEvent;
        // this handler creates the sub-accounts and completes the event's TCS.
        services.AddScoped<IEventHandler<CategoryGlAccountsRequestedEvent>,
                           CategoryGlAccountsRequestedEventHandler>();

        // Register handler that responds to Inventory GL account resolution requests.
        // Inventory publishes GlAccountsRequestedEvent; this handler looks up the
        // four standard COA accounts and completes the event's TaskCompletionSource.
        services.AddScoped<IEventHandler<GlAccountsRequestedEvent>, GlAccountsRequestedEventHandler>();

        // ?? Sales integration ?????????????????????????????????????????????????
        // SalesInvoicePostedEvent       ? AR debit / per-item revenue credit
        // SalesCogsPostedEvent          ? COGS debit / inventory asset credit (on shipment)
        // SalesPaymentReceivedEvent     ? Cash debit / AR clearing credit
        // PosTransactionAccountingEvent ? compound revenue + COGS entry (POS immediate sale)
        services.AddScoped<IEventHandler<SalesInvoicePostedEvent>,        SalesAccountingEventHandler>();
        services.AddScoped<IEventHandler<SalesCogsPostedEvent>,           SalesAccountingEventHandler>();
        services.AddScoped<IEventHandler<SalesPaymentReceivedEvent>,      SalesAccountingEventHandler>();
        services.AddScoped<IEventHandler<PosTransactionAccountingEvent>,  SalesAccountingEventHandler>();

        // Procurement (accounts payable):
        // PurchaseInvoicePostedEvent  → DR Expense/Inventory / CR Accounts Payable
        // VendorPaymentClearedEvent   → DR Accounts Payable / CR Bank or Cash
        services.AddScoped<IEventHandler<PurchaseInvoicePostedEvent>,     ProcurementAccountingEventHandler>();
        services.AddScoped<IEventHandler<VendorPaymentClearedEvent>,      ProcurementAccountingEventHandler>();

        // ========== REGISTER REPORTING SERVICES ==========
        // Register Financial Report Service
        services.AddScoped<IFinancialReportService, FinancialReportService>();

        return services;
    }
}
