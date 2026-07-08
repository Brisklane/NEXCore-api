using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Inventory.Application.DTOs;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Helpers;
using Nexcore.SharedKernel.Repository;

namespace Inventory.Infrastructure.Repositories.Implementations;

// ---------------------------------------------------------------------------
// Master Data
// ---------------------------------------------------------------------------

public class UnitRepository : TenantAwareRepository<Unit>, IUnitRepository
{
    public UnitRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Unit?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(u => u.Code == code);

    public async Task<List<Unit>> GetActiveUnitsAsync()
    {
        var units = await FindAsync(u => u.IsActive);
        return units.OrderBy(u => u.DisplayOrder).ToList();
    }
}

public class ItemCategoryRepository : TenantAwareRepository<ItemCategory>, IItemCategoryRepository
{
    public ItemCategoryRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<ItemCategory?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(c => c.Code == code);

    public async Task<List<ItemCategory>> GetCategoryHierarchyAsync(Guid? parentId = null)
    {
        var categories = parentId.HasValue
            ? await FindAsync(c => c.ParentCategoryId == parentId)
            : await FindAsync(c => c.ParentCategoryId == null);

        return categories.OrderBy(c => c.Name).ToList();
    }

    public async Task<List<ItemCategory>> GetActiveategoriesAsync()
    {
        var categories = await FindAsync(c => c.IsActive);
        return categories.OrderBy(c => c.Name).ToList();
    }
}

