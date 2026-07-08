using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Inventory.Api.Controllers;
using Inventory.Application.DTOs;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories.Interfaces;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using System.Linq.Expressions;

namespace Inventory.Tests.Unit.Controllers;

// ── BrandController ──────────────────────────────────────────────────────────

public class BrandControllerTests
{
    private readonly Mock<IBrandRepository> _repo = new();
    private readonly Mock<ILogger<BrandController>> _logger = new();
    private readonly BrandController _controller;

    public BrandControllerTests() => _controller = new BrandController(_repo.Object, _logger.Object);

    [Fact]
    public async Task GetAll_ReturnsOkWithBrands()
    {
        var brands = new List<Brand>
        {
            new Brand { Id = Guid.NewGuid(), Code = "NIKE", Name = "Nike", IsActive = true },
            new Brand { Id = Guid.NewGuid(), Code = "ADIDAS", Name = "Adidas", IsActive = true }
        };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Brand, bool>>>(), It.IsAny<Func<IQueryable<Brand>, IOrderedQueryable<Brand>>>(), It.IsAny<Func<IQueryable<Brand>, IQueryable<Brand>>>()))
             .ReturnsAsync((brands.AsEnumerable(), brands.Count));
        var result = await _controller.GetAll(new PaginationParams(), null);
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<BrandDto>>(ok.Value);
        Assert.Equal(2, response.Data!.Count());
    }

    [Fact]
    public async Task GetById_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Brand { Id = id, Code = "NIKE", Name = "Nike", IsActive = true });
        var result = await _controller.GetById(id);
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BrandDto>>(ok.Value);
        Assert.Equal("NIKE", response.Data!.Code);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Brand?)null);
        var result = await _controller.GetById(Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetActive_ReturnsActiveBrands()
    {
        var brands = new List<Brand> { new Brand { Id = Guid.NewGuid(), Code = "NIKE", Name = "Nike", IsActive = true } };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Brand, bool>>>(), It.IsAny<Func<IQueryable<Brand>, IOrderedQueryable<Brand>>>(), It.IsAny<Func<IQueryable<Brand>, IQueryable<Brand>>>()))
             .ReturnsAsync((brands.AsEnumerable(), brands.Count));
        var result = await _controller.GetActive(new PaginationParams());
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<BrandDto>>(ok.Value);
        Assert.Single(response.Data!);
    }

    [Fact]
    public async Task Create_NewCode_ReturnsCreated()
    {
        _repo.Setup(r => r.GetByCodeAsync("BRAND1")).ReturnsAsync((Brand?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<Brand>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.Create(new CreateBrandDto { Code = "BRAND1", Name = "Brand 1" });
        Assert.IsType<CreatedAtActionResult>(result);
        _repo.Verify(r => r.AddAsync(It.IsAny<Brand>()), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsBadRequest()
    {
        _repo.Setup(r => r.GetByCodeAsync("NIKE")).ReturnsAsync(new Brand { Code = "NIKE" });
        var result = await _controller.Create(new CreateBrandDto { Code = "NIKE", Name = "Nike" });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var brand = new Brand { Id = id, Code = "BRAND1", Name = "Old Name", IsActive = true };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(brand);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.Update(id, new UpdateBrandDto { Name = "New Name" });
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BrandDto>>(ok.Value);
        Assert.Equal("New Name", response.Data!.Name);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Brand?)null);
        var result = await _controller.Update(Guid.NewGuid(), new UpdateBrandDto());
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Brand { Id = id, Code = "BRAND1", Name = "Brand 1" });
        _repo.Setup(r => r.Delete(It.IsAny<Brand>()));
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.Delete(id);
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.True(response.Data);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Brand?)null);
        var result = await _controller.Delete(Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(result);
    }
}

// ── ColorController ──────────────────────────────────────────────────────────

public class ColorControllerTests
{
    private readonly Mock<IColorRepository> _repo = new();
    private readonly Mock<ILogger<ColorController>> _logger = new();
    private readonly ColorController _controller;

    public ColorControllerTests() => _controller = new ColorController(_repo.Object, _logger.Object);

