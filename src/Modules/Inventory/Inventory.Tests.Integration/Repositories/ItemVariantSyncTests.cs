using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories.Implementations;

namespace Inventory.Tests.Integration.Repositories;

/// <summary>
/// Variant sync semantics. The important property is what does NOT happen: variant ids
/// are referenced by inventory balances, documents, transactions, batches, serials and
/// posted POS lines, so a variant removed from the payload must be retired with its id
/// intact — never deleted and re-inserted the way barcodes are.
/// </summary>
public class ItemVariantSyncTests
{
    private readonly InventoryDbContext _ctx;
    private readonly ItemRepository _repo;

    private readonly Guid _company = Guid.NewGuid();
    private readonly Guid _branch = Guid.NewGuid();
    private readonly Guid _bu = Guid.NewGuid();
    private readonly Guid _item = Guid.NewGuid();
    private readonly Guid _existingVariant = Guid.NewGuid();

    public ItemVariantSyncTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"VariantSync_{Guid.NewGuid()}")
            .Options;
        _ctx = new InventoryDbContext(options);

        var http = new Mock<IHttpContextAccessor>();
        _repo = new ItemRepository(_ctx, http.Object);

        _ctx.ItemVariants.Add(Variant(_existingVariant, "RED-XL", "Red / XL"));
        _ctx.SaveChanges();
    }

    private ItemVariant Variant(Guid id, string code, string name) => new()
    {
        Id = id,
        ItemId = _item,
        VariantCode = code,
        VariantName = name,
        CompanyId = _company,
        BranchId = _branch,
        BusinessUnitId = _bu,
    };

    [Fact]
    public async Task Sync_UpdatesAnExistingVariantInPlace()
    {
        var edited = Variant(_existingVariant, "RED-XL", "Red / Extra Large");
        edited.Barcode = "5000112600123";

        await _repo.SyncItemVariantsAsync(_item, [edited]);

        var row = await _ctx.ItemVariants.FirstAsync(v => v.Id == _existingVariant);
        Assert.Equal("Red / Extra Large", row.VariantName);
        Assert.Equal("5000112600123", row.Barcode);
        Assert.False(row.IsDeleted);

        // Same id — anything referencing it still resolves.
        Assert.Single(await _ctx.ItemVariants.Where(v => v.ItemId == _item && !v.IsDeleted).ToListAsync());
    }

    [Fact]
    public async Task Sync_AddsVariantsWithoutAnId()
    {
        var added = Variant(Guid.Empty, "BLU-S", "Blue / S");

        await _repo.SyncItemVariantsAsync(_item, [Variant(_existingVariant, "RED-XL", "Red / XL"), added]);

        var live = await _ctx.ItemVariants.Where(v => v.ItemId == _item && !v.IsDeleted).ToListAsync();
        Assert.Equal(2, live.Count);
        Assert.Contains(live, v => v.VariantCode == "BLU-S" && v.Id != Guid.Empty);
    }

    /// <summary>
    /// The behaviour that protects history: an omitted variant is retired, not removed.
    /// </summary>
    [Fact]
    public async Task Sync_RetiresOmittedVariantsButKeepsTheRow()
    {
        await _repo.SyncItemVariantsAsync(_item, [Variant(Guid.Empty, "BLU-S", "Blue / S")]);

        var retired = await _ctx.ItemVariants.FirstAsync(v => v.Id == _existingVariant);
        Assert.True(retired.IsDeleted);
        Assert.False(retired.IsActive);
        Assert.NotNull(retired.DeletedAt);

        // Still present, so a stock balance or sales line pointing at it is not orphaned.
        Assert.Equal(_existingVariant, retired.Id);
    }

    [Fact]
    public async Task Sync_WithEmptyListRetiresEverything()
    {
        await _repo.SyncItemVariantsAsync(_item, []);

        Assert.Empty(await _ctx.ItemVariants.Where(v => v.ItemId == _item && !v.IsDeleted).ToListAsync());
        Assert.Single(await _ctx.ItemVariants.Where(v => v.ItemId == _item).ToListAsync());
    }
}
