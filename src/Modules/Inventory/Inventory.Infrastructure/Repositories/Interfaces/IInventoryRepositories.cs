using Inventory.Application.DTOs;
using Inventory.Domain.Entities;
using Nexcore.SharedKernel.Repository;

namespace Inventory.Infrastructure.Repositories.Interfaces;

public interface IUnitRepository : IRepository<Unit>
{
    Task<Unit?> GetByCodeAsync(string code);
    Task<List<Unit>> GetActiveUnitsAsync();
}

public interface IItemCategoryRepository : IRepository<ItemCategory>
{
    Task<ItemCategory?> GetByCodeAsync(string code);
    Task<List<ItemCategory>> GetCategoryHierarchyAsync(Guid? parentId = null);
    Task<List<ItemCategory>> GetActiveategoriesAsync();
}

public interface IItemRepository : IRepository<Item>
{
    Task<Item?> GetByCodeAsync(string code);
    Task<List<Item>> GetItemsByCategory(Guid categoryId);
    Task<List<Item>> GetActiveItemsAsync();
    Task<Item?> GetWithUomConversionsAsync(Guid id);

    /// <summary>Full item with all navigation properties loaded (for detail page)</summary>
    Task<Item?> GetWithFullDetailsAsync(Guid id);

    /// <summary>
    /// Bring an item's variants in line with <paramref name="incoming"/>: rows with an Id
    /// are updated in place, rows without one are inserted, and existing rows absent from
    /// the list are soft-deleted.
    ///
    /// <para>Soft, not hard: variant ids are referenced by inventory balances, documents,
    /// transactions, batches, serials and POS transaction lines. Deleting the row would
    /// orphan stock and rewrite history.</para>
    /// </summary>
    Task SyncItemVariantsAsync(Guid itemId, IReadOnlyList<ItemVariant> incoming);

    /// <summary>
    /// Same as GetWithFullDetailsAsync but AsNoTracking + AsSplitQuery for read-only use (GetById API).
    /// Avoids cartesian product timeouts on Azure SQL and does not hold a change-tracking scope.
    /// </summary>
    Task<Item?> GetWithFullDetailsReadOnlyAsync(Guid id);

    /// <summary>Lookup item by any barcode value (for POS scanning)</summary>
    Task<Item?> GetByBarcodeAsync(string barcode);

    /// <summary>Items visible on e-commerce portals</summary>
    Task<List<Item>> GetPublishedItemsAsync();

    /// <summary>Featured items (banners, POS quick-access)</summary>
    Task<List<Item>> GetFeaturedItemsAsync();

    /// <summary>Items listed on a specific channel</summary>
    Task<List<Item>> GetItemsByChannelAsync(string channel);

    /// <summary>Search by code or name (partial match)</summary>
    Task<List<Item>> SearchAsync(string term);

    /// <summary>Insert an ItemImage record directly without loading the parent Item.</summary>
    Task AddImageAsync(ItemImage image);

    /// <summary>Find a single ItemImage by its own ID (no parent load required).</summary>
    Task<ItemImage?> GetImageByIdAsync(Guid imageId);

    /// <summary>Remove an ItemImage record directly.</summary>
    void RemoveImage(ItemImage image);

    /// <summary>
    /// Clears IsPrimary=true on all existing images for the item so a new
    /// primary image can be inserted without violating IX_ItemImage_Item_Primary.
    /// </summary>
    Task ClearPrimaryFlagAsync(Guid itemId);

    /// <summary>
    /// Full-replace an item's barcodes using set-based SQL (ExecuteDelete + insert),
    /// bypassing the change tracker. Avoids the DbUpdateConcurrencyException that arises
    /// when mutating barcode rows loaded as part of the heavy AsSplitQuery item graph,
    /// and the transient duplicate-primary against IX_ItemBarcode_Item_Primary.
    /// Pass barcodes with exactly one IsPrimary=true (or none).
    /// </summary>
    Task ReplaceItemBarcodesAsync(Guid itemId, IReadOnlyList<ItemBarcode> barcodes);