    [Fact]
    public async Task GetAll_ReturnsOkWithColors()
    {
        var colors = new List<Color>
        {
            new Color { Id = Guid.NewGuid(), Code = "RED", Name = "Red", HexCode = "#FF0000", IsActive = true },
            new Color { Id = Guid.NewGuid(), Code = "BLUE", Name = "Blue", HexCode = "#0000FF", IsActive = true }
        };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Color, bool>>>(), It.IsAny<Func<IQueryable<Color>, IOrderedQueryable<Color>>>(), It.IsAny<Func<IQueryable<Color>, IQueryable<Color>>>()))
             .ReturnsAsync((colors.AsEnumerable(), colors.Count));
        var result = await _controller.GetAll(new PaginationParams(), null);
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<ColorDto>>(ok.Value);
        Assert.Equal(2, response.Data!.Count());
    }

    [Fact]
    public async Task GetById_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Color { Id = id, Code = "RED", Name = "Red", IsActive = true });
        var result = await _controller.GetById(id);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ApiResponse<ColorDto>>(ok.Value);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Color?)null);
        Assert.IsType<NotFoundObjectResult>(await _controller.GetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetActive_ReturnsActiveColors()
    {
        var colors = new List<Color> { new Color { Code = "RED", Name = "Red", IsActive = true } };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Color, bool>>>(), It.IsAny<Func<IQueryable<Color>, IOrderedQueryable<Color>>>(), It.IsAny<Func<IQueryable<Color>, IQueryable<Color>>>()))
             .ReturnsAsync((colors.AsEnumerable(), colors.Count));
        var result = await _controller.GetActive(new PaginationParams());
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<ColorDto>>(ok.Value);
        Assert.Single(response.Data!);
    }

    [Fact]
    public async Task GetByFamily_ReturnsMatchingColors()
    {
        var colors = new List<Color> { new Color { Code = "RED", Name = "Red", ColorFamily = "Red", IsActive = true } };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Color, bool>>>(), It.IsAny<Func<IQueryable<Color>, IOrderedQueryable<Color>>>(), It.IsAny<Func<IQueryable<Color>, IQueryable<Color>>>()))
             .ReturnsAsync((colors.AsEnumerable(), colors.Count));
        var result = await _controller.GetByFamily("Red", new PaginationParams());
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResponse<ColorDto>>(ok.Value);
        Assert.Single(response.Data!);
    }

    [Fact]
    public async Task Create_NewCode_ReturnsCreated()
    {
        _repo.Setup(r => r.GetByCodeAsync("RED")).ReturnsAsync((Color?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<Color>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.Create(new CreateColorDto { Code = "RED", Name = "Red" });
        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsBadRequest()
    {
        _repo.Setup(r => r.GetByCodeAsync("RED")).ReturnsAsync(new Color { Code = "RED" });
        Assert.IsType<BadRequestObjectResult>(await _controller.Create(new CreateColorDto { Code = "RED", Name = "Red" }));
    }

    [Fact]
    public async Task Update_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Color { Id = id, Code = "RED", Name = "Red", IsActive = true });
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.Update(id, new UpdateColorDto { HexCode = "#FF0000" });
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("#FF0000", Assert.IsType<ApiResponse<ColorDto>>(ok.Value).Data!.HexCode);
    }

    [Fact]
    public async Task Delete_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Color { Id = id, Code = "RED", Name = "Red" });
        _repo.Setup(r => r.Delete(It.IsAny<Color>()));
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await _controller.Delete(id));
    }
}

// ── SizeController ───────────────────────────────────────────────────────────

public class SizeControllerTests
{
    private readonly Mock<ISizeRepository> _repo = new();
    private readonly Mock<ILogger<SizeController>> _logger = new();
    private readonly SizeController _controller;

    public SizeControllerTests() => _controller = new SizeController(_repo.Object, _logger.Object);