public class ItemRepository : TenantAwareRepository<Item>, IItemRepository
{
    public ItemRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Item?> GetByCodeAsync(string code)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Include(i => i.BaseUnit)
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Code == code &&
                                      i.CompanyId == companyId &&
                                      i.BranchId == branchId &&
                                      i.BusinessUnitId == businessUnitId &&
                                      !i.IsDeleted);
    }

    public async Task<Item?> GetWithFullDetailsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Include(i => i.BaseUnit)
            .Include(i => i.Category)
            .Include(i => i.Brand)
            .Include(i => i.DisplayColor)
            .Include(i => i.Images)
            .Include(i => i.Barcodes).ThenInclude(b => b.Unit)
            .Include(i => i.Prices).ThenInclude(p => p.Unit)
            .Include(i => i.Attributes).ThenInclude(a => a.AttributeDefinition)
            .Include(i => i.Taxes).ThenInclude(t => t.TaxDefinition)
            .Include(i => i.Colors).ThenInclude(c => c.Color)
            .Include(i => i.Sizes).ThenInclude(s => s.Size)
            .Include(i => i.Variants).ThenInclude(v => v.Color)
            .Include(i => i.Variants).ThenInclude(v => v.Size)
            .Include(i => i.Shipping)
            .Include(i => i.Seo)
            .Include(i => i.Warranty)
            .Include(i => i.ChannelListings)
            .Include(i => i.Suppliers)
            .Include(i => i.Discounts)
            .Include(i => i.BundleComponents).ThenInclude(b => b.ComponentItem)
            .Include(i => i.Substitutions).ThenInclude(s => s.SubstituteItem)
            .Include(i => i.Comments)
            .Include(i => i.UomConversions).ThenInclude(c => c.FromUnit)
            .Include(i => i.UomConversions).ThenInclude(c => c.ToUnit)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id &&
                                      i.CompanyId == companyId &&
                                      i.BranchId == branchId &&
                                      i.BusinessUnitId == businessUnitId &&
                                      !i.IsDeleted);
    }

    public async Task<Item?> GetWithFullDetailsReadOnlyAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .AsNoTracking()
            .Include(i => i.BaseUnit)
            .Include(i => i.Category)
            .Include(i => i.Brand)
            .Include(i => i.DisplayColor)
            .Include(i => i.Images)
            .Include(i => i.Barcodes).ThenInclude(b => b.Unit)
            .Include(i => i.Prices).ThenInclude(p => p.Unit)
            .Include(i => i.Attributes).ThenInclude(a => a.AttributeDefinition)
            .Include(i => i.Taxes).ThenInclude(t => t.TaxDefinition)
            .Include(i => i.Colors).ThenInclude(c => c.Color)
            .Include(i => i.Sizes).ThenInclude(s => s.Size)
            .Include(i => i.Variants).ThenInclude(v => v.Color)
            .Include(i => i.Variants).ThenInclude(v => v.Size)
            .Include(i => i.Shipping)
            .Include(i => i.Seo)
            .Include(i => i.Warranty)
            .Include(i => i.ChannelListings)
            .Include(i => i.Suppliers)
            .Include(i => i.Discounts)
            .Include(i => i.BundleComponents).ThenInclude(b => b.ComponentItem)
            .Include(i => i.Substitutions).ThenInclude(s => s.SubstituteItem)
            .Include(i => i.Comments)
            .Include(i => i.UomConversions).ThenInclude(c => c.FromUnit)
            .Include(i => i.UomConversions).ThenInclude(c => c.ToUnit)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id &&
                                      i.CompanyId == companyId &&
                                      i.BranchId == branchId &&
                                      i.BusinessUnitId == businessUnitId &&
                                      !i.IsDeleted);
    }

    public async Task<Item?> GetWithFullDetailsByCodeAsync(string code)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Include(i => i.BaseUnit)
            .Include(i => i.Category)
            .Include(i => i.Brand)
            .Include(i => i.DisplayColor)
            .Include(i => i.Images)
            .Include(i => i.Barcodes).ThenInclude(b => b.Unit)
            .Include(i => i.Prices).ThenInclude(p => p.Unit)
            .Include(i => i.Attributes).ThenInclude(a => a.AttributeDefinition)
            .Include(i => i.Taxes).ThenInclude(t => t.TaxDefinition)
            .Include(i => i.Colors).ThenInclude(c => c.Color)
            .Include(i => i.Sizes).ThenInclude(s => s.Size)
            .Include(i => i.Variants).ThenInclude(v => v.Color)
            .Include(i => i.Variants).ThenInclude(v => v.Size)
            .Include(i => i.Shipping)
            .Include(i => i.Seo)
            .Include(i => i.Warranty)
            .Include(i => i.ChannelListings)
            .Include(i => i.Suppliers)
            .Include(i => i.Discounts)
            .Include(i => i.BundleComponents).ThenInclude(b => b.ComponentItem)
            .Include(i => i.Substitutions).ThenInclude(s => s.SubstituteItem)
            .Include(i => i.Comments)
            .Include(i => i.UomConversions).ThenInclude(c => c.FromUnit)
            .Include(i => i.UomConversions).ThenInclude(c => c.ToUnit)
            .FirstOrDefaultAsync(i => i.Code == code &&
                                      i.CompanyId == companyId &&
                                      i.BranchId == branchId &&
                                      i.BusinessUnitId == businessUnitId &&
                                      !i.IsDeleted);
    }

    public async Task<Item?> GetByBarcodeAsync(string barcode)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Include(i => i.BaseUnit)
            .Include(i => i.Barcodes)
            .Include(i => i.Prices)
            .Include(i => i.Taxes).ThenInclude(t => t.TaxDefinition)
            .FirstOrDefaultAsync(i => i.Barcodes.Any(b => b.Barcode == barcode && b.IsActive) &&
                                      i.CompanyId == companyId &&
                                      i.BranchId == branchId &&
                                      i.BusinessUnitId == businessUnitId &&
                                      !i.IsDeleted);
    }

    public async Task<List<Item>> GetItemsByCategory(Guid categoryId)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(i => i.CategoryId == categoryId &&
                        i.IsActive &&
                        i.CompanyId == companyId &&
                        i.BranchId == branchId &&
                        i.BusinessUnitId == businessUnitId &&
                        !i.IsDeleted)
            .Include(i => i.Images)
            .Include(i => i.Barcodes)
            .Include(i => i.Prices)
            .Include(i => i.BaseUnit)
            .Include(i => i.Category)
            .Include(i => i.Brand)
            .Include(i => i.DisplayColor)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<List<Item>> GetActiveItemsAsync()
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(i => i.IsActive &&
                        i.CompanyId == companyId &&
                        i.BranchId == branchId &&
                        i.BusinessUnitId == businessUnitId &&
                        !i.IsDeleted)
            .Include(i => i.BaseUnit)
            .Include(i => i.Category)
            .Include(i => i.DisplayColor)
            .OrderBy(i => i.Code)
            .ToListAsync();
    }

    public async Task<List<Item>> GetPublishedItemsAsync()
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(i => i.IsActive && i.IsPublished &&
                        i.CompanyId == companyId &&
                        i.BranchId == branchId &&
                        i.BusinessUnitId == businessUnitId &&
                        !i.IsDeleted)
            .Include(i => i.BaseUnit)
            .Include(i => i.Category)
            .Include(i => i.Brand)
            .Include(i => i.DisplayColor)
            .Include(i => i.Images.Where(img => img.IsPrimary))
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<List<Item>> GetFeaturedItemsAsync()
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(i => i.IsActive && i.IsPublished && i.IsFeatured &&
                        i.CompanyId == companyId &&
                        i.BranchId == branchId &&
                        i.BusinessUnitId == businessUnitId &&
                        !i.IsDeleted)
            .Include(i => i.BaseUnit)
            .Include(i => i.DisplayColor)
            .Include(i => i.Images.Where(img => img.IsPrimary))
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<List<Item>> GetItemsByChannelAsync(string channel)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(i => i.IsActive &&
                        i.ChannelListings.Any(cl => cl.Channel == channel && cl.ListingStatus == "Active") &&
                        i.CompanyId == companyId &&
                        i.BranchId == branchId &&
                        i.BusinessUnitId == businessUnitId &&
                        !i.IsDeleted)
            .Include(i => i.BaseUnit)
            .Include(i => i.ChannelListings)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<List<Item>> SearchAsync(string term)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        var lower = term.ToLower();
        return await DbSet
            .Where(i => i.IsActive &&
                        (i.Code.ToLower().Contains(lower) || i.Name.ToLower().Contains(lower)) &&
                        i.CompanyId == companyId &&
                        i.BranchId == branchId &&
                        i.BusinessUnitId == businessUnitId &&
                        !i.IsDeleted)
            .Include(i => i.BaseUnit)
            .Include(i => i.DisplayColor)
            .OrderBy(i => i.Code)
            .Take(50)
            .ToListAsync();
    }

    public async Task<Item?> GetWithUomConversionsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Include(i => i.BaseUnit)
            .Include(i => i.Category)
            .Include(i => i.UomConversions)
            .FirstOrDefaultAsync(i => i.Id == id &&
                                      i.CompanyId == companyId &&
                                      i.BranchId == branchId &&
                                      i.BusinessUnitId == businessUnitId &&
                                      !i.IsDeleted);
    }

    public async Task AddImageAsync(ItemImage image)
        => await Context.Set<ItemImage>().AddAsync(image);

    public async Task<ItemImage?> GetImageByIdAsync(Guid imageId)
        => await Context.Set<ItemImage>().FindAsync(imageId);

    public void RemoveImage(ItemImage image)
        => Context.Set<ItemImage>().Remove(image);

    public async Task ClearPrimaryFlagAsync(Guid itemId)
        => await Context.Set<ItemImage>()
            .Where(img => img.ItemId == itemId && img.IsPrimary)
            .ExecuteUpdateAsync(s => s.SetProperty(img => img.IsPrimary, false));

    public async Task ReplaceItemBarcodesAsync(Guid itemId, IReadOnlyList<ItemBarcode> barcodes)
    {
        var set = Context.Set<ItemBarcode>();

        // Detach any barcodes for this item already tracked from a prior graph load
        // (e.g. GetWithFullDetailsAsync) so the set-based delete below can't collide with
        // stale change-tracker entries and raise a phantom concurrency exception.
        var stale = Context.ChangeTracker.Entries<ItemBarcode>()
            .Where(e => e.Entity.ItemId == itemId)
            .ToList();
        foreach (var e in stale) e.State = EntityState.Detached;

        // Full replacement: wipe then re-insert. Set-based delete issues a direct DELETE
        // (no per-row affected-count check), so it never throws DbUpdateConcurrencyException.
        await set.Where(b => b.ItemId == itemId).ExecuteDeleteAsync();

        if (barcodes.Count == 0) return;

        foreach (var b in barcodes) b.ItemId = itemId;
        await set.AddRangeAsync(barcodes);
        await Context.SaveChangesAsync();
    }

    public async Task<List<Item>> GetItemsBasicAsync()
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(i => i.IsActive &&
                        i.CompanyId == companyId &&
                        i.BranchId == branchId &&
                        i.BusinessUnitId == businessUnitId &&
                        !i.IsDeleted)
            .Include(i => i.BaseUnit)
            .Include(i => i.Category)
            .Include(i => i.Brand)
            .Include(i => i.Barcodes.Where(b => b.IsPrimary))
            .Include(i => i.Prices.Where(p => p.PriceList == "Default"))
            .OrderBy(i => i.Code)
            .ToListAsync();
    }

    public async Task<(List<Item> Items, int TotalCount)> GetItemsBasicPaginatedAsync(
        int pageNumber, 
        int pageSize, 
        string? searchTerm = null,
        string? itemType = null)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        var query = DbSet
            .Where(i => i.IsActive &&
                        i.CompanyId == companyId &&
                        i.BranchId == branchId &&
                        i.BusinessUnitId == businessUnitId &&
                        !i.IsDeleted);

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(i => 
                i.Code.ToLower().Contains(searchLower) || 
                i.Name.ToLower().Contains(searchLower) ||
                (i.ShortDescription != null && i.ShortDescription.ToLower().Contains(searchLower))
            );
        }

        // Apply item type filter if provided
        if (!string.IsNullOrWhiteSpace(itemType))
        {
            query = query.Where(i => i.ItemType == itemType);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination and includes
        var items = await query
            .Include(i => i.BaseUnit)
            .Include(i => i.Category)
            .Include(i => i.Brand)
            .Include(i => i.Barcodes.Where(b => b.IsPrimary))
            .Include(i => i.Prices.Where(p => p.PriceList == "Default"))
            .OrderBy(i => i.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<Item> Items, int Total)> GetAllPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, string? sortDirection = null, bool? isActive = null, Guid? warehouseId = null, string? itemType = null)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        var query = DbSet
            .Where(i => i.CompanyId == companyId &&
                        i.BranchId == branchId &&
                        i.BusinessUnitId == businessUnitId &&
                        !i.IsDeleted);

        if (isActive != null)
            query = query.Where(i => i.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(itemType))
            query = query.Where(i => i.ItemType == itemType);

        // Warehouse filter: keep only items that currently hold (non-zero) stock in that warehouse.
        if (warehouseId.HasValue)
        {
            var whId = warehouseId.Value;
            query = query.Where(i => Context.Set<InventoryBalance>()
                .Any(b => b.ItemId == i.Id && b.WarehouseId == whId && !b.IsDeleted && b.QuantityOnHand != 0));
        }

        var search = searchTerm?.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(i =>
                (i.Code != null && i.Code.ToLower().Contains(search)) ||
                (i.Name != null && i.Name.ToLower().Contains(search)) ||
                (i.ShortDescription != null && i.ShortDescription.ToLower().Contains(search)) ||
                (i.Description != null && i.Description.ToLower().Contains(search)) ||
                i.Barcodes.Any(b => b.Barcode.ToLower().Contains(search)));

        var total = await query.CountAsync();

        var items = await query
            .Include(i => i.Images.Where(img => img.IsPrimary))
            .Include(i => i.Barcodes)
            .Include(i => i.Prices)
            .AsSplitQuery()
            .ApplyOrderNewestFirst(sortBy, sortDirection, "Code")
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<Item> Items, int Total)> GetActivePagedAsync(int pageNumber, int pageSize)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        var query = DbSet
            .Where(i => i.IsActive &&
                        i.CompanyId == companyId &&
                        i.BranchId == branchId &&
                        i.BusinessUnitId == businessUnitId &&
                        !i.IsDeleted);

        var total = await query.CountAsync();

        var items = await query
            .Include(i => i.Images.Where(img => img.IsPrimary))
            .Include(i => i.Barcodes)
            .Include(i => i.Prices)
            .AsSplitQuery()
            .OrderBy(i => i.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}

