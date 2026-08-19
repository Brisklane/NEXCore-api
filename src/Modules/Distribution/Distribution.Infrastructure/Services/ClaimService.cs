using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Claims, supplier rebates and chargebacks.
///
/// A claim is a state machine with evidence, not an email thread. The design point is
/// <see cref="ChannelClaim.ComputedAmount"/>: for every claim the system independently works out
/// what it thinks is owed from its own records, and stores it beside what the partner asked for.
/// The variance between the two *is* the review conversation, pre-computed — which turns a
/// three-week argument into a five-minute decision.
/// </summary>
public class ClaimService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering) : IClaimService
{
    // ═══ Claims ══════════════════════════════════════════════════════════════

    public async Task<ClaimDto> SubmitAsync(SubmitClaimDto request, Guid userId)
    {
        if (request.Lines.Count == 0)
            throw new InvalidOperationException("A claim needs at least one line.");
        if (request.PartnerId is null && request.OutletId is null)
            throw new InvalidOperationException("A claim must name a partner or an outlet.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        // A window exists so claims arrive while the evidence still does. Without it, a claim for
        // a scheme that ran eighteen months ago lands on someone who cannot verify any of it.
        var window = settings?.ClaimSubmissionWindowDays ?? 45;
        if (window > 0 && request.PeriodEnd != default
            && (DateTime.UtcNow.Date - request.PeriodEnd.Date).TotalDays > window)
            throw new InvalidOperationException(
                $"The {window}-day claim window for the period ending {request.PeriodEnd:d} has closed.");

        var claim = new ChannelClaim
        {
            ClaimNumber = await numbering.NextClaimNumberAsync(DateTime.UtcNow),
            Kind = request.Kind,
            Status = request.SaveAsDraft ? ClaimStatus.Draft : ClaimStatus.Submitted,
            PartnerId = request.PartnerId,
            OutletId = request.OutletId,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            SubmittedOn = DateTime.UtcNow,
            SchemeId = request.SchemeId,
            ReturnId = request.ReturnId,
            MrpRevisionId = request.MrpRevisionId,
            CurrencyCode = request.CurrencyCode ?? settings?.BaseCurrencyCode ?? "USD",
            Note = request.Note,
        }.StampNew(tenant, userId);

        claim.TerritoryId = await ResolveTerritoryAsync(request.PartnerId, request.OutletId);

        var order = 0;
        foreach (var line in request.Lines)
        {
            claim.Lines.Add(new ChannelClaimLine
            {
                ClaimId = claim.Id,
                DisplayOrder = order++,
                ItemId = line.ItemId,
                ItemName = line.ItemName,
                BatchId = line.BatchId,
                BatchNumber = line.BatchNumber,
                SourceInvoiceId = line.SourceInvoiceId,
                SourceInvoiceNumber = line.SourceInvoiceNumber,
                SourceOrderId = line.SourceOrderId,
                SchemeApplicationId = line.SchemeApplicationId,
                Uom = string.IsNullOrWhiteSpace(line.Uom) ? "PCS" : line.Uom,
                Quantity = line.Quantity,
                UnitRate = line.UnitRate,
                ClaimedAmount = line.ClaimedAmount,
                Note = line.Note,
            }.StampNew(tenant, userId));
        }

        foreach (var document in request.Documents)
            claim.Documents.Add(new ClaimDocument
            {
                ClaimId = claim.Id,
                DocumentType = document.DocumentType,
                FileUrl = document.FileUrl,
                FileName = document.FileName,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = userId,
                Note = document.Note,
            }.StampNew(tenant, userId));

        claim.ClaimedAmount = claim.Lines.Sum(l => l.ClaimedAmount);

        // The independent second opinion, computed before anyone reviews it.
        await ComputeExpectedAsync(claim);

        if (!request.SaveAsDraft)
            claim.StatusEvents.Add(NewEvent(claim, ClaimStatus.Draft, ClaimStatus.Submitted, "Submitted", userId));

        db.Claims.Add(claim);
        await db.SaveChangesAsync();

        return (await GetAsync(claim.Id))!;
    }

    public async Task<ClaimDto?> GetAsync(Guid claimId)
    {
        var entity = await LoadAsync(claimId);
        if (entity is null) return null;

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var dto = entity.ToDto(settings?.ClaimSettlementSlaDays ?? 15);

        if (entity.OutletId.HasValue)
            dto.OutletName = await db.Outlets.ForTenant(tenant)
                .Where(o => o.Id == entity.OutletId).Select(o => o.Name).FirstOrDefaultAsync();

        if (entity.TerritoryId.HasValue)
            dto.TerritoryName = await db.Territories.ForTenant(tenant)
                .Where(t => t.Id == entity.TerritoryId).Select(t => t.Name).FirstOrDefaultAsync();

        if (entity.ReturnId.HasValue)
            dto.ReturnNumber = await db.Returns.ForTenant(tenant)
                .Where(r => r.Id == entity.ReturnId).Select(r => r.ReturnNumber).FirstOrDefaultAsync();

        var reasonIds = entity.Lines.Where(l => l.RejectionReasonCodeId.HasValue)
            .Select(l => l.RejectionReasonCodeId!.Value).Distinct().ToList();

        if (reasonIds.Count > 0)
        {
            var reasons = await db.ReasonCodes.ForCompany(tenant)
                .Where(r => reasonIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);

            foreach (var line in dto.Lines.Where(l => l.RejectionReasonCodeId.HasValue))
                line.RejectionReasonName = reasons.GetValueOrDefault(line.RejectionReasonCodeId!.Value);
        }

        return dto;
    }

    public async Task<PaginatedResponse<ClaimSummaryDto>> ListAsync(
        string? search, ClaimKind? kind, ClaimStatus? status, Guid? partnerId, Guid? schemeId,
        DateTime? from, DateTime? to, bool? breachingSlaOnly, PaginationParams pagination)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var slaDays = settings?.ClaimSettlementSlaDays ?? 15;

        var query = db.Claims.ForTenant(tenant)
            .Include(c => c.Partner).Include(c => c.Scheme)
            .Include(c => c.Lines.Where(l => !l.IsDeleted))
            .Include(c => c.Documents.Where(d => !d.IsDeleted))
            .WhereIf(kind.HasValue, c => c.Kind == kind)
            .WhereIf(status.HasValue, c => c.Status == status)
            .WhereIf(partnerId.HasValue, c => c.PartnerId == partnerId)
            .WhereIf(schemeId.HasValue, c => c.SchemeId == schemeId)
            .WhereIf(from.HasValue, c => c.SubmittedOn >= from)
            .WhereIf(to.HasValue, c => c.SubmittedOn <= to);

        if (breachingSlaOnly == true)
            query = query.Where(c => c.AgeingDays > slaDays
                                     && c.Status != ClaimStatus.Settled
                                     && c.Status != ClaimStatus.Rejected
                                     && c.Status != ClaimStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.ClaimNumber, term)
                                     || (c.Partner != null && EF.Functions.ILike(c.Partner.Name, term)));
        }

        var total = await query.CountAsync();
        var rows = await query
            // Oldest open first: a claims queue is a work list, and the SLA runs from submission.
            .OrderByDescending(c => c.AgeingDays).ThenByDescending(c => c.SubmittedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<ClaimSummaryDto>.Ok(
            rows.Select(r => r.ToSummary(slaDays)), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ClaimDto> StartReviewAsync(Guid claimId, Guid userId)
    {
        var claim = await LoadAsync(claimId)
            ?? throw new InvalidOperationException("That claim no longer exists.");

        if (claim.Status is not (ClaimStatus.Submitted or ClaimStatus.Resubmitted))
            throw new InvalidOperationException("This claim is not waiting for review.");

        var from = claim.Status;
        claim.Status = ClaimStatus.UnderReview;
        claim.ReviewStartedAt = DateTime.UtcNow;
        claim.ReviewerUserId = userId;
        claim.StatusEvents.Add(NewEvent(claim, from, ClaimStatus.UnderReview, "Review started", userId));
        claim.StampUpdated(userId);

        // Recompute on review: the underlying records may have changed since submission, and the
        // reviewer should be looking at today's truth.
        await ComputeExpectedAsync(claim);

        await db.SaveChangesAsync();
        return (await GetAsync(claimId))!;
    }

    public async Task<ClaimDto> QueryAsync(Guid claimId, QueryClaimDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.QueryNote))
            throw new InvalidOperationException("A query needs a note explaining what is missing.");

        var claim = await LoadAsync(claimId)
            ?? throw new InvalidOperationException("That claim no longer exists.");

        if (claim.Status is ClaimStatus.Settled or ClaimStatus.Rejected or ClaimStatus.Cancelled)
            throw new InvalidOperationException("This claim is already closed.");

        var from = claim.Status;
        claim.Status = ClaimStatus.QueryRaised;
        claim.QueryNote = request.QueryNote;
        claim.QueriedAt = DateTime.UtcNow;
        claim.StatusEvents.Add(NewEvent(claim, from, ClaimStatus.QueryRaised, request.QueryNote, userId));
        claim.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetAsync(claimId))!;
    }

    public async Task<ClaimDto> ResubmitAsync(Guid claimId, SubmitClaimDto request, Guid userId)
    {
        var claim = await LoadAsync(claimId)
            ?? throw new InvalidOperationException("That claim no longer exists.");

        if (claim.Status is not (ClaimStatus.QueryRaised or ClaimStatus.Draft))
            throw new InvalidOperationException("Only a queried or draft claim can be resubmitted.");

        foreach (var existing in claim.Lines.Where(l => !l.IsDeleted).ToList())
            if (request.Lines.All(l => l.Id != existing.Id))
                existing.StampDeleted(userId);

        var order = 0;
        foreach (var line in request.Lines)
        {
            var target = line.Id != Guid.Empty ? claim.Lines.FirstOrDefault(l => l.Id == line.Id) : null;
            if (target is null)
            {
                target = new ChannelClaimLine { ClaimId = claim.Id }.StampNew(tenant, userId);
                claim.Lines.Add(target);
            }

            target.DisplayOrder = order++;
            target.ItemId = line.ItemId;
            target.ItemName = line.ItemName;
            target.BatchId = line.BatchId;
            target.BatchNumber = line.BatchNumber;
            target.SourceInvoiceId = line.SourceInvoiceId;
            target.SourceInvoiceNumber = line.SourceInvoiceNumber;
            target.Uom = line.Uom;
            target.Quantity = line.Quantity;
            target.UnitRate = line.UnitRate;
            target.ClaimedAmount = line.ClaimedAmount;
            target.Note = line.Note;
        }

        foreach (var document in request.Documents.Where(d => d.Id == Guid.Empty))
            claim.Documents.Add(new ClaimDocument
            {
                ClaimId = claim.Id,
                DocumentType = document.DocumentType,
                FileUrl = document.FileUrl,
                FileName = document.FileName,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = userId,
                Note = document.Note,
            }.StampNew(tenant, userId));

        claim.ClaimedAmount = claim.Lines.Where(l => !l.IsDeleted).Sum(l => l.ClaimedAmount);
        await ComputeExpectedAsync(claim);

        var from = claim.Status;
        claim.Status = ClaimStatus.Resubmitted;
        claim.ResubmittedAt = DateTime.UtcNow;
        claim.QueryNote = null;
        claim.StatusEvents.Add(NewEvent(claim, from, ClaimStatus.Resubmitted, "Resubmitted with revisions", userId));
        claim.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetAsync(claimId))!;
    }

    public async Task<ClaimDto> DecideAsync(Guid claimId, DecideClaimDto request, Guid userId)
    {
        var claim = await LoadAsync(claimId)
            ?? throw new InvalidOperationException("That claim no longer exists.");

        if (claim.Status is ClaimStatus.Settled or ClaimStatus.Cancelled)
            throw new InvalidOperationException("This claim is already closed.");

        if (!request.IsApproved)
        {
            if (request.RejectionReasonCodeId is null && string.IsNullOrWhiteSpace(request.Note))
                throw new InvalidOperationException("Rejecting a claim needs a reason.");

            var fromStatus = claim.Status;
            claim.Status = ClaimStatus.Rejected;
            claim.RejectionReasonCodeId = request.RejectionReasonCodeId;
            claim.RejectionNote = request.Note;
            claim.DecidedAt = DateTime.UtcNow;
            claim.DecidedByUserId = userId;
            claim.ApprovedAmount = 0;
            claim.StatusEvents.Add(NewEvent(claim, fromStatus, ClaimStatus.Rejected, request.Note, userId));
            claim.StampUpdated(userId);

            await db.SaveChangesAsync();
            return (await GetAsync(claimId))!;
        }

        foreach (var line in claim.Lines.Where(l => !l.IsDeleted))
        {
            var decision = request.Lines.FirstOrDefault(l => l.LineId == line.Id);
            var approved = decision?.ApprovedAmount ?? line.ClaimedAmount;

            if (approved > line.ClaimedAmount)
                throw new InvalidOperationException(
                    $"Line {line.DisplayOrder + 1}: cannot approve more than the {line.ClaimedAmount:N2} claimed.");

            // A partial approval without a reason is a dispute waiting to happen.
            if (approved < line.ClaimedAmount
                && decision?.RejectionReasonCodeId is null
                && string.IsNullOrWhiteSpace(decision?.RejectionNote))
                throw new InvalidOperationException(
                    $"Line {line.DisplayOrder + 1} is approved below the claim. A reason is required.");

            line.ApprovedAmount = approved;
            line.RejectionReasonCodeId = decision?.RejectionReasonCodeId;
            line.RejectionNote = decision?.RejectionNote;
            line.StampUpdated(userId);
        }

        claim.ApprovedAmount = claim.Lines.Where(l => !l.IsDeleted).Sum(l => l.ApprovedAmount);
        claim.DecidedAt = DateTime.UtcNow;
        claim.DecidedByUserId = userId;

        var previous = claim.Status;
        claim.Status = claim.ApprovedAmount < claim.ClaimedAmount
            ? ClaimStatus.PartiallyApproved
            : ClaimStatus.Approved;

        claim.StatusEvents.Add(NewEvent(claim, previous, claim.Status,
            request.Note ?? $"Approved {claim.ApprovedAmount:N2} of {claim.ClaimedAmount:N2}", userId));
        claim.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetAsync(claimId))!;
    }

    public async Task<ClaimDto> SettleAsync(Guid claimId, SettleClaimDto request, Guid userId)
    {
        var claim = await LoadAsync(claimId)
            ?? throw new InvalidOperationException("That claim no longer exists.");

        if (claim.Status is not (ClaimStatus.Approved or ClaimStatus.PartiallyApproved))
            throw new InvalidOperationException("Only an approved claim can be settled.");

        var amount = request.SettledAmount > 0 ? request.SettledAmount : claim.ApprovedAmount;
        if (amount > claim.ApprovedAmount)
            throw new InvalidOperationException("Cannot settle more than the approved amount.");

        var from = claim.Status;
        claim.Status = ClaimStatus.Settled;
        claim.SettlementMode = request.SettlementMode;
        claim.SettledAmount = amount;
        claim.SettledAt = request.SettledAt ?? DateTime.UtcNow;
        claim.SettlementReference = request.SettlementReference;
        // The number distributors judge a principal by, computed rather than asserted.
        claim.SettlementDays = (int)(claim.SettledAt.Value.Date - claim.SubmittedOn.Date).TotalDays;
        claim.StatusEvents.Add(NewEvent(claim, from, ClaimStatus.Settled,
            request.Note ?? $"Settled by {request.SettlementMode}", userId));
        claim.StampUpdated(userId);

        // Deferred scheme applications behind this claim are now paid out.
        await db.SchemeApplications.ForTenant(tenant)
            .Where(a => a.ClaimId == claimId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsSettled, true)
                .SetProperty(a => a.SettledAt, DateTime.UtcNow));

        // Adjusting against outstanding reduces what the partner owes, so the ledger has to move.
        if (request.SettlementMode is ClaimSettlementMode.OffsetOutstanding
            or ClaimSettlementMode.AdjustAgainstNextInvoice && claim.PartnerId.HasValue)
        {
            var profile = await db.CreditProfiles.ForTenant(tenant)
                .FirstOrDefaultAsync(c => c.PartnerId == claim.PartnerId);

            if (profile is not null)
            {
                profile.OutstandingAmount = Math.Max(0, profile.OutstandingAmount - amount);
                profile.AvailableCredit = DistributionMapper.EffectiveLimit(profile)
                                          - profile.OutstandingAmount - profile.UnbilledOrderValue;
                profile.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return (await GetAsync(claimId))!;
    }

    public async Task<ClaimDto> CancelAsync(Guid claimId, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Cancelling a claim needs a reason.");

        var claim = await LoadAsync(claimId)
            ?? throw new InvalidOperationException("That claim no longer exists.");

        if (claim.Status == ClaimStatus.Settled)
            throw new InvalidOperationException("A settled claim cannot be cancelled.");

        var from = claim.Status;
        claim.Status = ClaimStatus.Cancelled;
        claim.RejectionNote = reason;
        claim.StatusEvents.Add(NewEvent(claim, from, ClaimStatus.Cancelled, reason, userId));
        claim.StampUpdated(userId);

        // Free the applications so they can be claimed again on a corrected submission.
        await db.SchemeApplications.ForTenant(tenant)
            .Where(a => a.ClaimId == claimId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ClaimId, (Guid?)null));

        await db.SaveChangesAsync();
        return (await GetAsync(claimId))!;
    }

    public async Task<ClaimDocumentDto> AddDocumentAsync(Guid claimId, ClaimDocumentDto request, Guid userId)
    {
        var claim = await db.Claims.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == claimId)
            ?? throw new InvalidOperationException("That claim no longer exists.");

        if (string.IsNullOrWhiteSpace(request.FileUrl))
            throw new InvalidOperationException("A document needs a file.");

        var entity = new ClaimDocument
        {
            ClaimId = claim.Id,
            DocumentType = request.DocumentType,
            FileUrl = request.FileUrl,
            FileName = request.FileName,
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = userId,
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.ClaimDocuments.Add(entity);
        await db.SaveChangesAsync();

        return entity.ToDto();
    }

    // ═══ Supplier rebates ════════════════════════════════════════════════════

    public async Task<PaginatedResponse<RebateAgreementDto>> ListRebatesAsync(
        string? search, Guid? supplierId, bool? activeOnly, PaginationParams pagination)
    {
        var today = DateTime.UtcNow.Date;

        var query = db.RebateAgreements.ForTenant(tenant)
            .Include(a => a.Accruals.Where(x => !x.IsDeleted))
            .WhereIf(supplierId.HasValue, a => a.SupplierId == supplierId)
            .WhereIf(activeOnly == true, a => a.IsActive && a.ValidFrom <= today && a.ValidTo >= today);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(a => EF.Functions.ILike(a.Name, term)
                                     || EF.Functions.ILike(a.AgreementNumber, term)
                                     || EF.Functions.ILike(a.SupplierName ?? "", term));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(a => a.ValidFrom)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<RebateAgreementDto>.Ok(
            rows.Select(MapRebate), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<RebateAgreementDto?> GetRebateAsync(Guid agreementId)
    {
        var entity = await db.RebateAgreements.ForTenant(tenant)
            .Include(a => a.Accruals.Where(x => !x.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == agreementId);

        return entity is null ? null : MapRebate(entity);
    }

    public async Task<RebateAgreementDto> SaveRebateAsync(Guid? agreementId, RebateAgreementDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A rebate agreement needs a name.");
        if (request.ValidTo < request.ValidFrom)
            throw new InvalidOperationException("The agreement's end date is before its start date.");

        SupplierRebateAgreement entity;
        if (agreementId.HasValue)
        {
            entity = await db.RebateAgreements.ForTenant(tenant)
                .Include(a => a.Accruals)
                .FirstOrDefaultAsync(a => a.Id == agreementId)
                ?? throw new InvalidOperationException("That agreement no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new SupplierRebateAgreement().StampNew(tenant, userId);
            entity.AgreementNumber = string.IsNullOrWhiteSpace(request.AgreementNumber)
                ? await numbering.NextMasterCodeAsync(db.RebateAgreements, "REB")
                : request.AgreementNumber;
            db.RebateAgreements.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.SupplierId = request.SupplierId;
        entity.SupplierName = request.SupplierName;
        entity.ValidFrom = request.ValidFrom;
        entity.ValidTo = request.ValidTo;
        entity.CurrencyCode = request.CurrencyCode;
        entity.RebateBasis = request.RebateBasis;
        entity.ThresholdQuantity = request.ThresholdQuantity;
        entity.ThresholdValue = request.ThresholdValue;
        entity.RebatePercent = request.RebatePercent;
        entity.RebatePerUnit = request.RebatePerUnit;
        entity.BaselineValue = request.BaselineValue;
        entity.BrandId = request.BrandId;
        entity.CategoryId = request.CategoryId;
        entity.ItemId = request.ItemId;
        entity.Terms = request.Terms;
        entity.FileUrl = request.FileUrl;
        entity.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return (await GetRebateAsync(entity.Id))!;
    }

    public async Task<List<RebateAccrualDto>> AccrueRebatesAsync(
        DateTime periodStart, DateTime periodEnd, Guid userId)
    {
        var agreements = await db.RebateAgreements.ForTenant(tenant)
            .Include(a => a.Accruals.Where(x => !x.IsDeleted))
            .Where(a => a.IsActive && a.ValidFrom <= periodEnd && a.ValidTo >= periodStart)
            .ToListAsync();

        var results = new List<RebateAccrual>();

        foreach (var agreement in agreements)
        {
            // Idempotent per period: running the job twice must not double the accrual.
            var existing = agreement.Accruals.FirstOrDefault(a => a.PeriodStart.Date == periodStart.Date);

            var purchases = await PurchaseVolumeAsync(agreement, periodStart, periodEnd);

            var qualifies = (agreement.ThresholdQuantity <= 0 || purchases.Quantity >= agreement.ThresholdQuantity)
                            && (agreement.ThresholdValue <= 0 || purchases.Value >= agreement.ThresholdValue);

            // Growth rebates pay on the increment over the baseline, not on the whole volume.
            var basis = agreement.RebateBasis.Equals("Growth", StringComparison.OrdinalIgnoreCase)
                ? Math.Max(0, purchases.Value - agreement.BaselineValue)
                : purchases.Value;

            var accrued = !qualifies ? 0
                : agreement.RebatePerUnit > 0
                    ? Math.Round(purchases.Quantity * agreement.RebatePerUnit, 4)
                    : Math.Round(basis * agreement.RebatePercent / 100m, 4);

            var accrual = existing;
            if (accrual is null)
            {
                accrual = new RebateAccrual
                {
                    AgreementId = agreement.Id,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                }.StampNew(tenant, userId);
                agreement.Accruals.Add(accrual);
            }
            else
            {
                agreement.AccruedAmount -= accrual.AccruedAmount;
                accrual.StampUpdated(userId);
            }

            accrual.QualifyingQuantity = purchases.Quantity;
            accrual.QualifyingValue = purchases.Value;
            accrual.AccruedAmount = accrued;
            accrual.IsPosted = accrued > 0;
            accrual.PostedAt = accrued > 0 ? DateTime.UtcNow : null;

            agreement.AccruedAmount += accrued;
            agreement.StampUpdated(userId);

            results.Add(accrual);
        }

        await db.SaveChangesAsync();

        return results.Select(a => new RebateAccrualDto
        {
            Id = a.Id,
            AgreementId = a.AgreementId,
            PeriodStart = a.PeriodStart,
            PeriodEnd = a.PeriodEnd,
            QualifyingQuantity = a.QualifyingQuantity,
            QualifyingValue = a.QualifyingValue,
            AccruedAmount = a.AccruedAmount,
            ReceivedAmount = a.ReceivedAmount,
            IsPosted = a.IsPosted,
            IsReconciled = a.IsReconciled,
        }).ToList();
    }

    public async Task<RebateAccrualDto> ReconcileAccrualAsync(
        Guid accrualId, decimal receivedAmount, string? reference, Guid userId)
    {
        var accrual = await db.RebateAccruals.ForTenant(tenant)
            .Include(a => a.Agreement)
            .FirstOrDefaultAsync(a => a.Id == accrualId)
            ?? throw new InvalidOperationException("That accrual no longer exists.");

        accrual.ReceivedAmount = receivedAmount;
        accrual.SupplierCreditReference = reference;
        accrual.IsReconciled = true;
        accrual.ReconciledAt = DateTime.UtcNow;
        accrual.StampUpdated(userId);

        if (accrual.Agreement is not null)
        {
            accrual.Agreement.ReceivedAmount += receivedAmount;
            accrual.Agreement.StampUpdated(userId);
        }

        await db.SaveChangesAsync();

        return new RebateAccrualDto
        {
            Id = accrual.Id,
            AgreementId = accrual.AgreementId,
            AgreementName = accrual.Agreement?.Name,
            PeriodStart = accrual.PeriodStart,
            PeriodEnd = accrual.PeriodEnd,
            QualifyingQuantity = accrual.QualifyingQuantity,
            QualifyingValue = accrual.QualifyingValue,
            AccruedAmount = accrual.AccruedAmount,
            ReceivedAmount = accrual.ReceivedAmount,
            IsPosted = accrual.IsPosted,
            IsReconciled = accrual.IsReconciled,
            SupplierCreditReference = accrual.SupplierCreditReference,
        };
    }

    // ═══ Chargebacks ═════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ChargebackDto>> ListChargebacksAsync(
        ClaimStatus? status, Guid? supplierId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Chargebacks.ForTenant(tenant)
            .WhereIf(status.HasValue, c => c.Status == status)
            .WhereIf(supplierId.HasValue, c => c.SupplierId == supplierId)
            .WhereIf(from.HasValue, c => c.SaleDate >= from)
            .WhereIf(to.HasValue, c => c.SaleDate <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(c => c.SaleDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        return PaginatedResponse<ChargebackDto>.Ok(
            rows.Select(MapChargeback), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ChargebackDto> SaveChargebackAsync(Guid? id, ChargebackDto request, Guid userId)
    {
        Chargeback entity;
        if (id.HasValue)
        {
            entity = await db.Chargebacks.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("That chargeback no longer exists.");

            if (entity.Status == ClaimStatus.Settled)
                throw new InvalidOperationException("A settled chargeback cannot be edited.");

            entity.StampUpdated(userId);
        }
        else
        {
            entity = new Chargeback().StampNew(tenant, userId);
            entity.ChargebackNumber = await numbering.NextChargebackNumberAsync(DateTime.UtcNow);
            db.Chargebacks.Add(entity);
        }

        entity.SupplierId = request.SupplierId;
        entity.SupplierName = request.SupplierName;
        entity.ContractCustomerId = request.ContractCustomerId;
        entity.ContractCustomerName = request.ContractCustomerName;
        entity.ContractReference = request.ContractReference;
        entity.ItemId = request.ItemId;
        entity.ItemName = request.ItemName;
        entity.SourceInvoiceId = request.SourceInvoiceId;
        entity.SourceInvoiceNumber = request.SourceInvoiceNumber;
        entity.SaleDate = request.SaleDate == default ? DateTime.UtcNow : request.SaleDate;
        entity.Uom = string.IsNullOrWhiteSpace(request.Uom) ? "PCS" : request.Uom;
        entity.Quantity = request.Quantity;
        entity.AcquisitionPrice = request.AcquisitionPrice;
        entity.ContractPrice = request.ContractPrice;
        entity.CurrencyCode = request.CurrencyCode;
        entity.Status = request.Status == default ? ClaimStatus.Draft : request.Status;
        entity.Note = request.Note;

        // The claim is arithmetic, not an opinion: what we paid less what we were told to charge.
        entity.ChargebackAmount = Math.Round(
            Math.Max(0, entity.AcquisitionPrice - entity.ContractPrice) * entity.Quantity, 4);

        if (entity.Status == ClaimStatus.Submitted) entity.SubmittedAt ??= DateTime.UtcNow;

        await db.SaveChangesAsync();
        return MapChargeback(entity);
    }

    public async Task<ChargebackDto> SettleChargebackAsync(
        Guid id, decimal settledAmount, string? reference, Guid userId)
    {
        var entity = await db.Chargebacks.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("That chargeback no longer exists.");

        if (settledAmount > entity.ChargebackAmount)
            throw new InvalidOperationException("Cannot settle more than the chargeback amount.");

        entity.Status = ClaimStatus.Settled;
        entity.ApprovedAmount = entity.ApprovedAmount > 0 ? entity.ApprovedAmount : settledAmount;
        entity.SettledAmount = settledAmount;
        entity.SettledAt = DateTime.UtcNow;
        entity.SettlementReference = reference;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return MapChargeback(entity);
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Works out what the system believes is owed, from its own records. This is the number that
    /// makes a claim review a comparison rather than an act of faith.
    /// </summary>
    private async Task ComputeExpectedAsync(ChannelClaim claim)
    {
        decimal computed = 0;

        switch (claim.Kind)
        {
            case ClaimKind.Scheme when claim.SchemeId.HasValue:
            {
                computed = await db.SchemeApplications.ForTenant(tenant)
                    .Where(a => a.SchemeId == claim.SchemeId
                                && !a.IsReversed
                                && (claim.PartnerId == null || a.PartnerId == claim.PartnerId)
                                && a.AppliedAt >= claim.PeriodStart
                                && a.AppliedAt <= claim.PeriodEnd)
                    .SumAsync(a => (decimal?)a.BenefitValue) ?? 0;
                break;
            }

            case ClaimKind.Damage or ClaimKind.Expiry or ClaimKind.MarketReturn when claim.ReturnId.HasValue:
            {
                computed = await db.Returns.ForTenant(tenant)
                    .Where(r => r.Id == claim.ReturnId)
                    .Select(r => r.CreditedValue > 0 ? r.CreditedValue : r.ApprovedValue)
                    .FirstOrDefaultAsync();
                break;
            }

            case ClaimKind.PriceProtection when claim.MrpRevisionId.HasValue:
            {
                // Compensation is per-unit protection times the channel stock that was already
                // out there when the price was cut.
                var revision = await db.MrpRevisions.ForTenant(tenant)
                    .FirstOrDefaultAsync(m => m.Id == claim.MrpRevisionId);

                if (revision is not null && claim.PartnerId.HasValue)
                {
                    var declared = await db.StockDeclarationLines.ForTenant(tenant)
                        .Where(l => l.ItemId == revision.ItemId
                                    && db.StockDeclarations.ForTenant(tenant).Any(d =>
                                        d.Id == l.DeclarationId && d.PartnerId == claim.PartnerId
                                        && d.AsOfDate <= revision.EffectiveFrom))
                        .OrderByDescending(l => l.CreatedAt)
                        .Select(l => (decimal?)l.BaseQuantity)
                        .FirstOrDefaultAsync() ?? 0;

                    computed = Math.Round(declared * revision.ProtectionPerUnit, 4);
                }
                break;
            }

            case ClaimKind.Freight or ClaimKind.Display or ClaimKind.Chargeback or ClaimKind.Manual:
            default:
                // Nothing to independently verify against; the claim stands on its documents.
                computed = claim.ClaimedAmount;
                break;
        }

        claim.ComputedAmount = computed;
        claim.VarianceAmount = claim.ClaimedAmount - computed;

        foreach (var line in claim.Lines.Where(l => !l.IsDeleted && l.ComputedAmount == 0))
            line.ComputedAmount = claim.ClaimedAmount == 0
                ? 0
                : Math.Round(computed * (line.ClaimedAmount / claim.ClaimedAmount), 4);
    }

    private async Task<(decimal Quantity, decimal Value)> PurchaseVolumeAsync(
        SupplierRebateAgreement agreement, DateTime from, DateTime to)
    {
        // Rebates are earned on what was sold out of the goods bought under the agreement, which
        // is the closest proxy this module has without reaching into Procurement's documents.
        var query = db.OrderLines.ForTenant(tenant)
            .Where(l => db.Orders.ForTenant(tenant).Any(o =>
                o.Id == l.OrderId && o.OrderDate >= from && o.OrderDate <= to
                && o.Status != DistributionOrderStatus.Cancelled
                && o.Status != DistributionOrderStatus.Rejected
                && o.Status != DistributionOrderStatus.Draft));

        if (agreement.ItemId.HasValue) query = query.Where(l => l.ItemId == agreement.ItemId);
        if (agreement.BrandId.HasValue) query = query.Where(l => l.BrandId == agreement.BrandId);
        if (agreement.CategoryId.HasValue) query = query.Where(l => l.CategoryId == agreement.CategoryId);

        var result = await query
            .GroupBy(_ => 1)
            .Select(g => new { Quantity = g.Sum(x => x.BaseQuantity), Value = g.Sum(x => x.UnitCost * x.BaseQuantity) })
            .FirstOrDefaultAsync();

        return (result?.Quantity ?? 0, result?.Value ?? 0);
    }

    private async Task<Guid?> ResolveTerritoryAsync(Guid? partnerId, Guid? outletId)
    {
        if (partnerId.HasValue)
        {
            var t = await db.Partners.ForTenant(tenant)
                .Where(p => p.Id == partnerId).Select(p => p.TerritoryId).FirstOrDefaultAsync();
            if (t.HasValue) return t;
        }

        if (outletId.HasValue)
            return await db.Outlets.ForTenant(tenant)
                .Where(o => o.Id == outletId).Select(o => o.TerritoryId).FirstOrDefaultAsync();

        return null;
    }

    private ClaimStatusEvent NewEvent(
        ChannelClaim claim, ClaimStatus from, ClaimStatus to, string? note, Guid userId)
        => new ClaimStatusEvent
        {
            ClaimId = claim.Id,
            FromStatus = from,
            ToStatus = to,
            OccurredAt = DateTime.UtcNow,
            ActorUserId = userId,
            Note = note,
        }.StampNew(tenant, userId);

    private async Task<ChannelClaim?> LoadAsync(Guid claimId)
        => await db.Claims.ForTenant(tenant)
            .Include(c => c.Partner).Include(c => c.Scheme)
            .Include(c => c.Lines.Where(l => !l.IsDeleted))
            .Include(c => c.Documents.Where(d => !d.IsDeleted))
            .Include(c => c.StatusEvents)
            .FirstOrDefaultAsync(c => c.Id == claimId);

    private static RebateAgreementDto MapRebate(SupplierRebateAgreement e) => new()
    {
        Id = e.Id,
        AgreementNumber = e.AgreementNumber,
        Name = e.Name,
        SupplierId = e.SupplierId,
        SupplierName = e.SupplierName,
        ValidFrom = e.ValidFrom,
        ValidTo = e.ValidTo,
        CurrencyCode = e.CurrencyCode,
        RebateBasis = e.RebateBasis,
        ThresholdQuantity = e.ThresholdQuantity,
        ThresholdValue = e.ThresholdValue,
        RebatePercent = e.RebatePercent,
        RebatePerUnit = e.RebatePerUnit,
        BaselineValue = e.BaselineValue,
        BrandId = e.BrandId,
        CategoryId = e.CategoryId,
        ItemId = e.ItemId,
        AccruedAmount = e.AccruedAmount,
        ReceivedAmount = e.ReceivedAmount,
        OutstandingAmount = e.AccruedAmount - e.ReceivedAmount,
        Terms = e.Terms,
        FileUrl = e.FileUrl,
        IsActive = e.IsActive,
        Accruals = e.Accruals.OrderByDescending(a => a.PeriodStart).Select(a => new RebateAccrualDto
        {
            Id = a.Id,
            AgreementId = a.AgreementId,
            PeriodStart = a.PeriodStart,
            PeriodEnd = a.PeriodEnd,
            QualifyingQuantity = a.QualifyingQuantity,
            QualifyingValue = a.QualifyingValue,
            AccruedAmount = a.AccruedAmount,
            ReceivedAmount = a.ReceivedAmount,
            IsPosted = a.IsPosted,
            IsReconciled = a.IsReconciled,
            SupplierCreditReference = a.SupplierCreditReference,
            Note = a.Note,
        }).ToList(),
    };

    private static ChargebackDto MapChargeback(Chargeback e) => new()
    {
        Id = e.Id,
        ChargebackNumber = e.ChargebackNumber,
        SupplierId = e.SupplierId,
        SupplierName = e.SupplierName,
        ContractCustomerId = e.ContractCustomerId,
        ContractCustomerName = e.ContractCustomerName,
        ContractReference = e.ContractReference,
        ItemId = e.ItemId,
        ItemName = e.ItemName,
        SourceInvoiceId = e.SourceInvoiceId,
        SourceInvoiceNumber = e.SourceInvoiceNumber,
        SaleDate = e.SaleDate,
        Uom = e.Uom,
        Quantity = e.Quantity,
        AcquisitionPrice = e.AcquisitionPrice,
        ContractPrice = e.ContractPrice,
        ChargebackAmount = e.ChargebackAmount,
        ApprovedAmount = e.ApprovedAmount,
        SettledAmount = e.SettledAmount,
        CurrencyCode = e.CurrencyCode,
        Status = e.Status,
        SubmittedAt = e.SubmittedAt,
        SettledAt = e.SettledAt,
        SettlementReference = e.SettlementReference,
        RejectionNote = e.RejectionNote,
        Note = e.Note,
    };
}
