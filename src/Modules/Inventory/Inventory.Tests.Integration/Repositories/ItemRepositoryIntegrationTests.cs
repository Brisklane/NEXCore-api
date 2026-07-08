using Xunit;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Implementations;
using Inventory.Domain.Entities;

namespace Inventory.Tests.Integration.Repositories;

public class ItemRepositoryIntegrationTests
{
    private readonly InventoryDbContext _context;
    private readonly ItemRepository _repository;
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _businessUnitId = Guid.NewGuid();

    public ItemRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: $"InventoryDb_{Guid.NewGuid()}")
            .Options;

        _context = new InventoryDbContext(options);
        _repository = new ItemRepository(_context, BuildHttpContextAccessor());
    }

    private IHttpContextAccessor BuildHttpContextAccessor()
    {
        var claims = new[]
        {
            new Claim("CompanyId", _companyId.ToString()),
            new Claim("BranchId", _branchId.ToString()),
            new Claim("BusinessUnitId", _businessUnitId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var user = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = user };

        var mock = new Mock<IHttpContextAccessor>();
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock.Object;
    }

    private Item BuildItem(Unit unit, string code, string name, bool isActive = true) => new()
    {
        Code = code,
        Name = name,
        BaseUnitId = unit.Id,
        ItemType = "Inventory",
        IsActive = isActive,
        CompanyId = _companyId,
        BranchId = _branchId,
        BusinessUnitId = _businessUnitId
    };

    [Fact]
    public async Task AddAsync_WithValidItem_ShouldAddToDatabase()
    {
        // Arrange
        var unit = new Unit { Code = "PCS", Name = "Pieces", DisplayOrder = 1, IsActive = true };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var item = BuildItem(unit, "ITEM001", "Test Item");

        // Act
        await _repository.AddAsync(item);
        await _repository.SaveChangesAsync();

        // Assert
        var savedItem = await _repository.GetByCodeAsync("ITEM001");
        Assert.NotNull(savedItem);
        Assert.Equal("ITEM001", savedItem.Code);
        Assert.Equal("Test Item", savedItem.Name);
    }

    [Fact]
    public async Task GetActiveItemsAsync_ShouldReturnOnlyActiveItems()
    {
        // Arrange
        var unit = new Unit { Code = "PCS", Name = "Pieces", DisplayOrder = 1, IsActive = true };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        _context.Items.AddRange(
            BuildItem(unit, "ACTIVE001", "Active Item", isActive: true),
            BuildItem(unit, "INACTIVE001", "Inactive Item", isActive: false));
        await _context.SaveChangesAsync();

        // Act
        var activeItems = await _repository.GetActiveItemsAsync();

        // Assert
        Assert.Single(activeItems);
        Assert.Equal("ACTIVE001", activeItems.First().Code);
    }

    [Fact]
    public async Task GetByCodeAsync_WithValidCode_ShouldReturnItem()
    {
        // Arrange
        var unit = new Unit { Code = "PCS", Name = "Pieces", DisplayOrder = 1, IsActive = true };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        _context.Items.Add(BuildItem(unit, "SEARCH001", "Search Item"));
        await _context.SaveChangesAsync();

        // Act
        var foundItem = await _repository.GetByCodeAsync("SEARCH001");

        // Assert
        Assert.NotNull(foundItem);
        Assert.Equal("SEARCH001", foundItem.Code);
    }
}