public class ItemUomConversionRepository : TenantAwareRepository<ItemUomConversion>, IItemUomConversionRepository
{
    public ItemUomConversionRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<ItemUomConversion>> GetConversionsForItemAsync(Guid itemId)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(c => c.ItemId == itemId && c.IsActive &&
                        c.CompanyId == companyId && c.BranchId == branchId &&
                        c.BusinessUnitId == businessUnitId && !c.IsDeleted)
            .Include(c => c.FromUnit)
            .Include(c => c.ToUnit)
            .ToListAsync();
    }

    public async Task<ItemUomConversion?> GetConversionAsync(Guid itemId, Guid fromUnitId, Guid toUnitId)
        => await FirstOrDefaultAsync(c => c.ItemId == itemId &&
                                          c.FromUnitId == fromUnitId &&
                                          c.ToUnitId == toUnitId &&
                                          c.IsActive);
}

// ---------------------------------------------------------------------------
// Attribute Definition
// ---------------------------------------------------------------------------

public class AttributeDefinitionRepository : TenantAwareRepository<AttributeDefinition>, IAttributeDefinitionRepository
{
    public AttributeDefinitionRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<AttributeDefinition?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(d => d.Code == code);

    public async Task<List<AttributeDefinition>> GetActiveAsync()
    {
        var defs = await FindAsync(d => d.IsActive);
        return defs.OrderBy(d => d.DisplayOrder).ToList();
    }