    [Fact]
    public async Task GetAll_ReturnsOkWithSizes()
    {
        var sizes = new List<Size>
        {
            new Size { Id = Guid.NewGuid(), Code = "SM", Name = "Small", SizeChart = "Apparel", IsActive = true },
            new Size { Id = Guid.NewGuid(), Code = "LG", Name = "Large", SizeChart = "Apparel", IsActive = true }
        };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Size, bool>>>(), It.IsAny<Func<IQueryable<Size>, IOrderedQueryable<Size>>>(), It.IsAny<Func<IQueryable<Size>, IQueryable<Size>>>()))
             .ReturnsAsync((sizes.AsEnumerable(), sizes.Count));
        var result = await _controller.GetAll(new PaginationParams(), null);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, Assert.IsType<PaginatedResponse<SizeDto>>(ok.Value).Data!.Count());
    }

    [Fact]
    public async Task GetById_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Size { Id = id, Code = "SM", Name = "Small", SizeChart = "Apparel", IsActive = true });
        Assert.IsType<OkObjectResult>(await _controller.GetById(id));
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Size?)null);
        Assert.IsType<NotFoundObjectResult>(await _controller.GetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetBySizeChart_ReturnsMatchingSizes()
    {
        var sizes = new List<Size> { new Size { Code = "SM", SizeChart = "Apparel", IsActive = true } };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Size, bool>>>(), It.IsAny<Func<IQueryable<Size>, IOrderedQueryable<Size>>>(), It.IsAny<Func<IQueryable<Size>, IQueryable<Size>>>()))
             .ReturnsAsync((sizes.AsEnumerable(), sizes.Count));
        var result = await _controller.GetBySizeChart("Apparel", new PaginationParams());
        Assert.Single(Assert.IsType<PaginatedResponse<SizeDto>>(Assert.IsType<OkObjectResult>(result).Value).Data!);
    }

    [Fact]
    public async Task Create_NewCode_ReturnsCreated()
    {
        _repo.Setup(r => r.GetByCodeAsync("SM", "Apparel")).ReturnsAsync((Size?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<Size>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<CreatedAtActionResult>(await _controller.Create(new CreateSizeDto { Code = "SM", Name = "Small", SizeChart = "Apparel" }));
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsBadRequest()
    {
        _repo.Setup(r => r.GetByCodeAsync("SM", "Apparel")).ReturnsAsync(new Size { Code = "SM" });
        Assert.IsType<BadRequestObjectResult>(await _controller.Create(new CreateSizeDto { Code = "SM", Name = "Small", SizeChart = "Apparel" }));
    }

    [Fact]
    public async Task Update_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Size { Id = id, Code = "SM", Name = "Small", SizeChart = "Apparel", IsActive = true });
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.Update(id, new UpdateSizeDto { Name = "Small (Updated)" });
        Assert.Equal("Small (Updated)", Assert.IsType<ApiResponse<SizeDto>>(Assert.IsType<OkObjectResult>(result).Value).Data!.Name);
    }

    [Fact]
    public async Task Delete_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Size { Id = id, Code = "SM", Name = "Small", SizeChart = "Apparel" });
        _repo.Setup(r => r.Delete(It.IsAny<Size>()));
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await _controller.Delete(id));
    }
}

// ── AttributeDefinitionController ────────────────────────────────────────────

public class AttributeDefinitionControllerTests
{
    private readonly Mock<IAttributeDefinitionRepository> _repo = new();
    private readonly Mock<ILogger<AttributeDefinitionController>> _logger = new();
    private readonly AttributeDefinitionController _controller;

