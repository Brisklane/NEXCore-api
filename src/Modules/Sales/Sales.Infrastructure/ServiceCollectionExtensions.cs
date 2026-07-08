using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Repository;
using Sales.Application.Services.Interfaces;
using Sales.Infrastructure.Events;
using Sales.Infrastructure.Persistence;
using Sales.Infrastructure.Repositories.Implementations;
using Sales.Infrastructure.Repositories.Interfaces;
using Sales.Infrastructure.Services;

namespace Sales.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSalesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SalesDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b =>
                {
                    b.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    b.MigrationsAssembly("Sales.Infrastructure");
                }));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // ?? Repositories ??????????????????????????????????????????????????????
        // Note: Customer (Contact) CRUD is handled by CRM module.
        // Sales stores ContactId (cross-module Guid) on all transactional documents.

        // Tax Engine
        // TaxDefinition lives in Inventory — Sales only owns TaxGroup, TaxGroupRate, TaxRule
        services.AddScoped<ITaxGroupRepository, TaxGroupRepository>();
        services.AddScoped<ITaxRuleRepository, TaxRuleRepository>();

        // Approval Engine
        services.AddScoped<IApprovalPolicyRepository, ApprovalPolicyRepository>();
        services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();

        // Commission
        services.AddScoped<ICommissionRuleRepository, CommissionRuleRepository>();
        services.AddScoped<ICommissionEntryRepository, CommissionEntryRepository>();
        services.AddScoped<ISalesTargetRepository, SalesTargetRepository>();

        // Orders
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<ISalesOrderLineRepository, SalesOrderLineRepository>();
        // Quotation
        services.AddScoped<IQuotationRepository, QuotationRepository>();
        // Delivery
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        // Currency & Exchange Rates
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<ICurrencyRateRepository, CurrencyRateRepository>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        // Sales Teams
        services.AddScoped<ISalesTeamRepository, SalesTeamRepository>();
        // Invoice & Payment
        services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepository>();
        services.AddScoped<ISalesPaymentRepository, SalesPaymentRepository>();
        services.AddScoped<IPaymentAllocationRepository, PaymentAllocationRepository>();
        // POS
        services.AddScoped<IPosSettingsRepository, PosSettingsRepository>();
        services.AddScoped<IPosSettingsService, PosSettingsService>();
        services.AddScoped<IPosStoreRepository, PosStoreRepository>();
        services.AddScoped<IPosTerminalRepository, PosTerminalRepository>();
        services.AddScoped<IPosCashierRepository, PosCashierRepository>();
        services.AddScoped<IPosSessionRepository, PosSessionRepository>();
        services.AddScoped<IPosTransactionRepository, PosTransactionRepository>();
        services.AddScoped<IPosReceiptTemplateRepository, PosReceiptTemplateRepository>();
        services.AddScoped<IPosBarcodeLabelTemplateRepository, PosBarcodeLabelTemplateRepository>();
        services.AddScoped<IPosCashDrawerRepository, PosCashDrawerRepository>();
        // Rider
        services.AddScoped<IRiderRepository, RiderRepository>();
        services.AddScoped<IRiderAssignmentRepository, RiderAssignmentRepository>();
        // Promotions
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IPromotionItemRepository, PromotionItemRepository>();
        // Loyalty & Coupons
        services.AddScoped<ILoyaltyAccountRepository, LoyaltyAccountRepository>();
        services.AddScoped<IPriceListRepository, PriceListRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        // Vendor Profiles
        services.AddScoped<IStoreVendorProfileRepository, StoreVendorProfileRepository>();
        // Store Offers & Menu
        services.AddScoped<IStoreOfferRepository, StoreOfferRepository>();
        services.AddScoped<IStoreMenuRepository, StoreMenuRepository>();

        // Document Sequences
        services.AddScoped<IDocumentSequenceRepository, DocumentSequenceRepository>();
        services.AddScoped<IDocumentSequenceService, DocumentSequenceService>();

        // ?? Services ??????????????????????????????????????????????????????????
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
        services.AddScoped<IPosCheckoutService, PosCheckoutService>();
        services.AddScoped<IPosOfflineSyncService, PosOfflineSyncService>();
        services.AddScoped<IReceiptRenderingService, ReceiptRenderingService>();
        // SQL Server-backed file storage (sales.StoredFiles) — replaces Azure Blob Storage.
        // Scoped because it uses the per-request SalesDbContext.
        services.AddScoped<IReceiptLogoService, SqlReceiptLogoService>();
        services.AddScoped<IBarcodeService, BarcodeService>();
        services.AddScoped<IBarcodeLabelService, BarcodeLabelService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IPriceListService, PriceListService>();
        services.AddScoped<IRiderService, RiderService>();

        // ?? Initialization & Events ???????????????????????????????????????????
        services.AddScoped<ISalesInitializationService, SalesInitializationService>();
        services.AddScoped<IEventHandler<CompanyCreatedEvent>, SalesCompanyCreatedEventHandler>();

        return services;
    }
}
