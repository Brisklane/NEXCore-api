using Fitness.Domain.Entities;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Human-facing document numbers.
///
/// Two shapes, on purpose. **Member numbers are dense and permanent** — <c>MEM-000123</c> — because
/// a member reads theirs out on the phone for years and a date in it would be noise. **Documents
/// carry their date** — <c>INV-260822-0042</c> — because an invoice, a work order or a cash session
/// is reconciled against a day, and knowing which day from the number alone saves a lookup.
///
/// Uniqueness is enforced by the database (see the tenant+number indexes in
/// <see cref="FitnessDbContext"/>); this only has to produce a good candidate, and callers retry
/// on the rare collision between two tills numbering in the same millisecond.
/// </summary>
public class FitnessNumbering(FitnessDbContext db, IFitnessTenant tenant)
{
    /// <summary>
    /// Dense sequential member number. Uses the highest existing number rather than a count, so a
    /// deleted member never causes the next join to reuse a number that has already been printed
    /// on a card.
    /// </summary>
    public async Task<string> NextMemberNumberAsync(string prefix = "MEM")
    {
        var existing = await db.Members
            .ForTenantIncludingDeleted(tenant)
            .Where(m => m.MemberNumber.StartsWith(prefix + "-"))
            .Select(m => m.MemberNumber)
            .ToListAsync();

        var highest = existing
            .Select(n => int.TryParse(n[(prefix.Length + 1)..], out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}-{highest + 1:D6}";
    }

    public Task<string> NextAgreementNumberAsync()
        => NextDenseAsync(db.Agreements.Select(a => a.AgreementNumber), "AGR");

    public Task<string> NextInvoiceNumberAsync(DateTime now)
        => NextDatedAsync(db.Invoices, "INV", now, i => i.IssuedOn >= now.Date);

    public Task<string> NextPaymentNumberAsync(DateTime now)
        => NextDatedAsync(db.Payments, "PAY", now, p => p.ReceivedOn >= now.Date);

    public Task<string> NextCreditNoteNumberAsync(DateTime now)
        => NextDatedAsync(db.CreditNotes, "CRN", now, c => c.IssuedOn >= now.Date);

    public Task<string> NextRefundNumberAsync(DateTime now)
        => NextDatedAsync(db.Refunds, "REF", now, r => r.RequestedOn >= now.Date);

    public Task<string> NextSaleNumberAsync(Guid clubId, DateTime now)
        => NextDatedAsync(db.Sales, "SAL", now, s => s.ClubId == clubId && s.SoldAt >= now.Date);

    public Task<string> NextSessionNumberAsync(Guid clubId, DateTime now)
        => NextDatedAsync(db.CashSessions, "CSH", now, s => s.ClubId == clubId && s.OpenedAt >= now.Date);

    public Task<string> NextAppointmentNumberAsync(DateTime now)
        => NextDatedAsync(db.Appointments, "APT", now, a => a.CreatedAt >= now.Date);

    public Task<string> NextPackageNumberAsync(DateTime now)
        => NextDatedAsync(db.PackagePurchases, "PKG", now, p => p.PurchasedOn >= now.Date);

    public Task<string> NextDunningCaseNumberAsync(DateTime now)
        => NextDatedAsync(db.DunningCases, "DUN", now, c => c.OpenedOn >= now.Date);

    public Task<string> NextBillingRunNumberAsync(DateTime now)
        => NextDatedAsync(db.BillingRuns, "RUN", now, r => r.CreatedAt >= now.Date);

    public Task<string> NextIncidentNumberAsync(DateTime now)
        => NextDatedAsync(db.Incidents, "INC", now, i => i.ReportedAt >= now.Date);

    public Task<string> NextComplaintNumberAsync(DateTime now)
        => NextDatedAsync(db.Complaints, "CMP", now, c => c.RaisedOn >= now.Date);

    public Task<string> NextWorkOrderNumberAsync(DateTime now)
        => NextDatedAsync(db.WorkOrders, "WKO", now, w => w.RaisedOn >= now.Date);

    public Task<string> NextDayPassNumberAsync(Guid clubId, DateTime now)
        => NextDatedAsync(db.DayPasses, "DAY", now, p => p.ClubId == clubId && p.ValidFrom >= now.Date);

    public Task<string> NextResourceBookingNumberAsync(DateTime now)
        => NextDatedAsync(db.ResourceBookings, "RES", now, r => r.CreatedAt >= now.Date);

    public Task<string> NextStatementNumberAsync(DateTime now)
        => NextDatedAsync(db.CommissionStatements, "CMS", now, s => s.CreatedAt >= now.Date);

    public Task<string> NextCorporateInvoiceNumberAsync(DateTime now)
        => NextDatedAsync(db.CorporateInvoices, "CIN", now, i => i.IssuedOn >= now.Date);

    /// <summary>
    /// A gift-card number a person can read off a card without transcription errors: no letters
    /// that look like digits, grouped in fours.
    /// </summary>
    public static string NewGiftCardNumber()
    {
        const string alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        var chars = new char[16];
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        for (var i = 0; i < 16; i++) chars[i] = alphabet[bytes[i] % alphabet.Length];
        return $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}-{new string(chars, 8, 4)}-{new string(chars, 12, 4)}";
    }

    /// <summary>A referral code short enough to say out loud and paste into a message.</summary>
    public static string NewReferralCode(string firstName)
    {
        var stem = new string((firstName ?? "REF").Where(char.IsLetter).Take(4).ToArray()).ToUpperInvariant();
        if (stem.Length == 0) stem = "REF";
        var suffix = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000, 9999);
        return $"{stem}{suffix}";
    }

    private async Task<string> NextDatedAsync<T>(
        DbSet<T> set, string prefix, DateTime now,
        System.Linq.Expressions.Expression<Func<T, bool>> todayFilter) where T : BaseEntity
    {
        var count = await set.ForTenant(tenant).Where(todayFilter).CountAsync();
        return $"{prefix}-{now:yyMMdd}-{count + 1:D4}";
    }

    private async Task<string> NextDenseAsync(IQueryable<string> numbers, string prefix)
    {
        var existing = await numbers
            .Where(n => n.StartsWith(prefix + "-"))
            .ToListAsync();

        var highest = existing
            .Select(n => int.TryParse(n[(prefix.Length + 1)..], out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}-{highest + 1:D6}";
    }
}