    /// <summary>Get items with basic info (lightweight for dropdowns and lists)</summary>
    Task<List<Item>> GetItemsBasicAsync();

    /// <summary>Get paginated items with basic info (lightweight for dropdowns and lists)</summary>
    Task<(List<Item> Items, int TotalCount)> GetItemsBasicPaginatedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? itemType = null);

    /// <summary>Get paginated active items with Images, Barcodes, and Prices included</summary>
    Task<(List<Item> Items, int Total)> GetActivePagedAsync(int pageNumber, int pageSize);

    /// <summary>Get paginated items (all statuses) with Images, Barcodes, and Prices included, with optional search/sort/status filtering</summary>
    Task<(List<Item> Items, int Total)> GetAllPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, string? sortDirection = null, bool? isActive = null, Guid? warehouseId = null, string? itemType = null);

    /// <summary>Full item by code with all navigation properties loaded</summary>
    Task<Item?> GetWithFullDetailsByCodeAsync(string code);
}

public interface IItemUomConversionRepository : IRepository<ItemUomConversion>
{
    Task<List<ItemUomConversion>> GetConversionsForItemAsync(Guid itemId);
    Task<ItemUomConversion?> GetConversionAsync(Guid itemId, Guid fromUnitId, Guid toUnitId);
}

// ?? after IItemCategoryRepository ?????????????????????????????????????????????

public interface IAttributeDefinitionRepository : IRepository<AttributeDefinition>
{
    Task<AttributeDefinition?> GetByCodeAsync(string code);
    Task<List<AttributeDefinition>> GetActiveAsync();
    Task<List<AttributeDefinition>> GetVariantAttributesAsync();
}

public interface ITaxDefinitionRepository : IRepository<TaxDefinition>
{
    Task<TaxDefinition?> GetByCodeAsync(string code);
    Task<List<TaxDefinition>> GetActiveAsync();
    Task<List<TaxDefinition>> GetSalesTaxesAsync();
    Task<List<TaxDefinition>> GetPurchaseTaxesAsync();
}

// ?? Brand / Color / Size master data ???????????????????????????????????????

public interface IBrandRepository : IRepository<Brand>
{
    Task<Brand?> GetByCodeAsync(string code);
    Task<List<Brand>> GetActiveBrandsAsync();
}

public interface IColorRepository : IRepository<Color>
{
    Task<Color?> GetByCodeAsync(string code);
    Task<List<Color>> GetActiveColorsAsync();
    Task<List<Color>> GetByFamilyAsync(string colorFamily);
}

public interface ISizeRepository : IRepository<Size>
{
    Task<Size?> GetByCodeAsync(string code, string sizeChart);
    Task<List<Size>> GetBySizeChartAsync(string sizeChart);
}

// ?? Item sub-entity repositories ????????????????????????????????????????????

public interface IItemBarcodeRepository : IRepository<ItemBarcode>
{
    Task<ItemBarcode?> GetByValueAsync(string barcode);
    Task<List<ItemBarcode>> GetByItemAsync(Guid itemId);
}

public interface IItemPriceRepository : IRepository<ItemPrice>
{
    Task<List<ItemPrice>> GetByItemAsync(Guid itemId);
    Task<ItemPrice?> GetByItemAndPriceListAsync(Guid itemId, Guid unitId, string priceList, string currencyCode);
    Task<List<ItemPrice>> GetActivePricesAsync(Guid itemId, string priceList);
}

public interface IItemVariantRepository : IRepository<ItemVariant>
{
    Task<ItemVariant?> GetByCodeAsync(string variantCode);
    Task<List<ItemVariant>> GetByItemAsync(Guid itemId);
    Task<ItemVariant?> GetByItemColorSizeAsync(Guid itemId, Guid? colorId, Guid? sizeId);
}

public interface IItemChannelListingRepository : IRepository<ItemChannelListing>
{
    Task<ItemChannelListing?> GetByItemAndChannelAsync(Guid itemId, string channel);
    Task<List<ItemChannelListing>> GetByItemAsync(Guid itemId);
    Task<List<ItemChannelListing>> GetByChannelAsync(string channel, string? status = null);
}

