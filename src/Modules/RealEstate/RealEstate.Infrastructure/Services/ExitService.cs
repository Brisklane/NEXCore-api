using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// How a booking ends: cancellation, refund, resale, transfer, possession and the defect period.
///
/// This file is the cancellation half. The deduction is the whole argument in a cancellation —
/// customers dispute it, regulators review it, and a number nobody can explain is a number nobody
/// collects. So it is computed as named lines, each carrying the slab or rule that produced it,
/// and the preview shows the customer the working before anything is committed.
/// </summary>
public partial class ExitService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering,
    IBrokerageService brokerage)
    : RealEstateServiceBase(db, tenant), IExitService
{
    // ═══ Cancellation ════════════════════════════════════════════════════════

    public async Task<CancellationPreviewDto> PreviewCancellationAsync(CancellationRequestDto dto)
        => (await BuildCancellationAsync(dto)).Preview;

    /// <summary>
    /// The one place the cancellation arithmetic lives. Preview and commit both call it, so what
    /// the customer was shown and what is written can never disagree.
    /// </summary>
    private async Task<(CancellationPreviewDto Preview, DeductionPolicy? Policy, Booking Booking)> BuildCancellationAsync(
        CancellationRequestDto dto)
    {
        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw new InvalidOperationException("That booking does not exist.");

        if (booking.Status == BookingStatus.Cancelled)
            throw new InvalidOperationException($"{booking.Reference} is already cancelled.");

        var settings = await SettingsAsync();
        var currency = booking.CurrencyCode ?? settings.CurrencyCode;
        var requestedOn = dto.RequestedOn == default ? Today : dto.RequestedOn;

        var policy = await ResolvePolicyAsync(dto.DeductionPolicyId, booking.ProjectId);

        var surcharge = await Db.Instalments.ForCompany(Tenant)
            .Where(i => i.BookingId == booking.Id)
            .SumAsync(i => i.SurchargeAccrued - i.SurchargePaid - i.SurchargeWaived);

        var bookedOn = booking.BookingDate;
        var months = Math.Max(0, ((requestedOn.Year - bookedOn.Year) * 12) + requestedOn.Month - bookedOn.Month);
        var paidPercent = RealEstateMapper.Percent(booking.TotalPaid, booking.TotalConsideration);

        var preview = new CancellationPreviewDto
        {
            TotalConsideration = booking.TotalConsideration,
            TotalPaid = booking.TotalPaid,
            SurchargeOutstanding = Math.Max(0m, surcharge),
            CurrencyCode = currency,
            MonthsSinceBooking = months,
            PaidPercent = paidPercent,
            RefundOnlyAfterResale = policy?.RefundOnlyAfterResale ?? false,
            RefundInstalmentCount = Math.Max(1, policy?.RefundInstalmentCount ?? 1),
        };

        var order = 0;

        void Line(string label, decimal amount, string? basis)
        {
            if (amount <= 0m) return;

            preview.Deductions.Add(new DeductionLineDto
            {
                Label = label,
                Amount = RealEstateMapper.Money(amount),
                Basis = basis,
                SortOrder = order += 10,
            });
        }

        // The principal deduction.
        if (policy is null)
        {
            preview.SlabApplied = "No deduction policy is configured, so nothing is deducted.";
        }
        else
        {
            var (amount, basis, slab) = ComputeDeduction(policy, booking, months, paidPercent);
            preview.SlabApplied = slab;
            Line("Cancellation deduction", amount, basis);
        }

        // The administrative charge, the accrued surcharge, and what the dealer was paid.
        if (policy is not null && policy.AdministrativeCharge > 0m)
            Line("Administrative charge", policy.AdministrativeCharge, "Fixed charge under the cancellation policy");

        if (policy?.ForfeitAccruedSurcharge == true && preview.SurchargeOutstanding > 0m)
            Line("Late-payment surcharge accrued", preview.SurchargeOutstanding, "Surcharge outstanding is forfeited under the policy");

        if (policy?.ClawBackCommission == true)
        {
            var paidCommission = await Db.CommissionCalculations.ForCompany(Tenant)
                .Where(c => c.BookingId == booking.Id && c.Status == CommissionStatus.Paid)
                .SumAsync(c => c.NetDistributable);

            preview.CommissionClawback = RealEstateMapper.Money(paidCommission);
            Line("Commission recovered", paidCommission, "Commission already paid on this booking");
        }

        // Anything else still owed rides along rather than being written off quietly.
        var otherCharges = await Db.BookingChargeLines.ForCompany(Tenant)
            .Where(c => c.BookingId == booking.Id && c.IsAccepted && !c.IsPartOfSalePrice)
            .Select(c => new { c.Label, Amount = c.Amount + c.TaxAmount })
            .ToListAsync();

        foreach (var charge in otherCharges)
            Line(charge.Label, charge.Amount, "Charge outside the sale price, not refundable");

        if (dto.OverrideDeductionAmount is not null)
        {
            var computed = preview.Deductions.Sum(d => d.Amount);
            var difference = computed - dto.OverrideDeductionAmount.Value;

            if (difference > 0m)
            {
                preview.Deductions.Add(new DeductionLineDto
                {
                    Label = "Waiver approved",
                    Amount = RealEstateMapper.Money(-difference),
                    Basis = dto.OverrideReason ?? "Manual override",
                    IsWaived = true,
                    SortOrder = order + 10,
                });
            }
        }

        preview.DeductionTotal = RealEstateMapper.Money(preview.Deductions.Sum(d => d.Amount));

        // A deduction can consume everything, but it never creates a debt on the way out.
        preview.RefundableAmount = RealEstateMapper.Money(Math.Max(0m, booking.TotalPaid - preview.DeductionTotal));

        preview.RefundableInWords = RealEstateMapper.AmountInWords(
            preview.RefundableAmount, currency, RealEstateMapper.UsesIndianScale(currency));

        preview.RequiresApproval = true;
        preview.ApprovalsRequired.Add("Cancellation approval");

        if (dto.OverrideDeductionAmount is not null)
            preview.ApprovalsRequired.Add("Deduction waiver approval");

        if (preview.RefundableAmount > 0m)
            preview.ApprovalsRequired.Add("Refund approval");

        return (preview, policy, booking);
    }

    private async Task<DeductionPolicy?> ResolvePolicyAsync(Guid? explicitId, Guid projectId)
    {
        if (explicitId is not null)
        {
            return await Db.DeductionPolicies.ForCompany(Tenant)
                .Include(p => p.Slabs)
                .FirstOrDefaultAsync(p => p.Id == explicitId);
        }

        var project = await Db.Projects.ForCompany(Tenant)
            .Where(p => p.Id == projectId)
            .Select(p => p.DefaultDeductionPolicyId)
            .FirstOrDefaultAsync();

        if (project is not null)
        {
            return await Db.DeductionPolicies.ForCompany(Tenant)
                .Include(p => p.Slabs)
                .FirstOrDefaultAsync(p => p.Id == project);
        }

        // Project-specific policy first, then the company-wide fallback.
        return await Db.DeductionPolicies.ForCompany(Tenant)
            .Include(p => p.Slabs)
            .Where(p => p.IsActive && (p.ProjectId == projectId || p.ProjectId == null))
            .OrderByDescending(p => p.ProjectId != null)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// The deduction itself. A pure function of the policy and the booking, so the same inputs
    /// give the same number in a preview, in a commit, and in a dispute two years later.
    /// </summary>
    private static (decimal Amount, string Basis, string? Slab) ComputeDeduction(
        DeductionPolicy policy, Booking booking, int months, decimal paidPercent)
        => policy.Basis switch
        {
            DeductionBasis.NoDeduction => (0m, "Policy deducts nothing", "No deduction"),

            DeductionBasis.ForfeitBookingAmount => (
                booking.TotalConsideration * 0.1m,
                "The booking amount is forfeited in full",
                "Booking amount forfeited"),

            DeductionBasis.FlatAmount => (
                policy.FlatAmount,
                $"Flat deduction of {policy.FlatAmount:N0}",
                "Flat amount"),

            DeductionBasis.PercentOfPrice => (
                booking.TotalConsideration * policy.FlatPercent / 100m,
                $"{policy.FlatPercent:N2}% of the sale price",
                $"{policy.FlatPercent:N2}% of price"),

            DeductionBasis.PercentOfPaid => (
                booking.TotalPaid * policy.FlatPercent / 100m,
                $"{policy.FlatPercent:N2}% of what has been paid",
                $"{policy.FlatPercent:N2}% of paid"),

            DeductionBasis.SlabByElapsed => ApplySlab(policy, booking, months, paidPercent),

            _ => (0m, "No rule matched", null),
        };

    private static (decimal Amount, string Basis, string? Slab) ApplySlab(
        DeductionPolicy policy, Booking booking, int months, decimal paidPercent)
    {
        // Slabs are expressed either by elapsed months or by how much has been paid; a policy can
        // legitimately use both, so both are matched and the most specific band wins.
        var slab = policy.Slabs
            .Where(s => months >= s.FromMonth && (s.ToMonth is null || months <= s.ToMonth))
            .Where(s => s.FromPaidPercent is null || paidPercent >= s.FromPaidPercent)
            .Where(s => s.ToPaidPercent is null || paidPercent <= s.ToPaidPercent)
            .OrderByDescending(s => s.FromPaidPercent is not null)
            .ThenByDescending(s => s.FromMonth)
            .FirstOrDefault();

        if (slab is null)
            return (0m, "No slab covers this booking's age", "No slab matched");

        var band = slab.ToMonth is null
            ? $"month {slab.FromMonth} onward"
            : $"months {slab.FromMonth} to {slab.ToMonth}";

        if (slab.DeductionAmount > 0m)
            return (slab.DeductionAmount, $"Fixed deduction for {band}", band);

        var basis = slab.AppliesToPaidAmount ? booking.TotalPaid : booking.TotalConsideration;
        var label = slab.AppliesToPaidAmount ? "of what has been paid" : "of the sale price";

        return (
            basis * slab.DeductionPercent / 100m,
            $"{slab.DeductionPercent:N2}% {label}, the rate for {band}",
            $"{slab.DeductionPercent:N2}% — {band}");
    }

    public async Task<CancellationDto> RequestCancellationAsync(CancellationRequestDto dto, Guid userId)
    {
        var (preview, policy, booking) = await BuildCancellationAsync(dto);

        if (dto.DryRun)
            throw new InvalidOperationException("This was a dry run. Nothing was written.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var requestedOn = dto.RequestedOn == default ? Today : dto.RequestedOn;

        var cancellation = new Cancellation
        {
            Reference = await numbering.NextCancellationNumberAsync(DateTime.UtcNow),
            BookingId = booking.Id,
            PartyId = booking.PrimaryApplicantPartyId,
            UnitId = booking.UnitId,
            PlotFileId = booking.PlotFileId,
            Trigger = dto.Trigger,
            ReasonCodeId = dto.ReasonCodeId,
            Note = dto.Note,
            RequestedOn = requestedOn,
            EffectiveOn = dto.EffectiveOn,
            TotalConsideration = preview.TotalConsideration,
            TotalPaid = preview.TotalPaid,
            SurchargeOutstanding = preview.SurchargeOutstanding,
            DeductionPolicyId = policy?.Id,
            DeductionAmount = preview.DeductionTotal,
            AdministrativeCharge = policy?.AdministrativeCharge ?? 0m,
            CommissionClawback = preview.CommissionClawback,
            RefundableAmount = preview.RefundableAmount,
            RequestedByUserId = userId,
            Outcome = ApprovalOutcome.Pending,
        }.StampNew(Tenant, userId);

        Db.Cancellations.Add(cancellation);

        foreach (var line in preview.Deductions)
        {
            cancellation.Deductions.Add(new DeductionLine
            {
                Label = line.Label,
                Amount = line.Amount,
                Basis = line.Basis,
                IsWaived = line.IsWaived,
                SortOrder = line.SortOrder,
            }.StampNew(Tenant, userId));
        }

        // The booking goes into cancellation rather than straight to cancelled. Someone still has
        // to approve, and until they do the money position must not move.
        booking.Status = BookingStatus.UnderCancellation;
        booking.StampUpdated(userId);

        var approval = await RaiseApprovalAsync(
            "BookingCancellation", cancellation.Id, cancellation.Reference, preview.RefundableAmount,
            $"Cancel {booking.Reference}. {preview.DeductionTotal:N0} deducted, {preview.RefundableAmount:N0} refundable.",
            userId, projectId: booking.ProjectId, reasonCodeId: dto.ReasonCodeId);

        cancellation.ApprovalRequestId = approval?.Id;

        if (approval is null)
        {
            // Auto-approved by the matrix — carry straight on rather than parking it.
            await ApplyCancellationAsync(cancellation, booking, dto.ReleaseUnitImmediately, userId);
        }

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await GetCancellationAsync(cancellation.Id))!;
    }

    public async Task<CancellationDto> DecideCancellationAsync(Guid id, ApprovalOutcome outcome, string? comment, Guid userId)
    {
        var cancellation = await Db.Cancellations.ForCompany(Tenant)
            .Include(c => c.Deductions)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("That cancellation does not exist.");

        if (cancellation.Outcome != ApprovalOutcome.Pending)
            throw new InvalidOperationException($"This cancellation was already {cancellation.Outcome.ToString().ToLowerInvariant()}.");

        var booking = await RequireAsync<Booking>(cancellation.BookingId, "The booking behind this cancellation is missing.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        cancellation.Outcome = outcome;
        cancellation.ApprovedByUserId = userId;
        cancellation.ApprovedAt = DateTime.UtcNow;
        cancellation.StampUpdated(userId);

        if (outcome is ApprovalOutcome.Approved or ApprovalOutcome.AutoApproved)
        {
            await ApplyCancellationAsync(cancellation, booking, true, userId);
        }
        else
        {
            // Refused: the booking goes back exactly where it was, not to some neutral state.
            booking.Status = booking.AgreementSignedOn is null ? BookingStatus.Confirmed : BookingStatus.AgreementSigned;
            booking.StampUpdated(userId);
        }

        await WriteAuditNoteAsync(
            "Cancellation", id, $"Cancellation{outcome}", cancellation.ReasonCodeId, userId,
            amountImpact: cancellation.RefundableAmount,
            note: comment,
            entityReference: cancellation.Reference,
            highRisk: true);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await GetCancellationAsync(id))!;
    }

    /// <summary>
    /// Everything that happens once a cancellation is approved: the unit goes back on the board,
    /// the schedule stops, the dealer's commission is clawed back and the refund is raised.
    /// </summary>
    private async Task ApplyCancellationAsync(Cancellation cancellation, Booking booking, bool releaseUnit, Guid userId)
    {
        var today = Today;

        booking.Status = BookingStatus.Cancelled;
        booking.CancellationId = cancellation.Id;
        booking.StampUpdated(userId);

        // Remaining instalments stop being collectable, but they are marked rather than deleted —
        // the plan is evidence of what was owed at the point it ended.
        var instalments = await Db.Instalments.ForCompany(Tenant)
            .Where(i => i.BookingId == booking.Id
                     && i.Status != InstalmentStatus.Paid
                     && i.Status != InstalmentStatus.Cancelled)
            .ToListAsync();

        foreach (var instalment in instalments)
        {
            instalment.Status = InstalmentStatus.Cancelled;
            instalment.StampUpdated(userId);
        }

        if (releaseUnit && booking.UnitId is not null)
        {
            var unit = await Db.Units.ForCompany(Tenant).FirstOrDefaultAsync(u => u.Id == booking.UnitId);

            if (unit is not null)
            {
                unit.Status = PropertyStatus.Available;
                unit.CurrentBookingId = null;
                unit.CurrentHoldId = null;
                unit.StampUpdated(userId);

                var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == unit.PropertyId);

                if (property is not null)
                {
                    property.Status = PropertyStatus.Available;
                    property.CurrentBookingId = null;
                    property.StampUpdated(userId);
                }
            }

            cancellation.UnitReleased = true;
            cancellation.UnitReleasedOn = today;
        }

        if (booking.PlotFileId is not null)
        {
            var file = await Db.PlotFiles.ForCompany(Tenant).FirstOrDefaultAsync(f => f.Id == booking.PlotFileId);

            if (file is not null)
            {
                file.Status = PropertyStatus.Available;
                file.CurrentBookingId = null;
                file.StampUpdated(userId);
            }
        }

        if (cancellation.CommissionClawback > 0m)
            await brokerage.ClawBackAsync(booking.Id, userId);

        // The refund is a separate approval with its own dual control. Cancelling a booking and
        // paying money out are two different decisions and two different risks.
        if (cancellation.RefundableAmount > 0m && cancellation.RefundRequestId is null)
        {
            var policy = cancellation.DeductionPolicyId is null
                ? null
                : await Db.DeductionPolicies.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == cancellation.DeductionPolicyId);

            var refund = await CreateRefundAsync(new RefundRequestDto
            {
                PartyId = cancellation.PartyId,
                BookingId = booking.Id,
                CancellationId = cancellation.Id,
                RequestedAmount = cancellation.RefundableAmount,
                RequestedOn = today,
                AwaitingResale = policy?.RefundOnlyAfterResale ?? false,
            }, userId);

            cancellation.RefundRequestId = refund.Id;
        }

        await QueueNotificationAsync(
            "BookingCancelled",
            $"{booking.Reference} cancelled",
            cancellation.RefundableAmount > 0m
                ? $"{cancellation.RefundableAmount:N0} is refundable after deductions of {cancellation.DeductionAmount:N0}."
                : $"Deductions of {cancellation.DeductionAmount:N0} leave nothing refundable.",
            $"/realestate/cancellations/{cancellation.Id}",
            recipientPartyId: cancellation.PartyId,
            entityType: "Cancellation",
            entityId: cancellation.Id,
            severity: AlertSeverity.Warning);
    }

    public async Task<PaginatedResponse<CancellationDto>> GetCancellationsAsync(ListQueryDto query)
    {
        var q = Db.Cancellations.ForCompany(Tenant)
            .Include(c => c.Deductions)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), c => c.Reference.Contains(query.Search!))
            .WhereIf(query.FromDate.HasValue, c => c.RequestedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, c => c.RequestedOn <= query.ToDate)
            .OrderByDescending(c => c.RequestedOn);

        return await PageAsync(q, query, MapCancellationsAsync);
    }

    private async Task<CancellationDto?> GetCancellationAsync(Guid id)
    {
        var cancellation = await Db.Cancellations.ForCompany(Tenant)
            .Include(c => c.Deductions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cancellation is null) return null;
        return (await MapCancellationsAsync([cancellation]))[0];
    }

    private async Task<List<CancellationDto>> MapCancellationsAsync(List<Cancellation> cancellations)
    {
        if (cancellations.Count == 0) return [];

        var currency = await CurrencyAsync();
        var bookingIds = cancellations.Select(c => c.BookingId).Distinct().ToList();

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => bookingIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Reference, b.ProjectId, b.UnitId, b.CurrencyCode })
            .ToListAsync();

        var unitIds = bookings.Where(b => b.UnitId.HasValue).Select(b => b.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant)
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var projects = await ProjectNamesAsync(bookings.Select(b => (Guid?)b.ProjectId));
        var parties = await PartyNamesAsync(cancellations.Select(c => c.PartyId));
        var reasons = await ReasonLabelsAsync(cancellations.Select(c => (Guid?)c.ReasonCodeId));

        var users = await AgentUserNamesAsync(
            cancellations.Select(c => (Guid?)c.RequestedByUserId).Concat(cancellations.Select(c => c.ApprovedByUserId)));

        return cancellations.Select(c =>
        {
            var booking = bookings.FirstOrDefault(b => b.Id == c.BookingId);

            return new CancellationDto
            {
                Id = c.Id,
                Reference = c.Reference,
                BookingId = c.BookingId,
                BookingReference = booking?.Reference ?? "—",
                PartyId = c.PartyId,
                PartyName = parties.GetValueOrDefault(c.PartyId, "—"),
                UnitNumber = booking?.UnitId is null ? null : units.GetValueOrDefault(booking.UnitId.Value),
                ProjectName = booking is null ? "—" : projects.GetValueOrDefault(booking.ProjectId, "—"),

                Trigger = c.Trigger,
                ReasonLabel = reasons.GetValueOrDefault(c.ReasonCodeId, "—"),
                Note = c.Note,
                RequestedOn = c.RequestedOn,
                EffectiveOn = c.EffectiveOn,

                TotalConsideration = c.TotalConsideration,
                TotalPaid = c.TotalPaid,
                SurchargeOutstanding = c.SurchargeOutstanding,
                DeductionAmount = c.DeductionAmount,
                AdministrativeCharge = c.AdministrativeCharge,
                CommissionClawback = c.CommissionClawback,
                RefundableAmount = c.RefundableAmount,
                CurrencyCode = booking?.CurrencyCode ?? currency,

                RequestedByName = users.GetValueOrDefault(c.RequestedByUserId, "—"),
                Outcome = c.Outcome,
                ApprovedByName = c.ApprovedByUserId is null ? null : users.GetValueOrDefault(c.ApprovedByUserId.Value),
                ApprovedAt = c.ApprovedAt,
                RefundRequestId = c.RefundRequestId,
                UnitReleased = c.UnitReleased,
                UnitReleasedOn = c.UnitReleasedOn,
                CustomerAcknowledged = c.CustomerAcknowledged,
                LegalNoticeId = c.LegalNoticeId,

                Deductions = c.Deductions.OrderBy(d => d.SortOrder).Select(d => new DeductionLineDto
                {
                    Id = d.Id,
                    Label = d.Label,
                    Amount = d.Amount,
                    Basis = d.Basis,
                    IsWaived = d.IsWaived,
                    SortOrder = d.SortOrder,
                }).ToList(),
            };
        }).ToList();
    }

    protected async Task<Dictionary<Guid, string>> AgentUserNamesAsync(IEnumerable<Guid?> userIds)
    {
        var ids = userIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => a.UserId != null && ids.Contains(a.UserId.Value))
            .ToDictionaryAsync(a => a.UserId!.Value, a => a.DisplayName);
    }

    // ═══ Deduction policies ══════════════════════════════════════════════════

    public async Task<List<DeductionPolicyDto>> GetDeductionPoliciesAsync(Guid? projectId)
    {
        var policies = await Db.DeductionPolicies.ForCompany(Tenant)
            .Include(p => p.Slabs)
            .WhereIf(projectId.HasValue, p => p.ProjectId == projectId || p.ProjectId == null)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var projects = await ProjectNamesAsync(policies.Select(p => p.ProjectId));

        return policies.Select(p => new DeductionPolicyDto
        {
            Id = p.Id,
            Name = p.Name,
            ProjectId = p.ProjectId,
            ProjectName = p.ProjectId is null ? null : projects.GetValueOrDefault(p.ProjectId.Value),
            Basis = p.Basis,
            FlatPercent = p.FlatPercent,
            FlatAmount = p.FlatAmount,
            AdministrativeCharge = p.AdministrativeCharge,
            ForfeitAccruedSurcharge = p.ForfeitAccruedSurcharge,
            ClawBackCommission = p.ClawBackCommission,
            RefundOnlyAfterResale = p.RefundOnlyAfterResale,
            RefundInstalmentCount = p.RefundInstalmentCount,
            IsActive = p.IsActive,
            Slabs = p.Slabs.OrderBy(s => s.SortOrder).ThenBy(s => s.FromMonth).Select(s => new DeductionSlabDto
            {
                Id = s.Id,
                FromMonth = s.FromMonth,
                ToMonth = s.ToMonth,
                FromPaidPercent = s.FromPaidPercent,
                ToPaidPercent = s.ToPaidPercent,
                DeductionPercent = s.DeductionPercent,
                DeductionAmount = s.DeductionAmount,
                AppliesToPaidAmount = s.AppliesToPaidAmount,
                SortOrder = s.SortOrder,
            }).ToList(),
        }).ToList();
    }

    public async Task<DeductionPolicyDto> SaveDeductionPolicyAsync(DeductionPolicyDto dto, Guid userId)
    {
        var policy = dto.Id != Guid.Empty
            ? await Db.DeductionPolicies.ForCompany(Tenant).Include(p => p.Slabs).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (policy is null)
        {
            policy = new DeductionPolicy().StampNew(Tenant, userId);
            Db.DeductionPolicies.Add(policy);
        }
        else policy.StampUpdated(userId);

        policy.Name = dto.Name;
        policy.ProjectId = dto.ProjectId;
        policy.Basis = dto.Basis;
        policy.FlatPercent = dto.FlatPercent;
        policy.FlatAmount = dto.FlatAmount;
        policy.AdministrativeCharge = dto.AdministrativeCharge;
        policy.ForfeitAccruedSurcharge = dto.ForfeitAccruedSurcharge;
        policy.ClawBackCommission = dto.ClawBackCommission;
        policy.RefundOnlyAfterResale = dto.RefundOnlyAfterResale;
        policy.RefundInstalmentCount = Math.Max(1, dto.RefundInstalmentCount);
        policy.IsActive = dto.IsActive;

        if (dto.Basis == DeductionBasis.SlabByElapsed)
        {
            if (dto.Slabs.Count == 0)
                throw new InvalidOperationException("A slab policy needs at least one slab.");

            // Overlapping slabs mean two defensible answers to the same question, which is exactly
            // the ambiguity a customer's lawyer will use.
            var ordered = dto.Slabs.OrderBy(s => s.FromMonth).ToList();

            for (var i = 1; i < ordered.Count; i++)
            {
                var previous = ordered[i - 1];
                if (previous.ToMonth is null || ordered[i].FromMonth <= previous.ToMonth)
                {
                    if (previous.FromPaidPercent is null && ordered[i].FromPaidPercent is null)
                    {
                        throw new InvalidOperationException(
                            $"The slab starting at month {ordered[i].FromMonth} overlaps the one before it. " +
                            "Bands have to be contiguous, not overlapping.");
                    }
                }
            }

            Db.DeductionSlabs.RemoveRange(policy.Slabs);

            var order = 0;

            foreach (var s in ordered)
            {
                policy.Slabs.Add(new DeductionSlab
                {
                    FromMonth = s.FromMonth,
                    ToMonth = s.ToMonth,
                    FromPaidPercent = s.FromPaidPercent,
                    ToPaidPercent = s.ToPaidPercent,
                    DeductionPercent = s.DeductionPercent,
                    DeductionAmount = s.DeductionAmount,
                    AppliesToPaidAmount = s.AppliesToPaidAmount,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        return (await GetDeductionPoliciesAsync(policy.ProjectId)).First(p => p.Id == policy.Id);
    }

    // ═══ Refunds ═════════════════════════════════════════════════════════════

    public async Task<RefundRequestDto> CreateRefundAsync(RefundRequestDto dto, Guid userId)
    {
        if (dto.RequestedAmount <= 0m)
            throw new InvalidOperationException("A refund has to be for more than zero.");

        var settings = await SettingsAsync();

        var refund = new RefundRequest
        {
            Reference = await numbering.NextRefundNumberAsync(DateTime.UtcNow),
            PartyId = dto.PartyId,
            BookingId = dto.BookingId,
            CancellationId = dto.CancellationId,
            TenancyId = dto.TenancyId,
            TokenReservationId = dto.TokenReservationId,
            Status = dto.AwaitingResale ? RefundStatus.AwaitingResale : RefundStatus.Requested,
            RequestedAmount = dto.RequestedAmount,
            CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? settings.CurrencyCode : dto.CurrencyCode,
            RequestedOn = dto.RequestedOn == default ? Today : dto.RequestedOn,
            RequestedByUserId = userId,
            PayeeName = dto.PayeeName,
            BankName = dto.BankName,
            AccountNumber = dto.AccountNumber,
            AwaitingResale = dto.AwaitingResale,
        }.StampNew(Tenant, userId);

        Db.RefundRequests.Add(refund);
        await Db.SaveChangesAsync();

        var approval = await RaiseApprovalAsync(
            "Refund", refund.Id, refund.Reference, refund.RequestedAmount,
            $"Refund {refund.RequestedAmount:N0} to {(await PartyNamesAsync([refund.PartyId])).GetValueOrDefault(refund.PartyId, "the customer")}.",
            userId);

        refund.ApprovalRequestId = approval?.Id;

        if (approval is null)
            await ApproveRefundAsync(refund, refund.RequestedAmount, userId);

        await Db.SaveChangesAsync();
        return (await GetRefundAsync(refund.Id))!;
    }

    public async Task<RefundRequestDto> DecideRefundAsync(
        Guid id, ApprovalOutcome outcome, decimal? approvedAmount, string? comment, Guid userId)
    {
        var refund = await Db.RefundRequests.ForCompany(Tenant)
            .Include(r => r.Schedule)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("That refund does not exist.");

        if (refund.Status is RefundStatus.Paid or RefundStatus.PartiallyPaid)
            throw new InvalidOperationException("Money has already gone out on this refund.");

        if (outcome is ApprovalOutcome.Approved or ApprovalOutcome.AutoApproved)
        {
            var amount = approvedAmount ?? refund.RequestedAmount;

            if (amount > refund.RequestedAmount)
                throw new InvalidOperationException("An approval cannot be for more than was requested.");

            // Paying into unverified bank details is the single most common refund fraud. It is
            // refused rather than warned about.
            if (!refund.BankDetailsVerified && string.IsNullOrWhiteSpace(refund.AccountNumber))
                throw new InvalidOperationException("Record and verify the payee's bank details before approving this refund.");

            await ApproveRefundAsync(refund, amount, userId);
        }
        else
        {
            refund.Status = RefundStatus.Rejected;
            refund.RejectionReason = comment;
            refund.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        return (await GetRefundAsync(id))!;
    }

    /// <summary>
    /// Approves and schedules. Instalment refunds are a real term — the last instalment absorbs
    /// the rounding so the schedule sums exactly to what was approved.
    /// </summary>
    private async Task ApproveRefundAsync(RefundRequest refund, decimal amount, Guid userId)
    {
        refund.ApprovedAmount = RealEstateMapper.Money(amount);
        refund.ApprovedByUserId = userId;
        refund.ApprovedAt = DateTime.UtcNow;
        refund.Status = refund.AwaitingResale ? RefundStatus.AwaitingResale : RefundStatus.Scheduled;
        refund.StampUpdated(userId);

        if (refund.Schedule.Count > 0) return;

        var policy = refund.CancellationId is null
            ? null
            : await Db.Cancellations.ForCompany(Tenant)
                .Where(c => c.Id == refund.CancellationId)
                .Select(c => c.DeductionPolicyId)
                .FirstOrDefaultAsync();

        var count = policy is null
            ? 1
            : await Db.DeductionPolicies.ForCompany(Tenant)
                .Where(p => p.Id == policy)
                .Select(p => p.RefundInstalmentCount)
                .FirstOrDefaultAsync();

        count = Math.Max(1, count);

        var each = RealEstateMapper.Money(refund.ApprovedAmount / count);
        var running = 0m;
        var start = Today.AddDays(30);

        for (var i = 1; i <= count; i++)
        {
            var value = i == count ? refund.ApprovedAmount - running : each;
            running += value;

            refund.Schedule.Add(new RefundSchedule
            {
                SequenceNumber = i,
                DueDate = start.AddMonths(i - 1),
                Amount = value,
            }.StampNew(Tenant, userId));
        }
    }

    public async Task<RefundRequestDto> RecordRefundPaymentAsync(
        Guid scheduleId, DateOnly paidOn, PaymentInstrument instrument, string? reference, Guid userId)
    {
        var line = await Db.RefundSchedules.ForCompany(Tenant).FirstOrDefaultAsync(s => s.Id == scheduleId)
            ?? throw new InvalidOperationException("That refund instalment does not exist.");

        if (line.IsPaid)
            throw new InvalidOperationException($"Instalment {line.SequenceNumber} was already paid on {line.PaidOn:dd MMM yyyy}.");

        var refund = await Db.RefundRequests.ForCompany(Tenant)
            .Include(r => r.Schedule)
            .FirstOrDefaultAsync(r => r.Id == line.RefundRequestId)
            ?? throw new InvalidOperationException("The refund behind this instalment is missing.");

        if (refund.AwaitingResale && refund.ResaleBookingId is null)
            throw new InvalidOperationException("This refund only funds once the unit resells, and it has not yet.");

        line.PaidAmount = line.Amount;
        line.PaidOn = paidOn;
        line.Instrument = instrument;
        line.PaymentReference = reference;
        line.IsPaid = true;
        line.StampUpdated(userId);

        refund.PaidAmount = refund.Schedule.Sum(s => s.PaidAmount);
        refund.Status = refund.PaidAmount >= refund.ApprovedAmount ? RefundStatus.Paid : RefundStatus.PartiallyPaid;
        refund.StampUpdated(userId);

        await QueueNotificationAsync(
            "RefundPaid",
            $"Refund of {line.Amount:N0} paid",
            refund.Status == RefundStatus.Paid
                ? "Your refund is now settled in full."
                : $"{refund.ApprovedAmount - refund.PaidAmount:N0} remains to be paid.",
            $"/realestate/refunds/{refund.Id}",
            recipientPartyId: refund.PartyId,
            entityType: "RefundRequest",
            entityId: refund.Id);

        await Db.SaveChangesAsync();
        return (await GetRefundAsync(refund.Id))!;
    }

    public async Task<PaginatedResponse<RefundRequestDto>> GetRefundsAsync(ListQueryDto query, RefundStatus? status)
    {
        var q = Db.RefundRequests.ForCompany(Tenant)
            .Include(r => r.Schedule)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), r => r.Reference.Contains(query.Search!))
            .WhereIf(status.HasValue, r => r.Status == status)
            .WhereIf(query.FromDate.HasValue, r => r.RequestedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, r => r.RequestedOn <= query.ToDate)
            .OrderByDescending(r => r.RequestedOn);

        return await PageAsync(q, query, MapRefundsAsync);
    }

    private async Task<RefundRequestDto?> GetRefundAsync(Guid id)
    {
        var refund = await Db.RefundRequests.ForCompany(Tenant)
            .Include(r => r.Schedule)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (refund is null) return null;
        return (await MapRefundsAsync([refund]))[0];
    }

    private async Task<List<RefundRequestDto>> MapRefundsAsync(List<RefundRequest> refunds)
    {
        if (refunds.Count == 0) return [];

        var today = Today;
        var parties = await PartyNamesAsync(refunds.Select(r => r.PartyId));

        var users = await AgentUserNamesAsync(
            refunds.Select(r => (Guid?)r.RequestedByUserId)
                .Concat(refunds.Select(r => r.ApprovedByUserId))
                .Concat(refunds.Select(r => r.SecondApproverUserId)));

        var bookingIds = refunds.Where(r => r.BookingId.HasValue).Select(r => r.BookingId!.Value).Distinct().ToList();

        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant)
                .Where(b => bookingIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Reference);

        return refunds.Select(r => new RefundRequestDto
        {
            Id = r.Id,
            Reference = r.Reference,
            PartyId = r.PartyId,
            PartyName = parties.GetValueOrDefault(r.PartyId, "—"),
            BookingId = r.BookingId,
            BookingReference = r.BookingId is null ? null : bookings.GetValueOrDefault(r.BookingId.Value),
            CancellationId = r.CancellationId,
            TenancyId = r.TenancyId,
            TokenReservationId = r.TokenReservationId,
            Status = r.Status,
            RequestedAmount = r.RequestedAmount,
            ApprovedAmount = r.ApprovedAmount,
            PaidAmount = r.PaidAmount,
            Outstanding = RealEstateMapper.Money(r.ApprovedAmount - r.PaidAmount),
            CurrencyCode = r.CurrencyCode,
            RequestedOn = r.RequestedOn,
            RequestedByName = users.GetValueOrDefault(r.RequestedByUserId, "—"),
            ApprovedByName = r.ApprovedByUserId is null ? null : users.GetValueOrDefault(r.ApprovedByUserId.Value),
            ApprovedAt = r.ApprovedAt,
            SecondApproverName = r.SecondApproverUserId is null ? null : users.GetValueOrDefault(r.SecondApproverUserId.Value),
            PayeeName = r.PayeeName,
            BankName = r.BankName,

            // Never echo a full account number back to a screen. The last four identify it.
            AccountNumber = string.IsNullOrWhiteSpace(r.AccountNumber) || r.AccountNumber.Length <= 4
                ? r.AccountNumber
                : $"••••{r.AccountNumber[^4..]}",

            BankDetailsVerified = r.BankDetailsVerified,
            AwaitingResale = r.AwaitingResale,
            ResaleBookingId = r.ResaleBookingId,
            RejectionReason = r.RejectionReason,

            Schedule = r.Schedule.OrderBy(s => s.SequenceNumber).Select(s => new RefundScheduleDto
            {
                Id = s.Id,
                SequenceNumber = s.SequenceNumber,
                DueDate = s.DueDate,
                Amount = s.Amount,
                PaidAmount = s.PaidAmount,
                PaidOn = s.PaidOn,
                Instrument = s.Instrument,
                PaymentReference = s.PaymentReference,
                IsPaid = s.IsPaid,
                IsOverdue = !s.IsPaid && s.DueDate < today,
            }).ToList(),
        }).ToList();
    }

    // ═══ Resale ══════════════════════════════════════════════════════════════

    /// <summary>
    /// The customer selling their booked unit on before possession. Chained to a transfer rather
    /// than editing the original booking, so both halves keep their own history and the gain —
    /// which is taxable in several markets — is computed rather than guessed at.
    /// </summary>
    public async Task<ResaleRequestDto> CreateResaleAsync(ResaleRequestDto dto, Guid userId)
    {
        var booking = await RequireAsync<Booking>(dto.BookingId, "That booking does not exist.");

        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Transferred)
            throw new InvalidOperationException($"{booking.Reference} is {booking.Status.ToString().ToLowerInvariant()} and cannot be resold.");

        if (booking.Status is BookingStatus.Possessed or BookingStatus.Completed)
            throw new InvalidOperationException("This unit has been handed over. Sell it as a resale property rather than transferring the booking.");

        var settings = await SettingsAsync();

        var outstanding = booking.Outstanding;

        if (settings.BlockTransferOnDues && outstanding > 0m)
            throw new InvalidOperationException($"{outstanding:N0} is still owed on {booking.Reference}. Clear it or raise a dues override first.");

        var gain = dto.ResalePrice - booking.TotalConsideration;

        var resale = new ResaleRequest
        {
            Reference = await numbering.NextMasterCodeAsync(Db.ResaleRequests, "RSL"),
            BookingId = dto.BookingId,
            SellerPartyId = booking.PrimaryApplicantPartyId,
            BuyerPartyId = dto.BuyerPartyId,
            RequestedOn = dto.RequestedOn == default ? Today : dto.RequestedOn,
            OriginalPrice = booking.TotalConsideration,
            ResalePrice = dto.ResalePrice,
            GainAmount = RealEstateMapper.Money(gain),
            ResaleFee = dto.ResaleFee,
            WithholdingTax = dto.WithholdingTax,
            Status = TransferStatus.Requested,
            Note = dto.Note,
        }.StampNew(Tenant, userId);

        Db.ResaleRequests.Add(resale);
        await Db.SaveChangesAsync();

        // A resale is a transfer with a price on it, so it opens the transfer file rather than
        // running a parallel process nobody maintains.
        if (dto.BuyerPartyId is not null)
        {
            var transfer = await CreateTransferAsync(new TransferRequestCreateDto
            {
                BookingId = booking.Id,
                UnitId = booking.UnitId,
                PlotFileId = booking.PlotFileId,
                ProjectId = booking.ProjectId,
                Kind = TransferKind.Sale,
                RequestedOn = resale.RequestedOn,
                RequestedByPartyId = booking.PrimaryApplicantPartyId,
                SaleConsideration = dto.ResalePrice,
                Parties =
                [
                    new TransferPartyDto { PartyId = booking.PrimaryApplicantPartyId, Side = "Transferor", SharePercent = 100m },
                    new TransferPartyDto { PartyId = dto.BuyerPartyId.Value, Side = "Transferee", SharePercent = 100m },
                ],
                Note = dto.Note,
            }, userId);

            resale.TransferRequestId = transfer.Id;
            await Db.SaveChangesAsync();
        }

        var names = await PartyNamesAsync(new[] { resale.SellerPartyId }.Concat(dto.BuyerPartyId is null ? [] : [dto.BuyerPartyId.Value]));

        dto.Id = resale.Id;
        dto.Reference = resale.Reference;
        dto.BookingReference = booking.Reference;
        dto.SellerPartyId = resale.SellerPartyId;
        dto.SellerName = names.GetValueOrDefault(resale.SellerPartyId, "—");
        dto.BuyerName = dto.BuyerPartyId is null ? null : names.GetValueOrDefault(dto.BuyerPartyId.Value);
        dto.OriginalPrice = resale.OriginalPrice;
        dto.GainAmount = resale.GainAmount;
        dto.Status = resale.Status;
        dto.TransferRequestId = resale.TransferRequestId;

        return dto;
    }
}
