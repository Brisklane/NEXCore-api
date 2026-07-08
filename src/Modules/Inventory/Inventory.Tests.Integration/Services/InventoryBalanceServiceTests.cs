using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Services;

namespace Inventory.Tests.Integration.Services;

/// <summary>
/// Exercises the shared <see cref="InventoryBalanceService"/> — the single source of truth for balance
/// keying + moving-average costing used by both inbound posting and outbound POS deduction.
/// </summary>
public class InventoryBalanceServiceTests
{
    private readonly InventoryDbContext _ctx;
    private readonly InventoryBalanceService _service;

    private readonly Guid _company = Guid.NewGuid();
    private readonly Guid _branch = Guid.NewGuid();
    private readonly Guid _bu = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();
    private readonly Guid _item = Guid.NewGuid();
    private readonly Guid _wh = Guid.NewGuid();

    public InventoryBalanceServiceTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"InvBalSvc_{Guid.NewGuid()}")
            .Options;
        _ctx = new InventoryDbContext(options);
        _service = new InventoryBalanceService(_ctx, NullLogger<InventoryBalanceService>.Instance);
    }

    private StockMovement Move(decimal qty, decimal unitCost, Guid? bin = null, Guid? variant = null) =>
        new(_item, _wh, bin, variant, qty, unitCost, _company, _branch, _bu, _user);

    [Fact]
    public async Task ApplyMovement_Receipt_CreatesBalanceWithQtyAndCost()
    {
        await _service.ApplyMovementAsync(Move(10m, 100m));
        await _ctx.SaveChangesAsync();

        var bal = await _service.FindBalanceAsync(_item, _wh, null, null, _company);
        Assert.NotNull(bal);
        Assert.Equal(10m, bal!.QuantityOnHand);
        Assert.Equal(100m, bal.AverageCost);
        Assert.Equal(1000m, bal.TotalValue);
        Assert.Equal(10m, bal.QuantityAvailable);
    }

    [Fact]
    public async Task ApplyMovement_SecondReceipt_RecomputesMovingAverage()
    {
        await _service.ApplyMovementAsync(Move(10m, 100m));
        await _service.ApplyMovementAsync(Move(10m, 200m));
        await _ctx.SaveChangesAsync();

        var bal = await _service.FindBalanceAsync(_item, _wh, null, null, _company);
        Assert.Equal(20m, bal!.QuantityOnHand);
        Assert.Equal(150m, bal.AverageCost);   // (10*100 + 10*200) / 20
        Assert.Equal(3000m, bal.TotalValue);
    }

    [Fact]
    public async Task ApplyMovement_Issue_DecrementsAndKeepsAverageCost()
    {
        await _service.ApplyMovementAsync(Move(10m, 100m));   // receipt
        await _service.ApplyMovementAsync(Move(-3m, 999m));   // issue at a different cost — must be ignored
        await _ctx.SaveChangesAsync();

        var bal = await _service.FindBalanceAsync(_item, _wh, null, null, _company);
        Assert.Equal(7m, bal!.QuantityOnHand);
        Assert.Equal(100m, bal.AverageCost);    // issue does NOT move the average cost
        Assert.Equal(700m, bal.TotalValue);
    }

    [Fact]
    public async Task ApplyMovement_IssueWithNoBalance_CreatesNegativeShortageRow()
    {
        await _service.ApplyMovementAsync(Move(-5m, 100m));
        await _ctx.SaveChangesAsync();

        var bal = await _service.FindBalanceAsync(_item, _wh, null, null, _company);
        Assert.NotNull(bal);
        Assert.Equal(-5m, bal!.QuantityOnHand);   // shortage flagged as negative on-hand
    }

    [Fact]
    public async Task FindBalance_NullBin_DoesNotMatchBinnedRow()
    {
        // Regression for the previous `binId == null || b.BinId == binId` bug, which matched ANY bin.
        var binnedBin = Guid.NewGuid();
        await _service.ApplyMovementAsync(Move(10m, 100m, bin: binnedBin));
        await _ctx.SaveChangesAsync();

        // A warehouse-level (bin-null) lookup must NOT find the binned row.
        var warehouseLevel = await _service.FindBalanceAsync(_item, _wh, binId: null, variantId: null, _company);
        Assert.Null(warehouseLevel);

        // The binned lookup finds it.
        var binned = await _service.FindBalanceAsync(_item, _wh, binId: binnedBin, variantId: null, _company);
        Assert.NotNull(binned);
        Assert.Equal(10m, binned!.QuantityOnHand);
    }

    [Fact]
    public async Task ApplyMovement_VariantAndItemLevel_AreSeparateBalances()
    {
        var variant = Guid.NewGuid();
        await _service.ApplyMovementAsync(Move(10m, 100m));                 // item-level
        await _service.ApplyMovementAsync(Move(4m, 100m, variant: variant)); // variant-level
        await _service.ApplyMovementAsync(Move(-1m, 100m, variant: variant));
        await _ctx.SaveChangesAsync();

        var itemLevel = await _service.FindBalanceAsync(_item, _wh, null, null, _company);
        var variantLevel = await _service.FindBalanceAsync(_item, _wh, null, variant, _company);

        Assert.Equal(10m, itemLevel!.QuantityOnHand);   // untouched by the variant movements
        Assert.Equal(3m, variantLevel!.QuantityOnHand);
    }

    [Fact]
    public async Task GetOnHand_SumsAcrossBins_ForVariant()
    {
        await _service.ApplyMovementAsync(Move(10m, 100m, bin: Guid.NewGuid()));
        await _service.ApplyMovementAsync(Move(5m, 100m, bin: Guid.NewGuid()));
        await _service.ApplyMovementAsync(Move(3m, 100m, variant: Guid.NewGuid())); // different variant — excluded
        await _ctx.SaveChangesAsync();

        var onHand = await _service.GetAvailableAsync(_item, _wh, variantId: null, _company);
        Assert.Equal(15m, onHand);
    }

    [Fact]
    public async Task GetOnHand_NoBalanceRow_ReturnsNull_NotZero()
    {
        // Distinguishes "untracked / never received" (null → POS check fails open) from a real zero.
        var onHand = await _service.GetAvailableAsync(Guid.NewGuid(), _wh, variantId: null, _company);
        Assert.Null(onHand);
    }

    [Fact]
    public async Task Reserve_RaisesReservedAndLowersAvailable()
    {
        await _service.ApplyMovementAsync(Move(10m, 100m));   // receipt → on-hand 10
        await _service.ApplyMovementAsync(new StockMovement(
            _item, _wh, null, null, 0m, 0m, _company, _branch, _bu, _user, ReservationDelta: 4m));
        await _ctx.SaveChangesAsync();

        var bal = await _service.FindBalanceAsync(_item, _wh, null, null, _company);
        Assert.Equal(10m, bal!.QuantityOnHand);    // physical untouched
        Assert.Equal(4m, bal.QuantityReserved);
        Assert.Equal(6m, bal.QuantityAvailable);   // available = on-hand − reserved
    }

    [Fact]
    public async Task Issue_ReleasesReservation()
    {
        await _service.ApplyMovementAsync(Move(10m, 100m));   // on-hand 10
        await _service.ApplyMovementAsync(new StockMovement(
            _item, _wh, null, null, 0m, 0m, _company, _branch, _bu, _user, ReservationDelta: 4m)); // reserve 4
        // ship 4: on-hand −4 AND release reserve −4
        await _service.ApplyMovementAsync(new StockMovement(
            _item, _wh, null, null, -4m, 100m, _company, _branch, _bu, _user, ReservationDelta: -4m));
        await _ctx.SaveChangesAsync();

        var bal = await _service.FindBalanceAsync(_item, _wh, null, null, _company);
        Assert.Equal(6m, bal!.QuantityOnHand);
        Assert.Equal(0m, bal.QuantityReserved);
        Assert.Equal(6m, bal.QuantityAvailable);
    }

    [Fact]
    public async Task Release_ClampsAtZero_WhenNothingReserved()
    {
        await _service.ApplyMovementAsync(Move(10m, 100m));   // on-hand 10, reserved 0 (e.g. POS sale path)
        await _service.ApplyMovementAsync(new StockMovement(
            _item, _wh, null, null, -3m, 100m, _company, _branch, _bu, _user, ReservationDelta: -3m));
        await _ctx.SaveChangesAsync();

        var bal = await _service.FindBalanceAsync(_item, _wh, null, null, _company);
        Assert.Equal(7m, bal!.QuantityOnHand);
        Assert.Equal(0m, bal.QuantityReserved);   // clamped — never negative
        Assert.Equal(7m, bal.QuantityAvailable);
    }

    [Fact]
    public async Task FindBalance_IsScopedByCompany()
    {
        await _service.ApplyMovementAsync(Move(10m, 100m));
        await _ctx.SaveChangesAsync();

        var otherCompany = await _service.FindBalanceAsync(_item, _wh, null, null, Guid.NewGuid());
        Assert.Null(otherCompany);
    }
}