    public AttributeDefinitionControllerTests() => _controller = new AttributeDefinitionController(_repo.Object, _logger.Object);

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var defs = new List<AttributeDefinition> { new AttributeDefinition { Id = Guid.NewGuid(), Code = "COLOR", Name = "Color", DataType = "Text", IsActive = true } };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<AttributeDefinition, bool>>>(), It.IsAny<Func<IQueryable<AttributeDefinition>, IOrderedQueryable<AttributeDefinition>>>(), It.IsAny<Func<IQueryable<AttributeDefinition>, IQueryable<AttributeDefinition>>>()))
             .ReturnsAsync((defs.AsEnumerable(), defs.Count));
        var result = await _controller.GetAll(new PaginationParams(), null);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new AttributeDefinition { Id = id, Code = "COLOR", Name = "Color", DataType = "Text", IsActive = true });
        Assert.IsType<OkObjectResult>(await _controller.GetById(id));
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AttributeDefinition?)null);
        Assert.IsType<NotFoundObjectResult>(await _controller.GetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetActive_ReturnsActive()
    {
        var defs = new List<AttributeDefinition> { new AttributeDefinition { Code = "COLOR", Name = "Color", DataType = "Text", IsActive = true } };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<AttributeDefinition, bool>>>(), It.IsAny<Func<IQueryable<AttributeDefinition>, IOrderedQueryable<AttributeDefinition>>>(), It.IsAny<Func<IQueryable<AttributeDefinition>, IQueryable<AttributeDefinition>>>()))
             .ReturnsAsync((defs.AsEnumerable(), defs.Count));
        Assert.IsType<OkObjectResult>(await _controller.GetActive(new PaginationParams()));
    }

    [Fact]
    public async Task GetVariantAttributes_ReturnsVariantOnly()
    {
        var defs = new List<AttributeDefinition> { new AttributeDefinition { Code = "SIZE", Name = "Size", DataType = "List", IsVariant = true, IsActive = true } };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<AttributeDefinition, bool>>>(), It.IsAny<Func<IQueryable<AttributeDefinition>, IOrderedQueryable<AttributeDefinition>>>(), It.IsAny<Func<IQueryable<AttributeDefinition>, IQueryable<AttributeDefinition>>>()))
             .ReturnsAsync((defs.AsEnumerable(), defs.Count));
        var result = await _controller.GetVariantAttributes(new PaginationParams());
        Assert.Single(Assert.IsType<PaginatedResponse<AttributeDefinitionDto>>(Assert.IsType<OkObjectResult>(result).Value).Data!);
    }

    [Fact]
    public async Task Create_NewCode_ReturnsCreated()
    {
        _repo.Setup(r => r.GetByCodeAsync("COLOR")).ReturnsAsync((AttributeDefinition?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<AttributeDefinition>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<CreatedAtActionResult>(await _controller.Create(new CreateAttributeDefinitionDto { Code = "COLOR", Name = "Color", DataType = "Text" }));
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsBadRequest()
    {
        _repo.Setup(r => r.GetByCodeAsync("COLOR")).ReturnsAsync(new AttributeDefinition { Code = "COLOR" });
        Assert.IsType<BadRequestObjectResult>(await _controller.Create(new CreateAttributeDefinitionDto { Code = "COLOR", Name = "Color", DataType = "Text" }));
    }

    [Fact]
    public async Task Update_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new AttributeDefinition { Id = id, Code = "COLOR", Name = "Color", DataType = "Text", IsActive = true });
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await _controller.Update(id, new UpdateAttributeDefinitionDto { Name = "Colour" }));
    }

    [Fact]
    public async Task Delete_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new AttributeDefinition { Id = id, Code = "COLOR", Name = "Color", DataType = "Text" });
        _repo.Setup(r => r.Delete(It.IsAny<AttributeDefinition>()));
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await _controller.Delete(id));
    }
}

// ── TaxDefinitionController ───────────────────────────────────────────────────

public class TaxDefinitionControllerTests
{
    private readonly Mock<ITaxDefinitionRepository> _repo = new();
    private readonly Mock<ILogger<TaxDefinitionController>> _logger = new();
    private readonly TaxDefinitionController _controller;

    public TaxDefinitionControllerTests() => _controller = new TaxDefinitionController(_repo.Object, _logger.Object);