public interface IItemDiscountRepository : IRepository<ItemDiscount>
{
    Task<List<ItemDiscount>> GetByItemAsync(Guid itemId);
    Task<List<ItemDiscount>> GetActiveDiscountsAsync(Guid itemId, string? channel = null);
}

public interface IItemSupplierRepository : IRepository<ItemSupplier>
{
    Task<List<ItemSupplier>> GetByItemAsync(Guid itemId);
    Task<ItemSupplier?> GetPrimarySupplierAsync(Guid itemId);
}

public interface IItemWarrantyRepository : IRepository<ItemWarranty>
{
    Task<ItemWarranty?> GetByItemAsync(Guid itemId);
}

public interface IItemShippingRepository : IRepository<ItemShipping>
{
    Task<ItemShipping?> GetByItemAsync(Guid itemId);
}

public interface IItemSeoRepository : IRepository<ItemSeo>
{
    Task<ItemSeo?> GetByItemAsync(Guid itemId);
    Task<ItemSeo?> GetBySlugAsync(string slug);
}

public interface IItemBundleRepository : IRepository<ItemBundle>
{
    Task<List<ItemBundle>> GetComponentsAsync(Guid bundleItemId);
    Task<List<ItemBundle>> GetBundleParentsAsync(Guid componentItemId);
}

public interface IItemSubstitutionRepository : IRepository<ItemSubstitution>
{
    Task<List<ItemSubstitution>> GetSubstitutionsAsync(Guid itemId);
}

// ?? Warehouse & Bin ??????????????????????????????????????????????????????????

public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<Warehouse?> GetByCodeAsync(string code);
    Task<List<Warehouse>> GetActiveWarehousesAsync();
    Task<Warehouse?> GetWithBinsAsync(Guid id);
}

public interface IBinRepository : IRepository<Bin>
{
    Task<Bin?> GetByCodeAsync(string code, Guid warehouseId);
    Task<List<Bin>> GetBinsByWarehouseAsync(Guid warehouseId);
    Task<List<Bin>> GetActiveBinsAsync(Guid warehouseId);
}

// ?? Documents ????????????????????????????????????????????????????????????????

public interface IInventoryDocumentRepository : IRepository<InventoryDocument>
{
    Task<InventoryDocument?> GetByNumberAsync(string documentNumber);
    Task<InventoryDocument?> GetWithLinesAsync(Guid id);
    Task<List<InventoryDocument>> GetDocumentsByStatusAsync(string status);
    Task<List<InventoryDocument>> GetDocumentsByTypeAsync(string documentType);
    Task<List<InventoryDocument>> GetDocumentsByWarehouseAsync(Guid warehouseId);
    Task<List<InventoryDocument>> GetDocumentsByDateRangeAsync(DateTime fromDate, DateTime toDate);
}

public interface IInventoryDocumentLineRepository : IRepository<InventoryDocumentLine>
{
    Task<List<InventoryDocumentLine>> GetLinesByDocumentAsync(Guid documentId);
    Task<List<InventoryDocumentLine>> GetLinesByItemAsync(Guid itemId);
}

// ?? Transactions & Balances ??????????????????????????????????????????????????

public interface IInventoryTransactionRepository : IRepository<InventoryTransaction>
{
    Task<List<InventoryTransaction>> GetTransactionsByItemAsync(Guid itemId);
    Task<List<InventoryTransaction>> GetTransactionsByWarehouseAsync(Guid warehouseId);
    Task<List<InventoryTransaction>> GetTransactionsByDocumentAsync(Guid documentId);
    Task<List<InventoryTransaction>> GetTransactionsByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<List<InventoryTransaction>> GetTransactionsByTypeAsync(string transactionType);
    Task<List<InventoryTransaction>> GetStockLedgerAsync(Guid itemId, Guid warehouseId, DateTime? fromDate = null, DateTime? toDate = null);
}