    public async Task<List<AttributeDefinition>> GetVariantAttributesAsync()
    {
        var defs = await FindAsync(d => d.IsVariant && d.IsActive);
        return defs.OrderBy(d => d.DisplayOrder).ToList();
    }
}

// ---------------------------------------------------------------------------
// Tax Definition
// ---------------------------------------------------------------------------

public class TaxDefinitionRepository : TenantAwareRepository<TaxDefinition>, ITaxDefinitionRepository
{
    public TaxDefinitionRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<TaxDefinition?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(t => t.Code == code);

    public async Task<List<TaxDefinition>> GetActiveAsync()
    {
        var taxes = await FindAsync(t => t.IsActive);
        return taxes.OrderBy(t => t.Code).ToList();
    }

    public async Task<List<TaxDefinition>> GetSalesTaxesAsync()
    {
        var taxes = await FindAsync(t => t.ApplyOnSales && t.IsActive);
        return taxes.OrderBy(t => t.Code).ToList();
    }

    public async Task<List<TaxDefinition>> GetPurchaseTaxesAsync()
    {
        var taxes = await FindAsync(t => t.ApplyOnPurchases && t.IsActive);
        return taxes.OrderBy(t => t.Code).ToList();
    }
}

// ---------------------------------------------------------------------------
// Brand
// ---------------------------------------------------------------------------

public class BrandRepository : TenantAwareRepository<Brand>, IBrandRepository
{
    public BrandRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Brand?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(b => b.Code == code);

    public async Task<List<Brand>> GetActiveBrandsAsync()
    {
        var brands = await FindAsync(b => b.IsActive);
        return brands.OrderBy(b => b.Name).ToList();
    }
}

// ---------------------------------------------------------------------------
// Color
// ---------------------------------------------------------------------------

public class ColorRepository : TenantAwareRepository<Color>, IColorRepository
{
    public ColorRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Color?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(c => c.Code == code);

    public async Task<List<Color>> GetActiveColorsAsync()
    {
        var colors = await FindAsync(c => c.IsActive);
        return colors.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
    }

    public async Task<List<Color>> GetByFamilyAsync(string colorFamily)
    {
        var colors = await FindAsync(c => c.ColorFamily == colorFamily && c.IsActive);
        return colors.OrderBy(c => c.DisplayOrder).ToList();
    }
}

// ---------------------------------------------------------------------------
// Size
// ---------------------------------------------------------------------------

public class SizeRepository : TenantAwareRepository<Size>, ISizeRepository
{
    public SizeRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Size?> GetByCodeAsync(string code, string sizeChart)
        => await FirstOrDefaultAsync(s => s.Code == code && s.SizeChart == sizeChart);

    public async Task<List<Size>> GetBySizeChartAsync(string sizeChart)
    {
        var sizes = await FindAsync(s => s.SizeChart == sizeChart && s.IsActive);
        return sizes.OrderBy(s => s.SortOrder).ToList();
    }
}

// ---------------------------------------------------------------------------
// Item Barcode
// ---------------------------------------------------------------------------

public class ItemBarcodeRepository : TenantAwareRepository<ItemBarcode>, IItemBarcodeRepository
{
    public ItemBarcodeRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<ItemBarcode?> GetByValueAsync(string barcode)
        => await FirstOrDefaultAsync(b => b.Barcode == barcode && b.IsActive);

    public async Task<List<ItemBarcode>> GetByItemAsync(Guid itemId)
    {
        var barcodes = await FindAsync(b => b.ItemId == itemId && b.IsActive);
        return barcodes.OrderByDescending(b => b.IsPrimary).ToList();
    }
}

// ---------------------------------------------------------------------------
// Item Price
// ---------------------------------------------------------------------------

public class ItemPriceRepository : TenantAwareRepository<ItemPrice>, IItemPriceRepository
{
    public ItemPriceRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<ItemPrice>> GetByItemAsync(Guid itemId)
    {
        var prices = await FindAsync(p => p.ItemId == itemId && p.IsActive);
        return prices.OrderBy(p => p.PriceList).ToList();
    }

    public async Task<ItemPrice?> GetByItemAndPriceListAsync(Guid itemId, Guid unitId, string priceList, string currencyCode)
        => await FirstOrDefaultAsync(p => p.ItemId == itemId &&
                                          p.UnitId == unitId &&
                                          p.PriceList == priceList &&
                                          p.CurrencyCode == currencyCode &&
                                          p.IsActive);

    public async Task<List<ItemPrice>> GetActivePricesAsync(Guid itemId, string priceList)
    {
        var now = DateTime.UtcNow;
        var prices = await FindAsync(p => p.ItemId == itemId &&
                                          p.PriceList == priceList &&
                                          p.IsActive &&
                                          (p.ValidFrom == null || p.ValidFrom <= now) &&
                                          (p.ValidTo == null || p.ValidTo >= now));
        return prices.OrderBy(p => p.CurrencyCode).ToList();
    }
}

// ---------------------------------------------------------------------------
// Item Variant
// ---------------------------------------------------------------------------

public class ItemVariantRepository : TenantAwareRepository<ItemVariant>, IItemVariantRepository
{
    public ItemVariantRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<ItemVariant?> GetByCodeAsync(string variantCode)
        => await FirstOrDefaultAsync(v => v.VariantCode == variantCode);

    public async Task<List<ItemVariant>> GetByItemAsync(Guid itemId)
    {
        var variants = await FindAsync(v => v.ItemId == itemId && v.IsActive);
        return variants.OrderBy(v => v.DisplayOrder).ToList();
    }

