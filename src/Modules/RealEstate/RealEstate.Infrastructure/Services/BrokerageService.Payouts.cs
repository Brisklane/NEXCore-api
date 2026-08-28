using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>Reading calculations back, disbursement, payout runs, cap position and clawback.</summary>
public partial class BrokerageService
{
    public async Task<PaginatedResponse<CommissionCalculationDto>> GetCalculationsAsync(
        ListQueryDto query, CommissionStatus? status)
    {
        var q = Db.CommissionCalculations.ForCompany(Tenant)
            .WhereIf(status.HasValue, c => c.Status == status)
            .WhereIf(query.ProjectId.HasValue, c => c.ProjectId == query.ProjectId)
            .WhereIf(query.FromDate.HasValue, c => c.CalculatedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, c => c.CalculatedOn <= query.ToDate)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), c => c.Reference.Contains(query.Search!))
            .Include(c => c.Splits)
            .OrderByDescending(c => c.CalculatedOn);

        return await PageAsync(q, query, rows => MapCalculationsAsync(rows));
    }

    private async Task<List<CommissionCalculationDto>> MapCalculationsAsync(
        List<CommissionCalculation> calculations, List<CommissionSplit>? preloadedSplits = null)
    {
        if (calculations.Count == 0) return [];

        var currency = await CurrencyAsync();
        var ids = calculations.Where(c => c.Id != Guid.Empty).Select(c => c.Id).ToList();

        var splits = preloadedSplits
            ?? (calculations.SelectMany(c => c.Splits).Any()
                ? calculations.SelectMany(c => c.Splits).ToList()
                : ids.Count == 0
                    ? []
                    : await Db.CommissionSplits.ForCompany(Tenant)
                        .Where(s => ids.Contains(s.CommissionCalculationId))
                        .ToListAsync());

        var splitIds = splits.Where(s => s.Id != Guid.Empty).Select(s => s.Id).ToList();

        var deductions = splits.SelectMany(s => s.Deductions).Any()
            ? splits.SelectMany(s => s.Deductions).ToList()
            : splitIds.Count == 0
                ? []
                : await Db.CommissionDeductions.ForCompany(Tenant)
                    .Where(d => splitIds.Contains(d.CommissionSplitId))
                    .ToListAsync();

        var agents = await AgentDisplayNamesAsync(splits.Select(s => s.AgentProfileId));

        var partnerIds = splits.Where(s => s.ChannelPartnerId != null)
            .Select(s => s.ChannelPartnerId!.Value).Distinct().ToList();

        var partners = partnerIds.Count == 0
            ? []
            : await Db.ChannelPartners.ForCompany(Tenant)
                .Where(p => partnerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        var teamIds = splits.Where(s => s.SalesTeamId != null)
            .Select(s => s.SalesTeamId!.Value).Distinct().ToList();

        var teams = teamIds.Count == 0
            ? []
            : await Db.SalesTeams.ForCompany(Tenant)
                .Where(t => teamIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name);

        var dealIds = calculations.Where(c => c.DealId != null).Select(c => c.DealId!.Value).Distinct().ToList();
        var bookingIds = calculations.Where(c => c.BookingId != null).Select(c => c.BookingId!.Value).Distinct().ToList();

        var deals = dealIds.Count == 0
            ? []
            : await Db.Deals.ForCompany(Tenant)
                .Where(d => dealIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Reference);

        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant)
                .Where(b => bookingIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Reference);

        var projects = await ProjectNamesAsync(calculations.Select(c => c.ProjectId));

        var referrers = await PartyNamesAsync(
            splits.Where(s => s.ReferrerPartyId != null).Select(s => s.ReferrerPartyId!.Value));

        return calculations.Select(c => new CommissionCalculationDto
        {
            Id = c.Id,
            Reference = c.Reference,
            DealId = c.DealId,
            DealReference = c.DealId is null ? null : deals.GetValueOrDefault(c.DealId.Value),
            BookingId = c.BookingId,
            BookingReference = c.BookingId is null ? null : bookings.GetValueOrDefault(c.BookingId.Value),
            TenancyId = c.TenancyId,
            ProjectName = c.ProjectId is null ? null : projects.GetValueOrDefault(c.ProjectId.Value),
            Trigger = c.Trigger,
            Status = c.Status,
            TransactionValue = c.TransactionValue,
            GrossFee = c.GrossFee,
            TaxOnFee = c.TaxOnFee,
            TotalDeductions = c.TotalDeductions,
            NetDistributable = c.NetDistributable,
            CurrencyCode = string.IsNullOrWhiteSpace(c.CurrencyCode) ? currency : c.CurrencyCode,
            CalculatedOn = c.CalculatedOn,
            EarnedOn = c.EarnedOn,
            DueOn = c.DueOn,
            CollectionPercentAtCalculation = c.CollectionPercentAtCalculation,
            DisbursementId = c.DisbursementId,
            IsDisputed = c.IsDisputed,
            DisputeNote = c.DisputeNote,

            // The trace is the product. Every figure above can be followed line by line, which is
            // the only version of this screen an agent trusts.
            CalculationTrace = (c.CalculationTrace ?? string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .ToList(),
            Splits = splits
                .Where(s => s.CommissionCalculationId == c.Id)
                .OrderByDescending(s => s.GrossAmount)
                .Select(s => new CommissionSplitDto
                {
                    Id = s.Id,
                    AgentProfileId = s.AgentProfileId,
                    AgentName = s.AgentProfileId is null ? null : agents.GetValueOrDefault(s.AgentProfileId.Value),
                    SalesTeamId = s.SalesTeamId,
                    TeamName = s.SalesTeamId is null ? null : teams.GetValueOrDefault(s.SalesTeamId.Value),
                    ChannelPartnerId = s.ChannelPartnerId,
                    PartnerName = s.ChannelPartnerId is null ? null : partners.GetValueOrDefault(s.ChannelPartnerId.Value),
                    ReferrerName = s.ReferrerPartyId is null ? null : referrers.GetValueOrDefault(s.ReferrerPartyId.Value),
                    Role = s.Role,
                    BaseAmount = s.BaseAmount,
                    SharePercent = s.SharePercent,
                    GrossAmount = s.GrossAmount,
                    DeductionTotal = s.DeductionTotal,
                    WithholdingAmount = s.WithholdingAmount,
                    NetAmount = s.NetAmount,
                    PaidAmount = s.PaidAmount,
                    Status = s.Status,
                    TierApplied = s.TierApplied,
                    CapReached = s.CapReached,
                    CapContribution = s.CapContribution,
                    Deductions = deductions
                        .Where(d => d.CommissionSplitId == s.Id)
                        .OrderBy(d => d.SortOrder)
                        .Select(d => new CommissionDeductionDto
                        {
                            Id = d.Id,
                            Kind = d.Kind,
                            Label = d.Label,
                            Percent = d.Percent,
                            Amount = d.Amount,
                            Note = d.Note,
                            SortOrder = d.SortOrder,
                        }).ToList(),
                }).ToList(),
        }).ToList();
    }

    // ═══ Disbursement ════════════════════════════════════════════════════════

    public async Task<CommissionDisbursementDto> CreateDisbursementAsync(Guid calculationId, Guid userId)
    {
        var calculation = await Db.CommissionCalculations.ForCompany(Tenant)
            .Include(c => c.Splits)
            .FirstOrDefaultAsync(c => c.Id == calculationId)
            ?? throw new InvalidOperationException("That calculation does not exist.");

        if (calculation.DisbursementId is not null)
            throw new InvalidOperationException("A disbursement has already been raised on this calculation.");

        if (calculation.IsDisputed)
            throw new InvalidOperationException(
                "That calculation is disputed. Settle the dispute before paying anything out.");

        if (calculation.Status == CommissionStatus.ClawedBack)
            throw new InvalidOperationException("That commission has been clawed back.");

        // Fee money that has not arrived cannot be paid out. Doing so funds an agent's share from
        // client money or from the company's own float, and one of those is an offence.
        if (calculation.DealId is not null)
        {
            var feeReceived = await Db.Deals.ForCompany(Tenant)
                .Where(d => d.Id == calculation.DealId)
                .Select(d => d.FeeReceived)
                .FirstOrDefaultAsync();

            if (!feeReceived)
                throw new InvalidOperationException(
                    "The fee on that deal has not been received yet. Commission cannot be disbursed before it is.");
        }

        var distributable = calculation.Splits.Where(s => s.Role != "House").Sum(s => s.NetAmount);
        var houseRetained = RealEstateMapper.Money(calculation.GrossFee - distributable);

        var disbursement = new CommissionDisbursement
        {
            Reference = await numbering.NextMasterCodeAsync(Db.CommissionDisbursements, "DSB"),
            CommissionCalculationId = calculation.Id,
            DealId = calculation.DealId,
            BookingId = calculation.BookingId,
            IssuedOn = Today,
            GrossFee = calculation.GrossFee,
            TotalDisbursed = RealEstateMapper.Money(distributable),
            HouseRetained = houseRetained,
            PreparedByUserId = userId,
            Outcome = ApprovalOutcome.Pending,
        }.StampNew(Tenant, userId);

        var approval = await RaiseApprovalAsync(
            nameof(CommissionDisbursement), disbursement.Id, disbursement.Reference, disbursement.TotalDisbursed,
            $"Commission disbursement of {disbursement.TotalDisbursed:N0} on {calculation.Reference}",
            userId, projectId: calculation.ProjectId);

        disbursement.ApprovalRequestId = approval?.Id;

        if (approval is null)
        {
            disbursement.Outcome = ApprovalOutcome.AutoApproved;
            disbursement.ApprovedAt = DateTime.UtcNow;
            disbursement.ApprovedByUserId = userId;
        }

        Db.CommissionDisbursements.Add(disbursement);

        calculation.DisbursementId = disbursement.Id;
        calculation.Status = CommissionStatus.Approved;
        calculation.StampUpdated(userId);

        await Db.SaveChangesAsync();

        return await MapDisbursementAsync(disbursement, calculation);
    }

    public async Task<CommissionDisbursementDto> DecideDisbursementAsync(
        Guid id, ApprovalOutcome outcome, string? comment, Guid userId)
    {
        var disbursement = await RequireAsync<CommissionDisbursement>(id, "That disbursement does not exist.");

        if (disbursement.Outcome is ApprovalOutcome.Approved or ApprovalOutcome.AutoApproved)
            throw new InvalidOperationException("That disbursement has already been approved.");

        var calculation = await Db.CommissionCalculations.ForCompany(Tenant)
            .Include(c => c.Splits)
            .FirstOrDefaultAsync(c => c.Id == disbursement.CommissionCalculationId)
            ?? throw new InvalidOperationException("The underlying calculation has gone.");

        disbursement.Outcome = outcome;
        disbursement.ApprovedByUserId = userId;
        disbursement.ApprovedAt = DateTime.UtcNow;
        disbursement.Description = comment;
        disbursement.StampUpdated(userId);

        if (outcome is ApprovalOutcome.Rejected or ApprovalOutcome.Withdrawn)
        {
            calculation.DisbursementId = null;
            calculation.Status = CommissionStatus.Accrued;
            calculation.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return await MapDisbursementAsync(disbursement, calculation);
    }

    private async Task<CommissionDisbursementDto> MapDisbursementAsync(
        CommissionDisbursement disbursement, CommissionCalculation calculation)
    {
        var currency = await CurrencyAsync();
        var mapped = await MapCalculationsAsync([calculation]);
        var users = await AgentUserNamesAsync([disbursement.PreparedByUserId, disbursement.ApprovedByUserId]);

        return new CommissionDisbursementDto
        {
            Id = disbursement.Id,
            Reference = disbursement.Reference,
            CommissionCalculationId = disbursement.CommissionCalculationId,
            DealId = disbursement.DealId,
            BookingId = disbursement.BookingId,
            Subject = mapped[0].DealReference ?? mapped[0].BookingReference,
            IssuedOn = disbursement.IssuedOn,
            GrossFee = disbursement.GrossFee,
            TotalDisbursed = disbursement.TotalDisbursed,
            HouseRetained = disbursement.HouseRetained,
            CurrencyCode = currency,
            PreparedByName = disbursement.PreparedByUserId is null
                ? null
                : users.GetValueOrDefault(disbursement.PreparedByUserId.Value),
            Outcome = disbursement.Outcome,
            ApprovedByName = disbursement.ApprovedByUserId is null
                ? null
                : users.GetValueOrDefault(disbursement.ApprovedByUserId.Value),
            ApprovedAt = disbursement.ApprovedAt,
            DocumentUrl = disbursement.DocumentUrl,
            FromClientAccount = disbursement.FromClientAccount,
            Splits = mapped[0].Splits,
        };
    }

    // ═══ Payout runs ═════════════════════════════════════════════════════════

    public async Task<CommissionPayoutDto> CreatePayoutAsync(
        Guid? agentId, Guid? partnerId, DateOnly from, DateOnly to, Guid userId)
    {
        if (agentId is null && partnerId is null)
            throw new InvalidOperationException("A payout is either to an agent or to a channel partner.");

        if (to < from) throw new InvalidOperationException("The period ends before it begins.");

        var splits = await Db.CommissionSplits.ForCompany(Tenant)
            .WhereIf(agentId.HasValue, s => s.AgentProfileId == agentId)
            .WhereIf(partnerId.HasValue, s => s.ChannelPartnerId == partnerId)
            .Where(s => s.Status == CommissionStatus.Approved || s.Status == CommissionStatus.PartiallyPaid)
            .Where(s => s.NetAmount > s.PaidAmount)
            .ToListAsync();

        var calculationIds = splits.Select(s => s.CommissionCalculationId).Distinct().ToList();

        var inPeriod = calculationIds.Count == 0
            ? []
            : await Db.CommissionCalculations.ForCompany(Tenant)
                .Where(c => calculationIds.Contains(c.Id) && c.CalculatedOn >= from && c.CalculatedOn <= to)
                .Select(c => c.Id)
                .ToListAsync();

        splits = splits.Where(s => inPeriod.Contains(s.CommissionCalculationId)).ToList();

        if (splits.Count == 0)
            throw new InvalidOperationException("There is nothing approved and unpaid in that period.");

        var gross = RealEstateMapper.Money(splits.Sum(s => s.NetAmount - s.PaidAmount));
        var withholding = RealEstateMapper.Money(splits.Sum(s => s.WithholdingAmount));

        // Advances come off the payout before anything is paid. Recovering them afterwards means
        // asking somebody to give back money that is already in their account.
        var advanceRecovered = 0m;

        if (partnerId is not null)
        {
            var advances = await Db.PartnerAdvances.ForCompany(Tenant)
                .Where(a => a.ChannelPartnerId == partnerId && a.OutstandingAmount > 0m && !a.IsWrittenOff)
                .ToListAsync();

            foreach (var advance in advances)
            {
                var slice = Math.Min(
                    advance.OutstandingAmount,
                    RealEstateMapper.Money(gross * advance.RecoveryPercent / 100m));

                if (slice <= 0m) continue;

                advance.RecoveredAmount = RealEstateMapper.Money(advance.RecoveredAmount + slice);
                advance.OutstandingAmount = RealEstateMapper.Money(advance.Amount - advance.RecoveredAmount);

                if (advance.OutstandingAmount <= 0.01m) advance.FullyRecoveredOn = Today;

                advance.StampUpdated(userId);
                advanceRecovered += slice;
            }
        }

        var clawback = RealEstateMapper.Money(splits.Where(s => s.NetAmount < 0m).Sum(s => -s.NetAmount));

        var payout = new CommissionPayout
        {
            Reference = await numbering.NextMasterCodeAsync(Db.CommissionPayouts, "PAY"),
            AgentProfileId = agentId,
            ChannelPartnerId = partnerId,
            PeriodFrom = from,
            PeriodTo = to,
            PaidOn = Today,
            GrossAmount = gross,
            WithholdingAmount = withholding,
            AdvanceRecovered = RealEstateMapper.Money(advanceRecovered),
            ClawbackAmount = clawback,
            NetAmount = RealEstateMapper.Money(gross - advanceRecovered - clawback),
            Instrument = PaymentInstrument.BankTransfer,
        }.StampNew(Tenant, userId);

        Db.CommissionPayouts.Add(payout);

        foreach (var split in splits)
        {
            var payable = RealEstateMapper.Money(split.NetAmount - split.PaidAmount);

            Db.CommissionPayoutLines.Add(new CommissionPayoutLine
            {
                CommissionPayoutId = payout.Id,
                CommissionSplitId = split.Id,
                CommissionCalculationId = split.CommissionCalculationId,
                Amount = payable,
                IsClawback = payable < 0m,
            }.StampNew(Tenant, userId));

            split.PaidAmount = split.NetAmount;
            split.Status = CommissionStatus.Paid;
            split.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await GetPayoutsAsync(new ListQueryDto { PageSize = 1 })).Data?.FirstOrDefault(p => p.Id == payout.Id)
            ?? await MapPayoutAsync(payout);
    }

    public async Task<PaginatedResponse<CommissionPayoutDto>> GetPayoutsAsync(ListQueryDto query)
    {
        var q = Db.CommissionPayouts.ForCompany(Tenant)
            .WhereIf(query.FromDate.HasValue, p => p.PaidOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, p => p.PaidOn <= query.ToDate)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), p => p.Reference.Contains(query.Search!))
            .Include(p => p.Lines)
            .OrderByDescending(p => p.PaidOn);

        return await PageAsync(q, query, async rows =>
        {
            var result = new List<CommissionPayoutDto>();

            foreach (var row in rows) result.Add(await MapPayoutAsync(row));

            return result;
        });
    }

    private async Task<CommissionPayoutDto> MapPayoutAsync(CommissionPayout payout)
    {
        var currency = await CurrencyAsync();
        var agents = await AgentDisplayNamesAsync([payout.AgentProfileId]);

        var partnerName = payout.ChannelPartnerId is null
            ? null
            : await Db.ChannelPartners.ForCompany(Tenant)
                .Where(p => p.Id == payout.ChannelPartnerId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();

        var lines = payout.Lines.Count > 0
            ? payout.Lines.ToList()
            : await Db.CommissionPayoutLines.ForCompany(Tenant)
                .Where(l => l.CommissionPayoutId == payout.Id)
                .ToListAsync();

        var calculationIds = lines.Where(l => l.CommissionCalculationId != null)
            .Select(l => l.CommissionCalculationId!.Value).Distinct().ToList();

        var references = calculationIds.Count == 0
            ? []
            : await Db.CommissionCalculations.ForCompany(Tenant)
                .Where(c => calculationIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Reference);

        return new CommissionPayoutDto
        {
            Id = payout.Id,
            Reference = payout.Reference,
            AgentProfileId = payout.AgentProfileId,
            AgentName = payout.AgentProfileId is null ? null : agents.GetValueOrDefault(payout.AgentProfileId.Value),
            ChannelPartnerId = payout.ChannelPartnerId,
            PartnerName = partnerName,
            PeriodFrom = payout.PeriodFrom,
            PeriodTo = payout.PeriodTo,
            PaidOn = payout.PaidOn,
            GrossAmount = payout.GrossAmount,
            DeductionAmount = payout.DeductionAmount,
            WithholdingAmount = payout.WithholdingAmount,
            AdvanceRecovered = payout.AdvanceRecovered,
            ClawbackAmount = payout.ClawbackAmount,
            NetAmount = payout.NetAmount,
            CurrencyCode = currency,
            Instrument = payout.Instrument,
            PaymentReference = payout.PaymentReference,
            PaidViaPayroll = payout.PaidViaPayroll,
            IsPaid = payout.IsPaid,
            Lines = lines.Select(l => new CommissionPayoutLineDto
            {
                Id = l.Id,
                CommissionSplitId = l.CommissionSplitId,
                Description = l.CommissionCalculationId is null
                    ? "Commission"
                    : references.GetValueOrDefault(l.CommissionCalculationId.Value, "Commission"),
                Amount = l.Amount,
                IsClawback = l.IsClawback,
            }).ToList(),
        };
    }

    public async Task<AgentCapLedgerDto?> GetCapPositionAsync(Guid agentId, int? year)
    {
        var target = year ?? Today.Year;

        var ledger = await Db.AgentCapLedgers.ForCompany(Tenant)
            .Where(l => l.AgentProfileId == agentId && l.PeriodFrom.Year == target)
            .OrderByDescending(l => l.PeriodFrom)
            .FirstOrDefaultAsync();

        if (ledger is null) return null;

        var currency = await CurrencyAsync();
        var names = await AgentDisplayNamesAsync([agentId]);

        return new AgentCapLedgerDto
        {
            AgentProfileId = ledger.AgentProfileId,
            AgentName = names.GetValueOrDefault(agentId, "—"),
            PeriodFrom = ledger.PeriodFrom,
            PeriodTo = ledger.PeriodTo,
            CapAmount = ledger.CapAmount,
            ContributedAmount = ledger.ContributedAmount,
            RemainingToCap = ledger.RemainingToCap,
            PercentToCap = RealEstateMapper.Percent(ledger.ContributedAmount, ledger.CapAmount),
            CapReached = ledger.CapReached,
            CapReachedOn = ledger.CapReachedOn,
            RolledOverAmount = ledger.RolledOverAmount,
            GrossCommissionEarned = ledger.GrossCommissionEarned,
            NetCommissionEarned = ledger.NetCommissionEarned,
            DealCount = ledger.DealCount,
            TransactionVolume = ledger.TransactionVolume,
            CurrencyCode = currency,
        };
    }

    /// <summary>
    /// Reverses commission on a booking that has since been cancelled.
    ///
    /// This is the least popular feature in any brokerage system and the one that keeps the books
    /// honest. It writes negative splits rather than editing the originals, so the history reads
    /// as what happened — earned, then reversed — rather than as though the deal never existed.
    /// </summary>
    public async Task<int> ClawBackAsync(Guid bookingId, Guid userId)
    {
        var booking = await RequireAsync<Booking>(bookingId, "That booking does not exist.");

        if (booking.Status != BookingStatus.Cancelled)
            throw new InvalidOperationException(
                "That booking has not been cancelled, so there is nothing to claw back.");

        var calculations = await Db.CommissionCalculations.ForCompany(Tenant)
            .Include(c => c.Splits)
            .Where(c => c.BookingId == bookingId && c.Status != CommissionStatus.ClawedBack)
            .ToListAsync();

        if (calculations.Count == 0) return 0;

        var reversed = 0;

        foreach (var calculation in calculations)
        {
            foreach (var split in calculation.Splits.Where(s => s.Role != "House"))
            {
                if (split.NetAmount <= 0m) continue;

                var reversal = new CommissionSplit
                {
                    CommissionCalculationId = calculation.Id,
                    AgentProfileId = split.AgentProfileId,
                    SalesTeamId = split.SalesTeamId,
                    ChannelPartnerId = split.ChannelPartnerId,
                    ReferrerPartyId = split.ReferrerPartyId,
                    Role = split.Role + "Clawback",
                    BaseAmount = -split.BaseAmount,
                    SharePercent = split.SharePercent,
                    GrossAmount = -split.GrossAmount,
                    DeductionTotal = -split.DeductionTotal,
                    WithholdingAmount = -split.WithholdingAmount,

                    // Only what was actually paid is clawed back. Reversing an accrual nobody
                    // received would leave the agent owing money they never had.
                    NetAmount = -split.PaidAmount,
                    Status = CommissionStatus.ClawedBack,
                }.StampNew(Tenant, userId);

                Db.CommissionSplits.Add(reversal);

                split.Status = CommissionStatus.ClawedBack;
                split.StampUpdated(userId);

                reversed++;
            }

            calculation.Status = CommissionStatus.ClawedBack;
            calculation.StampUpdated(userId);
        }

        // A partner's own commission ledger is separate and has to be reversed alongside.
        var entries = await Db.PartnerCommissionEntries.ForCompany(Tenant)
            .Where(e => e.BookingId == bookingId && e.Status != CommissionStatus.ClawedBack)
            .ToListAsync();

        foreach (var entry in entries)
        {
            entry.ClawedBackAmount = entry.PaidAmount;
            entry.Status = CommissionStatus.ClawedBack;
            entry.StampUpdated(userId);

            var partner = await Db.ChannelPartners.ForCompany(Tenant)
                .FirstOrDefaultAsync(p => p.Id == entry.ChannelPartnerId);

            if (partner is not null)
            {
                partner.CancellationCount++;
                partner.CommissionPending = RealEstateMapper.Money(
                    Math.Max(0m, partner.CommissionPending - (entry.NetAmount - entry.PaidAmount)));
                partner.StampUpdated(userId);
            }

            reversed++;
        }

        await QueueNotificationAsync(
            "commission.clawback",
            $"Commission clawed back on {booking.Reference}",
            $"{reversed} entries reversed following cancellation.",
            "/realestate/brokerage/commission",
            entityType: nameof(Booking),
            entityId: bookingId,
            severity: AlertSeverity.Warning);

        await Db.SaveChangesAsync();

        return reversed;
    }
}
