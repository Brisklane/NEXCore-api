using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sales.Domain.Entities;
using Sales.Infrastructure.Persistence;

namespace Sales.Tests.Integration.Controllers;

/// <summary>
/// The shift read is what a shop balances its drawer against, so these tests are written
/// around the ways a till actually loses money: refunds paid in cash, safe drops, voids that
/// must not count as sales, and the session's own counters drifting from the transactions.
/// </summary>
[Collection(SalesTestCollection.Name)]
public class PosReportControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "api/sales/PosReport";

    private readonly SalesCollectionFixture _fixture;
    private readonly HttpClient _client;
    private readonly SalesTestDataBuilder _builder;

    private PosCashier _cashier = null!;
    private PosTerminal _terminal = null!;
    private PosSession _session = null!;
    private readonly List<Guid> _txnIds = [];

    private static readonly DateTime TradingDay = new(2026, 3, 4, 0, 0, 0, DateTimeKind.Utc);

    public PosReportControllerTests(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
        _builder = new SalesTestDataBuilder(fixture);
    }

    public async Task InitializeAsync()
    {
        _cashier = await _builder.CreatePosCashierAsync("Report Cashier");
        _terminal = await _builder.CreatePosTerminalAsync(_fixture.SharedStore.Id);
        await SeedShiftAsync();
    }

    public async Task DisposeAsync()
    {
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

            db.PosPayments.RemoveRange(db.PosPayments.Where(p => _txnIds.Contains(p.PosTransactionId)));
            db.PosTransactionLines.RemoveRange(db.PosTransactionLines.Where(l => _txnIds.Contains(l.PosTransactionId)));
            db.PosCashMovements.RemoveRange(db.PosCashMovements.Where(m => m.PosSessionId == _session.Id));
            await db.SaveChangesAsync();

            db.PosTransactions.RemoveRange(db.PosTransactions.Where(t => _txnIds.Contains(t.Id)));
            await db.SaveChangesAsync();
        }

        await _builder.DeletePosTerminalAsync(_terminal.Id);
        await _builder.DeletePosCashierAsync(_cashier.Id);
        _client.Dispose();
    }

    /// <summary>
    /// One shift: two cash sales, one card sale, a cash refund, a void, and a safe drop.
    ///
    /// The session's own counters are seeded deliberately wrong (they are maintained
    /// incrementally at checkout and nothing decrements them on a refund) so the report has
    /// something real to disagree with.
    /// </summary>
    private async Task SeedShiftAsync()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        _session = new PosSession
        {
            SessionNumber = SalesTestDataBuilder.NextSessionNumber(),
            PosTerminalId = _terminal.Id,
            PosCashierId = _cashier.Id,
            Status = PosSessionStatus.Open,
            OpenedAt = TradingDay.AddHours(8),
            OpeningFloat = 1_000m,
            // Deliberately stale: sales counter excludes nothing, cash counter ignores the refund.
            TotalSalesAmount = 900m,
            CashCollected = 600m,
            TransactionCount = 3,
            CompanyId = TestJwtSettings.CompanyId,
            BranchId = TestJwtSettings.BranchId,
            BusinessUnitId = TestJwtSettings.BusinessUnitId,
        };
        db.PosSessions.Add(_session);
        await db.SaveChangesAsync();

        // 09:00 — cash sale of 300 (2 items)
        var sale1 = Txn(PosTransactionType.Sale, 300m, 30m, 12m, TradingDay.AddHours(9));
        // 09:30 — cash sale of 200 (1 item)
        var sale2 = Txn(PosTransactionType.Sale, 200m, 0m, 8m, TradingDay.AddHours(9).AddMinutes(30));
        // 14:00 — card sale of 400 (3 items)
        var sale3 = Txn(PosTransactionType.Sale, 400m, 20m, 16m, TradingDay.AddHours(14));
        // 15:00 — cash refund of 100
        var refund = Txn(PosTransactionType.Refund, -100m, 0m, -4m, TradingDay.AddHours(15));
        // 16:00 — a void, which is not a sale and must not be counted as one
        var voided = Txn(PosTransactionType.Void, 250m, 0m, 0m, TradingDay.AddHours(16));

        db.PosTransactions.AddRange(sale1, sale2, sale3, refund, voided);
        await db.SaveChangesAsync();
        _txnIds.AddRange(new[] { sale1.Id, sale2.Id, sale3.Id, refund.Id, voided.Id });

        db.PosTransactionLines.AddRange(
            Line(sale1, "COLA-330", "Cola 330ml", 2m, 300m, 30m),
            Line(sale2, "WATER-500", "Water 500ml", 1m, 200m, 0m),
            Line(sale3, "TEE-BLK", "T-Shirt", 3m, 400m, 20m, variantName: "Large"),
            Line(refund, "COLA-330", "Cola 330ml", -1m, -100m, 0m));

        db.PosPayments.AddRange(
            Pay(sale1, PosTenderType.Cash, 300m),
            Pay(sale2, PosTenderType.Cash, 200m),
            Pay(sale3, PosTenderType.CreditCard, 400m),
            Pay(refund, PosTenderType.Cash, -100m));

        db.PosCashMovements.Add(new PosCashMovement
        {
            PosSessionId = _session.Id,
            PosCashierId = _cashier.Id,
            MovementType = PosCashMovementType.SafeDrop,
            Amount = 250m,
            Reason = "Banked mid-shift",
            MovementDate = TradingDay.AddHours(13),
            CompanyId = TestJwtSettings.CompanyId,
            BranchId = TestJwtSettings.BranchId,
            BusinessUnitId = TestJwtSettings.BusinessUnitId,
        });

        await db.SaveChangesAsync();
    }

    private PosTransaction Txn(PosTransactionType type, decimal total, decimal discount, decimal tax, DateTime at) => new()
    {
        TransactionNumber = $"TXN-RPT-{Guid.NewGuid():N}"[..20],
        PosSessionId = _session.Id,
        PosTerminalId = _terminal.Id,
        PosStoreId = _fixture.SharedStore.Id,
        PosCashierId = _cashier.Id,
        TransactionType = type,
        Status = PosTransactionStatus.Completed,
        TransactionDate = at,
        SubtotalAmount = total - tax,
        DiscountAmount = discount,
        TaxAmount = tax,
        TotalAmount = total,
        TenderedAmount = total,
        CompanyId = TestJwtSettings.CompanyId,
        BranchId = TestJwtSettings.BranchId,
        BusinessUnitId = TestJwtSettings.BusinessUnitId,
    };

    private static PosTransactionLine Line(
        PosTransaction t, string code, string name, decimal qty, decimal amount, decimal discount,
        string? variantName = null) => new()
    {
        PosTransactionId = t.Id,
        LineNumber = 1,
        ProductId = Guid.Parse(BuildProductId(code)),
        ProductCode = code,
        ProductName = name,
        VariantName = variantName,
        Quantity = qty,
        UnitOfMeasure = "EA",
        UnitPrice = qty == 0 ? 0 : amount / qty,
        DiscountAmount = discount,
        LineAmount = amount,
        TotalAmount = amount,
        CompanyId = t.CompanyId,
        BranchId = t.BranchId,
        BusinessUnitId = t.BusinessUnitId,
    };

    /// <summary>Stable per-code product id, so the same SKU groups across transactions.</summary>
    private static string BuildProductId(string code)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(code));
        return new Guid(bytes).ToString();
    }

    private static PosPayment Pay(PosTransaction t, PosTenderType tender, decimal amount) => new()
    {
        PosTransactionId = t.Id,
        TenderType = tender,
        Amount = amount,
        IsApproved = true,
        CompanyId = t.CompanyId,
        BranchId = t.BranchId,
        BusinessUnitId = t.BusinessUnitId,
    };

    private static string Range =>
        $"from={TradingDay:yyyy-MM-dd}&to={TradingDay:yyyy-MM-dd}";

    // ── X read ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task XRead_SumsSalesAndExcludesVoids()
    {
        var res = await _client.GetAsync($"{BaseUrl}/session/{_session.Id}/x");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<PosShiftReportDto>>();
        var r = body!.Data!;

        r.GrossSales.Should().Be(900m);      // 300 + 200 + 400, the void excluded
        r.Refunds.Should().Be(100m);         // reported as a magnitude
        r.NetSales.Should().Be(800m);
        r.Discounts.Should().Be(50m);
        r.SaleCount.Should().Be(3);
        r.RefundCount.Should().Be(1);
        r.VoidCount.Should().Be(1);
        r.ItemsSold.Should().Be(5m);         // 2 + 1 + 3, less the refunded 1
        r.AverageBasket.Should().Be(300m);
    }

    [Fact]
    public async Task XRead_ExpectedCashAccountsForRefundsAndSafeDrops()
    {
        var res = await _client.GetAsync($"{BaseUrl}/session/{_session.Id}/x");
        var r = (await res.Content.ReadFromJsonAsync<ApiResponse<PosShiftReportDto>>())!.Data!;

        r.OpeningFloat.Should().Be(1_000m);
        r.CashSales.Should().Be(500m);
        r.CashRefunds.Should().Be(100m);
        r.CashOut.Should().Be(250m);         // the safe drop
        r.CashIn.Should().Be(0m);

        // 1000 + 500 − 100 + 0 − 250. Missing any term here is how a cashier gets accused.
        r.ExpectedCash.Should().Be(1_150m);
    }

    [Fact]
    public async Task XRead_OnOpenSession_IsProvisionalAndReportsNoVariance()
    {
        var res = await _client.GetAsync($"{BaseUrl}/session/{_session.Id}/x");
        var r = (await res.Content.ReadFromJsonAsync<ApiResponse<PosShiftReportDto>>())!.Data!;

        r.IsProvisional.Should().BeTrue();
        // There is nothing counted yet, so a variance would be an invented number.
        r.CountedCash.Should().BeNull();
        r.CashVariance.Should().BeNull();
    }

    [Fact]
    public async Task XRead_FlagsSessionCountersThatDisagreeWithTransactions()
    {
        var res = await _client.GetAsync($"{BaseUrl}/session/{_session.Id}/x");
        var r = (await res.Content.ReadFromJsonAsync<ApiResponse<PosShiftReportDto>>())!.Data!;

        // The seeded counter says 600 cash; the payments say 500 taken less 100 refunded.
        r.RecordedCashCollected.Should().Be(600m);
        r.CashSales.Should().Be(500m);
        r.CountersDisagree.Should().BeTrue();
    }

    [Fact]
    public async Task XRead_SplitsTendersAndSharesAddUp()
    {
        var res = await _client.GetAsync($"{BaseUrl}/session/{_session.Id}/x");
        var r = (await res.Content.ReadFromJsonAsync<ApiResponse<PosShiftReportDto>>())!.Data!;

        var cash = r.Tenders.Single(t => t.TenderType == PosTenderType.Cash);
        var card = r.Tenders.Single(t => t.TenderType == PosTenderType.CreditCard);

        cash.Amount.Should().Be(400m);       // 300 + 200 − 100
        card.Amount.Should().Be(400m);
        r.Tenders.Sum(t => t.SharePercent).Should().BeApproximately(100m, 0.05m);
    }

    [Fact]
    public async Task XRead_ForUnknownSession_Returns404()
    {
        var res = await _client.GetAsync($"{BaseUrl}/session/{Guid.NewGuid()}/x");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Z read ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ZRead_OnClosedSession_ReportsCountedCashAndVariance()
    {
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
            var s = await db.PosSessions.FirstAsync(x => x.Id == _session.Id);
            s.Status = PosSessionStatus.Closed;
            s.ClosedAt = TradingDay.AddHours(18);
            s.ClosingFloat = 1_140m;          // counted 10 short
            await db.SaveChangesAsync();
        }

        var res = await _client.GetAsync($"{BaseUrl}/session/{_session.Id}/z");
        var r = (await res.Content.ReadFromJsonAsync<ApiResponse<PosShiftReportDto>>())!.Data!;

        r.IsProvisional.Should().BeFalse();
        r.CountedCash.Should().Be(1_140m);
        r.CashVariance.Should().Be(-10m);
    }

    // ── Period reports ────────────────────────────────────────────────────────

    [Fact]
    public async Task SalesSummary_CoversTheWholeLastDay()
    {
        // `to` is a bare date; without normalising it to end-of-day the 14:00 and later
        // trading would silently vanish from the total.
        var res = await _client.GetAsync($"{BaseUrl}/sales-summary?{Range}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var r = (await res.Content.ReadFromJsonAsync<ApiResponse<PosSalesSummaryDto>>())!.Data!;
        r.GrossSales.Should().BeGreaterThanOrEqualTo(900m);
        r.Refunds.Should().BeGreaterThanOrEqualTo(100m);
        r.SessionCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SalesSummary_RejectsAnInvertedRange()
    {
        var res = await _client.GetAsync($"{BaseUrl}/sales-summary?from=2026-03-10&to=2026-03-01");
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ByProduct_RanksBySalesAndKeepsVariantsSeparate()
    {
        var res = await _client.GetAsync($"{BaseUrl}/by-product?{Range}&top=10");
        var rows = (await res.Content.ReadFromJsonAsync<ApiResponse<List<PosProductSalesDto>>>())!.Data!;

        var tee = rows.FirstOrDefault(r => r.ProductCode == "TEE-BLK");
        tee.Should().NotBeNull();
        tee!.VariantName.Should().Be("Large");
        tee.NetSales.Should().Be(400m);

        // Cola was sold twice and refunded once — the refund line nets it down.
        var cola = rows.FirstOrDefault(r => r.ProductCode == "COLA-330");
        cola!.Quantity.Should().Be(1m);
        cola.NetSales.Should().Be(200m);
    }

    [Fact]
    public async Task ByCashier_NetsRefundsOffTheCashiersTotal()
    {
        var res = await _client.GetAsync($"{BaseUrl}/by-cashier?{Range}");
        var rows = (await res.Content.ReadFromJsonAsync<ApiResponse<List<PosCashierSalesDto>>>())!.Data!;

        var mine = rows.Single(r => r.CashierId == _cashier.Id);
        mine.CashierName.Should().Be("Report Cashier");
        mine.GrossSales.Should().Be(900m);
        mine.Refunds.Should().Be(100m);
        mine.NetSales.Should().Be(800m);
        mine.TransactionCount.Should().Be(3);
    }

    [Fact]
    public async Task ByHour_ReturnsEveryHourSoQuietHoursAreVisible()
    {
        var res = await _client.GetAsync($"{BaseUrl}/by-hour?{Range}");
        var rows = (await res.Content.ReadFromJsonAsync<ApiResponse<List<PosHourlySalesDto>>>())!.Data!;

        rows.Should().HaveCount(24);
        rows.Select(r => r.Hour).Should().BeInAscendingOrder();
        rows.Single(r => r.Hour == 9).TransactionCount.Should().BeGreaterThanOrEqualTo(2);
        // 03:00 saw no trade — that must read as zero, not be missing from the series.
        rows.Single(r => r.Hour == 3).NetSales.Should().Be(0m);
    }

    [Fact]
    public async Task TenderMix_ReportsCashNetOfRefunds()
    {
        var res = await _client.GetAsync($"{BaseUrl}/tender-mix?{Range}");
        var rows = (await res.Content.ReadFromJsonAsync<ApiResponse<List<PosTenderTotalDto>>>())!.Data!;

        rows.Single(r => r.TenderType == PosTenderType.Cash).Amount.Should().Be(400m);
        rows.Single(r => r.TenderType == PosTenderType.CreditCard).Amount.Should().Be(400m);
    }
}