    public async Task<ItemVariant?> GetByItemColorSizeAsync(Guid itemId, Guid? colorId, Guid? sizeId)
        => await FirstOrDefaultAsync(v => v.ItemId == itemId &&
                                          v.ColorId == colorId &&
                                          v.SizeId == sizeId &&
                                          v.IsActive);
}

// ---------------------------------------------------------------------------
// Item Channel Listing
// ---------------------------------------------------------------------------

public class ItemChannelListingRepository : TenantAwareRepository<ItemChannelListing>, IItemChannelListingRepository
{
    public ItemChannelListingRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<ItemChannelListing?> GetByItemAndChannelAsync(Guid itemId, string channel)
        => await FirstOrDefaultAsync(l => l.ItemId == itemId && l.Channel == channel);

    public async Task<List<ItemChannelListing>> GetByItemAsync(Guid itemId)
    {
        var listings = await FindAsync(l => l.ItemId == itemId);
        return listings.OrderBy(l => l.Channel).ToList();
    }

    public async Task<List<ItemChannelListing>> GetByChannelAsync(string channel, string? status = null)
    {
        var listings = status != null
            ? await FindAsync(l => l.Channel == channel && l.ListingStatus == status)
            : await FindAsync(l => l.Channel == channel);
        return listings.OrderBy(l => l.ListingStatus).ToList();
    }
}

// ---------------------------------------------------------------------------
// Item Discount
// ---------------------------------------------------------------------------

public class ItemDiscountRepository : TenantAwareRepository<ItemDiscount>, IItemDiscountRepository
{
    public ItemDiscountRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<ItemDiscount>> GetByItemAsync(Guid itemId)
    {
        var discounts = await FindAsync(d => d.ItemId == itemId);
        return discounts.OrderBy(d => d.Priority).ToList();
    }

    public async Task<List<ItemDiscount>> GetActiveDiscountsAsync(Guid itemId, string? channel = null)
    {
        var now = DateTime.UtcNow;
        var discounts = await FindAsync(d => d.ItemId == itemId &&
                                             d.IsActive &&
                                             d.ValidFrom <= now &&
                                             d.ValidTo >= now);
        if (channel != null)
            discounts = discounts
                .Where(d => d.ApplicableChannels == null ||
                            d.ApplicableChannels.Contains(channel))
                .ToList();

        return discounts.OrderBy(d => d.Priority).ToList();
    }
}

// ---------------------------------------------------------------------------
// Item Supplier
// ---------------------------------------------------------------------------

public class ItemSupplierRepository : TenantAwareRepository<ItemSupplier>, IItemSupplierRepository
{
    public ItemSupplierRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<ItemSupplier>> GetByItemAsync(Guid itemId)
    {
        var suppliers = await FindAsync(s => s.ItemId == itemId && s.IsActive);
        return suppliers.OrderByDescending(s => s.IsPrimary).ToList();
    }

    public async Task<ItemSupplier?> GetPrimarySupplierAsync(Guid itemId)
        => await FirstOrDefaultAsync(s => s.ItemId == itemId && s.IsPrimary && s.IsActive);
}

// ---------------------------------------------------------------------------
// Item Warranty
// ---------------------------------------------------------------------------

public class ItemWarrantyRepository : TenantAwareRepository<ItemWarranty>, IItemWarrantyRepository
{
    public ItemWarrantyRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<ItemWarranty?> GetByItemAsync(Guid itemId)
        => await FirstOrDefaultAsync(w => w.ItemId == itemId && w.IsActive);
}

// ---------------------------------------------------------------------------
// Item Shipping
// ---------------------------------------------------------------------------

public class ItemShippingRepository : TenantAwareRepository<ItemShipping>, IItemShippingRepository
{
    public ItemShippingRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<ItemShipping?> GetByItemAsync(Guid itemId)
        => await FirstOrDefaultAsync(s => s.ItemId == itemId);
}

// ---------------------------------------------------------------------------
// Item SEO
// ---------------------------------------------------------------------------

public class ItemSeoRepository : TenantAwareRepository<ItemSeo>, IItemSeoRepository
{
    public ItemSeoRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<ItemSeo?> GetByItemAsync(Guid itemId)
        => await FirstOrDefaultAsync(s => s.ItemId == itemId);

    public async Task<ItemSeo?> GetBySlugAsync(string slug)
        => await FirstOrDefaultAsync(s => s.Slug == slug);
}

// ---------------------------------------------------------------------------
// Item Bundle
// ---------------------------------------------------------------------------

public class ItemBundleRepository : TenantAwareRepository<ItemBundle>, IItemBundleRepository
{
    public ItemBundleRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<ItemBundle>> GetComponentsAsync(Guid bundleItemId)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(b => b.BundleItemId == bundleItemId &&
                        b.CompanyId == companyId && b.BranchId == branchId &&
                        b.BusinessUnitId == businessUnitId && !b.IsDeleted)
            .Include(b => b.ComponentItem)
            .Include(b => b.Unit)
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<ItemBundle>> GetBundleParentsAsync(Guid componentItemId)
    {
        var bundles = await FindAsync(b => b.ComponentItemId == componentItemId);
        return bundles.OrderBy(b => b.DisplayOrder).ToList();
    }
}

// ---------------------------------------------------------------------------
// Item Substitution
// ---------------------------------------------------------------------------

public class ItemSubstitutionRepository : TenantAwareRepository<ItemSubstitution>, IItemSubstitutionRepository
{
    public ItemSubstitutionRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<ItemSubstitution>> GetSubstitutionsAsync(Guid itemId)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(s => s.ItemId == itemId && s.IsActive &&
                        s.CompanyId == companyId && s.BranchId == branchId &&
                        s.BusinessUnitId == businessUnitId && !s.IsDeleted)
            .Include(s => s.SubstituteItem)
            .OrderBy(s => s.Priority)
            .ToListAsync();
    }
}