    private static TaxDefinition MakeTax(string code = "VAT15", string name = "VAT 15%", decimal rate = 15m) =>
        new() { Id = Guid.NewGuid(), Code = code, Name = name, TaxType = Inventory.Domain.Enums.TaxType.VAT, IsPercentage = true, Rate = rate, IsActive = true };

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var taxes = new List<TaxDefinition> { MakeTax() };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<TaxDefinition, bool>>>(), It.IsAny<Func<IQueryable<TaxDefinition>, IOrderedQueryable<TaxDefinition>>>(), It.IsAny<Func<IQueryable<TaxDefinition>, IQueryable<TaxDefinition>>>()))
             .ReturnsAsync((taxes.AsEnumerable(), taxes.Count));
        Assert.IsType<OkObjectResult>(await _controller.GetAll(new PaginationParams(), null));
    }

    [Fact]
    public async Task GetById_Found_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var tax = MakeTax(); tax.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(tax);
        Assert.IsType<OkObjectResult>(await _controller.GetById(id));
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaxDefinition?)null);
        Assert.IsType<NotFoundObjectResult>(await _controller.GetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetActive_ReturnsActive()
    {
        var taxes = new List<TaxDefinition> { MakeTax() };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<TaxDefinition, bool>>>(), It.IsAny<Func<IQueryable<TaxDefinition>, IOrderedQueryable<TaxDefinition>>>(), It.IsAny<Func<IQueryable<TaxDefinition>, IQueryable<TaxDefinition>>>()))
             .ReturnsAsync((taxes.AsEnumerable(), taxes.Count));
        Assert.IsType<OkObjectResult>(await _controller.GetActive(new PaginationParams()));
    }

    [Fact]
    public async Task GetSalesTaxes_ReturnsSalesOnly()
    {
        var t = MakeTax(); t.ApplyOnSales = true;
        var taxes = new List<TaxDefinition> { t };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<TaxDefinition, bool>>>(), It.IsAny<Func<IQueryable<TaxDefinition>, IOrderedQueryable<TaxDefinition>>>(), It.IsAny<Func<IQueryable<TaxDefinition>, IQueryable<TaxDefinition>>>()))
             .ReturnsAsync((taxes.AsEnumerable(), taxes.Count));
        var result = await _controller.GetSalesTaxes(new PaginationParams());
        Assert.Single(Assert.IsType<PaginatedResponse<TaxDefinitionDto>>(Assert.IsType<OkObjectResult>(result).Value).Data!);
    }

    [Fact]
    public async Task GetPurchaseTaxes_ReturnsPurchaseOnly()
    {
        var t = MakeTax(); t.ApplyOnPurchases = true;
        var taxes = new List<TaxDefinition> { t };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<TaxDefinition, bool>>>(), It.IsAny<Func<IQueryable<TaxDefinition>, IOrderedQueryable<TaxDefinition>>>(), It.IsAny<Func<IQueryable<TaxDefinition>, IQueryable<TaxDefinition>>>()))
             .ReturnsAsync((taxes.AsEnumerable(), taxes.Count));
        Assert.IsType<OkObjectResult>(await _controller.GetPurchaseTaxes(new PaginationParams()));
    }

    [Fact]
    public async Task Create_NewCode_ReturnsCreated()
    {
        _repo.Setup(r => r.GetByCodeAsync("VAT15")).ReturnsAsync((TaxDefinition?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<TaxDefinition>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<CreatedAtActionResult>(await _controller.Create(new CreateTaxDefinitionDto
        {
            Code = "VAT15", Name = "VAT 15%", TaxType = "VAT", Rate = 15m, IsPercentage = true, InclusionType = "Exclusive"
        }));
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsBadRequest()
    {
        _repo.Setup(r => r.GetByCodeAsync("VAT15")).ReturnsAsync(MakeTax());
        Assert.IsType<BadRequestObjectResult>(await _controller.Create(new CreateTaxDefinitionDto
        {
            Code = "VAT15", Name = "VAT 15%", TaxType = "VAT", Rate = 15m, InclusionType = "Exclusive"
        }));
    }

    [Fact]
    public async Task Update_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var tax = MakeTax(); tax.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(tax);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.Update(id, new UpdateTaxDefinitionDto { Rate = 18m });
        Assert.Equal(18m, Assert.IsType<ApiResponse<TaxDefinitionDto>>(Assert.IsType<OkObjectResult>(result).Value).Data!.Rate);
    }

    [Fact]
    public async Task Delete_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var tax = MakeTax(); tax.Id = id;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(tax);
        _repo.Setup(r => r.Delete(It.IsAny<TaxDefinition>()));
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await _controller.Delete(id));
    }
}

// ── UnitController — Update / Delete tests ───────────────────────────────────

public class UnitControllerUpdateDeleteTests
{
    private readonly Mock<IUnitRepository> _unitRepo = new();
    private readonly Mock<IItemUomConversionRepository> _convRepo = new();
    private readonly Mock<ILogger<UnitController>> _logger = new();
    private readonly UnitController _controller;

