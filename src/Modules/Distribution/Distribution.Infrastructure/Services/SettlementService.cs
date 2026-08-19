using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Route settlement — the moment a distribution day is proved correct.
///
/// The whole service exists to enforce one rule: **a route cannot close with an unexplained
/// variance**. Everything else — recomputing both sides, the tolerance bands, the approval
/// escalation — is scaffolding around that constraint.
///
/// Settlement recomputes rather than trusting running totals. A day's numbers are assembled from
/// the source documents every time it is opened or recomputed, because a running counter that
/// drifted at four in the afternoon is exactly the kind of error settlement is meant to catch.
/// </summary>
public class SettlementService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering,
    IEventPublisher events) : ISettlementService
{
    public async Task<SettlementBoardDto> GetBoardAsync(DateTime? date, Guid? territoryId)
    {
        var day = (date ?? DateTime.UtcNow).Date;

        var settlements = await db.Settlements.ForTenant(tenant)
            .Include(s => s.FieldRep)
            .Include(s => s.Variances.Where(v => !v.IsDeleted))
            .Where(s => s.SettlementDate == day)
            .ToListAsync();

        var board = new SettlementBoardDto
        {
            SettlementDate = day,
            OpenCount = settlements.Count(s => s.Status == SettlementStatus.Open),
            SubmittedCount = settlements.Count(s => s.Status == SettlementStatus.Submitted),
            PendingApprovalCount = settlements.Count(s => s.Status == SettlementStatus.PendingApproval),
            ClosedCount = settlements.Count(s => s.Status == SettlementStatus.Closed),
            TotalSales = settlements.Sum(s => s.TotalSalesValue),
            TotalCollected = settlements.Sum(s => s.TotalCollected),
            TotalCashVariance = settlements.Sum(s => s.CashVariance),
            TotalStockVariance = settlements.Sum(s => s.StockVarianceValue),
            CashToDeposit = settlements.Sum(s => s.CashToDeposit),
            Settlements = settlements.Select(s => s.ToDto()).ToList(),
        };

        // The days that finished but never settled: the ones to chase tonight, not tomorrow.
        var settledDayIds = settlements.Where(s => s.FieldDayId.HasValue)
            .Select(s => s.FieldDayId!.Value).ToList();

        var unsettled = await db.FieldDays.ForTenant(tenant)
            .Include(d => d.FieldRep).Include(d => d.Route)
            .Where(d => d.WorkDate == day
                        && (d.Status == FieldDayStatus.Closed || d.Status == FieldDayStatus.ForceClosed)
                        && !settledDayIds.Contains(d.Id))
            .ToListAsync();

        board.UnsettledDayCount = unsettled.Count;
        board.UnsettledDays = unsettled.Select(d => d.ToDto()).ToList();

        board.UndepositedCash = await db.Collections.ForTenant(tenant)
            .Where(c => !c.IsDeposited && !c.IsReversed && c.Tender == PaymentTender.Cash)
            .SumAsync(c => (decimal?)c.Amount) ?? 0;

        return board;
    }

    public async Task<SettlementDto> OpenAsync(OpenSettlementDto request, Guid userId)
    {
        var date = (request.SettlementDate ?? DateTime.UtcNow).Date;

        FieldDay? day = null;
        if (request.FieldDayId.HasValue)
        {
            day = await db.FieldDays.ForTenant(tenant)
                .FirstOrDefaultAsync(d => d.Id == request.FieldDayId)
                ?? throw new InvalidOperationException("That day no longer exists.");

            if (day.Status == FieldDayStatus.Started)
                throw new InvalidOperationException("Close the rep's day before settling it.");

            date = day.WorkDate.Date;
        }

        var existing = await db.Settlements.ForTenant(tenant)
            .Include(s => s.Variances.Where(v => !v.IsDeleted))
            .Include(s => s.FieldRep)
            .FirstOrDefaultAsync(s => request.FieldDayId != null
                ? s.FieldDayId == request.FieldDayId
                : s.FieldRepId == request.FieldRepId && s.SettlementDate == date);

        if (existing is not null)
        {
            if (existing.Status is SettlementStatus.Closed && !existing.IsReversed)
                throw new InvalidOperationException("This route has already been settled and closed.");
            return await RecomputeAsync(existing.Id, userId);
        }

        var settlement = new RouteSettlement
        {
            SettlementNumber = await numbering.NextSettlementNumberAsync(DateTime.UtcNow),
            SettlementDate = date,
            Status = SettlementStatus.Open,
            FieldDayId = request.FieldDayId,
            FieldRepId = request.FieldRepId ?? day?.FieldRepId,
            RouteId = request.RouteId ?? day?.RouteId,
            VanUnitId = request.VanUnitId ?? day?.VanUnitId,
            TripId = request.TripId,
            OpeningFloat = request.OpeningFloat,
            IsPartial = request.IsPartial,
        }.StampNew(tenant, userId);

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        settlement.CurrencyCode = settings?.BaseCurrencyCode ?? "USD";

        // A multi-day van carries yesterday's closing forward as today's opening.
        if (request.IsPartial || settlement.VanUnitId.HasValue)
        {
            var previous = await db.Settlements.ForTenant(tenant)
                .Where(s => s.VanUnitId == settlement.VanUnitId && s.SettlementDate < date
                            && s.Status == SettlementStatus.Closed)
                .OrderByDescending(s => s.SettlementDate)
                .FirstOrDefaultAsync();

            if (previous is not null)
            {
                settlement.PreviousSettlementId = previous.Id;
                settlement.OpeningStockValue = previous.CountedClosingStockValue;
                if (settlement.OpeningFloat == 0) settlement.OpeningFloat = previous.CashToDeposit;
            }
        }

        db.Settlements.Add(settlement);
        await db.SaveChangesAsync();

        return await RecomputeAsync(settlement.Id, userId);
    }

    public async Task<SettlementDto?> GetAsync(Guid settlementId)
    {
        var entity = await LoadAsync(settlementId);
        if (entity is null) return null;
        return await DecorateAsync(entity);
    }

    public async Task<SettlementDto> RecomputeAsync(Guid settlementId, Guid userId)
    {
        var settlement = await LoadAsync(settlementId)
            ?? throw new InvalidOperationException("That settlement no longer exists.");

        if (settlement.Status is SettlementStatus.Closed && !settlement.IsReversed)
            throw new InvalidOperationException("A closed settlement cannot be recomputed. Reverse it first.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        await ComputeSalesAsync(settlement);
        await ComputeCollectionsAsync(settlement);
        await ComputeReturnsAsync(settlement);
        await ComputeExpensesAsync(settlement);
        await ComputeVanStockAsync(settlement);

        // Cash the rep should be holding: float, plus cash taken in, less what they spent.
        settlement.ExpectedCash = settlement.OpeningFloat + settlement.CashCollected - settlement.ExpenseAmount;
        settlement.CashVariance = settlement.DeclaredCash - settlement.ExpectedCash;
        settlement.CashToDeposit = Math.Max(0, settlement.DeclaredCash);

        await RebuildVariancesAsync(settlement, settings, userId);

        settlement.StampUpdated(userId);
        await db.SaveChangesAsync();

        return await DecorateAsync((await LoadAsync(settlementId))!);
    }

    public async Task<SettlementDto> SubmitAsync(SubmitSettlementDto request, Guid userId)
    {
        var settlement = await LoadAsync(request.SettlementId)
            ?? throw new InvalidOperationException("That settlement no longer exists.");

        if (settlement.Status is SettlementStatus.Closed or SettlementStatus.Approved)
            throw new InvalidOperationException("This settlement has already been submitted.");

        settlement.DeclaredCash = request.DeclaredCash;
        settlement.ClosingCountId = request.ClosingCountId ?? settlement.ClosingCountId;
        settlement.Note = request.Note;
        settlement.SubmittedAt = DateTime.UtcNow;
        settlement.SubmittedByUserId = userId;
        settlement.StampUpdated(userId);

        await db.SaveChangesAsync();
        await RecomputeAsync(settlement.Id, userId);

        settlement = (await LoadAsync(request.SettlementId))!;
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        // Anything above tolerance escalates; everything else can close as soon as it is explained.
        var needsApproval = settlement.Variances.Any(v => !v.IsDeleted && v.RequiresApproval && v.IsApproved != true);

        settlement.Status = needsApproval ? SettlementStatus.PendingApproval : SettlementStatus.Submitted;
        settlement.StampUpdated(userId);

        await db.SaveChangesAsync();
        return await DecorateAsync(settlement);
    }

    public async Task<SettlementDto> ExplainVarianceAsync(ExplainVarianceDto request, Guid userId)
    {
        var variance = await db.SettlementVariances.ForTenant(tenant)
            .FirstOrDefaultAsync(v => v.Id == request.VarianceId)
            ?? throw new InvalidOperationException("That variance no longer exists.");

        var reason = await db.ReasonCodes.ForCompany(tenant)
            .FirstOrDefaultAsync(r => r.Id == request.ReasonCodeId)
            ?? throw new InvalidOperationException("That reason is not recognised.");

        if (reason.RequiresNote && string.IsNullOrWhiteSpace(request.Note))
            throw new InvalidOperationException($"'{reason.Name}' needs a note.");

        variance.ReasonCodeId = request.ReasonCodeId;
        variance.ReasonNote = request.Note;
        variance.IsRecoverable = request.IsRecoverable || reason.IsRecoverable;
        variance.RecoveredAmount = request.RecoveredAmount;
        if (reason.RequiresApproval) variance.RequiresApproval = true;
        variance.StampUpdated(userId);

        await db.SaveChangesAsync();

        var settlement = await LoadAsync(variance.SettlementId);
        if (settlement is not null)
        {
            settlement.UnexplainedVarianceCount = settlement.Variances
                .Count(v => !v.IsDeleted && v.ReasonCodeId is null);
            settlement.StampUpdated(userId);
            await db.SaveChangesAsync();
        }

        return await DecorateAsync(settlement!);
    }

    public async Task<SettlementDto> ApproveAsync(ApproveSettlementDto request, Guid userId)
    {
        var settlement = await LoadAsync(request.SettlementId)
            ?? throw new InvalidOperationException("That settlement no longer exists.");

        if (settlement.Status is not (SettlementStatus.PendingApproval or SettlementStatus.Submitted))
            throw new InvalidOperationException("This settlement is not waiting for approval.");

        if (!request.IsApproved)
        {
            if (string.IsNullOrWhiteSpace(request.Note))
                throw new InvalidOperationException("Rejecting a settlement needs a reason.");

            settlement.Status = SettlementStatus.Open;
            settlement.Note = $"{settlement.Note} Returned: {request.Note}".Trim();
            settlement.StampUpdated(userId);
            await db.SaveChangesAsync();
            return await DecorateAsync(settlement);
        }

        foreach (var variance in settlement.Variances.Where(v => !v.IsDeleted && v.RequiresApproval))
        {
            variance.IsApproved = true;
            variance.ApprovedByUserId = userId;
            variance.ApprovedAt = DateTime.UtcNow;
            variance.StampUpdated(userId);
        }

        settlement.Status = SettlementStatus.Approved;
        settlement.ApprovedAt = DateTime.UtcNow;
        settlement.ApprovedByUserId = userId;
        settlement.StampUpdated(userId);

        await db.SaveChangesAsync();
        return await DecorateAsync(settlement);
    }

    public async Task<SettlementDto> CloseAsync(Guid settlementId, Guid userId)
    {
        var settlement = await LoadAsync(settlementId)
            ?? throw new InvalidOperationException("That settlement no longer exists.");

        if (settlement.Status == SettlementStatus.Closed && !settlement.IsReversed)
            throw new InvalidOperationException("This settlement is already closed.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        // The gate. Everything in this service exists to make this check meaningful.
        if (settings?.BlockSettlementOnUnexplainedVariance ?? true)
        {
            var unexplained = settlement.Variances.Where(v => !v.IsDeleted && v.ReasonCodeId is null).ToList();
            if (unexplained.Count > 0)
                throw new InvalidOperationException(
                    $"{unexplained.Count} variance(s) still have no reason. A route cannot close on an unexplained gap.");
        }

        var awaiting = settlement.Variances
            .Where(v => !v.IsDeleted && v.RequiresApproval && v.IsApproved != true).ToList();

        if (awaiting.Count > 0)
            throw new InvalidOperationException(
                $"{awaiting.Count} variance(s) are above tolerance and still need approval.");

        settlement.Status = SettlementStatus.Closed;
        settlement.ClosedAt = DateTime.UtcNow;
        settlement.IsReversed = false;
        settlement.StampUpdated(userId);

        // Collections settled tonight belong to this settlement, so tomorrow's does not claim them.
        await db.Collections.ForTenant(tenant)
            .Where(c => c.FieldDayId == settlement.FieldDayId && c.SettlementId == null && !c.IsReversed)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.SettlementId, settlement.Id));

        if (settlement.VanUnitId.HasValue)
            await db.VanUnits.ForTenant(tenant).Where(v => v.Id == settlement.VanUnitId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.LastSettledAt, DateTime.UtcNow));

        await db.SaveChangesAsync();
        await PostToAccountingAsync(settlement, userId);

        return await DecorateAsync(settlement);
    }

    public async Task<SettlementDto> ReverseAsync(Guid settlementId, ReverseSettlementDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Reversing a settlement needs a reason.");

        var settlement = await LoadAsync(settlementId)
            ?? throw new InvalidOperationException("That settlement no longer exists.");

        if (settlement.Status != SettlementStatus.Closed)
            throw new InvalidOperationException("Only a closed settlement can be reversed.");

        // A reversal is a new fact, not an edit. The original stays exactly as it was.
        settlement.Status = SettlementStatus.Reversed;
        settlement.IsReversed = true;
        settlement.ReversedAt = DateTime.UtcNow;
        settlement.ReversedByUserId = userId;
        settlement.ReversalReason = request.Reason;
        settlement.StampUpdated(userId);

        await db.SaveChangesAsync();
        return await DecorateAsync(settlement);
    }

    public async Task<PaginatedResponse<SettlementDto>> ListAsync(
        Guid? fieldRepId, Guid? routeId, SettlementStatus? status, DateTime? from, DateTime? to,
        PaginationParams pagination)
    {
        var query = db.Settlements.ForTenant(tenant)
            .Include(s => s.FieldRep)
            .Include(s => s.Variances.Where(v => !v.IsDeleted))
            .WhereIf(fieldRepId.HasValue, s => s.FieldRepId == fieldRepId)
            .WhereIf(routeId.HasValue, s => s.RouteId == routeId)
            .WhereIf(status.HasValue, s => s.Status == status)
            .WhereIf(from.HasValue, s => s.SettlementDate >= from)
            .WhereIf(to.HasValue, s => s.SettlementDate <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(s => s.SettlementDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = r.ToDto();
            dto.CanClose = dto.UnexplainedVarianceCount == 0;
            return dto;
        });

        return PaginatedResponse<SettlementDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ Deposits ════════════════════════════════════════════════════════════

    public async Task<CashDepositDto> RecordDepositAsync(RecordDepositDto request, Guid userId)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("A deposit needs an amount.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var deposit = new CashDeposit
        {
            DepositNumber = await numbering.NextDepositNumberAsync(DateTime.UtcNow),
            FieldRepId = request.FieldRepId,
            SettlementId = request.SettlementId,
            PartnerId = request.PartnerId,
            DepositedOn = request.DepositedOn == default ? DateTime.UtcNow : request.DepositedOn,
            BankName = request.BankName,
            BankAccount = request.BankAccount,
            SlipReference = request.SlipReference,
            SlipImageUrl = request.SlipImageUrl,
            Amount = request.Amount,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.CashDeposits.Add(deposit);

        // Mark the cash that made up this deposit, oldest first, so undeposited cash ages honestly.
        var remaining = request.Amount;
        var collections = await db.Collections.ForTenant(tenant)
            .Where(c => !c.IsDeposited && !c.IsReversed && c.Tender == PaymentTender.Cash
                        && (request.SettlementId == null || c.SettlementId == request.SettlementId)
                        && (request.FieldRepId == null || c.FieldRepId == request.FieldRepId))
            .OrderBy(c => c.CollectedAt)
            .ToListAsync();

        foreach (var collection in collections)
        {
            if (remaining <= 0) break;
            if (collection.Amount > remaining) continue;

            collection.IsDeposited = true;
            collection.DepositId = deposit.Id;
            collection.StampUpdated(userId);
            remaining -= collection.Amount;
        }

        await db.SaveChangesAsync();
        return deposit.ToDto();
    }

    public async Task<CashDepositDto> ReconcileDepositAsync(Guid depositId, Guid userId)
    {
        var deposit = await db.CashDeposits.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == depositId)
            ?? throw new InvalidOperationException("That deposit no longer exists.");

        deposit.IsReconciled = true;
        deposit.ReconciledAt = DateTime.UtcNow;
        deposit.ReconciledByUserId = userId;
        deposit.StampUpdated(userId);

        await db.SaveChangesAsync();
        return deposit.ToDto();
    }

    public async Task<PaginatedResponse<CashDepositDto>> ListDepositsAsync(
        Guid? fieldRepId, bool? unreconciledOnly, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.CashDeposits.ForTenant(tenant)
            .WhereIf(fieldRepId.HasValue, d => d.FieldRepId == fieldRepId)
            .WhereIf(unreconciledOnly == true, d => !d.IsReconciled)
            .WhereIf(from.HasValue, d => d.DepositedOn >= from)
            .WhereIf(to.HasValue, d => d.DepositedOn <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(d => d.DepositedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = r.ToDto();
            dto.AgeingDays = (int)(DateTime.UtcNow.Date - r.DepositedOn.Date).TotalDays;
            return dto;
        }).ToList();

        var repIds = rows.Where(r => r.FieldRepId.HasValue).Select(r => r.FieldRepId!.Value).Distinct().ToList();
        var reps = await db.FieldReps.ForTenant(tenant)
            .Where(r => repIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.FullName);

        foreach (var dto in dtos.Where(d => d.FieldRepId.HasValue))
            dto.FieldRepName = reps.GetValueOrDefault(dto.FieldRepId!.Value);

        return PaginatedResponse<CashDepositDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ Computation ═════════════════════════════════════════════════════════

    private async Task ComputeSalesAsync(RouteSettlement s)
    {
        var orders = await OrderScope(s).ToListAsync();

        s.InvoiceCount = orders.Count;
        s.TotalSalesValue = orders.Sum(o => o.TotalAmount);
        s.TaxValue = orders.Sum(o => o.TaxAmount);
        s.DiscountValue = orders.Sum(o => o.DiscountAmount);
        s.SchemeValue = orders.Sum(o => o.SchemeDiscountAmount);
        s.FreeGoodsValue = orders.Sum(o => o.FreeGoodsValue);
        s.SoldStockValue = orders.Sum(o => o.CostAmount);

        // Cash sales are the ones already paid in full at the counter; the rest is credit that
        // the collections side has to account for separately.
        s.CashSalesValue = orders.Where(o => o.PaidAmount >= o.TotalAmount).Sum(o => o.TotalAmount);
        s.CreditSalesValue = s.TotalSalesValue - s.CashSalesValue;
    }

    private async Task ComputeCollectionsAsync(RouteSettlement s)
    {
        var collections = await db.Collections.ForTenant(tenant)
            .Where(c => !c.IsReversed)
            .Where(c => s.FieldDayId != null
                ? c.FieldDayId == s.FieldDayId
                : c.FieldRepId == s.FieldRepId && c.CollectedAt >= s.SettlementDate
                  && c.CollectedAt < s.SettlementDate.AddDays(1))
            .ToListAsync();

        s.CashCollected = collections.Where(c => c.Tender == PaymentTender.Cash).Sum(c => c.Amount);
        s.ChequeCollected = collections.Where(c => c.Tender == PaymentTender.Cheque).Sum(c => c.Amount);
        s.DigitalCollected = collections
            .Where(c => c.Tender is PaymentTender.BankTransfer or PaymentTender.Upi
                        or PaymentTender.Wallet or PaymentTender.Card)
            .Sum(c => c.Amount);

        s.TotalCollected = collections.Sum(c => c.Amount);
    }

    private async Task ComputeReturnsAsync(RouteSettlement s)
    {
        s.ReturnValue = await db.Returns.ForTenant(tenant)
            .Where(r => r.RequestedOn >= s.SettlementDate && r.RequestedOn < s.SettlementDate.AddDays(1))
            .Where(r => s.FieldRepId == null || r.FieldRepId == s.FieldRepId)
            .SumAsync(r => (decimal?)r.ClaimedValue) ?? 0;
    }

    private async Task ComputeExpensesAsync(RouteSettlement s)
    {
        if (s.TripId is null)
        {
            s.ExpenseAmount = 0;
            return;
        }

        s.ExpenseAmount = await db.TripExpenses.ForTenant(tenant)
            .Where(e => e.TripId == s.TripId)
            .SumAsync(e => (decimal?)e.Amount) ?? 0;
    }

    private async Task ComputeVanStockAsync(RouteSettlement s)
    {
        if (s.VanUnitId is null) return;

        var loaded = await db.VanStockMovements.ForTenant(tenant)
            .Where(m => m.VanUnitId == s.VanUnitId && m.Kind == VanMovementKind.LoadOut
                        && m.OccurredAt >= s.SettlementDate && m.OccurredAt < s.SettlementDate.AddDays(1))
            .SumAsync(m => (decimal?)(m.Quantity * m.UnitCost)) ?? 0;

        var returned = await db.VanStockMovements.ForTenant(tenant)
            .Where(m => m.VanUnitId == s.VanUnitId && m.Kind == VanMovementKind.CustomerReturn
                        && m.OccurredAt >= s.SettlementDate && m.OccurredAt < s.SettlementDate.AddDays(1))
            .SumAsync(m => (decimal?)(m.Quantity * m.UnitCost)) ?? 0;

        s.LoadedStockValue = loaded;
        s.ReturnedStockValue = returned;

        // The identity the count is checked against.
        s.ExpectedClosingStockValue = s.OpeningStockValue + s.LoadedStockValue - s.SoldStockValue + s.ReturnedStockValue;

        if (s.ClosingCountId.HasValue)
        {
            var count = await db.VanCycleCounts.ForTenant(tenant)
                .Include(c => c.Lines.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == s.ClosingCountId);

            if (count is not null)
                s.CountedClosingStockValue = count.Lines.Sum(l => l.CountedQuantity * l.UnitCost);
        }
        else
        {
            s.CountedClosingStockValue = await db.VanStockBalances.ForTenant(tenant)
                .Where(b => b.VanUnitId == s.VanUnitId)
                .SumAsync(b => (decimal?)(b.Quantity * b.UnitCost)) ?? 0;
        }

        s.StockVarianceValue = s.CountedClosingStockValue - s.ExpectedClosingStockValue;
    }

    /// <summary>
    /// Rebuilds the variance list from the recomputed figures, carrying forward any reason that
    /// was already given so an explained gap does not have to be explained twice.
    /// </summary>
    private async Task RebuildVariancesAsync(RouteSettlement s, DistributionSettings? settings, Guid userId)
    {
        var cashTolerance = settings?.CashVarianceTolerance ?? 0;
        var approvalThreshold = settings?.VarianceApprovalThreshold ?? 0;

        var existing = s.Variances.Where(v => !v.IsDeleted).ToList();

        void Upsert(VarianceKind kind, decimal expected, decimal actual, string? itemName = null, Guid? itemId = null)
        {
            var delta = actual - expected;
            var row = existing.FirstOrDefault(v => v.Kind == kind && v.ItemId == itemId);

            if (Math.Abs(delta) < 0.001m)
            {
                if (row is not null) row.StampDeleted(userId);
                return;
            }

            if (row is null)
            {
                row = new SettlementVariance { SettlementId = s.Id, Kind = kind, ItemId = itemId }.StampNew(tenant, userId);
                s.Variances.Add(row);
            }
            else
            {
                row.StampUpdated(userId);
            }

            row.ItemName = itemName;
            row.ExpectedAmount = expected;
            row.ActualAmount = actual;
            row.VarianceAmount = delta;
            row.RequiresApproval = approvalThreshold > 0 && Math.Abs(delta) > approvalThreshold;
        }

        if (Math.Abs(s.CashVariance) > cashTolerance)
            Upsert(s.CashVariance < 0 ? VarianceKind.CashShort : VarianceKind.CashOver,
                s.ExpectedCash, s.DeclaredCash);
        else
            foreach (var row in existing.Where(v => v.Kind is VarianceKind.CashShort or VarianceKind.CashOver))
                row.StampDeleted(userId);

        if (s.VanUnitId.HasValue)
        {
            var tolerancePercent = settings?.StockVarianceTolerancePercent ?? 1;
            var allowed = s.ExpectedClosingStockValue * tolerancePercent / 100m;

            if (Math.Abs(s.StockVarianceValue) > allowed)
                Upsert(s.StockVarianceValue < 0 ? VarianceKind.StockShort : VarianceKind.StockExcess,
                    s.ExpectedClosingStockValue, s.CountedClosingStockValue);
            else
                foreach (var row in existing.Where(v => v.Kind is VarianceKind.StockShort or VarianceKind.StockExcess))
                    row.StampDeleted(userId);

            // Per-SKU detail from the closing count, so "short by 400" becomes a list of items.
            if (s.ClosingCountId.HasValue)
            {
                var lines = await db.VanCycleCountLines.ForTenant(tenant)
                    .Where(l => l.CountId == s.ClosingCountId && l.VarianceQuantity != 0)
                    .ToListAsync();

                foreach (var line in lines)
                {
                    var row = existing.FirstOrDefault(v => v.ItemId == line.ItemId
                        && v.Kind is VarianceKind.StockShort or VarianceKind.StockExcess
                        && v.BatchId == line.BatchId);

                    if (row is null)
                    {
                        row = new SettlementVariance
                        {
                            SettlementId = s.Id,
                            Kind = line.VarianceQuantity < 0 ? VarianceKind.StockShort : VarianceKind.StockExcess,
                            ItemId = line.ItemId,
                            BatchId = line.BatchId,
                        }.StampNew(tenant, userId);
                        s.Variances.Add(row);
                    }

                    row.ItemName = line.ItemName;
                    row.BatchNumber = line.BatchNumber;
                    row.Uom = line.Uom;
                    row.ExpectedQuantity = line.ExpectedQuantity;
                    row.ActualQuantity = line.CountedQuantity;
                    row.VarianceQuantity = line.VarianceQuantity;
                    row.VarianceAmount = line.VarianceValue;
                    row.RequiresApproval = approvalThreshold > 0 && Math.Abs(line.VarianceValue) > approvalThreshold;

                    // Carry the reason across from the count so it is not asked for twice.
                    row.ReasonCodeId ??= line.ReasonCodeId;
                    row.ReasonNote ??= line.ReasonNote;
                    row.StampUpdated(userId);
                }
            }
        }

        s.VarianceCount = s.Variances.Count(v => !v.IsDeleted);
        s.UnexplainedVarianceCount = s.Variances.Count(v => !v.IsDeleted && v.ReasonCodeId is null);
    }

    private IQueryable<DistributionOrder> OrderScope(RouteSettlement s)
        => db.Orders.ForTenant(tenant)
            .Where(o => o.Status != DistributionOrderStatus.Cancelled
                        && o.Status != DistributionOrderStatus.Rejected
                        && o.Status != DistributionOrderStatus.Draft)
            .Where(o => s.FieldDayId != null
                ? o.FieldDayId == s.FieldDayId
                : o.FieldRepId == s.FieldRepId && o.OrderDate >= s.SettlementDate
                  && o.OrderDate < s.SettlementDate.AddDays(1));

    /// <summary>
    /// Publishes the day's money into Accounting. Sent once, on close, because a settlement that
    /// posts before it balances is how a ledger acquires entries nobody can explain.
    /// </summary>
    private async Task PostToAccountingAsync(RouteSettlement s, Guid userId)
    {
        if (s.IsPosted) return;

        // One posting per settlement rather than per invoice: the day's revenue, tax and COGS as
        // a single balanced entry, which is how a route-accounting ledger is read.
        var lines = await OrderScope(s)
            .SelectMany(o => o.Lines.Where(l => !l.IsDeleted))
            .Select(l => new SalesAccountingLine
            {
                ProductId = l.ItemId,
                ProductCode = l.ItemCode ?? string.Empty,
                ProductName = l.ItemName,
                Quantity = l.BaseQuantity,
                UnitPrice = l.UnitPrice,
                UnitCost = l.UnitCost,
                DiscountAmount = l.DiscountAmount + l.SchemeDiscountAmount,
                TaxAmount = l.TaxAmount,
                LineTotal = l.LineTotal - l.TaxAmount,
            })
            .ToListAsync();

        await events.PublishAsync(new SalesInvoicePostedEvent
        {
            InvoiceId = s.Id,
            InvoiceNumber = s.SettlementNumber,
            ContactId = s.PartnerId,
            SubtotalAmount = s.TotalSalesValue - s.TaxValue,
            TaxAmount = s.TaxValue,
            TotalAmount = s.TotalSalesValue,
            CurrencyCode = s.CurrencyCode,
            InvoiceDate = s.SettlementDate,
            DueDate = s.SettlementDate,
            Lines = lines,
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            BusinessUnitId = tenant.BusinessUnitId,
            CreatedByUserId = userId,
        });

        await events.PublishAsync(new SalesCogsPostedEvent
        {
            DeliveryId = s.Id,
            ShippedAt = s.SettlementDate,
            CurrencyCode = s.CurrencyCode,
            Lines = lines,
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            BusinessUnitId = tenant.BusinessUnitId,
            CreatedByUserId = userId,
        });

        s.IsPosted = true;
        s.PostedAt = DateTime.UtcNow;
        s.StampUpdated(userId);

        await db.SaveChangesAsync();
    }

    private async Task<RouteSettlement?> LoadAsync(Guid settlementId)
        => await db.Settlements.ForTenant(tenant)
            .Include(s => s.FieldRep)
            .Include(s => s.Variances.Where(v => !v.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == settlementId);

    private async Task<SettlementDto> DecorateAsync(RouteSettlement entity)
    {
        var dto = entity.ToDto();

        if (entity.RouteId.HasValue)
            dto.RouteName = await db.Routes.ForTenant(tenant)
                .Where(r => r.Id == entity.RouteId).Select(r => r.Name).FirstOrDefaultAsync();

        if (entity.VanUnitId.HasValue)
            dto.VanUnitName = await db.VanUnits.ForTenant(tenant)
                .Where(v => v.Id == entity.VanUnitId).Select(v => v.Name).FirstOrDefaultAsync();

        dto.Invoices = (await OrderScope(entity).Include(o => o.Outlet).Take(200).ToListAsync())
            .Select(o => o.ToSummary()).ToList();

        dto.Collections = (await db.Collections.ForTenant(tenant)
                .Include(c => c.Outlet)
                .Where(c => !c.IsReversed)
                .Where(c => entity.FieldDayId != null
                    ? c.FieldDayId == entity.FieldDayId
                    : c.FieldRepId == entity.FieldRepId && c.CollectedAt >= entity.SettlementDate
                      && c.CollectedAt < entity.SettlementDate.AddDays(1))
                .Take(200).ToListAsync())
            .Select(c => c.ToSummary()).ToList();

        dto.Returns = (await db.Returns.ForTenant(tenant)
                .Include(r => r.Outlet).Include(r => r.Lines)
                .Where(r => r.RequestedOn >= entity.SettlementDate
                            && r.RequestedOn < entity.SettlementDate.AddDays(1)
                            && (entity.FieldRepId == null || r.FieldRepId == entity.FieldRepId))
                .Take(100).ToListAsync())
            .Select(r => r.ToSummary()).ToList();

        if (entity.TripId.HasValue)
            dto.Expenses = (await db.TripExpenses.ForTenant(tenant)
                    .Where(e => e.TripId == entity.TripId).ToListAsync())
                .Select(e => e.ToDto()).ToList();

        var reasonIds = entity.Variances.Where(v => v.ReasonCodeId.HasValue)
            .Select(v => v.ReasonCodeId!.Value).Distinct().ToList();

        if (reasonIds.Count > 0)
        {
            var reasons = await db.ReasonCodes.ForCompany(tenant)
                .Where(r => reasonIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);

            foreach (var variance in dto.Variances.Where(v => v.ReasonCodeId.HasValue))
                variance.ReasonCodeName = reasons.GetValueOrDefault(variance.ReasonCodeId!.Value);
        }

        dto.UnexplainedVarianceCount = dto.Variances.Count(v => v.ReasonCodeId is null);
        dto.CanClose = dto.UnexplainedVarianceCount == 0
                       && dto.Variances.All(v => !v.RequiresApproval || v.IsApproved == true);

        if (dto.UnexplainedVarianceCount > 0)
            dto.Blockers.Add($"{dto.UnexplainedVarianceCount} variance(s) still need a reason.");

        var awaiting = dto.Variances.Count(v => v.RequiresApproval && v.IsApproved != true);
        if (awaiting > 0) dto.Blockers.Add($"{awaiting} variance(s) are waiting for approval.");

        return dto;
    }
}
