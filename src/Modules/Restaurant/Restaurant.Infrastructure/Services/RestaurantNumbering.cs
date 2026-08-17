using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Human-facing document numbers: <c>ORD-260816-0042</c>.
///
/// The date is part of the number on purpose. Restaurant documents are read aloud across a
/// noisy pass and reconciled against a paper day-book, so a number that says which service it
/// belongs to is worth more than a globally dense sequence. Numbers reset each day per outlet.
///
/// Uniqueness is enforced by the database (see the tenant+number indexes in
/// <see cref="RestaurantDbContext"/>); this only has to produce a good candidate, and the callers
/// retry on the rare collision between two tills numbering the same millisecond.
/// </summary>
public class RestaurantNumbering(RestaurantDbContext db, IRestaurantTenant tenant)
{
    public Task<string> NextOrderNumberAsync(Guid outletId, DateTime now)
        => NextAsync(db.Orders, "ORD", outletId, now, o => o.OutletId == outletId && o.OpenedAt >= now.Date);

    public Task<string> NextCheckNumberAsync(Guid outletId, DateTime now)
        => NextAsync(db.Checks, "CHK", outletId, now, c => c.OutletId == outletId && c.CreatedAt >= now.Date);

    public Task<string> NextTicketNumberAsync(Guid outletId, DateTime now)
        => NextAsync(db.KitchenTickets, "KOT", outletId, now, t => t.OutletId == outletId && t.FiredAt >= now.Date);

    public Task<string> NextSessionNumberAsync(Guid outletId, DateTime now)
        => NextAsync(db.Sessions, "SES", outletId, now, s => s.OutletId == outletId && s.OpenedAt >= now.Date);

    public Task<string> NextReservationNumberAsync(Guid outletId, DateTime now)
        => NextAsync(db.Reservations, "RSV", outletId, now, r => r.OutletId == outletId && r.CreatedAt >= now.Date);

    /// <summary>
    /// Short queue token for counter service. Letter rolls A–Z over the day so the number stays
    /// two characters wide on a small display; guests only need it to be unique among the dozen
    /// orders currently waiting.
    /// </summary>
    public async Task<string> NextTokenAsync(Guid outletId, DateTime now)
    {
        var todayCount = await db.Orders
            .ForTenant(tenant)
            .CountAsync(o => o.OutletId == outletId && o.OpenedAt >= now.Date);

        var letter = (char)('A' + todayCount / 99 % 26);
        return $"{letter}-{todayCount % 99 + 1:D2}";
    }

    private async Task<string> NextAsync<T>(
        DbSet<T> set, string prefix, Guid outletId, DateTime now,
        System.Linq.Expressions.Expression<Func<T, bool>> todayFilter) where T : BaseEntity
    {
        var count = await set.ForTenant(tenant).Where(todayFilter).CountAsync();
        return $"{prefix}-{now:yyMMdd}-{count + 1:D4}";
    }
}