    public UnitControllerUpdateDeleteTests() =>
        _controller = new UnitController(_unitRepo.Object, _convRepo.Object, _logger.Object);

    [Fact]
    public async Task Update_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _unitRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Inventory.Domain.Entities.Unit { Id = id, Code = "PCS", Name = "Pieces", IsActive = true });
        _unitRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.Update(id, new UpdateUnitDto { Name = "Piece" });
        Assert.Equal("Piece", Assert.IsType<ApiResponse<UnitDto>>(Assert.IsType<OkObjectResult>(result).Value).Data!.Name);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        _unitRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Inventory.Domain.Entities.Unit?)null);
        Assert.IsType<NotFoundObjectResult>(await _controller.Update(Guid.NewGuid(), new UpdateUnitDto()));
    }

    [Fact]
    public async Task Update_DuplicateCode_ReturnsBadRequest()
    {
        var id = Guid.NewGuid();
        _unitRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Inventory.Domain.Entities.Unit { Id = id, Code = "PCS", Name = "Pieces", IsActive = true });
        _unitRepo.Setup(r => r.GetByCodeAsync("BOX")).ReturnsAsync(new Inventory.Domain.Entities.Unit { Code = "BOX" });
        Assert.IsType<BadRequestObjectResult>(await _controller.Update(id, new UpdateUnitDto { Code = "BOX" }));
    }

    [Fact]
    public async Task Delete_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _unitRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Inventory.Domain.Entities.Unit { Id = id, Code = "PCS", Name = "Pieces" });
        _unitRepo.Setup(r => r.Delete(It.IsAny<Inventory.Domain.Entities.Unit>()));
        _unitRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await _controller.Delete(id));
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        _unitRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Inventory.Domain.Entities.Unit?)null);
        Assert.IsType<NotFoundObjectResult>(await _controller.Delete(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateConversion_NewConversion_ReturnsCreated()
    {
        var itemId = Guid.NewGuid();
        _convRepo.Setup(r => r.GetConversionAsync(itemId, It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((ItemUomConversion?)null);
        _convRepo.Setup(r => r.AddAsync(It.IsAny<ItemUomConversion>())).Returns(Task.CompletedTask);
        _convRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.CreateConversion(itemId, new CreateItemUomConversionDto { FromUnitId = Guid.NewGuid(), ToUnitId = Guid.NewGuid(), ConversionFactor = 12 });
        Assert.Equal(201, ((ObjectResult)result).StatusCode);
    }

    [Fact]
    public async Task DeleteConversion_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _convRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new ItemUomConversion { Id = id });
        _convRepo.Setup(r => r.Delete(It.IsAny<ItemUomConversion>()));
        _convRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await _controller.DeleteConversion(id));
    }
}

// ── WarehouseController — Update / Delete / Bin tests ────────────────────────

public class WarehouseControllerUpdateDeleteTests
{
    private readonly Mock<IWarehouseRepository> _whRepo = new();
    private readonly Mock<IBinRepository> _binRepo = new();
    private readonly Mock<ILogger<WarehouseController>> _logger = new();
    private readonly WarehouseController _controller;

    public WarehouseControllerUpdateDeleteTests() =>
        _controller = new WarehouseController(_whRepo.Object, _binRepo.Object, _logger.Object);

