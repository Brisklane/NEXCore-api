using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexcore.Tests.Infrastructure.Auth;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Persistence;

namespace Sales.Tests.Integration.Fixtures;

/// <summary>
/// Collection-level fixture for all Sales integration tests.
///
/// Lifecycle (managed by xUnit):
///   InitializeAsync  → starts PostgreSQL, applies migrations, seeds the shared test store
///   DisposeAsync     → stops the container and disposes the WebApplicationFactory
///
/// All test classes in SalesTestCollection share this single instance:
///   - One Docker container per test run
///   - One migration per test run
///   - SharedStore is a PosStore whose Id = TestJwtSettings.BranchId
///     so that PosCashier.PosStoreId and PosTerminal.BranchId from
///     TenantAwareRepository always match the JWT token's BranchId claim.
/// </summary>
public class SalesCollectionFixture : NexcoreWebApplicationFactory
{
    /// <summary>
    /// A PosStore with Id == TestJwtSettings.BranchId.
    /// Use its Id wherever a FK to PosStore is required (PosCashier.PosStoreId,
    /// PosTerminal branch queries, etc.).
    /// </summary>
    public PosStore SharedStore { get; private set; } = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ApplyMigrationsAsync(typeof(SalesDbContext));
        await SeedDocumentSequencesAsync();
        await SeedSharedStoreAsync();
    }

    private async Task SeedDocumentSequencesAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        if (await db.DocumentSequences.AnyAsync())
            return;

        static DocumentSequence Seq(DocumentType type, string prefix, int pad = 5,
            SequenceResetPeriod reset = SequenceResetPeriod.Yearly) => new()
        {
            DocumentType       = type,
            Prefix             = prefix,
            Separator          = "-",
            IncludeYear        = true,
            YearFormat         = SequenceYearFormat.Full,
            IncludeMonth       = false,
            SequencePadding    = pad,
            ResetOn            = reset,
            NextSequenceNumber = 1,
            IsActive           = true,
            CompanyId          = TestJwtSettings.CompanyId,
            BranchId           = TestJwtSettings.BranchId,
            BusinessUnitId     = TestJwtSettings.BusinessUnitId,
            CreatedByUserId    = TestJwtSettings.UserId,
            CreatedAt          = DateTime.UtcNow,
        };

        db.DocumentSequences.AddRange(
            Seq(DocumentType.SalesOrder,    "SO"),
            Seq(DocumentType.Quotation,     "QT"),
            Seq(DocumentType.Invoice,       "INV"),
            Seq(DocumentType.CreditNote,    "CN"),
            Seq(DocumentType.Payment,       "PAY"),
            Seq(DocumentType.Delivery,      "DLV"),
            Seq(DocumentType.SalesReturn,   "RET"),
            Seq(DocumentType.PosSession,    "POSS",  reset: SequenceResetPeriod.Never),
            Seq(DocumentType.PosTransaction,"POST",  pad: 6, reset: SequenceResetPeriod.Never),
            Seq(DocumentType.RiderAssignment,"RA",   reset: SequenceResetPeriod.Never)
        );

        await db.SaveChangesAsync();
    }

    private async Task SeedSharedStoreAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var existing = await db.PosStores.FindAsync(TestJwtSettings.BranchId);
        if (existing is not null)
        {
            SharedStore = existing;
            return;
        }

        SharedStore = new PosStore
        {
            Id = TestJwtSettings.BranchId,
            Code = "TEST-STORE-001",
            CodeInt = 1,
            TradingName = "Shared Test Store",
            StoreType = PosStoreType.Retail,
            StoreFormat = PosStoreFormat.Physical,
            CountryCode = "PK",
            IsActive = true,
            OnlineStatus = StoreOnlineStatus.Closed,
            CompanyId = TestJwtSettings.CompanyId,
            BranchId = TestJwtSettings.BranchId,
            BusinessUnitId = TestJwtSettings.BusinessUnitId,
        };

        db.PosStores.Add(SharedStore);
        await db.SaveChangesAsync();
    }

    public SalesDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<SalesDbContext>();
    }
}
