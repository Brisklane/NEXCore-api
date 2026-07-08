using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Implementations;
using Inventory.Domain.Entities;

namespace Inventory.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for all new master-data repositories:
/// Brand, Color, Size, AttributeDefinition, TaxDefinition, Bin
/// </summary>
public class MasterDataRepositoryIntegrationTests
{
    private readonly Guid _companyId  = Guid.NewGuid();
    private readonly Guid _branchId   = Guid.NewGuid();
    private readonly Guid _businessId = Guid.NewGuid();

    private InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"MasterData_{Guid.NewGuid()}")
            .Options;
        return new InventoryDbContext(options);
    }

    private IHttpContextAccessor BuildAccessor()
    {
        var claims = new[]
        {
            new Claim("CompanyId",      _companyId.ToString()),
            new Claim("BranchId",       _branchId.ToString()),
            new Claim("BusinessUnitId", _businessId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var user     = new ClaimsPrincipal(identity);
        var ctx      = new DefaultHttpContext { User = user };
        var mock     = new Mock<IHttpContextAccessor>();
        mock.Setup(x => x.HttpContext).Returns(ctx);
        return mock.Object;
    }

    // ?? Brand ?????????????????????????????????????????????????????????????

    [Fact]
    public async Task BrandRepository_AddAndGetByCode_Works()
    {
        await using var ctx  = CreateContext();
        var repo = new BrandRepository(ctx, BuildAccessor());

        var brand = new Brand { Code = "NIKE", Name = "Nike", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId };
        await repo.AddAsync(brand);
        await repo.SaveChangesAsync();

        var found = await repo.GetByCodeAsync("NIKE");
        Assert.NotNull(found);
        Assert.Equal("Nike", found.Name);
    }

    [Fact]
    public async Task BrandRepository_GetActiveBrands_ReturnsOnlyActive()
    {
        await using var ctx  = CreateContext();
        var repo = new BrandRepository(ctx, BuildAccessor());

        ctx.Brands.AddRange(
            new Brand { Code = "ACTIVE", Name = "Active Brand", IsActive = true,  CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId },
            new Brand { Code = "INACTIVE", Name = "Inactive",   IsActive = false, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId }
        );
        await ctx.SaveChangesAsync();

        var active = await repo.GetActiveBrandsAsync();
        Assert.Single(active);
        Assert.Equal("ACTIVE", active[0].Code);
    }

    [Fact]
    public async Task BrandRepository_UpdateBrand_PersistsChanges()
    {
        await using var ctx  = CreateContext();
        var repo = new BrandRepository(ctx, BuildAccessor());

        var brand = new Brand { Code = "BRAND1", Name = "Old Name", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId };
        await repo.AddAsync(brand);
        await repo.SaveChangesAsync();

        brand.Name = "New Name";
        await repo.SaveChangesAsync();

        var updated = await repo.GetByCodeAsync("BRAND1");
        Assert.Equal("New Name", updated!.Name);
    }

    [Fact]
    public async Task BrandRepository_SoftDelete_HidesFromGetAll()
    {
        await using var ctx  = CreateContext();
        var repo = new BrandRepository(ctx, BuildAccessor());

        var brand = new Brand { Code = "DEL", Name = "Delete Me", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId };
        await repo.AddAsync(brand);
        await repo.SaveChangesAsync();

        repo.Delete(brand);
        await repo.SaveChangesAsync();

        var all = await repo.GetAllAsync();
        Assert.Empty(all);
    }

    // ?? Color ?????????????????????????????????????????????????????????????

    [Fact]
    public async Task ColorRepository_AddAndGetByCode_Works()
    {
        await using var ctx  = CreateContext();
        var repo = new ColorRepository(ctx, BuildAccessor());

        var color = new Color { Code = "RED", Name = "Red", HexCode = "#FF0000", ColorFamily = "Warm", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId };
        await repo.AddAsync(color);
        await repo.SaveChangesAsync();

        var found = await repo.GetByCodeAsync("RED");
        Assert.NotNull(found);
        Assert.Equal("#FF0000", found.HexCode);
    }

    [Fact]
    public async Task ColorRepository_GetActiveColors_ReturnsActiveOnly()
    {
        await using var ctx  = CreateContext();
        var repo = new ColorRepository(ctx, BuildAccessor());

        ctx.Colors.AddRange(
            new Color { Code = "RED",   Name = "Red",   IsActive = true,  CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId },
            new Color { Code = "GREY",  Name = "Grey",  IsActive = false, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId }
        );
        await ctx.SaveChangesAsync();

        var active = await repo.GetActiveColorsAsync();
        Assert.Single(active);
        Assert.Equal("RED", active[0].Code);
    }

    [Fact]
    public async Task ColorRepository_GetByFamily_FiltersCorrectly()
    {
        await using var ctx  = CreateContext();
        var repo = new ColorRepository(ctx, BuildAccessor());

        ctx.Colors.AddRange(
            new Color { Code = "RED",  Name = "Red",  ColorFamily = "Warm", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId },
            new Color { Code = "BLUE", Name = "Blue", ColorFamily = "Cool", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId }
        );
        await ctx.SaveChangesAsync();

        var warm = await repo.GetByFamilyAsync("Warm");
        Assert.Single(warm);
        Assert.Equal("RED", warm[0].Code);
    }

    // ?? Size ??????????????????????????????????????????????????????????????

    [Fact]
    public async Task SizeRepository_AddAndGetByCode_Works()
    {
        await using var ctx  = CreateContext();
        var repo = new SizeRepository(ctx, BuildAccessor());

        var size = new Size { Code = "SM", Name = "Small", SizeChart = "Apparel", SortOrder = 1, IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId };
        await repo.AddAsync(size);
        await repo.SaveChangesAsync();

        var found = await repo.GetByCodeAsync("SM", "Apparel");
        Assert.NotNull(found);
        Assert.Equal("Small", found.Name);
    }

    [Fact]
    public async Task SizeRepository_GetBySizeChart_ReturnsSortedBySortOrder()
    {
        await using var ctx  = CreateContext();
        var repo = new SizeRepository(ctx, BuildAccessor());

        ctx.Sizes.AddRange(
            new Size { Code = "LG", Name = "Large",  SizeChart = "Apparel", SortOrder = 3, IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId },
            new Size { Code = "SM", Name = "Small",  SizeChart = "Apparel", SortOrder = 1, IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId },
            new Size { Code = "MD", Name = "Medium", SizeChart = "Apparel", SortOrder = 2, IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId }
        );
        await ctx.SaveChangesAsync();

        var sizes = await repo.GetBySizeChartAsync("Apparel");
        Assert.Equal(3, sizes.Count);
        Assert.Equal("SM", sizes[0].Code);
        Assert.Equal("MD", sizes[1].Code);
        Assert.Equal("LG", sizes[2].Code);
    }

    [Fact]
    public async Task SizeRepository_GetBySizeChart_DoesNotReturnOtherCharts()
    {
        await using var ctx  = CreateContext();
        var repo = new SizeRepository(ctx, BuildAccessor());

        ctx.Sizes.AddRange(
            new Size { Code = "SM",   Name = "Small",  SizeChart = "Apparel",  SortOrder = 1, IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId },
            new Size { Code = "UK7",  Name = "UK 7",   SizeChart = "Footwear", SortOrder = 1, IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId }
        );
        await ctx.SaveChangesAsync();

        var apparel = await repo.GetBySizeChartAsync("Apparel");
        Assert.Single(apparel);
    }

    // ?? AttributeDefinition ???????????????????????????????????????????????

    [Fact]
    public async Task AttributeDefinitionRepository_AddAndGetByCode_Works()
    {
        await using var ctx  = CreateContext();
        var repo = new AttributeDefinitionRepository(ctx, BuildAccessor());

        var def = new AttributeDefinition { Code = "COLOR", Name = "Color", DataType = "Text", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId };
        await repo.AddAsync(def);
        await repo.SaveChangesAsync();

        var found = await repo.GetByCodeAsync("COLOR");
        Assert.NotNull(found);
        Assert.Equal("Color", found.Name);
    }

    [Fact]
    public async Task AttributeDefinitionRepository_GetVariantAttributes_ReturnsVariantOnly()
    {
        await using var ctx  = CreateContext();
        var repo = new AttributeDefinitionRepository(ctx, BuildAccessor());

        ctx.AttributeDefinitions.AddRange(
            new AttributeDefinition { Code = "COLOR",    Name = "Color",    DataType = "Text", IsVariant = true,  IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId },
            new AttributeDefinition { Code = "MATERIAL", Name = "Material", DataType = "Text", IsVariant = false, IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId }
        );
        await ctx.SaveChangesAsync();

        var variants = await repo.GetVariantAttributesAsync();
        Assert.Single(variants);
        Assert.Equal("COLOR", variants[0].Code);
    }

    // ?? TaxDefinition ?????????????????????????????????????????????????????????

    private TaxDefinition MakeTaxDef(string code, bool applyOnSales = true, bool applyOnPurchases = true) =>
        new()
        {
            Code = code, Name = code,
            TaxType = Inventory.Domain.Enums.TaxType.VAT,
            IsPercentage = true,
            Rate = 15m,
            InclusionType = Inventory.Domain.Enums.TaxInclusionType.Exclusive,
            ApplyOnSales = applyOnSales,
            ApplyOnPurchases = applyOnPurchases,
            IsActive = true,
            ValidFrom = DateTime.UtcNow,
            CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId
        };

    [Fact]
    public async Task TaxDefinitionRepository_AddAndGetByCode_Works()
    {
        await using var ctx  = CreateContext();
        var repo = new TaxDefinitionRepository(ctx, BuildAccessor());

        await repo.AddAsync(MakeTaxDef("VAT15"));
        await repo.SaveChangesAsync();

        var found = await repo.GetByCodeAsync("VAT15");
        Assert.NotNull(found);
        Assert.Equal(15m, found.Rate);
    }

    [Fact]
    public async Task TaxDefinitionRepository_GetSalesTaxes_ReturnsSalesOnly()
    {
        await using var ctx  = CreateContext();
        var repo = new TaxDefinitionRepository(ctx, BuildAccessor());

        ctx.TaxDefinitions.AddRange(
            MakeTaxDef("VAT15",   applyOnSales: true,  applyOnPurchases: false),
            MakeTaxDef("IMPORT5", applyOnSales: false, applyOnPurchases: true)
        );
        await ctx.SaveChangesAsync();

        var salesTaxes = await repo.GetSalesTaxesAsync();
        Assert.Single(salesTaxes);
        Assert.Equal("VAT15", salesTaxes[0].Code);
    }

    [Fact]
    public async Task TaxDefinitionRepository_GetPurchaseTaxes_ReturnsPurchaseOnly()
    {
        await using var ctx  = CreateContext();
        var repo = new TaxDefinitionRepository(ctx, BuildAccessor());

        ctx.TaxDefinitions.AddRange(
            MakeTaxDef("VAT15",   applyOnSales: true,  applyOnPurchases: false),
            MakeTaxDef("IMPORT5", applyOnSales: false, applyOnPurchases: true)
        );
        await ctx.SaveChangesAsync();

        var purchaseTaxes = await repo.GetPurchaseTaxesAsync();
        Assert.Single(purchaseTaxes);
        Assert.Equal("IMPORT5", purchaseTaxes[0].Code);
    }

    // ?? Bin (via WarehouseRepository + BinRepository) ?????????????????????

    [Fact]
    public async Task BinRepository_CreateAndGetByCode_Works()
    {
        await using var ctx  = CreateContext();
        var binRepo = new BinRepository(ctx, BuildAccessor());

        var warehouse = new Warehouse { Code = "WH01", Name = "Main Warehouse", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId };
        ctx.Warehouses.Add(warehouse);
        await ctx.SaveChangesAsync();

        var bin = new Bin { WarehouseId = warehouse.Id, Code = "A1", Name = "Aisle A Rack 1", Aisle = "A", Rack = "1", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId };
        await binRepo.AddAsync(bin);
        await binRepo.SaveChangesAsync();

        var found = await binRepo.GetByCodeAsync("A1", warehouse.Id);
        Assert.NotNull(found);
        Assert.Equal("Aisle A Rack 1", found.Name);
    }

    [Fact]
    public async Task BinRepository_GetBinsByWarehouse_ReturnsAllBins()
    {
        await using var ctx  = CreateContext();
        var binRepo = new BinRepository(ctx, BuildAccessor());

        var warehouse = new Warehouse { Code = "WH01", Name = "Main Warehouse", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId };
        ctx.Warehouses.Add(warehouse);
        await ctx.SaveChangesAsync();

        ctx.Bins.AddRange(
            new Bin { WarehouseId = warehouse.Id, Code = "A1", Name = "A1", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId },
            new Bin { WarehouseId = warehouse.Id, Code = "A2", Name = "A2", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId },
            new Bin { WarehouseId = warehouse.Id, Code = "B1", Name = "B1", IsActive = false, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId }
        );
        await ctx.SaveChangesAsync();

        var all    = await binRepo.GetBinsByWarehouseAsync(warehouse.Id);
        var active = await binRepo.GetActiveBinsAsync(warehouse.Id);

        Assert.Equal(3, all.Count);
        Assert.Equal(2, active.Count);
    }

    [Fact]
    public async Task BinRepository_SoftDelete_HidesBinFromList()
    {
        await using var ctx  = CreateContext();
        var binRepo = new BinRepository(ctx, BuildAccessor());

        var warehouse = new Warehouse { Code = "WH01", Name = "Main", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId };
        ctx.Warehouses.Add(warehouse);
        await ctx.SaveChangesAsync();

        var bin = new Bin { WarehouseId = warehouse.Id, Code = "A1", Name = "A1", IsActive = true, CompanyId = _companyId, BranchId = _branchId, BusinessUnitId = _businessId };
        await binRepo.AddAsync(bin);
        await binRepo.SaveChangesAsync();

        binRepo.Delete(bin);
        await binRepo.SaveChangesAsync();

        var all = await binRepo.GetBinsByWarehouseAsync(warehouse.Id);
        Assert.Empty(all);
    }
}
