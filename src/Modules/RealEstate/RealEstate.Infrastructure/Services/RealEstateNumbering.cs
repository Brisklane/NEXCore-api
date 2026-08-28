using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Human-facing document numbers: <c>BKG-26-00042</c>, <c>RCP-260825-0042</c>, <c>TRF-26-00117</c>.
///
/// Real estate documents are quoted for decades, not days. An allotment letter is produced in
/// front of a sub-registrar eleven years after it was issued and a transfer deed is read out in
/// court, so most series are **yearly** — <c>BKG-26-00042</c> says which year's book to pull.
/// Receipts are the exception and reset **daily**, because a cashier reconciles a day-book at
/// close of business and a date in the number is what makes that possible.
///
/// Uniqueness is enforced by the database (see the tenant+number unique indexes in
/// <see cref="RealEstateDbContext"/>); this only has to produce a good candidate, and the callers
/// retry on the rare collision between two counters numbering in the same second.
/// </summary>
public class RealEstateNumbering(RealEstateDbContext db, IRealEstateTenant tenant)
{
    // ── Yearly series ────────────────────────────────────────────────────────
    // Everything a customer keeps a copy of.

    public Task<string> NextBookingNumberAsync(DateTime now)
        => NextYearlyAsync(db.Bookings, "BKG", now);

    public Task<string> NextAllotmentNumberAsync(DateTime now)
        => NextYearlyAsync(db.Allotments, "ALT", now);

    public Task<string> NextAgreementNumberAsync(DateTime now)
        => NextYearlyAsync(db.SaleAgreements, "AGR", now);

    public Task<string> NextDemandNumberAsync(DateTime now)
        => NextYearlyAsync(db.Demands, "DMD", now);

    public Task<string> NextTransferNumberAsync(DateTime now)
        => NextYearlyAsync(db.TransferRequests, "TRF", now);

    public Task<string> NextCancellationNumberAsync(DateTime now)
        => NextYearlyAsync(db.Cancellations, "CNL", now);

    public Task<string> NextRefundNumberAsync(DateTime now)
        => NextYearlyAsync(db.RefundRequests, "RFD", now);

    public Task<string> NextNocNumberAsync(DateTime now)
        => NextYearlyAsync(db.NocIssuances, "NOC", now);

    public Task<string> NextNoticeNumberAsync(DateTime now)
        => NextYearlyAsync(db.LegalNotices, "NTC", now);

    public Task<string> NextPossessionNumberAsync(DateTime now)
        => NextYearlyAsync(db.PossessionOffers, "POS", now);

    public Task<string> NextHandoverNumberAsync(DateTime now)
        => NextYearlyAsync(db.Handovers, "HND", now);

    public Task<string> NextTenancyNumberAsync(DateTime now)
        => NextYearlyAsync(db.Tenancies, "TNY", now);

    public Task<string> NextDealNumberAsync(DateTime now)
        => NextYearlyAsync(db.Deals, "DEL", now);

    public Task<string> NextOfferNumberAsync(DateTime now)
        => NextYearlyAsync(db.Offers, "OFR", now);

    public Task<string> NextEnquiryNumberAsync(DateTime now)
        => NextYearlyAsync(db.Enquiries, "ENQ", now);

    public Task<string> NextInstructionNumberAsync(DateTime now)
        => NextYearlyAsync(db.Instructions, "INS", now);

    public Task<string> NextListingNumberAsync(DateTime now)
        => NextYearlyAsync(db.Listings, "LST", now);

    public Task<string> NextWorkOrderNumberAsync(DateTime now)
        => NextYearlyAsync(db.WorkOrders, "WRK", now);

    public Task<string> NextComplaintNumberAsync(DateTime now)
        => NextYearlyAsync(db.Complaints, "TKT", now);

    public Task<string> NextMaintenanceBillNumberAsync(DateTime now)
        => NextYearlyAsync(db.MaintenanceBills, "MNT", now);

    public Task<string> NextServiceChargeInvoiceNumberAsync(DateTime now)
        => NextYearlyAsync(db.ServiceChargeInvoices, "SVC", now);

    public Task<string> NextIpcNumberAsync(DateTime now)
        => NextYearlyAsync(db.InterimPaymentCertificates, "IPC", now);

    public Task<string> NextVariationNumberAsync(DateTime now)
        => NextYearlyAsync(db.VariationOrders, "VAR", now);

    public Task<string> NextSubcontractNumberAsync(DateTime now)
        => NextYearlyAsync(db.Subcontracts, "SUB", now);

    public Task<string> NextTenderNumberAsync(DateTime now)
        => NextYearlyAsync(db.Tenders, "TND", now);

