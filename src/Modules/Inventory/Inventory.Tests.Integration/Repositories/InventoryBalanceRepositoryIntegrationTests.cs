using Xunit;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Implementations;
using Inventory.Domain.Entities;

namespace Inventory.Tests.Integration.Repositories;

public class InventoryBalanceRepositoryIntegrationTests
{
    private readonly InventoryDbContext _context;
    private readonly InventoryBalanceRepository _repository;
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _businessUnitId = Guid.NewGuid();

    public InventoryBalanceRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: $"InventoryDb_{Guid.NewGuid()}")
            .Options;

        _context = new InventoryDbContext(options);
        _repository = new InventoryBalanceRepository(_context, BuildHttpContextAccessor());
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

    private InventoryBalance BuildBalance(Guid itemId, Guid warehouseId,
        decimal qtyOnHand, decimal avgCost, decimal totalValue,
        decimal qtyReserved = 0) => new()
    {
        ItemId = itemId,
        WarehouseId = warehouseId,
        QuantityOnHand = qtyOnHand,
        QuantityReserved = qtyReserved,
        QuantityAvailable = qtyOnHand - qtyReserved,
        AverageCost = avgCost,
        TotalValue = totalValue,
        CompanyId = _companyId,
        BranchId = _branchId,
        BusinessUnitId = _businessUnitId
    };

    [Fact]
    public async Task GetBalanceAsync_WithValidItemAndWarehouse_ShouldReturnBalance()
    {
        // Arrange
        var unit = new Unit { Code = "PCS", Name = "Pieces", DisplayOrder = 1, IsActive = true };
        _context.Units.Add(unit);

        var warehouse = new Warehouse { Code = "WH001", Name = "Main Warehouse", IsActive = true, WarehouseType = "Main" };
        _context.Warehouses.Add(warehouse);

        var item = new Item { Code = "ITEM001", Name = "Test Item", BaseUnitId = unit.Id, IsActive = true };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        _context.InventoryBalances.Add(BuildBalance(item.Id, warehouse.Id, 100, 50, 5000, qtyReserved: 10));
        await _context.SaveChangesAsync();

        // Act
        var retrievedBalance = await _repository.GetBalanceAsync(item.Id, warehouse.Id);

        // Assert
        Assert.NotNull(retrievedBalance);
        Assert.Equal(100, retrievedBalance.QuantityOnHand);
        Assert.Equal(50, retrievedBalance.AverageCost);
    }

    [Fact]
    public async Task GetLowStockAsync_ShouldReturnItemsBelowThreshold()
    {
        // Arrange
        var unit = new Unit { Code = "PCS", Name = "Pieces", DisplayOrder = 1, IsActive = true };
        _context.Units.Add(unit);

        var warehouse = new Warehouse { Code = "WH001", Name = "Main Warehouse", IsActive = true, WarehouseType = "Main" };
        _context.Warehouses.Add(warehouse);

        var item = new Item { Code = "ITEM001", Name = "Test Item", BaseUnitId = unit.Id, IsActive = true };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        _context.InventoryBalances.AddRange(
            BuildBalance(item.Id, warehouse.Id, qtyOnHand: 5,   avgCost: 50, totalValue: 250),
            BuildBalance(item.Id, warehouse.Id, qtyOnHand: 100, avgCost: 50, totalValue: 5000));
        await _context.SaveChangesAsync();

        // Act
        var lowStockItems = await _repository.GetLowStockAsync(50);

        // Assert
        Assert.Single(lowStockItems);
        Assert.Equal(5, lowStockItems.First().QuantityOnHand);
    }

    [Fact]
    public async Task GetTotalValueAsync_ShouldSumAllBalancesInWarehouse()
    {
        // Arrange
        var unit = new Unit { Code = "PCS", Name = "Pieces", DisplayOrder = 1, IsActive = true };
        _context.Units.Add(unit);

        var warehouse = new Warehouse { Code = "WH001", Name = "Main Warehouse", IsActive = true, WarehouseType = "Main" };
        _context.Warehouses.Add(warehouse);

        var item1 = new Item { Code = "ITEM001", Name = "Item 1", BaseUnitId = unit.Id, IsActive = true };
        var item2 = new Item { Code = "ITEM002", Name = "Item 2", BaseUnitId = unit.Id, IsActive = true };
        _context.Items.AddRange(item1, item2);
        await _context.SaveChangesAsync();

        _context.InventoryBalances.AddRange(
            BuildBalance(item1.Id, warehouse.Id, qtyOnHand: 100, avgCost: 50, totalValue: 5000),
            BuildBalance(item2.Id, warehouse.Id, qtyOnHand: 50,  avgCost: 50, totalValue: 2500));
        await _context.SaveChangesAsync();

        // Act
        var totalValue = await _repository.GetTotalValueAsync(warehouse.Id);

        // Assert
        Assert.Equal(7500, totalValue);
    }
}