// ---------------------------------------------------------------------------
// Warehouse & Bin
// ---------------------------------------------------------------------------

public class WarehouseRepository : TenantAwareRepository<Warehouse>, IWarehouseRepository
{
    public WarehouseRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Warehouse?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(w => w.Code == code);

    public async Task<List<Warehouse>> GetActiveWarehousesAsync()
    {
        var warehouses = await FindAsync(w => w.IsActive);
        return warehouses.OrderBy(w => w.Code).ToList();
    }

    public async Task<Warehouse?> GetWithBinsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Include(w => w.Bins)
            .FirstOrDefaultAsync(w => w.Id == id &&
                                      w.CompanyId == companyId &&
                                      w.BranchId == branchId &&
                                      w.BusinessUnitId == businessUnitId &&
                                      !w.IsDeleted);
    }
}

public class BinRepository : TenantAwareRepository<Bin>, IBinRepository
{
    public BinRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<Bin?> GetByCodeAsync(string code, Guid warehouseId)
        => await FirstOrDefaultAsync(b => b.Code == code && b.WarehouseId == warehouseId);

    public async Task<List<Bin>> GetBinsByWarehouseAsync(Guid warehouseId)
    {
        var bins = await FindAsync(b => b.WarehouseId == warehouseId);
        return bins.OrderBy(b => b.Code).ToList();
    }

    public async Task<List<Bin>> GetActiveBinsAsync(Guid warehouseId)
    {
        var bins = await FindAsync(b => b.WarehouseId == warehouseId && b.IsActive);
        return bins.OrderBy(b => b.Code).ToList();
    }
}

// ---------------------------------------------------------------------------
// Documents
// ---------------------------------------------------------------------------

public class InventoryDocumentRepository : TenantAwareRepository<InventoryDocument>, IInventoryDocumentRepository
{
    public InventoryDocumentRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<InventoryDocument?> GetByNumberAsync(string documentNumber)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.DocumentNumber == documentNumber &&
                                      d.CompanyId == companyId &&
                                      d.BranchId == branchId &&
                                      d.BusinessUnitId == businessUnitId &&
                                      !d.IsDeleted);
    }

    public async Task<InventoryDocument?> GetWithLinesAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == id &&
                                      d.CompanyId == companyId &&
                                      d.BranchId == branchId &&
                                      d.BusinessUnitId == businessUnitId &&
                                      !d.IsDeleted);
    }

    public async Task<List<InventoryDocument>> GetDocumentsByStatusAsync(string status)
    {
        var docs = await FindAsync(d => d.Status == status);
        return docs.OrderByDescending(d => d.DocumentDate).ToList();
    }

    public async Task<List<InventoryDocument>> GetDocumentsByTypeAsync(string documentType)
    {
        var docs = await FindAsync(d => d.DocumentType == documentType);
        return docs.OrderByDescending(d => d.DocumentDate).ToList();
    }

    public async Task<List<InventoryDocument>> GetDocumentsByWarehouseAsync(Guid warehouseId)
    {
        var docs = await FindAsync(d => d.ToWarehouseId == warehouseId || d.FromWarehouseId == warehouseId);
        return docs.OrderByDescending(d => d.DocumentDate).ToList();
    }

    public async Task<List<InventoryDocument>> GetDocumentsByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var docs = await FindAsync(d => d.DocumentDate >= fromDate && d.DocumentDate <= toDate);
        return docs.OrderByDescending(d => d.DocumentDate).ToList();
    }
}

public class InventoryDocumentLineRepository : TenantAwareRepository<InventoryDocumentLine>, IInventoryDocumentLineRepository
{
    public InventoryDocumentLineRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<InventoryDocumentLine>> GetLinesByDocumentAsync(Guid documentId)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(l => l.DocumentId == documentId &&
                        l.CompanyId == companyId &&
                        l.BranchId == branchId &&
                        l.BusinessUnitId == businessUnitId &&
                        !l.IsDeleted)
            .Include(l => l.Item)
            .Include(l => l.Unit)
            .OrderBy(l => l.LineNumber)
            .ToListAsync();
    }

    public async Task<List<InventoryDocumentLine>> GetLinesByItemAsync(Guid itemId)
    {
        var lines = await FindAsync(l => l.ItemId == itemId);
        return lines.OrderByDescending(l => l.Document!.DocumentDate).ToList();
    }
}

// ---------------------------------------------------------------------------
// Transactions & Balances
// ---------------------------------------------------------------------------

public class InventoryTransactionRepository : TenantAwareRepository<InventoryTransaction>, IInventoryTransactionRepository
{
    public InventoryTransactionRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<InventoryTransaction>> GetTransactionsByItemAsync(Guid itemId)
    {
        var txs = await FindAsync(t => t.ItemId == itemId);
        return txs.OrderByDescending(t => t.TransactionDate).ToList();
    }

    public async Task<List<InventoryTransaction>> GetTransactionsByWarehouseAsync(Guid warehouseId)
    {
        var txs = await FindAsync(t => t.WarehouseId == warehouseId);
        return txs.OrderByDescending(t => t.TransactionDate).ToList();
    }

    public async Task<List<InventoryTransaction>> GetTransactionsByDocumentAsync(Guid documentId)
    {
        var txs = await FindAsync(t => t.DocumentId == documentId);
        return txs.OrderByDescending(t => t.TransactionDate).ToList();
    }

