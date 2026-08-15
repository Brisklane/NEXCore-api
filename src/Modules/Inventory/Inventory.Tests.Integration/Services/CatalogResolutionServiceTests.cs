using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Inventory.Application.DTOs;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Services;
using DomainUnit = Inventory.Domain.Entities.Unit;

namespace Inventory.Tests.Integration.Services;

/// <summary>
/// Pins down catalogue resolution — the thing every selling surface depends on.
///
/// Each test here corresponds to a way the old client-side lookup got it wrong:
/// substring instead of exact matching, variants ignored entirely, and per-UOM
/// barcodes resolving to a single base unit (selling one bottle instead of a case).
/// </summary>
public class CatalogResolutionServiceTests
{
    private readonly InventoryDbContext _ctx;
    private readonly CatalogResolutionService _service;

    private readonly Guid _company = Guid.NewGuid();
    private readonly Guid _otherCompany = Guid.NewGuid();
    private readonly Guid _branch = Guid.NewGuid();
    private readonly Guid _bu = Guid.NewGuid();

    private readonly Guid _pcsUnit = Guid.NewGuid();
    private readonly Guid _caseUnit = Guid.NewGuid();
    private readonly Guid _item = Guid.NewGuid();

    public CatalogResolutionServiceTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"CatalogRes_{Guid.NewGuid()}")
            .Options;
        _ctx = new InventoryDbContext(options);

        var http = new Mock<IHttpContextAccessor>();
        var identity = new ClaimsIdentity(
        [
            new Claim("CompanyId", _company.ToString()),
            new Claim("BranchId", _branch.ToString()),
            new Claim("BusinessUnitId", _bu.ToString()),
        ], "test");
        http.Setup(h => h.HttpContext)
            .Returns(new DefaultHttpContext { User = new ClaimsPrincipal(identity) });

        _service = new CatalogResolutionService(_ctx, http.Object);

        Seed();
    }

    /// <summary>
    /// A cola sold loose (PCS, base unit) and by the case (1 CASE = 24 PCS), with the case
    /// carrying its own barcode — the exact shape that used to sell a case as one bottle.
    /// </summary>
    private void Seed()
    {
        _ctx.Units.AddRange(
            Unit(_pcsUnit, "Pieces"),
            Unit(_caseUnit, "Case"));

        _ctx.Items.Add(new Item
        {
            Id = _item,
            Code = "COLA-330",
            Name = "Cola 330ml",
            BaseUnitId = _pcsUnit,
            ItemType = "Inventory",
            TrackingType = "None",
            IsActive = true,
            CompanyId = _company,
            BranchId = _branch,
            BusinessUnitId = _bu,
        });

        _ctx.ItemBarcodes.AddRange(
            Barcode("5000112637922", unitId: null),        // single bottle, base unit
            Barcode("5000112600001", unitId: _caseUnit));  // case of 24

        _ctx.ItemUomConversions.Add(new ItemUomConversion
        {
            Id = Guid.NewGuid(),
            ItemId = _item,
            FromUnitId = _caseUnit,
            ToUnitId = _pcsUnit,
            ConversionFactor = 24m,   // 1 CASE = 24 PCS
            CompanyId = _company,
            BranchId = _branch,
            BusinessUnitId = _bu,
        });

        _ctx.ItemVariants.Add(new ItemVariant
        {
            Id = Guid.NewGuid(),
            ItemId = _item,
            VariantCode = "COLA-330-DIET",
            VariantName = "Diet",
            Barcode = "5000112699999",
            CompanyId = _company,
            BranchId = _branch,
            BusinessUnitId = _bu,
        });

        _ctx.ItemPrices.Add(new ItemPrice
        {
            Id = Guid.NewGuid(),
            ItemId = _item,
            UnitId = _pcsUnit,
            PriceList = "Default",
            SalePrice = 150m,
            CompanyId = _company,
            BranchId = _branch,
            BusinessUnitId = _bu,
        });

        _ctx.SaveChanges();
    }

    private DomainUnit Unit(Guid id, string name) => new()
    {
        Id = id,
        Code = name[..Math.Min(3, name.Length)].ToUpperInvariant(),
        Name = name,
        CompanyId = _company,
        BranchId = _branch,
        BusinessUnitId = _bu,
    };

    private ItemBarcode Barcode(string value, Guid? unitId) => new()
    {
        Id = Guid.NewGuid(),
        ItemId = _item,
        Barcode = value,
        UnitId = unitId,
        IsActive = true,
        CompanyId = _company,
        BranchId = _branch,
        BusinessUnitId = _bu,
    };

    // ── Exact matching, in priority order ─────────────────────────────────

    [Fact]
    public async Task Resolve_ItemBarcode_ReturnsExactMatchInBaseUnit()
    {
        var result = await _service.ResolveAsync("5000112637922");

        Assert.True(result.IsExactMatch);
        Assert.Equal(CatalogMatchType.ItemBarcode, result.Match!.MatchType);
        Assert.Equal(_item, result.Match.ItemId);
        Assert.Equal(_pcsUnit, result.Match.UnitId);
        Assert.Equal(1m, result.Match.QuantityInBaseUnits);
    }

    /// <summary>
    /// The defect that cost money: a case barcode must resolve to the CASE unit and report
    /// that it is 24 base units, not silently sell one bottle.
    /// </summary>
    [Fact]
    public async Task Resolve_CaseBarcode_ResolvesToCaseUnitWithPackQuantity()
    {
        var result = await _service.ResolveAsync("5000112600001");

        Assert.True(result.IsExactMatch);
        Assert.Equal(_caseUnit, result.Match!.UnitId);
        Assert.Equal(_pcsUnit, result.Match.BaseUnitId);
        Assert.Equal(24m, result.Match.QuantityInBaseUnits);
    }

    [Fact]
    public async Task Resolve_VariantBarcode_ReturnsVariant()
    {
        var result = await _service.ResolveAsync("5000112699999");

        Assert.True(result.IsExactMatch);
        Assert.Equal(CatalogMatchType.VariantBarcode, result.Match!.MatchType);
        Assert.Equal("COLA-330-DIET", result.Match.VariantCode);
        Assert.NotNull(result.Match.VariantId);
    }

    [Fact]
    public async Task Resolve_VariantSku_ReturnsVariant()
    {
        var result = await _service.ResolveAsync("COLA-330-DIET");

        Assert.True(result.IsExactMatch);
        Assert.Equal(CatalogMatchType.VariantCode, result.Match!.MatchType);
        Assert.Equal("COLA-330-DIET", result.Match.VariantCode);
    }

    [Fact]
    public async Task Resolve_ItemSku_IsCaseInsensitive()
    {
        var result = await _service.ResolveAsync("cola-330");

        Assert.True(result.IsExactMatch);
        Assert.Equal(CatalogMatchType.ItemCode, result.Match!.MatchType);
        Assert.Equal(_item, result.Match.ItemId);
    }

    // ── Exactness: a scan must never match "close enough" ─────────────────

    /// <summary>
    /// The old lookup used substring matching, so a partial barcode could ring up a
    /// different product. A partial code must NOT be treated as an exact match.
    /// </summary>
    [Fact]
    public async Task Resolve_PartialBarcode_IsNotAnExactMatch()
    {
        var result = await _service.ResolveAsync("50001126");

        Assert.False(result.IsExactMatch);
        Assert.Null(result.Match);
    }

    [Fact]
    public async Task Resolve_UnknownCode_ReturnsNameCandidatesForTheTill()
    {
        var result = await _service.ResolveAsync("Cola");

        Assert.False(result.IsExactMatch);
        Assert.NotEmpty(result.Candidates);
        Assert.All(result.Candidates, c => Assert.Equal(CatalogMatchType.Name, c.MatchType));
    }

    [Fact]
    public async Task Resolve_UnknownCode_ReturnsNothingWhenFallbackDisabled()
    {
        var result = await _service.ResolveAsync("Cola", fallbackToSearch: false);

        Assert.False(result.IsExactMatch);
        Assert.Empty(result.Candidates);
    }

    // ── Tenancy ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_DoesNotLeakAcrossCompanies()
    {
        _ctx.Items.Add(new Item
        {
            Id = Guid.NewGuid(),
            Code = "OTHER-CO-SKU",
            Name = "Someone else's product",
            BaseUnitId = _pcsUnit,
            IsActive = true,
            CompanyId = _otherCompany,
            BranchId = Guid.NewGuid(),
            BusinessUnitId = Guid.NewGuid(),
        });
        await _ctx.SaveChangesAsync();

        var result = await _service.ResolveAsync("OTHER-CO-SKU");

        Assert.False(result.IsExactMatch);
    }

    /// <summary>
    /// Identity is company-wide by design: one physical product keeps one barcode across
    /// every store, so a differing branch/business-unit must not stop it resolving.
    /// </summary>
    [Fact]
    public async Task Resolve_MatchesAcrossBranchesWithinTheCompany()
    {
        var otherBranchItem = Guid.NewGuid();
        _ctx.Items.Add(new Item
        {
            Id = otherBranchItem,
            Code = "BRANCH2-SKU",
            Name = "Stocked at another branch",
            BaseUnitId = _pcsUnit,
            IsActive = true,
            CompanyId = _company,
            BranchId = Guid.NewGuid(),        // different branch
            BusinessUnitId = Guid.NewGuid(),  // different business unit
        });
        await _ctx.SaveChangesAsync();

        var result = await _service.ResolveAsync("BRANCH2-SKU");

        Assert.True(result.IsExactMatch);
        Assert.Equal(otherBranchItem, result.Match!.ItemId);
    }

    // ── Search ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_IsCaseInsensitiveAndFindsVariants()
    {
        var byName = await _service.SearchAsync("cola");
        Assert.Contains(byName, r => r.ItemId == _item);

        var byVariant = await _service.SearchAsync("diet");
        Assert.Contains(byVariant, r => r.VariantCode == "COLA-330-DIET");
    }

    [Fact]
    public async Task Search_RespectsLimit()
    {
        var results = await _service.SearchAsync("cola", limit: 1);
        Assert.Single(results);
    }

    // ── Delta sync ────────────────────────────────────────────────────────

    [Fact]
    public async Task Sync_FirstCall_ReturnsEverythingWithPackFactors()
    {
        var page = await _service.GetSyncPageAsync(since: null, sinceId: null);

        var cola = Assert.Single(page.Entries, e => e.ItemId == _item);
        Assert.False(cola.IsDeleted);
        Assert.Equal("COLA-330", cola.Code);

        // The case barcode must carry its pack factor, or an offline till sells 1 for 24.
        var caseBarcode = Assert.Single(cola.Barcodes, b => b.Barcode == "5000112600001");
        Assert.Equal(24m, caseBarcode.QuantityInBaseUnits);

        var single = Assert.Single(cola.Barcodes, b => b.Barcode == "5000112637922");
        Assert.Equal(1m, single.QuantityInBaseUnits);

        Assert.Single(cola.Variants, v => v.VariantCode == "COLA-330-DIET");
    }

    [Fact]
    public async Task Sync_WithWatermark_ReturnsOnlyLaterChanges()
    {
        var first = await _service.GetSyncPageAsync(null, null);
        Assert.NotEmpty(first.Entries);

        // Nothing has changed since we caught up.
        var second = await _service.GetSyncPageAsync(first.NextSince, first.NextSinceId);
        Assert.Empty(second.Entries);
        Assert.False(second.HasMore);
    }

    [Fact]
    public async Task Sync_PicksUpAnItemChangedAfterTheWatermark()
    {
        var caughtUp = await _service.GetSyncPageAsync(null, null);

        _ctx.Items.Add(new Item
        {
            Id = Guid.NewGuid(),
            Code = "NEW-SKU",
            Name = "Added later",
            BaseUnitId = _pcsUnit,
            IsActive = true,
            CompanyId = _company,
            BranchId = _branch,
            BusinessUnitId = _bu,
            CreatedAt = DateTime.UtcNow.AddMinutes(5),
        });
        await _ctx.SaveChangesAsync();

        var delta = await _service.GetSyncPageAsync(caughtUp.NextSince, caughtUp.NextSinceId);

        Assert.Single(delta.Entries);
        Assert.Equal("NEW-SKU", delta.Entries[0].Code);
    }

    /// <summary>
    /// A withdrawn product has to actively disappear from tills, so deactivation must come
    /// back as a tombstone rather than simply vanishing from the feed.
    /// </summary>
    [Fact]
    public async Task Sync_DeactivatedItem_ComesBackAsATombstone()
    {
        var caughtUp = await _service.GetSyncPageAsync(null, null);

        var item = await _ctx.Items.FirstAsync(i => i.Id == _item);
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow.AddMinutes(5);
        await _ctx.SaveChangesAsync();

        var delta = await _service.GetSyncPageAsync(caughtUp.NextSince, caughtUp.NextSinceId);

        var entry = Assert.Single(delta.Entries);
        Assert.True(entry.IsDeleted);
        Assert.Empty(entry.Barcodes);   // tombstones carry identity only
    }

    [Fact]
    public async Task Sync_PagesWithoutSkippingRowsThatShareATimestamp()
    {
        // Same instant on every row: ordering by timestamp alone would lose rows across
        // the page boundary, which is what the (ChangedAt, Id) keyset prevents.
        var sameInstant = DateTime.UtcNow.AddMinutes(10);
        for (var i = 0; i < 5; i++)
        {
            _ctx.Items.Add(new Item
            {
                Id = Guid.NewGuid(),
                Code = $"TIE-{i}",
                Name = $"Tie {i}",
                BaseUnitId = _pcsUnit,
                IsActive = true,
                CompanyId = _company,
                BranchId = _branch,
                BusinessUnitId = _bu,
                CreatedAt = sameInstant,
            });
        }
        await _ctx.SaveChangesAsync();

        var seen = new List<string>();
        DateTime? since = null;
        Guid? sinceId = null;

        for (var guard = 0; guard < 20; guard++)
        {
            var page = await _service.GetSyncPageAsync(since, sinceId, pageSize: 2);
            seen.AddRange(page.Entries.Select(e => e.Code));
            if (!page.HasMore) break;
            since = page.NextSince;
            sinceId = page.NextSinceId;
        }

        for (var i = 0; i < 5; i++)
            Assert.Contains($"TIE-{i}", seen);

        Assert.Equal(seen.Count, seen.Distinct().Count());   // and none served twice
    }

    [Fact]
    public async Task Sync_DoesNotLeakOtherCompanies()
    {
        _ctx.Items.Add(new Item
        {
            Id = Guid.NewGuid(),
            Code = "FOREIGN",
            Name = "Another company product",
            BaseUnitId = _pcsUnit,
            IsActive = true,
            CompanyId = _otherCompany,
            BranchId = Guid.NewGuid(),
            BusinessUnitId = Guid.NewGuid(),
        });
        await _ctx.SaveChangesAsync();

        var page = await _service.GetSyncPageAsync(null, null);

        Assert.DoesNotContain(page.Entries, e => e.Code == "FOREIGN");
    }

    [Fact]
    public async Task Resolve_CarriesIndicativeListPrice()
    {
        var result = await _service.ResolveAsync("COLA-330");
        Assert.Equal(150m, result.Match!.ListPrice);
    }
}
