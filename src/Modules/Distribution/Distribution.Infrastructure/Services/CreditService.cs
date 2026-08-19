using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Credit control, collections and cheques.
///
/// The design constraint that shapes this service is that the block has to work **at the counter,
/// offline**. So the credit position is kept as a stored snapshot rather than computed on demand:
/// the field terminal syncs <see cref="CreditSnapshotDto"/> down with the beat and evaluates the
/// rule locally, and the server re-checks on submission. A limit that only works when there is
/// signal is not a limit.
///
/// A bounced cheque is treated as a real event, not a status change: it reverses the allocation,
/// charges the fee, increments the counter and — if configured — blocks the outlet, all in one
/// call, because those four things always happen together and doing them separately is how one
/// gets forgotten.
/// </summary>
public class CreditService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering) : ICreditService
{
    // ═══ Position ════════════════════════════════════════════════════════════

    public async Task<CreditSnapshotDto> GetSnapshotAsync(Guid? outletId, Guid? partnerId)
    {
        var profile = await FindProfileAsync(outletId, partnerId);
        return profile?.ToSnapshot() ?? new CreditSnapshotDto { OutletId = outletId, PartnerId = partnerId };
    }

    public async Task<CreditCheckResultDto> CheckAsync(
        Guid? outletId, Guid? partnerId, decimal orderValue, CreditEnforcement? enforcement)
    {
        var profile = await FindProfileAsync(outletId, partnerId);

        // No profile means no limit has ever been set. Fail open with a warning rather than
        // blocking a sale on a master-data gap — but say so, so somebody fixes it.
        if (profile is null)
            return new CreditCheckResultDto
            {
                IsAllowed = true,
                Enforcement = CreditEnforcement.Off,
                OrderValue = orderValue,
                Message = "No credit limit is set for this account.",
            };

        var mode = enforcement ?? profile.Enforcement;
        var snapshot = profile.ToSnapshot();

        var result = new CreditCheckResultDto
        {
            Enforcement = mode,
            OrderValue = orderValue,
            AvailableCredit = snapshot.AvailableCredit,
            OverdueAmount = snapshot.OverdueAmount,
            IsBlocked = profile.IsBlocked,
            Snapshot = snapshot,
            IsAllowed = true,
        };

        if (profile.IsBlocked)
        {
            result.IsAllowed = mode != CreditEnforcement.Block;
            result.RequiresOverride = mode == CreditEnforcement.Block;
            result.Message = string.IsNullOrWhiteSpace(profile.BlockReason)
                ? "This account is blocked."
                : $"This account is blocked: {profile.BlockReason}";
            return result;
        }

        // An overdue balance blocks before the limit does. A customer who is inside their limit
        // but ninety days late is the more dangerous of the two.
        if (snapshot.OverdueAmount > 0 && mode != CreditEnforcement.Off)
        {
            result.RequiresOverride = mode == CreditEnforcement.Block;
            result.IsAllowed = mode != CreditEnforcement.Block;
            result.Message =
                $"{snapshot.OverdueAmount:N2} is overdue ({snapshot.OldestInvoiceDays} days on the oldest invoice).";
            if (mode == CreditEnforcement.Block) return result;
        }

        var excess = orderValue - snapshot.AvailableCredit;
        if (excess > 0 && mode != CreditEnforcement.Off)
        {
            result.ExcessAmount = excess;
            result.RequiresOverride = mode == CreditEnforcement.Block;
            result.IsAllowed = mode != CreditEnforcement.Block;
            result.Message =
                $"This order is {excess:N2} over the available credit of {snapshot.AvailableCredit:N2}.";
        }

        result.Message ??= "Within credit.";
        return result;
    }

    public async Task<CreditProfileDto> SetLimitAsync(SetCreditLimitDto request, Guid userId)
    {
        var profile = await FindProfileAsync(request.OutletId, request.PartnerId);

        if (profile is null)
        {
            profile = new CreditProfile
            {
                OutletId = request.OutletId,
                PartnerId = request.PartnerId,
            }.StampNew(tenant, userId);
            db.CreditProfiles.Add(profile);
        }
        else
        {
            profile.StampUpdated(userId);
        }

        profile.CreditLimit = request.CreditLimit;
        profile.CreditDays = request.CreditDays;
        profile.Enforcement = request.Enforcement;

        // Keep the master in step so the two never disagree on the screen.
        if (request.OutletId.HasValue)
            await db.Outlets.ForTenant(tenant).Where(o => o.Id == request.OutletId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.CreditLimit, request.CreditLimit)
                    .SetProperty(o => o.CreditDays, request.CreditDays)
                    .SetProperty(o => o.CreditEnforcement, request.Enforcement));