    public async Task<List<InventoryTransaction>> GetTransactionsByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var txs = await FindAsync(t => t.TransactionDate >= fromDate && t.TransactionDate <= toDate);
        return txs.OrderByDescending(t => t.TransactionDate).ToList();
    }

    public async Task<List<InventoryTransaction>> GetTransactionsByTypeAsync(string transactionType)
    {
        var txs = await FindAsync(t => t.TransactionType == transactionType);
        return txs.OrderByDescending(t => t.TransactionDate).ToList();
    }

    public async Task<List<InventoryTransaction>> GetStockLedgerAsync(
        Guid itemId, Guid warehouseId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        var query = DbSet.AsNoTracking()
            .Where(t => t.ItemId == itemId &&
                        t.WarehouseId == warehouseId &&
                        t.CompanyId == companyId &&
                        t.BranchId == branchId &&
                        t.BusinessUnitId == businessUnitId &&
                        !t.IsDeleted);

        if (fromDate.HasValue)
            query = query.Where(t => t.TransactionDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.TransactionDate <= toDate.Value);

        return await query
            .Include(t => t.Document)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();
    }
}

public class InventoryBalanceRepository : TenantAwareRepository<InventoryBalance>, IInventoryBalanceRepository
{
    public InventoryBalanceRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    // Exact null-aware Bin/Variant matching: a null binId/variantId matches the warehouse/item-level
    // row (BinId/VariantId IS NULL), NOT "any" row. (The previous `binId == null || b.BinId == binId`
    // collapsed to TRUE for a null bin and returned an arbitrary balance.)
    public async Task<InventoryBalance?> GetBalanceAsync(Guid itemId, Guid warehouseId, Guid? binId = null, Guid? variantId = null)
        => await FirstOrDefaultAsync(b => b.ItemId == itemId &&
                                          b.WarehouseId == warehouseId &&
                                          b.BinId == binId &&
                                          b.VariantId == variantId);

    public async Task<List<InventoryBalance>> GetBalancesByItemAsync(Guid itemId)
    {
        var balances = await FindAsync(b => b.ItemId == itemId);
        return balances.OrderBy(b => b.WarehouseId).ToList();
    }

    public async Task<List<InventoryBalance>> GetBalancesByWarehouseAsync(Guid warehouseId)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(b => b.WarehouseId == warehouseId &&
                        b.CompanyId == companyId &&
                        b.BranchId == branchId &&
                        b.BusinessUnitId == businessUnitId &&
                        !b.IsDeleted)
            .Include(b => b.Item)
            .OrderBy(b => b.Item!.Code)
            .ToListAsync();
    }

    public async Task<List<InventoryBalance>> GetLowStockAsync(decimal? threshold = null)
    {
        var balances = threshold.HasValue
            ? await FindAsync(b => b.QuantityOnHand <= threshold.Value)
            : await FindAsync(_ => true);

        return balances.OrderBy(b => b.QuantityOnHand).ToList();
    }

    public async Task<decimal> GetTotalValueAsync(Guid warehouseId)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await DbSet
            .Where(b => b.WarehouseId == warehouseId &&
                        b.CompanyId == companyId &&
                        b.BranchId == branchId &&
                        b.BusinessUnitId == businessUnitId &&
                        !b.IsDeleted)
            .SumAsync(b => b.TotalValue);
    }

    public async Task<List<ItemStockTotalDto>> GetStockTotalsByItemAsync(Guid? warehouseId = null)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        var query = DbSet
            .Where(b => b.CompanyId == companyId &&
                        b.BranchId == branchId &&
                        b.BusinessUnitId == businessUnitId &&
                        !b.IsDeleted);
        if (warehouseId.HasValue)
            query = query.Where(b => b.WarehouseId == warehouseId.Value);
        return await query
            .GroupBy(b => b.ItemId)
            .Select(g => new ItemStockTotalDto
            {
                ItemId            = g.Key,
                QuantityOnHand    = g.Sum(x => x.QuantityOnHand),
                QuantityReserved  = g.Sum(x => x.QuantityReserved),
                QuantityAvailable = g.Sum(x => x.QuantityAvailable),
            })
            .ToListAsync();
    }
}

// ---------------------------------------------------------------------------
// Valuation & Cost Layers
// ---------------------------------------------------------------------------

public class InventoryValuationRepository : TenantAwareRepository<InventoryValuation>, IInventoryValuationRepository
{
    public InventoryValuationRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<InventoryValuation>> GetValuationsByPeriodAsync(string periodReference)
    {
        var valuations = await FindAsync(v => v.PeriodReference == periodReference);
        return valuations.OrderBy(v => v.Item!.Code).ToList();
    }

    public async Task<List<InventoryValuation>> GetValuationsByDateAsync(DateTime valuationDate)
    {
        var valuations = await FindAsync(v => v.ValuationDate.Date == valuationDate.Date);
        return valuations.OrderBy(v => v.Item!.Code).ToList();
    }

    public async Task<List<InventoryValuation>> GetValuationsByWarehouseAsync(Guid warehouseId)
    {
        var valuations = await FindAsync(v => v.WarehouseId == warehouseId);
        return valuations.OrderByDescending(v => v.ValuationDate).ToList();
    }
}

public class InventoryCostLayerRepository : TenantAwareRepository<InventoryCostLayer>, IInventoryCostLayerRepository
{
    public InventoryCostLayerRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<InventoryCostLayer>> GetCostLayersForItemAsync(Guid itemId, Guid warehouseId)
    {
        var layers = await FindAsync(l => l.ItemId == itemId && l.WarehouseId == warehouseId);
        return layers.OrderBy(l => l.ReceiptDate).ToList();
    }

    public async Task<List<InventoryCostLayer>> GetActiveCostLayersAsync(Guid itemId, Guid warehouseId)
    {
        var layers = await FindAsync(l => l.ItemId == itemId && l.WarehouseId == warehouseId &&
                                          !l.IsExhausted && l.RemainingQuantity > 0);
        return layers.OrderBy(l => l.ReceiptDate).ToList();
    }

