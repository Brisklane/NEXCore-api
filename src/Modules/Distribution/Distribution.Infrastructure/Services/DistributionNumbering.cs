using Distribution.Domain.Entities;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Human-facing document numbers: <c>ORD-260818-0042</c>, <c>STL-260818-0007</c>.
///
/// The date is part of the number on purpose. Distribution documents are read out over a phone
/// from a market, quoted on a claim six weeks later, and reconciled against a paper day-book, so a
/// number that says which day it belongs to is worth more than a globally dense sequence. Numbers
/// reset daily.
///
/// Uniqueness is enforced by the database (see the tenant+number indexes in
/// <see cref="DistributionDbContext"/>); this only has to produce a good candidate, and the callers
/// retry on the rare collision between two devices numbering in the same second.
/// </summary>
public class DistributionNumbering(DistributionDbContext db, IDistributionTenant tenant)
{
    public Task<string> NextOrderNumberAsync(DateTime now)
        => NextAsync(db.Orders, "ORD", now, o => o.OrderDate >= now.Date);

    public Task<string> NextLoadNumberAsync(DateTime now)
        => NextAsync(db.VanLoadSheets, "LOD", now, l => l.LoadDate >= now.Date);

    public Task<string> NextCountNumberAsync(DateTime now)
        => NextAsync(db.VanCycleCounts, "CNT", now, c => c.CountedAt >= now.Date);

    public Task<string> NextTripNumberAsync(DateTime now)
        => NextAsync(db.Trips, "TRP", now, t => t.TripDate >= now.Date);

    public Task<string> NextDispatchNumberAsync(DateTime now)
        => NextAsync(db.Dispatches, "DSP", now, d => d.DispatchedAt >= now.Date);

    public Task<string> NextWaveNumberAsync(DateTime now)
        => NextAsync(db.PickWaves, "WAV", now, w => w.ReleasedAt >= now.Date);

    public Task<string> NextPodNumberAsync(DateTime now)
        => NextAsync(db.ProofsOfDelivery, "POD", now, p => p.DeliveredAt >= now.Date);

    public Task<string> NextReturnNumberAsync(DateTime now)
        => NextAsync(db.Returns, "RTN", now, r => r.RequestedOn >= now.Date);

    public Task<string> NextReturnReceiptNumberAsync(DateTime now)
        => NextAsync(db.ReturnReceipts, "RRC", now, r => r.ReceivedAt >= now.Date);

    public Task<string> NextCollectionNumberAsync(DateTime now)
        => NextAsync(db.Collections, "RCP", now, c => c.CollectedAt >= now.Date);

    public Task<string> NextSettlementNumberAsync(DateTime now)
        => NextAsync(db.Settlements, "STL", now, s => s.SettlementDate >= now.Date);

    public Task<string> NextDepositNumberAsync(DateTime now)
        => NextAsync(db.CashDeposits, "DEP", now, d => d.DepositedOn >= now.Date);

    public Task<string> NextClaimNumberAsync(DateTime now)
        => NextAsync(db.Claims, "CLM", now, c => c.SubmittedOn >= now.Date);

    public Task<string> NextTransferNumberAsync(DateTime now)
        => NextAsync(db.TransferRequests, "TRF", now, t => t.RequestedOn >= now.Date);

    public Task<string> NextDeclarationNumberAsync(DateTime now)
        => NextAsync(db.StockDeclarations, "DEC", now, d => d.SubmittedAt >= now.Date);

    public Task<string> NextUploadNumberAsync(DateTime now)
        => NextAsync(db.SecondaryUploads, "UPL", now, u => u.SubmittedAt >= now.Date);

    public Task<string> NextRecallNumberAsync(DateTime now)
        => NextAsync(db.Recalls, "RCL", now, r => r.InitiatedOn >= now.Date);

    public Task<string> NextChargebackNumberAsync(DateTime now)
        => NextAsync(db.Chargebacks, "CBK", now, c => c.SaleDate >= now.Date);

    /// <summary>
    /// Scheme numbers are yearly rather than daily. A scheme lives for a quarter and gets quoted
    /// in negotiations for a year after it ends, so <c>SCH-26-0042</c> reads better than a date.
    /// </summary>
    public async Task<string> NextSchemeNumberAsync(DateTime now)
    {
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var count = await db.Schemes.ForCompany(tenant).CountAsync(s => s.CreatedAt >= yearStart);
        return $"SCH-{now:yy}-{count + 1:D4}";
    }

    /// <summary>
    /// Codes for the masters: outlets, partners, routes, reps. Sequential per company and never
    /// reset, because these are quoted for the life of the relationship.
    /// </summary>
    public async Task<string> NextMasterCodeAsync<T>(DbSet<T> set, string prefix) where T : BaseEntity
    {
        var count = await set.ForCompany(tenant).CountAsync();
        return $"{prefix}-{count + 1:D5}";
    }

    private async Task<string> NextAsync<T>(
        DbSet<T> set, string prefix, DateTime now,
        System.Linq.Expressions.Expression<Func<T, bool>> todayFilter) where T : BaseEntity
    {
        var count = await set.ForTenant(tenant).Where(todayFilter).CountAsync();
        return $"{prefix}-{now:yyMMdd}-{count + 1:D4}";
    }
}