    public Task<string> NextClientBuildNumberAsync(DateTime now)
        => NextYearlyAsync(db.ClientBuildContracts, "CBC", now);

    public Task<string> NextSiteInstructionNumberAsync(DateTime now)
        => NextYearlyAsync(db.SiteInstructions, "SIN", now);

    public Task<string> NextDocumentNumberAsync(DateTime now)
        => NextYearlyAsync(db.GeneratedDocuments, "DOC", now);

    public Task<string> NextEscrowWithdrawalNumberAsync(DateTime now)
        => NextYearlyAsync(db.EscrowWithdrawals, "EWD", now);

    public Task<string> NextBallotNumberAsync(DateTime now)
        => NextYearlyAsync(db.Ballots, "BAL", now);

    public Task<string> NextTokenNumberAsync(DateTime now)
        => NextYearlyAsync(db.TokenReservations, "TKN", now);

    public Task<string> NextEoiNumberAsync(DateTime now)
        => NextYearlyAsync(db.ExpressionOfInterests, "EOI", now);

    // ── Daily series ─────────────────────────────────────────────────────────
    // What a cashier or a gate guard reconciles at the end of a shift.

    public Task<string> NextReceiptNumberAsync(DateTime now)
        => NextDailyAsync(db.Receipts, "RCP", now, r => r.ReceivedOn >= DateOnly.FromDateTime(now));

    public Task<string> NextGatePassNumberAsync(DateTime now)
        => NextDailyAsync(db.GatePasses, "GPS", now, g => g.ValidFrom >= now.Date);

    public Task<string> NextVisitorPassNumberAsync(DateTime now)
        => NextDailyAsync(db.VisitorPasses, "VIS", now, v => v.ValidFrom >= now.Date);

    // ── Master codes ─────────────────────────────────────────────────────────
    // Sequential per company and never reset, because these are quoted for the life of the
    // relationship: a property reference outlives every listing it was ever advertised under.

    public Task<string> NextPropertyReferenceAsync() => NextMasterCodeAsync(db.Properties, "PRP");
    public Task<string> NextPartyReferenceAsync() => NextMasterCodeAsync(db.Parties, "PTY");
    public Task<string> NextPartnerReferenceAsync() => NextMasterCodeAsync(db.ChannelPartners, "CPT");
    public Task<string> NextLandlordReferenceAsync() => NextMasterCodeAsync(db.Landlords, "LLD");
    public Task<string> NextParcelReferenceAsync() => NextMasterCodeAsync(db.LandParcels, "LND");
    public Task<string> NextContractorReferenceAsync() => NextMasterCodeAsync(db.Contractors, "CON");
    public Task<string> NextPhysicalFileNumberAsync() => NextMasterCodeAsync(db.PhysicalFiles, "FIL");

    /// <summary>
    /// A plot file number inside its scheme: <c>DHA-P8-05M-00417</c> in spirit, though the prefix
    /// is the project's own. Numbered per project rather than per company, because a file number is
    /// how a scheme's whole market identifies the asset and it must not collide across projects.
    /// </summary>
    public async Task<string> NextPlotFileNumberAsync(Guid projectId, string categoryCode)
    {
        var project = await db.Projects.ForCompany(tenant).FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new InvalidOperationException("That project does not exist.");

        var count = await db.PlotFiles.ForCompany(tenant).CountAsync(f => f.ProjectId == projectId);
        var prefix = string.IsNullOrWhiteSpace(project.Code) ? "PRJ" : project.Code;
        var category = string.IsNullOrWhiteSpace(categoryCode) ? "GEN" : categoryCode;

        return $"{prefix}-{category}-{count + 1:D5}";
    }

    public async Task<string> NextMasterCodeAsync<T>(DbSet<T> set, string prefix) where T : BaseEntity
    {
        var count = await set.ForCompany(tenant).CountAsync();
        return $"{prefix}-{count + 1:D5}";
    }

    private async Task<string> NextYearlyAsync<T>(DbSet<T> set, string prefix, DateTime now) where T : BaseEntity
    {
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var count = await set.ForCompany(tenant).CountAsync(x => x.CreatedAt >= yearStart);
        return $"{prefix}-{now:yy}-{count + 1:D5}";
    }

    private async Task<string> NextDailyAsync<T>(
        DbSet<T> set, string prefix, DateTime now,
        System.Linq.Expressions.Expression<Func<T, bool>> todayFilter) where T : BaseEntity
    {
        var count = await set.ForTenant(tenant).Where(todayFilter).CountAsync();
        return $"{prefix}-{now:yyMMdd}-{count + 1:D4}";
    }
}
