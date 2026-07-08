using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Interfaces;
using Inventory.Infrastructure.Repositories.Implementations;
using Inventory.Infrastructure.Services;
using Inventory.Infrastructure.Events;
using Inventory.Application.Services.Interfaces;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Repository;

namespace Inventory.Infrastructure;

/// <summary>
/// Service collection extensions for Inventory module
/// Registers all infrastructure services, repositories, and application services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Inventory infrastructure services
    /// Registers DbContext and repositories
    /// </summary>
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b =>
                {
                    b.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    b.MigrationsAssembly("Inventory.Infrastructure");
                }));

        // ========== REGISTER GENERIC REPOSITORY ==========
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // ========== REGISTER SPECIALIZED REPOSITORIES ==========
        // Master Data Repositories
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IItemCategoryRepository, ItemCategoryRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IItemUomConversionRepository, ItemUomConversionRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IBinRepository, BinRepository>();

        // Item master & classification
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IColorRepository, ColorRepository>();
        services.AddScoped<ISizeRepository, SizeRepository>();
        services.AddScoped<IAttributeDefinitionRepository, AttributeDefinitionRepository>();
        services.AddScoped<ITaxDefinitionRepository, TaxDefinitionRepository>();

        // Item sub-entity repositories
        services.AddScoped<IItemBarcodeRepository, ItemBarcodeRepository>();
        services.AddScoped<IItemPriceRepository, ItemPriceRepository>();
        services.AddScoped<IItemVariantRepository, ItemVariantRepository>();
        services.AddScoped<IItemChannelListingRepository, ItemChannelListingRepository>();
        services.AddScoped<IItemDiscountRepository, ItemDiscountRepository>();
        services.AddScoped<IItemSupplierRepository, ItemSupplierRepository>();
        services.AddScoped<IItemWarrantyRepository, ItemWarrantyRepository>();
        services.AddScoped<IItemShippingRepository, ItemShippingRepository>();
        services.AddScoped<IItemSeoRepository, ItemSeoRepository>();
        services.AddScoped<IItemBundleRepository, ItemBundleRepository>();
        services.AddScoped<IItemSubstitutionRepository, ItemSubstitutionRepository>();

        // Document Repositories
        services.AddScoped<IInventoryDocumentRepository, InventoryDocumentRepository>();
        services.AddScoped<IInventoryDocumentLineRepository, InventoryDocumentLineRepository>();

        // Transaction & Balance Repositories
        services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();
        services.AddScoped<IInventoryBalanceRepository, InventoryBalanceRepository>();

        // Valuation Repositories
        services.AddScoped<IInventoryValuationRepository, InventoryValuationRepository>();
        services.AddScoped<IInventoryCostLayerRepository, InventoryCostLayerRepository>();

        // Unit-level tracking (Serial / Lot) repositories
        services.AddScoped<IItemSerialRepository, ItemSerialRepository>();
        services.AddScoped<IItemBatchRepository, ItemBatchRepository>();

        // ========== REGISTER INITIALIZATION SERVICE ==========
        services.AddScoped<IInventoryInitializationService, InventoryInitializationService>();

        // ========== REGISTER BALANCE MUTATION SERVICE ==========
        // Single source of truth for InventoryBalance keying + moving-average costing,
        // shared by inbound (document posting) and outbound (POS/delivery deduction) paths.
        services.AddScoped<IInventoryBalanceService, InventoryBalanceService>();

        // ========== REGISTER UNIT-LEVEL TRACKING SERVICE ==========
        // Turns posted document lines into Serial / Lot registry changes (identity + lifecycle),
        // alongside the quantity ledger. Shares the scoped DbContext so it commits atomically.
        services.AddScoped<IItemTrackingService, ItemTrackingService>();

        // ========== REGISTER IMAGE STORAGE SERVICE ==========
        // SQL Server-backed file storage (inv.StoredFiles) — replaces Azure Blob Storage.
        // Scoped because it uses the per-request InventoryDbContext.
        services.AddScoped<IBlobStorageService, SqlImageStorageService>();

        // ========== REGISTER EVENT HANDLERS ==========
        services.AddScoped<IEventHandler<CompanyCreatedEvent>, InventoryCompanyCreatedEventHandler>();

        // Handle stock deductions triggered by the Sales module
        // POS immediate payment ? PosTransactionCompletedEvent
        // Delivery shipped      ? DeliveryPostedEvent
        services.AddScoped<IEventHandler<PosTransactionCompletedEvent>, SalesStockDeductionHandler>();
        services.AddScoped<IEventHandler<DeliveryPostedEvent>, SalesStockDeductionHandler>();

        // Inbound: add accepted stock when Procurement posts a Goods Receipt
        services.AddScoped<IEventHandler<GoodsReceiptPostedEvent>, PurchaseStockReceiptHandler>();

        // Soft allocation: order Confirmed → reserve, Cancelled/Rejected → release
        services.AddScoped<IEventHandler<StockReservationChangedEvent>, SalesStockReservationHandler>();

        // Synchronous item GL data lookup (TCS request/response pattern)
        services.AddScoped<IEventHandler<ItemGlLookupEvent>, ItemGlLookupHandler>();

        // Synchronous item price/category lookup for the Sales pricing engine
        services.AddScoped<IEventHandler<ItemPriceLookupEvent>, ItemPriceLookupHandler>();

        // Synchronous stock-availability lookup for the POS pre-payment check
        services.AddScoped<IEventHandler<StockAvailabilityLookupEvent>, StockAvailabilityLookupHandler>();

        // Manufacturing completion: backflush consumed materials (OUT) + receive finished goods (IN)
        services.AddScoped<IEventHandler<ProductionCompletedEvent>, ProductionStockHandler>();

        // ========== REGISTER GL ACCOUNT RESOLVER ==========
        services.AddScoped<InventoryGlAccountResolver>();

        return services;
    }
}
