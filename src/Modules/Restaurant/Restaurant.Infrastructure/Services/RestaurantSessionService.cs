using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Cash sessions and the X/Z reads that close a trading day.
///
/// The control that matters here is the blind close: the cashier counts the drawer without being
/// shown what the system expects. Showing the expected figure first turns a count into a
/// copy-and-paste, and a shortage becomes invisible. So the expected totals are stripped out of
/// the DTO for an open blind session and only appear once the count is committed.
/// </summary>
public class RestaurantSessionService(
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    RestaurantNumbering numbering,
    IRestaurantStaffService staff) : IRestaurantSessionService
{
    public async Task<RestaurantSessionDto> OpenSessionAsync(OpenSessionDto request, Guid userId)
    {
        var existing = await db.Sessions.ForTenant(tenant)
            .FirstOrDefaultAsync(s => s.OutletId == request.OutletId
                                   && s.Status == SessionStatus.Open
                                   && s.TerminalName == request.TerminalName);

        // Two open sessions on one drawer means neither count can be attributed to anybody.
        if (existing is not null)
            throw new InvalidOperationException(
                $"Terminal '{request.TerminalName}' already has an open session ({existing.SessionNumber}).");

        var now = DateTime.UtcNow;
        var cashier = request.CashierId.HasValue
            ? await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.CashierId)
            : null;

        var session = new RestaurantSession
        {
            OutletId = request.OutletId,
            SessionNumber = await numbering.NextSessionNumberAsync(request.OutletId, now),
            Status = SessionStatus.Open,
            CashierId = request.CashierId,
            CashierName = cashier?.DisplayName ?? cashier?.FullName,
            TerminalName = request.TerminalName,
            OpenedAt = now,
            OpeningFloat = request.OpeningFloat,
            ExpectedCash = request.OpeningFloat,
            IsBlindClose = request.IsBlindClose,
        }.StampNew(tenant, userId);

        session.Code = session.SessionNumber;
        db.Sessions.Add(session);

        if (request.OpeningFloat > 0)
        {
            db.CashMovements.Add(new SessionCashMovement
            {
                SessionId = session.Id,
                OutletId = session.OutletId,
                MovementType = CashMovementType.OpeningFloat,
                Amount = request.OpeningFloat,
                Reason = "Opening float",
                StaffId = request.CashierId,
                StaffName = session.CashierName,
                OccurredAt = now,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
        return Project(session);
    }

    public async Task<RestaurantSessionDto> CloseSessionAsync(CloseSessionDto request, Guid userId)
    {
        var session = await db.Sessions.ForTenant(tenant)
            .Include(s => s.CashMovements.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == request.SessionId)
            ?? throw new InvalidOperationException("Session not found.");

        if (session.Status == SessionStatus.Closed)
            throw new InvalidOperationException("This session is already closed.");

        // Closing with money still owed hides the shortfall in the next day's figures.
        var openChecks = await db.Checks.ForTenant(tenant)
            .CountAsync(c => c.SessionId == session.Id && !c.IsVoided && c.Status != CheckStatus.Paid);

        if (openChecks > 0)
            throw new InvalidOperationException(
                $"{openChecks} check(s) on this session are still unpaid. Settle or void them first.");

        if (!string.IsNullOrWhiteSpace(request.ApprovalPin))
        {
            _ = await staff.VerifyApprovalAsync(session.OutletId, request.ApprovalPin, "session")
                ?? throw new UnauthorizedAccessException("That PIN is not authorised to close a session.");
        }

        var now = DateTime.UtcNow;

        session.CountedCash = request.CountedCash;
        session.CountedCard = request.CountedCard;
        session.CountedOther = request.CountedOther;
        session.CashVariance = Math.Round(request.CountedCash - session.ExpectedCash, 2);
        session.ClosingNote = request.ClosingNote;
        session.Status = SessionStatus.Closed;
        session.ClosedAt = now;
        session.ZReadAt = now;
        session.StampUpdated(userId);

        db.CashMovements.Add(new SessionCashMovement
        {
            SessionId = session.Id,
            OutletId = session.OutletId,
            MovementType = CashMovementType.ClosingCount,
            Amount = request.CountedCash,
            Reason = "Closing count",
            StaffId = request.StaffId ?? session.CashierId,
            StaffName = session.CashierName,
            OccurredAt = now,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return Project(session);
    }

    public async Task<RestaurantSessionDto?> GetSessionAsync(Guid sessionId)
    {
        var session = await db.Sessions.ForTenant(tenant)
            .Include(s => s.CashMovements.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        return session is null ? null : Project(session);
    }

    public async Task<RestaurantSessionDto?> GetOpenSessionAsync(Guid outletId, string? terminalName)
    {
        var session = await db.Sessions.ForTenant(tenant)
            .Where(s => s.OutletId == outletId && s.Status == SessionStatus.Open)
            .WhereIf(!string.IsNullOrWhiteSpace(terminalName), s => s.TerminalName == terminalName)
            .Include(s => s.CashMovements.Where(m => !m.IsDeleted))
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync();

        return session is null ? null : Project(session);
    }

    public async Task<PaginatedResponse<RestaurantSessionDto>> ListSessionsAsync(
        Guid? outletId, SessionStatus? status, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Sessions.ForTenant(tenant)
            .WhereIf(outletId.HasValue, s => s.OutletId == outletId)
            .WhereIf(status.HasValue, s => s.Status == status)
            .WhereIf(from.HasValue, s => s.OpenedAt >= from)
            .WhereIf(to.HasValue, s => s.OpenedAt <= to);

        var total = await query.CountAsync();

        var sessions = await query
            .OrderByDescending(s => s.OpenedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var outletNames = await db.Outlets.ForTenant(tenant).ToDictionaryAsync(o => o.Id, o => o.Name);

        var dtos = sessions.Select(s =>
        {
            var dto = Project(s);
            dto.OutletName = outletNames.GetValueOrDefault(s.OutletId);
            return dto;
        }).ToList();

        return PaginatedResponse<RestaurantSessionDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<SessionCashMovementDto> AddCashMovementAsync(AddCashMovementDto request, Guid userId)
    {
        var session = await db.Sessions.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.SessionId)
            ?? throw new InvalidOperationException("Session not found.");

        if (session.Status != SessionStatus.Open)
            throw new InvalidOperationException("Cash can only move on an open session.");

        if (request.Amount == 0) throw new InvalidOperationException("The amount cannot be zero.");

        var member = request.StaffId.HasValue
            ? await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.StaffId)
            : null;

        // The sign is derived from the movement type rather than trusted from the client, so a
        // payout can never be recorded as cash coming in.
        var signed = request.MovementType switch
        {
            CashMovementType.CashIn or CashMovementType.OpeningFloat => Math.Abs(request.Amount),
            CashMovementType.CashOut or CashMovementType.Drop or CashMovementType.Payout => -Math.Abs(request.Amount),
            _ => request.Amount,
        };

        var movement = new SessionCashMovement
        {
            SessionId = session.Id,
            OutletId = session.OutletId,
            MovementType = request.MovementType,
            Amount = signed,
            Reason = request.Reason,
            Reference = request.Reference,
            StaffId = request.StaffId,
            StaffName = member?.DisplayName ?? member?.FullName,
            OccurredAt = DateTime.UtcNow,
        }.StampNew(tenant, userId);

        db.CashMovements.Add(movement);

        session.ExpectedCash = Math.Round(session.ExpectedCash + signed, 2);
        session.StampUpdated(userId);

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(movement);
    }

    public Task<ReadReportDto> GetXReadAsync(Guid sessionId) => BuildReadAsync(sessionId, "X", null);

    public async Task<ReadReportDto> GetZReadAsync(Guid sessionId, Guid userId)
    {
        var session = await db.Sessions.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new InvalidOperationException("Session not found.");

        // A Z-read is the definitive end-of-day figure and must be producible exactly once, so it
        // is only available on a closed session and stamps the time it was taken.
        if (session.Status != SessionStatus.Closed)
            throw new InvalidOperationException("Close the session before taking a Z-read.");

        return await BuildReadAsync(sessionId, "Z", userId);
    }

    private async Task<ReadReportDto> BuildReadAsync(Guid sessionId, string kind, Guid? userId)
    {
        var session = await db.Sessions.ForTenant(tenant)
            .Include(s => s.CashMovements.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new InvalidOperationException("Session not found.");

        var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == session.OutletId);

        var checks = await db.Checks.ForTenant(tenant)
            .Where(c => c.SessionId == session.Id && !c.IsVoided)
            .ToListAsync();

        var payments = await db.CheckPayments.ForTenant(tenant)
            .Where(p => p.SessionId == session.Id)
            .ToListAsync();

        var orderIds = checks.Select(c => c.OrderId).Distinct().ToList();
        var covers = await db.Orders.ForTenant(tenant)
            .Where(o => orderIds.Contains(o.Id)).SumAsync(o => (int?)o.GuestCount) ?? 0;

        var settled = checks.Where(c => c.Status == CheckStatus.Paid).ToList();
        var grossSales = Math.Round(checks.Sum(c => c.SubTotal), 2);
        var netSales = Math.Round(grossSales - checks.Sum(c => c.DiscountAmount), 2);

        var realPayments = payments.Where(p => !p.IsRefund).ToList();
        var totalTendered = realPayments.Sum(p => p.Amount);

        var tenderBreakdown = realPayments
            .GroupBy(p => p.TenderType)
            .Select(g => new TenderBreakdownDto
            {
                TenderType = g.Key,
                TenderName = g.Key.ToString(),
                Count = g.Count(),
                Amount = Math.Round(g.Sum(p => p.Amount), 2),
                TipAmount = Math.Round(g.Sum(p => p.TipAmount), 2),
                SharePercent = totalTendered > 0 ? Math.Round(g.Sum(p => p.Amount) / totalTendered * 100m, 2) : 0m,
            })
            .OrderByDescending(t => t.Amount)
            .ToList();

        var checkIds = checks.Select(c => c.Id).ToList();
        var lines = await db.CheckLines.ForTenant(tenant)
            .Where(l => checkIds.Contains(l.CheckId))
            .ToListAsync();

        var itemIds = lines.Select(l => l.MenuItemId).Distinct().ToList();
        var itemCategories = await db.MenuItems.ForTenant(tenant)
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.CategoryId })
            .ToListAsync();

        var categoryNames = await db.MenuCategories.ForTenant(tenant)
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var categoryBreakdown = lines
            .GroupBy(l => itemCategories.FirstOrDefault(i => i.Id == l.MenuItemId)?.CategoryId ?? Guid.Empty)
            .Select(g => new CategorySalesDto
            {
                CategoryId = g.Key,
                CategoryName = categoryNames.GetValueOrDefault(g.Key) ?? "Uncategorised",
                Quantity = Math.Round(g.Sum(l => l.Quantity), 3),
                Amount = Math.Round(g.Sum(l => l.LineTotal), 2),
                CostAmount = Math.Round(g.Sum(l => l.UnitCost * l.Quantity), 2),
                MarginAmount = Math.Round(g.Sum(l => l.LineTotal - l.UnitCost * l.Quantity), 2),
                SharePercent = grossSales > 0 ? Math.Round(g.Sum(l => l.LineTotal) / grossSales * 100m, 2) : 0m,
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        if (kind == "X")
        {
            session.XReadCount++;
            session.StampUpdated(userId ?? tenant.UserId);
            await db.SaveChangesAsync();
        }

        return new ReadReportDto
        {
            SessionId = session.Id,
            SessionNumber = session.SessionNumber,
            ReportType = kind,
            OutletName = outlet?.Name ?? string.Empty,
            CashierName = session.CashierName,
            TerminalName = session.TerminalName,
            OpenedAt = session.OpenedAt,
            ClosedAt = session.ClosedAt,
            GeneratedAt = DateTime.UtcNow,
            CurrencyCode = outlet?.CurrencyCode ?? "USD",
            OpeningFloat = session.OpeningFloat,
            GrossSales = grossSales,
            Discounts = Math.Round(checks.Sum(c => c.DiscountAmount), 2),
            ServiceCharge = Math.Round(checks.Sum(c => c.ServiceChargeAmount), 2),
            Tax = Math.Round(checks.Sum(c => c.TaxAmount), 2),
            NetSales = netSales,
            Tips = Math.Round(realPayments.Sum(p => p.TipAmount), 2),
            Voids = session.TotalVoids,
            Refunds = Math.Round(payments.Where(p => p.IsRefund).Sum(p => p.Amount), 2),
            ExpectedCash = session.ExpectedCash,
            CountedCash = session.CountedCash,
            CashVariance = session.CashVariance,
            OrderCount = orderIds.Count,
            CheckCount = settled.Count,
            CoverCount = covers,
            AverageCheck = settled.Count == 0 ? 0m : Math.Round(settled.Sum(c => c.TotalAmount) / settled.Count, 2),
            AveragePerCover = covers == 0 ? 0m : Math.Round(settled.Sum(c => c.TotalAmount) / covers, 2),
            TenderBreakdown = tenderBreakdown,
            CategoryBreakdown = categoryBreakdown,
            CashMovements = session.CashMovements.OrderBy(m => m.OccurredAt).Select(RestaurantMapper.ToDto).ToList(),
        };
    }

    /// <summary>
    /// Projects a session, withholding the expected figures while a blind-close session is still
    /// open. Sending them and hiding them client-side would put the control one dev-tools tab away.
    /// </summary>
    private static RestaurantSessionDto Project(RestaurantSession session)
    {
        var dto = RestaurantMapper.ToDto(session);

        if (session.IsBlindClose && session.Status == SessionStatus.Open)
        {
            dto.ExpectedCash = 0m;
            dto.ExpectedCard = 0m;
            dto.ExpectedOther = 0m;
            dto.CashVariance = 0m;
        }

        return dto;
    }
}
