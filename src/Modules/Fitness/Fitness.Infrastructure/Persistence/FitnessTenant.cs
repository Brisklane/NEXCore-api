using System.Linq.Expressions;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Helpers;

namespace Fitness.Infrastructure.Persistence;

/// <summary>
/// The caller's tenant scope, resolved once per request from the JWT.
///
/// Fitness reads are join-heavy — a member 360 is one person joined to agreements, invoices,
/// bookings, credits and alerts; a front-desk load is five of those at once — so the services
/// query the DbContext directly rather than going through one repository per entity. That makes
/// tenant filtering something a developer could forget, so it is centralised here and applied
/// through <see cref="FitnessQueryExtensions.ForTenant{T}"/> on every single query.
/// </summary>
public interface IFitnessTenant
{
    Guid CompanyId { get; }
    Guid BranchId { get; }
    Guid BusinessUnitId { get; }

    /// <summary>Signed-in ERP user, used for audit stamps. <see cref="Guid.Empty"/> when unauthenticated.</summary>
    Guid UserId { get; }
}

/// <inheritdoc />
public class HttpFitnessTenant : IFitnessTenant
{
    private readonly Lazy<(Guid Company, Guid Branch, Guid BusinessUnit, Guid User)> _scope;

    public HttpFitnessTenant(IHttpContextAccessor accessor)
    {
        _scope = new Lazy<(Guid, Guid, Guid, Guid)>(() =>
        {
            var user = accessor.HttpContext?.User
                ?? throw new InvalidOperationException("No authenticated request context.");

            var (companyId, branchId, businessUnitId) = TenantContextHelper.ExtractTenantContext(user);
            if (businessUnitId is null)
                throw new InvalidOperationException("BusinessUnitId claim not found or invalid");

            Guid userId;
            try { userId = TenantContextHelper.ExtractUserId(user); }
            catch (InvalidOperationException) { userId = Guid.Empty; }

            return (companyId, branchId, businessUnitId.Value, userId);
        });
    }

    public Guid CompanyId => _scope.Value.Company;
    public Guid BranchId => _scope.Value.Branch;
    public Guid BusinessUnitId => _scope.Value.BusinessUnit;
    public Guid UserId => _scope.Value.User;
}

/// <summary>Fixed scope, used by the seeder and the nightly jobs — both run outside any HTTP request.</summary>
public sealed class FixedFitnessTenant(Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
    : IFitnessTenant
{
    public Guid CompanyId { get; } = companyId;
    public Guid BranchId { get; } = branchId;
    public Guid BusinessUnitId { get; } = businessUnitId;
    public Guid UserId { get; } = userId;
}

public static class FitnessQueryExtensions
{
    /// <summary>
    /// Narrows a query to the caller's company, branch and business unit, and drops soft-deleted
    /// rows. Every read in this module goes through it — no exceptions.
    /// </summary>
    public static IQueryable<T> ForTenant<T>(this IQueryable<T> query, IFitnessTenant tenant)
        where T : BaseEntity
        => query.Where(e =>
            e.CompanyId == tenant.CompanyId &&
            e.BranchId == tenant.BranchId &&
            e.BusinessUnitId == tenant.BusinessUnitId &&
            !e.IsDeleted);

    /// <summary>Same as <see cref="ForTenant{T}"/>, but keeps soft-deleted rows for audit views.</summary>
    public static IQueryable<T> ForTenantIncludingDeleted<T>(this IQueryable<T> query, IFitnessTenant tenant)
        where T : BaseEntity
        => query.Where(e =>
            e.CompanyId == tenant.CompanyId &&
            e.BranchId == tenant.BranchId &&
            e.BusinessUnitId == tenant.BusinessUnitId);

    /// <summary>Stamps tenant scope and creation audit onto a new row.</summary>
    public static T StampNew<T>(this T entity, IFitnessTenant tenant, Guid? userId = null) where T : BaseEntity
    {
        entity.CompanyId = tenant.CompanyId;
        entity.BranchId = tenant.BranchId;
        entity.BusinessUnitId = tenant.BusinessUnitId;
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedByUserId = userId ?? tenant.UserId;
        return entity;
    }

    /// <summary>Stamps the update audit. Never touches tenant scope — a row cannot change owner.</summary>
    public static T StampUpdated<T>(this T entity, Guid userId) where T : BaseEntity
    {
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;
        return entity;
    }

    /// <summary>Marks a row deleted rather than removing it, preserving history and references.</summary>
    public static T StampDeleted<T>(this T entity, Guid userId) where T : BaseEntity
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        return entity;
    }

    /// <summary>Applies a filter only when the value is present, keeping query builders flat.</summary>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
        => condition ? query.Where(predicate) : query;

    /// <summary>
    /// True when a day-of-week bit mask covers this date. Sunday is bit 0, so Monday–Friday is 62.
    ///
    /// Kept here rather than inlined because six different engines — access, booking, resources,
    /// rota, checks, promotions — all ask the same question, and having them disagree by one day
    /// would be an extremely annoying bug to find.
    /// </summary>
    public static bool CoversDay(int daysOfWeekMask, DayOfWeek day)
        => (daysOfWeekMask & (1 << (int)day)) != 0;

    /// <summary>True when a time falls inside a window, handling one that wraps past midnight.</summary>
    public static bool WithinWindow(TimeSpan at, TimeSpan from, TimeSpan to)
        => from <= to ? at >= from && at <= to : at >= from || at <= to;

    /// <summary>Days in a billing period, for proration arithmetic.</summary>
    public static int DaysIn(BillingPeriod period, DateTime from) => period switch
    {
        BillingPeriod.Weekly => 7,
        BillingPeriod.Fortnightly => 14,
        BillingPeriod.FourWeekly => 28,
        BillingPeriod.Monthly => DateTime.DaysInMonth(from.Year, from.Month),
        BillingPeriod.Quarterly => 91,
        BillingPeriod.SemiAnnual => 182,
        BillingPeriod.Annual => DateTime.IsLeapYear(from.Year) ? 366 : 365,
        _ => 0,
    };

    /// <summary>Moves a date forward by one billing period.</summary>
    public static DateTime AddPeriod(this DateTime from, BillingPeriod period) => period switch
    {
        BillingPeriod.Weekly => from.AddDays(7),
        BillingPeriod.Fortnightly => from.AddDays(14),
        BillingPeriod.FourWeekly => from.AddDays(28),
        BillingPeriod.Monthly => from.AddMonths(1),
        BillingPeriod.Quarterly => from.AddMonths(3),
        BillingPeriod.SemiAnnual => from.AddMonths(6),
        BillingPeriod.Annual => from.AddYears(1),
        _ => from,
    };

    /// <summary>Monthly-equivalent value of a price on any billing period, for MRR reporting.</summary>
    public static decimal ToMonthly(decimal amount, BillingPeriod period) => period switch
    {
        BillingPeriod.Weekly => amount * 52m / 12m,
        BillingPeriod.Fortnightly => amount * 26m / 12m,
        BillingPeriod.FourWeekly => amount * 13m / 12m,
        BillingPeriod.Monthly => amount,
        BillingPeriod.Quarterly => amount / 3m,
        BillingPeriod.SemiAnnual => amount / 6m,
        BillingPeriod.Annual => amount / 12m,
        _ => 0m,
    };
}