public interface IInventoryBalanceRepository : IRepository<InventoryBalance>
{
    Task<InventoryBalance?> GetBalanceAsync(Guid itemId, Guid warehouseId, Guid? binId = null, Guid? variantId = null);
    Task<List<InventoryBalance>> GetBalancesByItemAsync(Guid itemId);
    Task<List<InventoryBalance>> GetBalancesByWarehouseAsync(Guid warehouseId);
    Task<List<InventoryBalance>> GetLowStockAsync(decimal? aboveThreshold = null);
    Task<decimal> GetTotalValueAsync(Guid warehouseId);

    /// <summary>
    /// Per-item on-hand/reserved/available totals for the current tenant, in a single GROUP BY query.
    /// With no <paramref name="warehouseId"/> the totals are summed across every warehouse (items grid);
    /// with one they are restricted to that warehouse (what a POS till draws from). Avoids the per-row
    /// item/warehouse lookups the paged stock-summary report does.
    /// </summary>
    Task<List<ItemStockTotalDto>> GetStockTotalsByItemAsync(Guid? warehouseId = null);
}

public interface IInventoryValuationRepository : IRepository<InventoryValuation>
{
    Task<List<InventoryValuation>> GetValuationsByPeriodAsync(string periodReference);
    Task<List<InventoryValuation>> GetValuationsByDateAsync(DateTime valuationDate);
    Task<List<InventoryValuation>> GetValuationsByWarehouseAsync(Guid warehouseId);
}

public interface IInventoryCostLayerRepository : IRepository<InventoryCostLayer>
{
    Task<List<InventoryCostLayer>> GetCostLayersForItemAsync(Guid itemId, Guid warehouseId);
    Task<List<InventoryCostLayer>> GetActiveCostLayersAsync(Guid itemId, Guid warehouseId);
    Task<InventoryCostLayer?> GetOldestCostLayerAsync(Guid itemId, Guid warehouseId);
}

// ?? Unit-level tracking (Serial / Lot) ??????????????????????????????????????

public interface IItemSerialRepository : IRepository<ItemSerial>
{
    /// <summary>All serialized units for an item (newest first), optionally filtered by status/search.</summary>
    Task<(List<ItemSerial> Items, int Total)> GetByItemPagedAsync(
        Guid itemId, int pageNumber, int pageSize, string? status = null, string? search = null);

    /// <summary>Global scan lookup: match a serial number OR any IMEI within the tenant.</summary>
    Task<ItemSerial?> GetBySerialOrImeiAsync(string value);

    /// <summary>Find a specific unit by (item, serial number).</summary>
    Task<ItemSerial?> GetBySerialNumberAsync(Guid itemId, string serialNumber);

    /// <summary>The audit trail for a unit (oldest → newest).</summary>
    Task<List<ItemSerialHistory>> GetHistoryAsync(Guid itemSerialId);

    /// <summary>Units whose warranty ends within the next <paramref name="days"/> days.</summary>
    Task<List<ItemSerial>> GetWarrantyExpiringAsync(int days);

    /// <summary>Of the candidate serial numbers, which already exist for the item (for bulk-generate collision checks).</summary>
    Task<HashSet<string>> GetExistingSerialNumbersAsync(Guid itemId, IEnumerable<string> candidates);

    /// <summary>
    /// Sets a unit's status (tracked) and appends a StatusChanged history row. Returns the updated unit,
    /// or null if not found. Caller owns SaveChanges.
    /// </summary>
    Task<ItemSerial?> UpdateStatusAsync(Guid id, string newStatus, string? notes);
}

public interface IItemBatchRepository : IRepository<ItemBatch>
{
    /// <summary>All batches for an item (soonest expiry first).</summary>
    Task<List<ItemBatch>> GetByItemAsync(Guid itemId);

    /// <summary>Find a batch by (item, batch number).</summary>
    Task<ItemBatch?> GetByNumberAsync(Guid itemId, string batchNumber);

    /// <summary>Active batches expiring within the next <paramref name="days"/> days.</summary>
    Task<List<ItemBatch>> GetExpiringAsync(int days);
}
