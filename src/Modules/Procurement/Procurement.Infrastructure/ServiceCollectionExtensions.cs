using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Repository;
using Procurement.Application.Services.Interfaces;
using Procurement.Infrastructure.Events;
using Procurement.Infrastructure.Persistence;
using Procurement.Infrastructure.Repositories.Implementations;
using Procurement.Infrastructure.Repositories.Interfaces;
using Procurement.Infrastructure.Services;

namespace Procurement.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProcurementInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ProcurementDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("Procurement.Infrastructure")));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<IDocumentSequenceRepository, DocumentSequenceRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IPurchaseRequisitionRepository, PurchaseRequisitionRepository>();
        services.AddScoped<IRequestForQuotationRepository, RequestForQuotationRepository>();
        services.AddScoped<IVendorQuotationRepository, VendorQuotationRepository>();
        services.AddScoped<IPurchaseContractRepository, PurchaseContractRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
        services.AddScoped<IPurchaseInvoiceRepository, PurchaseInvoiceRepository>();
        services.AddScoped<IVendorPaymentRepository, VendorPaymentRepository>();

        // ── Internal Services ─────────────────────────────────────────────────
        services.AddScoped<IDocumentSequenceService, DocumentSequenceService>();

        // ── Application Services ──────────────────────────────────────────────
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IPurchaseRequisitionService, PurchaseRequisitionService>();
        services.AddScoped<IRequestForQuotationService, RequestForQuotationService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
        services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();
        services.AddScoped<IVendorPaymentService, VendorPaymentService>();
        services.AddScoped<IPurchaseContractService, PurchaseContractService>();

        // ── Initialization & Seeding ──────────────────────────────────────────
        services.AddScoped<IProcurementInitializationService, ProcurementInitializationService>();
        services.AddScoped<ProcurementSeedDataService>();
        services.AddScoped<IEventHandler<CompanyCreatedEvent>, ProcurementCompanyCreatedEventHandler>();

        // ── Master Data Services ──────────────────────────────────────────────
        services.AddScoped<IVendorCategoryService, VendorCategoryService>();
        services.AddScoped<IProcurementCategoryService, ProcurementCategoryService>();
        services.AddScoped<IVendorDocumentService, VendorDocumentService>();
        services.AddScoped<IVendorPricelistService, VendorPricelistService>();
        services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
        services.AddScoped<IDocumentSequenceManagementService, DocumentSequenceManagementService>();
        services.AddScoped<IProcurementSettingsService, ProcurementSettingsService>();

        // ── Vendor Sourcing & Performance ─────────────────────────────────────
        services.AddScoped<IApprovedVendorListService, ApprovedVendorListService>();
        services.AddScoped<IVendorPerformanceService, VendorPerformanceService>();

        // ── Returns / Debit Notes / Landed Costs ──────────────────────────────
        services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();
        services.AddScoped<IVendorDebitNoteService, VendorDebitNoteService>();
        services.AddScoped<ILandedCostService, LandedCostService>();
        services.AddScoped<IProcurementReportsService, ProcurementReportsService>();

        return services;
    }
}
