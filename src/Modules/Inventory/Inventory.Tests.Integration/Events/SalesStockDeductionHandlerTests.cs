using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Events;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Services;
using Nexcore.SharedKernel.Events;

namespace Inventory.Tests.Integration.Events;

/// <summary>
/// Verifies the POS/Delivery stock-deduction handler actually decrements the REAL balance row
/// (the bug was that it created phantom rows), routes variants correctly, and guards empty warehouses.
/// </summary>
public class SalesStockDeductionHandlerTests
{
    private readonly InventoryDbContext _ctx;
    private readonly SalesStockDeductionHandler _handler;

    private readonly Guid _company = Guid.NewGuid();
    private readonly Guid _branch = Guid.NewGuid();
    private readonly Guid _bu = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();
    private readonly Guid _item = Guid.NewGuid();
    private readonly Guid _wh = Guid.NewGuid();

    public SalesStockDeductionHandlerTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"InvDeduct_{Guid.NewGuid()}")
            .Options;
        _ctx = new InventoryDbContext(options);
        var service = new InventoryBalanceService(_ctx, NullLogger<InventoryBalanceService>.Instance);
        _handler = new SalesStockDeductionHandler(_ctx, service, NullLogger<SalesStockDeductionHandler>.Instance);
    }

    private async Task SeedUnitAsync()
    {
        _ctx.Units.Add(new Unit { Code = "PCS", Name = "Pieces", CompanyId = _company, BranchId = _branch, BusinessUnitId = _bu });
        await _ctx.SaveChangesAsync();
    }

    private async Task SeedBalanceAsync(decimal qty, Guid? variant = null)
    {
        _ctx.InventoryBalances.Add(new InventoryBalance
        {
            CompanyId = _company, BranchId = _branch, BusinessUnitId = _bu,
            ItemId = _item, WarehouseId = _wh, VariantId = variant,
            QuantityOnHand = qty, QuantityAvailable = qty, AverageCost = 100m, TotalValue = qty * 100m,
        });
        await _ctx.SaveChangesAsync();
    }

    private PosTransactionCompletedEvent Event(params StockDeductionLine[] lines) => new()
    {
        PosTransactionId = Guid.NewGuid(),
        WarehouseId = _wh,
        CompanyId = _company,
        BranchId = _branch,
        BusinessUnitId = _bu,
        CreatedByUserId = _user,
        Lines = lines.ToList(),
    };

    [Fact]
    public async Task Handle_PosSale_DecrementsRealBalance()
    {
        await SeedUnitAsync();
        await SeedBalanceAsync(100m);

        await _handler.HandleAsync(Event(new StockDeductionLine
        {
            ProductId = _item, ProductCode = "ITEM", Quantity = 3m, UnitOfMeasure = "PCS", UnitCost = 100m,
        }));

        var bal = await _ctx.InventoryBalances.SingleAsync(b => b.ItemId == _item && b.VariantId == null);
        Assert.Equal(97m, bal.QuantityOnHand);

        // A ledger transaction was written as an outbound (negative) movement, variant null.
        var txn = await _ctx.InventoryTransactions.SingleAsync();
        Assert.Equal(-3m, txn.Quantity);
        Assert.Equal("OUT", txn.TransactionType);
        Assert.Null(txn.VariantId);

        // No phantom duplicate balance rows were created.
        Assert.Equal(1, await _ctx.InventoryBalances.CountAsync());
    }

    [Fact]
    public async Task Handle_EmptyWarehouse_SkipsLine_NoDeduction()
    {
        await SeedUnitAsync();

        var evt = new PosTransactionCompletedEvent
        {
            PosTransactionId = Guid.NewGuid(),
            WarehouseId = Guid.Empty,           // event warehouse empty
            CompanyId = _company, BranchId = _branch, BusinessUnitId = _bu, CreatedByUserId = _user,
            Lines = new List<StockDeductionLine>
            {
                new() { ProductId = _item, ProductCode = "ITEM", WarehouseId = null, Quantity = 3m, UnitOfMeasure = "PCS", UnitCost = 100m },
            },
        };

        await _handler.HandleAsync(evt);

        // Guarded: no transaction and no phantom balance at Guid.Empty.
        Assert.Equal(0, await _ctx.InventoryTransactions.CountAsync());
        Assert.Equal(0, await _ctx.InventoryBalances.CountAsync());
    }

    [Fact]
    public async Task Handle_VariantLine_DecrementsVariantBalanceOnly()
    {
        await SeedUnitAsync();
        var variant = Guid.NewGuid();
        await SeedBalanceAsync(100m, variant: null);    // item-level
        await SeedBalanceAsync(50m, variant: variant);  // variant-level

        await _handler.HandleAsync(Event(new StockDeductionLine
        {
            ProductId = _item, ProductCode = "ITEM", VariantId = variant, Quantity = 5m, UnitOfMeasure = "PCS", UnitCost = 100m,
        }));

        var itemLevel = await _ctx.InventoryBalances.SingleAsync(b => b.ItemId == _item && b.VariantId == null);
        var variantLevel = await _ctx.InventoryBalances.SingleAsync(b => b.ItemId == _item && b.VariantId == variant);

        Assert.Equal(100m, itemLevel.QuantityOnHand);   // untouched
        Assert.Equal(45m, variantLevel.QuantityOnHand);
    }

    [Fact]
    public async Task Handle_DeliveryShipped_DecrementsRealBalance()
    {
        // The second trigger: shipping a (non-POS) delivery must deduct stock just like a POS sale.
        await SeedUnitAsync();
        await SeedBalanceAsync(100m);

        await _handler.HandleAsync(new DeliveryPostedEvent
        {
            DeliveryId = Guid.NewGuid(),
            SalesOrderId = Guid.NewGuid(),
            WarehouseId = _wh,
            CompanyId = _company,
            BranchId = _branch,
            BusinessUnitId = _bu,
            CreatedByUserId = _user,
            Lines = new List<StockDeductionLine>
            {
                new() { ProductId = _item, ProductCode = "ITEM", Quantity = 4m, UnitOfMeasure = "PCS", UnitCost = 100m },
            },
        });

        var bal = await _ctx.InventoryBalances.SingleAsync(b => b.ItemId == _item && b.VariantId == null);
        Assert.Equal(96m, bal.QuantityOnHand);

        var txn = await _ctx.InventoryTransactions.SingleAsync();
        Assert.Equal(-4m, txn.Quantity);
    }
}