        if (request.PartnerId.HasValue)
            await db.Partners.ForTenant(tenant).Where(p => p.Id == request.PartnerId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.CreditLimit, request.CreditLimit)
                    .SetProperty(p => p.CreditDays, request.CreditDays)
                    .SetProperty(p => p.CreditEnforcement, request.Enforcement));

        await db.SaveChangesAsync();
        return await RecalculateAsync(request.OutletId, request.PartnerId);
    }

    public async Task<CreditProfileDto> RecalculateAsync(Guid? outletId, Guid? partnerId)
    {
        var profile = await FindProfileAsync(outletId, partnerId)
            ?? throw new InvalidOperationException("No credit profile exists for that account.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var b1 = settings?.AgeingBucket1Days ?? 30;
        var b2 = settings?.AgeingBucket2Days ?? 60;
        var b3 = settings?.AgeingBucket3Days ?? 90;

        var today = DateTime.UtcNow.Date;
        var terms = profile.CreditDays;

        // Outstanding is invoiced-but-unpaid. Orders that are not yet invoiced sit in a separate
        // bucket so a big pending order eats the limit without pretending to be a debt.
        var invoices = await db.Orders.ForTenant(tenant)
            .Where(o => (outletId != null ? o.OutletId == outletId : o.PartnerId == partnerId)
                        && o.Status >= DistributionOrderStatus.Delivered
                        && o.Status != DistributionOrderStatus.Cancelled
                        && o.Status != DistributionOrderStatus.Rejected
                        && o.TotalAmount > o.PaidAmount)
            .Select(o => new { o.OrderDate, Balance = o.TotalAmount - o.PaidAmount })
            .ToListAsync();

        var unbilled = await db.Orders.ForTenant(tenant)
            .Where(o => (outletId != null ? o.OutletId == outletId : o.PartnerId == partnerId)
                        && o.Status >= DistributionOrderStatus.Approved
                        && o.Status < DistributionOrderStatus.Delivered)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        profile.Bucket0To30 = 0;
        profile.Bucket31To60 = 0;
        profile.Bucket61To90 = 0;
        profile.Bucket90Plus = 0;
        profile.OverdueAmount = 0;
        profile.OldestInvoiceDays = 0;

        foreach (var invoice in invoices)
        {
            var age = (int)(today - invoice.OrderDate.Date).TotalDays;
            profile.OldestInvoiceDays = Math.Max(profile.OldestInvoiceDays, age);

            if (age <= b1) profile.Bucket0To30 += invoice.Balance;
            else if (age <= b2) profile.Bucket31To60 += invoice.Balance;
            else if (age <= b3) profile.Bucket61To90 += invoice.Balance;
            else profile.Bucket90Plus += invoice.Balance;

            if (age > terms) profile.OverdueAmount += invoice.Balance;
        }

        profile.OutstandingAmount = invoices.Sum(i => i.Balance);
        profile.UnbilledOrderValue = unbilled;
        profile.AvailableCredit = DistributionMapper.EffectiveLimit(profile)
                                  - profile.OutstandingAmount - profile.UnbilledOrderValue;

        var lastPayment = await db.Collections.ForTenant(tenant)
            .Where(c => (outletId != null ? c.OutletId == outletId : c.PartnerId == partnerId) && !c.IsReversed)
            .OrderByDescending(c => c.CollectedAt)
            .Select(c => new { c.CollectedAt, c.Amount })
            .FirstOrDefaultAsync();

        profile.LastPaymentAt = lastPayment?.CollectedAt;
        profile.LastPaymentAmount = lastPayment?.Amount ?? 0;

        profile.BouncedChequeCount = await db.Cheques.ForTenant(tenant)
            .CountAsync(c => (outletId != null ? c.OutletId == outletId : c.PartnerId == partnerId)
                             && c.Status == ChequeStatus.Bounced);

        profile.RecalculatedAt = DateTime.UtcNow;

        if (outletId.HasValue)
            await db.Outlets.ForTenant(tenant).Where(o => o.Id == outletId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.OutstandingAmount, profile.OutstandingAmount));

        await db.SaveChangesAsync();
        await db.Entry(profile).Reference(p => p.Outlet).LoadAsync();
        await db.Entry(profile).Reference(p => p.Partner).LoadAsync();

        return profile.ToDto();
    }

    public async Task<CreditProfileDto> BlockAsync(
        Guid? outletId, Guid? partnerId, bool isBlocked, string? reason, Guid userId)
    {
        if (isBlocked && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Blocking an account needs a reason.");

        var profile = await FindProfileAsync(outletId, partnerId)
            ?? throw new InvalidOperationException("No credit profile exists for that account.");

        profile.IsBlocked = isBlocked;
        profile.BlockReason = isBlocked ? reason : null;
        profile.BlockedAt = isBlocked ? DateTime.UtcNow : null;
        profile.StampUpdated(userId);

        // The outlet's own status mirrors the block so the field list shows it without a join.
        if (outletId.HasValue)
        {
            var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == outletId);
            if (outlet is not null)
            {
                if (isBlocked)
                {
                    outlet.Status = OutletStatus.CreditBlocked;
                    outlet.StatusReason = reason;
                }
                else if (outlet.Status == OutletStatus.CreditBlocked)
                {
                    outlet.Status = OutletStatus.Active;
                    outlet.StatusReason = null;
                }
                outlet.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        await db.Entry(profile).Reference(p => p.Outlet).LoadAsync();
        return profile.ToDto();
    }

    public async Task<PaginatedResponse<CreditProfileDto>> ListProfilesAsync(
        string? search, Guid? territoryId, Guid? routeId, Guid? partnerId,
        bool? overdueOnly, bool? blockedOnly, PaginationParams pagination)
    {
        var query = db.CreditProfiles.ForTenant(tenant)
            .Include(c => c.Outlet)
            .Include(c => c.Partner)
            .WhereIf(partnerId.HasValue, c => c.PartnerId == partnerId)
            .WhereIf(overdueOnly == true, c => c.OverdueAmount > 0)
            .WhereIf(blockedOnly == true, c => c.IsBlocked)
            .WhereIf(territoryId.HasValue, c => c.Outlet != null && c.Outlet.TerritoryId == territoryId);

        if (routeId.HasValue)
        {
            var outletIds = db.RouteOutlets.ForTenant(tenant).Where(r => r.RouteId == routeId).Select(r => r.OutletId);
            query = query.Where(c => c.OutletId != null && outletIds.Contains(c.OutletId.Value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(c =>
                (c.Outlet != null && (EF.Functions.ILike(c.Outlet.Name, term) || EF.Functions.ILike(c.Outlet.Code ?? "", term)))
                || (c.Partner != null && EF.Functions.ILike(c.Partner.Name, term)));
        }

        var total = await query.CountAsync();
        var rows = await query
            // Worst first: the collections screen is a work queue, not a directory.
            .OrderByDescending(c => c.OverdueAmount).ThenByDescending(c => c.OutstandingAmount)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();

        var outletIdsForRoute = rows.Where(r => r.OutletId.HasValue).Select(r => r.OutletId!.Value).ToList();
        var routeNames = await db.RouteOutlets.ForTenant(tenant)
            .Include(r => r.Route)
            .Where(r => outletIdsForRoute.Contains(r.OutletId) && r.Route != null)
            .GroupBy(r => r.OutletId)
            .Select(g => new { OutletId = g.Key, Name = g.First().Route!.Name })
            .ToDictionaryAsync(x => x.OutletId, x => x.Name);

        foreach (var dto in dtos.Where(d => d.OutletId.HasValue))
            dto.RouteName = routeNames.GetValueOrDefault(dto.OutletId!.Value);

        return PaginatedResponse<CreditProfileDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ Overrides ═══════════════════════════════════════════════════════════

    public async Task<CreditOverrideDto> RequestOverrideAsync(RequestCreditOverrideDto request, Guid userId)
    {
        if (request.RequestedAmount <= 0)
            throw new InvalidOperationException("An override needs an amount.");
        if (string.IsNullOrWhiteSpace(request.Justification))
            throw new InvalidOperationException("An override needs a justification.");

        var entity = new CreditOverride
        {
            OutletId = request.OutletId,
            PartnerId = request.PartnerId,
            OrderId = request.OrderId,
            RequestedAmount = request.RequestedAmount,
            ReasonCodeId = request.ReasonCodeId,
            Justification = request.Justification,
            RequestedByUserId = userId,
            RequestedAt = DateTime.UtcNow,
            // An override without an end date is just a higher limit under another name.
            ExpiresOn = request.ExpiresOn ?? DateTime.UtcNow.Date.AddDays(30),
        }.StampNew(tenant, userId);

        var profile = await FindProfileAsync(request.OutletId, request.PartnerId);
        entity.CurrencyCode = profile?.CurrencyCode ?? "USD";

        db.CreditOverrides.Add(entity);
        await db.SaveChangesAsync();

        return await MapOverrideAsync(entity);
    }

    public async Task<CreditOverrideDto> DecideOverrideAsync(
        Guid overrideId, DecideCreditOverrideDto request, Guid userId)
    {
        var entity = await db.CreditOverrides.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == overrideId)
            ?? throw new InvalidOperationException("That override request no longer exists.");

        if (entity.IsApproved.HasValue)
            throw new InvalidOperationException("This override has already been decided.");

        entity.IsApproved = request.IsApproved;
        entity.ApprovedAmount = request.IsApproved ? request.ApprovedAmount : 0;
        entity.ApprovedByUserId = userId;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.DecisionNote = request.DecisionNote;
        if (request.ExpiresOn.HasValue) entity.ExpiresOn = request.ExpiresOn;
        entity.StampUpdated(userId);

        if (request.IsApproved)
        {
            var profile = await FindProfileAsync(entity.OutletId, entity.PartnerId);
            if (profile is not null)
            {
                profile.TemporaryLimit = entity.ApprovedAmount;
                profile.TemporaryLimitExpiresOn = entity.ExpiresOn;
                profile.AvailableCredit = DistributionMapper.EffectiveLimit(profile)
                                          - profile.OutstandingAmount - profile.UnbilledOrderValue;
                profile.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return await MapOverrideAsync(entity);
    }

    public async Task<List<CreditOverrideDto>> ListOverridesAsync(Guid? outletId, Guid? partnerId, bool? pendingOnly)
    {
        var rows = await db.CreditOverrides.ForTenant(tenant)
            .WhereIf(outletId.HasValue, o => o.OutletId == outletId)
            .WhereIf(partnerId.HasValue, o => o.PartnerId == partnerId)
            .WhereIf(pendingOnly == true, o => o.IsApproved == null)
            .OrderByDescending(o => o.RequestedAt)
            .ToListAsync();

        var result = new List<CreditOverrideDto>();
        foreach (var row in rows) result.Add(await MapOverrideAsync(row));
        return result;
    }

    // ═══ Collections ═════════════════════════════════════════════════════════

    public async Task<CollectionDto> RecordCollectionAsync(RecordCollectionDto request, Guid userId)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("A collection needs an amount.");
        if (request.OutletId is null && request.PartnerId is null)
            throw new InvalidOperationException("A collection must name an outlet or a partner.");

        // Idempotency: a retried sync must never double-credit an outlet's ledger.
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await db.Collections.ForTenant(tenant)
                .Include(c => c.Outlet)
                .FirstOrDefaultAsync(c => c.IdempotencyKey == request.IdempotencyKey);
            if (existing is not null) return existing.ToDto();
        }

        if (request.Tender == PaymentTender.Cheque && request.Cheque is null)
            throw new InvalidOperationException("A cheque payment needs the cheque details.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var entity = new CollectionReceipt
        {
            ReceiptNumber = await numbering.NextCollectionNumberAsync(DateTime.UtcNow),
            OutletId = request.OutletId,
            PartnerId = request.PartnerId,
            VisitId = request.VisitId,
            FieldDayId = request.FieldDayId,
            FieldRepId = request.FieldRepId,
            TripId = request.TripId,
            CollectedAt = request.CollectedAt ?? DateTime.UtcNow,
            Tender = request.Tender,
            CurrencyCode = request.CurrencyCode ?? settings?.BaseCurrencyCode ?? "USD",
            Amount = request.Amount,
            CashDiscountAmount = request.CashDiscountAmount,
            Reference = request.Reference,
            BankName = request.BankName,
            CardLast4 = request.CardLast4,
            CardScheme = request.CardScheme,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ReceiptSentTo = request.ReceiptSentTo,
            Note = request.Note,
            IdempotencyKey = request.IdempotencyKey,
        }.StampNew(tenant, userId);

        if (request.Cheque is not null)
        {
            var cheque = new ChequeRecord
            {
                ChequeNumber = request.Cheque.ChequeNumber,
                OutletId = request.OutletId,
                PartnerId = request.PartnerId,
                CollectionReceiptId = entity.Id,
                BankName = request.Cheque.BankName,
                BranchName = request.Cheque.BranchName,
                AccountName = request.Cheque.AccountName,
                Amount = request.Cheque.Amount > 0 ? request.Cheque.Amount : request.Amount,
                CurrencyCode = entity.CurrencyCode,
                ChequeDate = request.Cheque.ChequeDate,
                ReceivedOn = entity.CollectedAt,
                IsPostDated = request.Cheque.ChequeDate.Date > DateTime.UtcNow.Date,
                Note = request.Cheque.Note,
            }.StampNew(tenant, userId);

            cheque.Status = cheque.IsPostDated ? ChequeStatus.Held : ChequeStatus.Received;
            db.Cheques.Add(cheque);
            entity.ChequeId = cheque.Id;
        }

        db.Collections.Add(entity);

        // Allocation: explicit if the caller decided, oldest-first otherwise. Oldest-first is the
        // right default because it is what ageing assumes, and any other order quietly makes the
        // 90+ bucket immortal.
        var allocations = request.Allocations.Count > 0
            ? await AllocateExplicitAsync(entity, request.Allocations)
            : await AllocateOldestFirstAsync(entity);

        entity.AllocatedAmount = allocations.Sum(a => a.AllocatedAmount);
        entity.UnallocatedAmount = entity.Amount - entity.AllocatedAmount;

        await db.SaveChangesAsync();

        // A cheque is not money until it clears, so it does not move the ledger yet.
        if (request.Tender != PaymentTender.Cheque)
            await RecalculateAsync(request.OutletId, request.PartnerId);

        if (request.FieldDayId.HasValue)
            await db.FieldDays.ForTenant(tenant).Where(d => d.Id == request.FieldDayId)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.CollectedAmount, d => d.CollectedAmount + request.Amount));

        if (request.VisitId.HasValue)
            await db.Visits.ForTenant(tenant).Where(v => v.Id == request.VisitId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.CollectedAmount, v => v.CollectedAmount + request.Amount));

        var dto = entity.ToDto();
        dto.Allocations = allocations;
        return dto;
    }

    public async Task<CollectionDto?> GetCollectionAsync(Guid collectionId)
    {
        var entity = await db.Collections.ForTenant(tenant)
            .Include(c => c.Outlet)
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (entity is null) return null;

        var dto = entity.ToDto();
        if (entity.ChequeId.HasValue)
        {
            var cheque = await db.Cheques.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == entity.ChequeId);
            dto.Cheque = cheque?.ToDto();
        }

        return dto;
    }

    public async Task<PaginatedResponse<CollectionSummaryDto>> ListCollectionsAsync(
        Guid? outletId, Guid? partnerId, Guid? fieldRepId, Guid? fieldDayId, PaymentTender? tender,
        DateTime? from, DateTime? to, bool? undepositedOnly, PaginationParams pagination)
    {
        var query = db.Collections.ForTenant(tenant)
            .Include(c => c.Outlet)
            .WhereIf(outletId.HasValue, c => c.OutletId == outletId)
            .WhereIf(partnerId.HasValue, c => c.PartnerId == partnerId)
            .WhereIf(fieldRepId.HasValue, c => c.FieldRepId == fieldRepId)
            .WhereIf(fieldDayId.HasValue, c => c.FieldDayId == fieldDayId)
            .WhereIf(tender.HasValue, c => c.Tender == tender)
            .WhereIf(from.HasValue, c => c.CollectedAt >= from)
            .WhereIf(to.HasValue, c => c.CollectedAt <= to)
            .WhereIf(undepositedOnly == true, c => !c.IsDeposited && !c.IsReversed);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(c => c.CollectedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToSummary()).ToList();

        var chequeIds = rows.Where(r => r.ChequeId.HasValue).Select(r => r.ChequeId!.Value).ToList();
        var cheques = await db.Cheques.ForTenant(tenant)
            .Where(c => chequeIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Status);

        var repNames = await db.FieldReps.ForTenant(tenant).ToDictionaryAsync(r => r.Id, r => r.FullName);

        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].ChequeId.HasValue && cheques.TryGetValue(rows[i].ChequeId!.Value, out var status))
                dtos[i].ChequeStatus = status;
            if (rows[i].FieldRepId.HasValue)
                dtos[i].FieldRepName = repNames.GetValueOrDefault(rows[i].FieldRepId!.Value);
        }

        return PaginatedResponse<CollectionSummaryDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<CollectionDto> ReverseCollectionAsync(Guid collectionId, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Reversing a collection needs a reason.");

        var entity = await db.Collections.ForTenant(tenant)
            .Include(c => c.Outlet)
            .FirstOrDefaultAsync(c => c.Id == collectionId)
            ?? throw new InvalidOperationException("That receipt no longer exists.");

        if (entity.IsReversed)
            throw new InvalidOperationException("This receipt has already been reversed.");

        if (entity.IsDeposited)
            throw new InvalidOperationException(
                "This money has already been banked. Reverse the deposit first.");

        entity.IsReversed = true;
        entity.ReversalReason = reason;
        entity.StampUpdated(userId);

        await UnallocateAsync(entity, userId);
        await db.SaveChangesAsync();
        await RecalculateAsync(entity.OutletId, entity.PartnerId);

        return entity.ToDto();
    }

    // ═══ Cheques ═════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ChequeDto>> ListChequesAsync(
        ChequeStatus? status, Guid? outletId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Cheques.ForTenant(tenant)
            .WhereIf(status.HasValue, c => c.Status == status)
            .WhereIf(outletId.HasValue, c => c.OutletId == outletId)
            .WhereIf(from.HasValue, c => c.ChequeDate >= from)
            .WhereIf(to.HasValue, c => c.ChequeDate <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(c => c.Status == ChequeStatus.Held ? c.ChequeDate : c.ReceivedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();

        var outletIds = rows.Where(r => r.OutletId.HasValue).Select(r => r.OutletId!.Value).Distinct().ToList();
        var names = await db.Outlets.ForTenant(tenant)
            .Where(o => outletIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name);

        foreach (var dto in dtos.Where(d => d.OutletId.HasValue))
            dto.OutletName = names.GetValueOrDefault(dto.OutletId!.Value);

        return PaginatedResponse<ChequeDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ChequeDto> UpdateChequeStatusAsync(Guid chequeId, UpdateChequeStatusDto request, Guid userId)
    {
        var cheque = await db.Cheques.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == chequeId)
            ?? throw new InvalidOperationException("That cheque no longer exists.");

        if (cheque.Status == request.Status)
            return cheque.ToDto();

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var effective = request.EffectiveOn ?? DateTime.UtcNow;

        switch (request.Status)
        {
            case ChequeStatus.Deposited:
                if (cheque.IsPostDated && cheque.ChequeDate.Date > DateTime.UtcNow.Date)
                    throw new InvalidOperationException(
                        $"This cheque is dated {cheque.ChequeDate:d} and cannot be banked yet.");
                cheque.DepositedOn = effective;
                cheque.DepositBankAccount = request.DepositBankAccount;
                break;

            case ChequeStatus.Cleared:
                cheque.ClearedOn = effective;
                // Money has genuinely arrived: only now does it move the customer's ledger.
                await RecalculateAsync(cheque.OutletId, cheque.PartnerId);
                break;

            case ChequeStatus.Bounced:
            {
                if (string.IsNullOrWhiteSpace(request.BounceReason))
                    throw new InvalidOperationException("A bounced cheque needs a reason.");

                cheque.BouncedOn = effective;
                cheque.BounceReason = request.BounceReason;
                cheque.BounceCharges = request.BounceCharges > 0
                    ? request.BounceCharges
                    : settings?.ChequeBounceCharge ?? 0;

                // Four things always happen together on a bounce. Doing them in one place is the
                // only way none of them gets forgotten.
                if (cheque.CollectionReceiptId.HasValue)
                {
                    var receipt = await db.Collections.ForTenant(tenant)
                        .FirstOrDefaultAsync(c => c.Id == cheque.CollectionReceiptId);

                    if (receipt is not null && !receipt.IsReversed)
                    {
                        receipt.IsReversed = true;
                        receipt.ReversalReason = $"Cheque {cheque.ChequeNumber} bounced: {request.BounceReason}";
                        receipt.StampUpdated(userId);
                        await UnallocateAsync(receipt, userId);
                    }
                }

                if (settings?.AutoBlockOnBouncedCheque ?? true)
                {
                    var profile = await FindProfileAsync(cheque.OutletId, cheque.PartnerId);
                    if (profile is not null)
                    {
                        profile.IsBlocked = true;
                        profile.BlockReason = $"Cheque {cheque.ChequeNumber} bounced";
                        profile.BlockedAt = DateTime.UtcNow;
                        profile.StampUpdated(userId);
                        cheque.TriggeredCreditBlock = true;

                        if (cheque.OutletId.HasValue)
                            await db.Outlets.ForTenant(tenant).Where(o => o.Id == cheque.OutletId)
                                .ExecuteUpdateAsync(s => s
                                    .SetProperty(o => o.Status, OutletStatus.CreditBlocked)
                                    .SetProperty(o => o.StatusReason, $"Cheque {cheque.ChequeNumber} bounced"));
                    }
                }
                break;
            }
        }

        cheque.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.Note)) cheque.Note = request.Note;
        cheque.StampUpdated(userId);

        await db.SaveChangesAsync();

        if (request.Status == ChequeStatus.Bounced)
            await RecalculateAsync(cheque.OutletId, cheque.PartnerId);

        return cheque.ToDto();
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private async Task<CreditProfile?> FindProfileAsync(Guid? outletId, Guid? partnerId)
    {
        if (outletId is null && partnerId is null) return null;

        return await db.CreditProfiles.ForTenant(tenant)
            .Include(c => c.Outlet)
            .Include(c => c.Partner)
            .FirstOrDefaultAsync(c => outletId != null ? c.OutletId == outletId : c.PartnerId == partnerId);
    }

    private async Task<CreditOverrideDto> MapOverrideAsync(CreditOverride e)
    {
        var dto = new CreditOverrideDto
        {
            Id = e.Id,
            OutletId = e.OutletId,
            PartnerId = e.PartnerId,
            OrderId = e.OrderId,
            RequestedAmount = e.RequestedAmount,
            ApprovedAmount = e.ApprovedAmount,
            CurrencyCode = e.CurrencyCode,
            Justification = e.Justification,
            RequestedAt = e.RequestedAt,
            ApproverName = e.ApproverName,
            ApprovedAt = e.ApprovedAt,
            IsApproved = e.IsApproved,
            DecisionNote = e.DecisionNote,
            ExpiresOn = e.ExpiresOn,
            IsConsumed = e.IsConsumed,
            IsExpired = e.ExpiresOn is not null && e.ExpiresOn.Value.Date < DateTime.UtcNow.Date,
        };

        if (e.OutletId.HasValue)
            dto.OutletName = await db.Outlets.ForTenant(tenant)
                .Where(o => o.Id == e.OutletId).Select(o => o.Name).FirstOrDefaultAsync();

        if (e.PartnerId.HasValue)
            dto.PartnerName = await db.Partners.ForTenant(tenant)
                .Where(p => p.Id == e.PartnerId).Select(p => p.Name).FirstOrDefaultAsync();

        if (e.OrderId.HasValue)
            dto.OrderNumber = await db.Orders.ForTenant(tenant)
                .Where(o => o.Id == e.OrderId).Select(o => o.OrderNumber).FirstOrDefaultAsync();

        if (e.ReasonCodeId.HasValue)
            dto.ReasonCodeName = await db.ReasonCodes.ForCompany(tenant)
                .Where(r => r.Id == e.ReasonCodeId).Select(r => r.Name).FirstOrDefaultAsync();

        return dto;
    }

    private async Task<List<PaymentAllocationDto>> AllocateOldestFirstAsync(CollectionReceipt receipt)
    {
        var open = await OpenInvoicesAsync(receipt.OutletId, receipt.PartnerId);
        var remaining = receipt.Amount + receipt.CashDiscountAmount;
        var result = new List<PaymentAllocationDto>();

        foreach (var invoice in open.OrderBy(i => i.OrderDate))
        {
            if (remaining <= 0) break;

            var applied = Math.Min(remaining, invoice.Balance);
            remaining -= applied;

            invoice.Entity.PaidAmount += applied;
            invoice.Entity.UpdatedAt = DateTime.UtcNow;

            result.Add(new PaymentAllocationDto
            {
                InvoiceId = invoice.Entity.Id,
                InvoiceNumber = invoice.Entity.OrderNumber,
                InvoiceDate = invoice.OrderDate,
                InvoiceAmount = invoice.Entity.TotalAmount,
                OutstandingBefore = invoice.Balance,
                AllocatedAmount = applied,
                OutstandingAfter = invoice.Balance - applied,
                AgeingDays = (int)(DateTime.UtcNow.Date - invoice.OrderDate.Date).TotalDays,
            });
        }

        return result;
    }

    private async Task<List<PaymentAllocationDto>> AllocateExplicitAsync(
        CollectionReceipt receipt, List<PaymentAllocationDto> requested)
    {
        var ids = requested.Select(r => r.InvoiceId).ToList();
        var orders = await db.Orders.ForTenant(tenant).Where(o => ids.Contains(o.Id)).ToListAsync();
        var result = new List<PaymentAllocationDto>();

        foreach (var line in requested)
        {
            var order = orders.FirstOrDefault(o => o.Id == line.InvoiceId);
            if (order is null) continue;

            var outstanding = order.TotalAmount - order.PaidAmount;
            var applied = Math.Min(line.AllocatedAmount, outstanding);
            if (applied <= 0) continue;

            order.PaidAmount += applied;
            order.UpdatedAt = DateTime.UtcNow;

            result.Add(new PaymentAllocationDto
            {
                InvoiceId = order.Id,
                InvoiceNumber = order.OrderNumber,
                InvoiceDate = order.OrderDate,
                InvoiceAmount = order.TotalAmount,
                OutstandingBefore = outstanding,
                AllocatedAmount = applied,
                OutstandingAfter = outstanding - applied,
                AgeingDays = (int)(DateTime.UtcNow.Date - order.OrderDate.Date).TotalDays,
            });
        }

        var over = result.Sum(r => r.AllocatedAmount) - receipt.Amount - receipt.CashDiscountAmount;
        if (over > 0.001m)
            throw new InvalidOperationException("The allocation adds up to more than the amount received.");

        return result;
    }

    private async Task UnallocateAsync(CollectionReceipt receipt, Guid userId)
    {
        // Without a stored allocation table, the reversal returns the receipt's value to the
        // newest paid invoices first — the mirror of oldest-first allocation.
        var remaining = receipt.AllocatedAmount;
        if (remaining <= 0) return;

        var paid = await db.Orders.ForTenant(tenant)
            .Where(o => (receipt.OutletId != null ? o.OutletId == receipt.OutletId : o.PartnerId == receipt.PartnerId)
                        && o.PaidAmount > 0)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        foreach (var order in paid)
        {
            if (remaining <= 0) break;
            var reclaim = Math.Min(remaining, order.PaidAmount);
            order.PaidAmount -= reclaim;
            order.StampUpdated(userId);
            remaining -= reclaim;
        }

        receipt.AllocatedAmount = 0;
        receipt.UnallocatedAmount = 0;
    }

    private async Task<List<(DistributionOrder Entity, DateTime OrderDate, decimal Balance)>> OpenInvoicesAsync(
        Guid? outletId, Guid? partnerId)
    {
        var orders = await db.Orders.ForTenant(tenant)
            .Where(o => (outletId != null ? o.OutletId == outletId : o.PartnerId == partnerId)
                        && o.Status >= DistributionOrderStatus.Delivered
                        && o.Status != DistributionOrderStatus.Cancelled
                        && o.Status != DistributionOrderStatus.Rejected
                        && o.TotalAmount > o.PaidAmount)
            .ToListAsync();

        return orders.Select(o => (o, o.OrderDate, o.TotalAmount - o.PaidAmount)).ToList();
    }
}