    public async Task<InventoryCostLayer?> GetOldestCostLayerAsync(Guid itemId, Guid warehouseId)
    {
        var layers = await FindAsync(l => l.ItemId == itemId && l.WarehouseId == warehouseId &&
                                          !l.IsExhausted && l.RemainingQuantity > 0);
        return layers.OrderBy(l => l.ReceiptDate).FirstOrDefault();
    }
}

// ---------------------------------------------------------------------------
// Unit-level tracking (Serial / Lot)
// ---------------------------------------------------------------------------

public class ItemSerialRepository : TenantAwareRepository<ItemSerial>, IItemSerialRepository
{
    public ItemSerialRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<(List<ItemSerial> Items, int Total)> GetByItemPagedAsync(
        Guid itemId, int pageNumber, int pageSize, string? status = null, string? search = null)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        var query = DbSet.AsNoTracking()
            .Where(s => s.ItemId == itemId &&
                        s.CompanyId == companyId && s.BranchId == branchId && s.BusinessUnitId == businessUnitId &&
                        !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.SerialNumber.Contains(term)
                                  || (s.Imei != null && s.Imei.Contains(term))
                                  || (s.Imei2 != null && s.Imei2.Contains(term))
                                  || (s.MacAddress != null && s.MacAddress.Contains(term)));
        }

        var total = await query.CountAsync();
        var items = await query
            .Include(s => s.Warehouse)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    public async Task<ItemSerial?> GetBySerialOrImeiAsync(string value)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        var v = value.Trim();
        return await DbSet.AsNoTracking()
            .Include(s => s.Item)
            .Include(s => s.Warehouse)
            .Where(s => s.CompanyId == companyId && s.BranchId == branchId && s.BusinessUnitId == businessUnitId && !s.IsDeleted)
            .FirstOrDefaultAsync(s => s.SerialNumber == v || s.Imei == v || s.Imei2 == v || s.MacAddress == v);
    }

    public async Task<ItemSerial?> GetBySerialNumberAsync(Guid itemId, string serialNumber)
        => await FirstOrDefaultAsync(s => s.ItemId == itemId && s.SerialNumber == serialNumber);

    public async Task<List<ItemSerialHistory>> GetHistoryAsync(Guid itemSerialId)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        return await Context.Set<ItemSerialHistory>().AsNoTracking()
            .Where(h => h.ItemSerialId == itemSerialId &&
                        h.CompanyId == companyId && h.BranchId == branchId && h.BusinessUnitId == businessUnitId &&
                        !h.IsDeleted)
            .OrderBy(h => h.EventDate).ThenBy(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ItemSerial>> GetWarrantyExpiringAsync(int days)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        var cutoff = DateTime.UtcNow.AddDays(days);
        return await DbSet.AsNoTracking()
            .Include(s => s.Item)
            .Where(s => s.CompanyId == companyId && s.BranchId == branchId && s.BusinessUnitId == businessUnitId && !s.IsDeleted)
            .Where(s => s.WarrantyEndDate != null && s.WarrantyEndDate <= cutoff)
            .OrderBy(s => s.WarrantyEndDate)
            .ToListAsync();
    }

    public async Task<HashSet<string>> GetExistingSerialNumbersAsync(Guid itemId, IEnumerable<string> candidates)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        var set = candidates.ToList();
        var existing = await DbSet.AsNoTracking()
            .Where(s => s.ItemId == itemId &&
                        s.CompanyId == companyId && s.BranchId == branchId && s.BusinessUnitId == businessUnitId &&
                        !s.IsDeleted && set.Contains(s.SerialNumber))
            .Select(s => s.SerialNumber)
            .ToListAsync();
        return existing.ToHashSet();
    }

    public async Task<ItemSerial?> UpdateStatusAsync(Guid id, string newStatus, string? notes)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        // Tracked (no AsNoTracking) so the status change is persisted on the caller's SaveChanges.
        var serial = await DbSet.FirstOrDefaultAsync(s => s.Id == id &&
            s.CompanyId == companyId && s.BranchId == branchId && s.BusinessUnitId == businessUnitId && !s.IsDeleted);
        if (serial == null) return null;

        var from = serial.Status;
        serial.Status = newStatus;

        Context.Set<ItemSerialHistory>().Add(new ItemSerialHistory
        {
            CompanyId = companyId,
            BranchId = branchId,
            BusinessUnitId = businessUnitId,
            ItemSerialId = serial.Id,
            EventType = Inventory.Domain.Constants.SerialEventType.StatusChanged,
            FromStatus = from,
            ToStatus = newStatus,
            WarehouseId = serial.WarehouseId,
            EventDate = DateTime.UtcNow,
            Notes = notes,
        });
        return serial;
    }
}

public class ItemBatchRepository : TenantAwareRepository<ItemBatch>, IItemBatchRepository
{
    public ItemBatchRepository(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }

    public async Task<List<ItemBatch>> GetByItemAsync(Guid itemId)
    {
        var batches = await FindAsync(b => b.ItemId == itemId);
        // Batches with an expiry come first (soonest first); undated batches last.
        return batches
            .OrderBy(b => b.ExpiryDate == null)
            .ThenBy(b => b.ExpiryDate)
            .ThenBy(b => b.BatchNumber)
            .ToList();
    }

    public async Task<ItemBatch?> GetByNumberAsync(Guid itemId, string batchNumber)
        => await FirstOrDefaultAsync(b => b.ItemId == itemId && b.BatchNumber == batchNumber);

    public async Task<List<ItemBatch>> GetExpiringAsync(int days)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();
        var cutoff = DateTime.UtcNow.AddDays(days);
        return await DbSet.AsNoTracking()
            .Include(b => b.Item)
            .Where(b => b.CompanyId == companyId && b.BranchId == branchId && b.BusinessUnitId == businessUnitId && !b.IsDeleted)
            .Where(b => b.RemainingQuantity > 0 && b.ExpiryDate != null && b.ExpiryDate <= cutoff)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync();
    }
}