    [Fact]
    public async Task Update_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _whRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Warehouse { Id = id, Code = "WH01", Name = "Main", IsActive = true });
        _whRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.Update(id, new UpdateWarehouseDto { Name = "Updated Warehouse" });
        Assert.Equal("Updated Warehouse", Assert.IsType<ApiResponse<WarehouseDto>>(Assert.IsType<OkObjectResult>(result).Value).Data!.Name);
    }

    [Fact]
    public async Task Delete_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _whRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Warehouse { Id = id, Code = "WH01", Name = "Main" });
        _whRepo.Setup(r => r.Delete(It.IsAny<Warehouse>()));
        _whRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await _controller.Delete(id));
    }

    [Fact]
    public async Task GetBins_ReturnsAllBinsForWarehouse()
    {
        var warehouseId = Guid.NewGuid();
        var bins = new List<Bin> { new Bin { Id = Guid.NewGuid(), WarehouseId = warehouseId, Code = "A1", Name = "Aisle A Rack 1", IsActive = true } };
        _binRepo.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Bin, bool>>>(), It.IsAny<Func<IQueryable<Bin>, IOrderedQueryable<Bin>>>(), It.IsAny<Func<IQueryable<Bin>, IQueryable<Bin>>>()))
                .ReturnsAsync((bins.AsEnumerable(), bins.Count));
        var result = await _controller.GetBins(warehouseId, new PaginationParams());
        Assert.Single(Assert.IsType<PaginatedResponse<BinDto>>(Assert.IsType<OkObjectResult>(result).Value).Data!);
    }

    [Fact]
    public async Task CreateBin_ValidWarehouse_ReturnsCreated()
    {
        var warehouseId = Guid.NewGuid();
        _whRepo.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync(new Warehouse { Id = warehouseId, Code = "WH01", Name = "Main" });
        _binRepo.Setup(r => r.GetByCodeAsync("A1", warehouseId)).ReturnsAsync((Bin?)null);
        _binRepo.Setup(r => r.AddAsync(It.IsAny<Bin>())).Returns(Task.CompletedTask);
        _binRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.CreateBin(warehouseId, new CreateBinDto { Code = "A1", Name = "Aisle A1", WarehouseId = warehouseId });
        Assert.Equal(201, ((ObjectResult)result).StatusCode);
    }

    [Fact]
    public async Task CreateBin_WarehouseNotFound_Returns404()
    {
        var warehouseId = Guid.NewGuid();
        _whRepo.Setup(r => r.GetByIdAsync(warehouseId)).ReturnsAsync((Warehouse?)null);
        Assert.IsType<NotFoundObjectResult>(await _controller.CreateBin(warehouseId, new CreateBinDto { Code = "A1", Name = "A1", WarehouseId = warehouseId }));
    }

    [Fact]
    public async Task UpdateBin_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _binRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Bin { Id = id, WarehouseId = Guid.NewGuid(), Code = "A1", Name = "A1", IsActive = true });
        _binRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.UpdateBin(id, new UpdateBinDto { Name = "Updated Bin" });
        Assert.Equal("Updated Bin", Assert.IsType<ApiResponse<BinDto>>(Assert.IsType<OkObjectResult>(result).Value).Data!.Name);
    }

    [Fact]
    public async Task DeleteBin_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _binRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Bin { Id = id, Code = "A1", Name = "A1" });
        _binRepo.Setup(r => r.Delete(It.IsAny<Bin>()));
        _binRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await _controller.DeleteBin(id));
    }
}

// ── ItemCategoryController — Update / Delete tests ────────────────────────────

public class ItemCategoryControllerUpdateDeleteTests
{
    private readonly Mock<IItemCategoryRepository>       _repo      = new();
    private readonly Mock<IEventPublisher>               _publisher = new();
    private readonly Mock<ILogger<ItemCategoryController>> _logger  = new();
    private readonly ItemCategoryController _controller;

    public ItemCategoryControllerUpdateDeleteTests() =>
        _controller = new ItemCategoryController(_repo.Object, _publisher.Object, _logger.Object);

    [Fact]
    public async Task Update_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new ItemCategory { Id = id, Code = "ELEC", Name = "Electronics", IsActive = true });
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var result = await _controller.Update(id, new UpdateItemCategoryDto { Name = "Electronics & Gadgets" });
        Assert.Equal("Electronics & Gadgets", Assert.IsType<ApiResponse<ItemCategoryDto>>(Assert.IsType<OkObjectResult>(result).Value).Data!.Name);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ItemCategory?)null);
        Assert.IsType<NotFoundObjectResult>(await _controller.Update(Guid.NewGuid(), new UpdateItemCategoryDto()));
    }

    [Fact]
    public async Task Delete_Existing_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new ItemCategory { Id = id, Code = "ELEC", Name = "Electronics" });
        _repo.Setup(r => r.Delete(It.IsAny<ItemCategory>()));
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        Assert.IsType<OkObjectResult>(await _controller.Delete(id));
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ItemCategory?)null);
        Assert.IsType<NotFoundObjectResult>(await _controller.Delete(Guid.NewGuid()));
    }
}
