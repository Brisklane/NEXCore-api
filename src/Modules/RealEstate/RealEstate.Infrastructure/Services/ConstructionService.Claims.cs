using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Subcontracts, subcontractor claims and contra-charges.
///
/// Certifying a claim is an act of judgement with money attached, so a disallowance always carries
/// a reason and the certified quantity can never exceed what has actually been measured. And a
/// contra-charge — material we supplied, plant we lent, work we had to redo — is only deductible
/// once it is evidenced and undisputed, because deducting a contested charge is how a
/// subcontractor stops working.
/// </summary>
public partial class ConstructionService
{
    // ═══ Subcontracts ════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<SubcontractListItemDto>> GetSubcontractsAsync(
        ListQueryDto query, Guid? constructionProjectId, SubcontractStatus? status)
    {
        var q = Db.Subcontracts.ForCompany(Tenant)
            .WhereIf(constructionProjectId.HasValue, s => s.ConstructionProjectId == constructionProjectId)
            .WhereIf(status.HasValue, s => s.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                s => s.Reference.Contains(query.Search!) || s.Name.Contains(query.Search!))
            .OrderBy(s => s.Reference);

        return await PageAsync(q, query, MapSubcontractListAsync);
    }

    private async Task<List<SubcontractListItemDto>> MapSubcontractListAsync(List<Subcontract> subcontracts)
    {
        if (subcontracts.Count == 0) return [];

        var today = Today;
        var ids = subcontracts.Select(s => s.Id).ToList();

        var projectIds = subcontracts.Select(s => s.ConstructionProjectId).Distinct().ToList();

        var projects = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var contractorIds = subcontracts.Select(s => s.ContractorId).Distinct().ToList();

        var contractors = await Db.Contractors.ForCompany(Tenant)
            .Where(c => contractorIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var claims = await Db.SubcontractorClaims.ForCompany(Tenant)
            .Where(c => ids.Contains(c.SubcontractId)
                     && c.Status != CertificateStatus.Approved
                     && c.Status != CertificateStatus.Rejected)
            .GroupBy(c => c.SubcontractId)
            .Select(g => new { SubcontractId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SubcontractId, x => x.Count);

        var variations = await Db.VariationOrders.ForCompany(Tenant)
            .Where(v => v.SubcontractId != null && ids.Contains(v.SubcontractId.Value)
                     && v.Status != VariationStatus.Approved && v.Status != VariationStatus.Rejected)
            .GroupBy(v => v.SubcontractId!.Value)
            .Select(g => new { SubcontractId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SubcontractId, x => x.Count);

        var contra = await Db.ContraCharges.ForCompany(Tenant)
            .Where(c => ids.Contains(c.SubcontractId) && !c.IsRecovered)
            .GroupBy(c => c.SubcontractId)
            .Select(g => new { SubcontractId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.SubcontractId, x => x.Amount);

        var advances = await Db.AdvancePayments.ForCompany(Tenant)
            .Where(a => a.SubcontractId != null && ids.Contains(a.SubcontractId.Value))
            .GroupBy(a => a.SubcontractId!.Value)
            .Select(g => new { SubcontractId = g.Key, Outstanding = g.Sum(x => x.OutstandingAmount) })
            .ToDictionaryAsync(x => x.SubcontractId, x => x.Outstanding);

        return subcontracts.Select(s =>
        {
            var finish = s.FinishDate.AddDays(s.ExtensionDaysGranted);

            // Delay is measured against the extended date, not the original — an approved
            // extension of time is a change to the contract, not a favour.
            var delay = s.ActualFinishDate is not null
                ? s.ActualFinishDate.Value.DayNumber - finish.DayNumber
                : s.Status is SubcontractStatus.Completed or SubcontractStatus.Terminated
                    ? (int?)null
                    : Math.Max(0, today.DayNumber - finish.DayNumber);

            return new SubcontractListItemDto
            {
                Id = s.Id,
                Reference = s.Reference,
                Name = s.Name,
                ConstructionProjectId = s.ConstructionProjectId,
                ProjectName = projects.GetValueOrDefault(s.ConstructionProjectId, "—"),
                ContractorId = s.ContractorId,
                ContractorName = contractors.GetValueOrDefault(s.ContractorId, "—"),
                Status = s.Status,
                Kind = s.Kind,
                ContractValue = s.ContractValue,
                ApprovedVariations = s.ApprovedVariations,
                RevisedValue = s.RevisedValue > 0m ? s.RevisedValue : s.ContractValue + s.ApprovedVariations,
                CurrencyCode = s.CurrencyCode,
                StartDate = s.StartDate,
                FinishDate = s.FinishDate,
                ActualFinishDate = s.ActualFinishDate,
                ExtensionDaysGranted = s.ExtensionDaysGranted,
                DelayDays = delay is > 0 ? delay : null,
                CertifiedToDate = s.CertifiedToDate,
                PaidToDate = s.PaidToDate,
                RetentionHeld = s.RetentionHeld,
                RetentionReleased = s.RetentionReleased,
                AdvanceOutstanding = advances.GetValueOrDefault(s.Id),
                ProgressPercent = s.ProgressPercent,
                InsuranceVerified = s.InsuranceVerified,
                LicenceVerified = s.LicenceVerified,
                DefectsPeriodEndsOn = s.DefectsPeriodEndsOn,
                OpenClaimCount = claims.GetValueOrDefault(s.Id),
                OpenVariationCount = variations.GetValueOrDefault(s.Id),
                UnrecoveredContraCharges = contra.GetValueOrDefault(s.Id),
            };
        }).ToList();
    }

    public async Task<SubcontractDetailDto?> GetSubcontractAsync(Guid id)
    {
        var subcontract = await Db.Subcontracts.ForCompany(Tenant)
            .Include(s => s.BoqLines)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subcontract is null) return null;

        var head = (await MapSubcontractListAsync([subcontract]))[0];
        var today = Today;

        var detail = new SubcontractDetailDto
        {
            Id = head.Id,
            Reference = head.Reference,
            Name = head.Name,
            ConstructionProjectId = head.ConstructionProjectId,
            ProjectName = head.ProjectName,
            ContractorId = head.ContractorId,
            ContractorName = head.ContractorName,
            Status = head.Status,
            Kind = head.Kind,
            ContractValue = head.ContractValue,
            ApprovedVariations = head.ApprovedVariations,
            RevisedValue = head.RevisedValue,
            CurrencyCode = head.CurrencyCode,
            StartDate = head.StartDate,
            FinishDate = head.FinishDate,
            ActualFinishDate = head.ActualFinishDate,
            ExtensionDaysGranted = head.ExtensionDaysGranted,
            DelayDays = head.DelayDays,
            CertifiedToDate = head.CertifiedToDate,
            PaidToDate = head.PaidToDate,
            RetentionHeld = head.RetentionHeld,
            RetentionReleased = head.RetentionReleased,
            AdvanceOutstanding = head.AdvanceOutstanding,
            ProgressPercent = head.ProgressPercent,
            InsuranceVerified = head.InsuranceVerified,
            LicenceVerified = head.LicenceVerified,
            DefectsPeriodEndsOn = head.DefectsPeriodEndsOn,
            OpenClaimCount = head.OpenClaimCount,
            OpenVariationCount = head.OpenVariationCount,
            UnrecoveredContraCharges = head.UnrecoveredContraCharges,

            WbsNodeId = subcontract.WbsNodeId,
            TenderId = subcontract.TenderId,
            AwardedOn = subcontract.AwardedOn,
            RetentionPercent = subcontract.RetentionPercent,
            RetentionCapPercent = subcontract.RetentionCapPercent,
            AdvancePercent = subcontract.AdvancePercent,
            AdvancePaid = subcontract.AdvancePaid,
            AdvanceRecovered = subcontract.AdvanceRecovered,
            PaymentTermDays = subcontract.PaymentTermDays,
            LiquidatedDamagesPerDay = subcontract.LiquidatedDamagesPerDay,
            LiquidatedDamagesCapPercent = subcontract.LiquidatedDamagesCapPercent,
            DefectsPeriodMonths = subcontract.DefectsPeriodMonths,
            BillOfQuantitiesId = subcontract.BillOfQuantitiesId,
            DocumentUrl = null,
            TerminationReason = subcontract.TerminationReason,
        };

        // Liquidated damages accrued to date, capped. This is the number that makes a final
        // account conversation short, and it is arithmetic rather than negotiation.
        if (head.DelayDays is > 0 && subcontract.LiquidatedDamagesPerDay > 0m)
        {
            var accrued = subcontract.LiquidatedDamagesPerDay * head.DelayDays.Value;

            var cap = subcontract.LiquidatedDamagesCapPercent > 0m
                ? subcontract.RevisedValue * subcontract.LiquidatedDamagesCapPercent / 100m
                : decimal.MaxValue;

            detail.LiquidatedDamagesAccrued = RealEstateMapper.Money(Math.Min(accrued, cap));
        }

        if (subcontract.WbsNodeId is not null)
        {
            detail.PackageName = await Db.WbsNodes.ForCompany(Tenant)
                .Where(n => n.Id == subcontract.WbsNodeId)
                .Select(n => n.Name)
                .FirstOrDefaultAsync();
        }

        var boqLineIds = subcontract.BoqLines.Select(l => l.BoqLineId).ToList();

        var boqLines = boqLineIds.Count == 0
            ? []
            : await Db.BoqLines.ForCompany(Tenant)
                .Where(l => boqLineIds.Contains(l.Id))
                .ToListAsync();

        detail.BoqLines = subcontract.BoqLines.Select(l =>
        {
            var boq = boqLines.FirstOrDefault(b => b.Id == l.BoqLineId);
            var saleRate = boq?.Rate ?? 0m;

            return new SubcontractBoqLineDto
            {
                Id = l.Id,
                BoqLineId = l.BoqLineId,
                ItemCode = boq?.ItemCode ?? "—",
                Description = boq?.Description ?? string.Empty,
                Uom = boq?.Uom ?? string.Empty,
                AwardedQuantity = l.AwardedQuantity,
                AwardedRate = l.AwardedRate,
                AwardedAmount = l.AwardedAmount,
                SaleRate = saleRate,

                // The margin between what we sell an item for and what we buy it in at. A negative
                // one means the package was bought above the contract rate and the job is
                // losing money on that line every time it is measured.
                MarginPercent = saleRate > 0m ? RealEstateMapper.Percent(saleRate - l.AwardedRate, saleRate) : 0m,

                ExecutedQuantity = l.ExecutedQuantity,
                CertifiedQuantity = l.CertifiedQuantity,
                ProgressPercent = l.AwardedQuantity > 0m ? RealEstateMapper.Percent(l.ExecutedQuantity, l.AwardedQuantity) : 0m,
            };
        }).ToList();

        detail.Claims = await MapClaimsAsync(
            await Db.SubcontractorClaims.ForCompany(Tenant)
                .Where(c => c.SubcontractId == id)
                .OrderByDescending(c => c.SequenceNumber)
                .ToListAsync(), false);

        detail.Variations = await MapVariationListAsync(
            await Db.VariationOrders.ForCompany(Tenant)
                .Where(v => v.SubcontractId == id)
                .OrderByDescending(v => v.RaisedOn)
                .ToListAsync());

        detail.ContraCharges = await MapContraChargesAsync(
            await Db.ContraCharges.ForCompany(Tenant)
                .Where(c => c.SubcontractId == id)
                .OrderByDescending(c => c.IncurredOn)
                .ToListAsync());

        detail.Retention = await MapRetentionAsync(
            await Db.RetentionLedgerEntries.ForCompany(Tenant)
                .Where(r => r.SubcontractId == id)
                .OrderByDescending(r => r.EntryDate)
                .ToListAsync());

        var compliance = await Db.ContractorCompliances.ForCompany(Tenant)
            .Where(c => c.ContractorId == subcontract.ContractorId)
            .OrderBy(c => c.ExpiresOn)
            .ToListAsync();

        detail.Compliance = compliance.Select(c => new ContractorComplianceDto
        {
            Id = c.Id,
            ComplianceType = c.ComplianceType,
            ReferenceNumber = c.ReferenceNumber,
            Provider = c.Provider,
            CoverAmount = c.CoverAmount,
            IssuedOn = c.IssuedOn,
            ExpiresOn = c.ExpiresOn,
            DaysToExpiry = c.ExpiresOn.DayNumber - today.DayNumber,
            IsExpired = c.ExpiresOn < today,
            DocumentUrl = c.DocumentUrl,
            IsMandatory = c.IsMandatory,
            BlocksAssignmentWhenExpired = c.BlocksAssignmentWhenExpired,
            IsVerified = c.IsVerified,
        }).ToList();

        return detail;
    }

    public async Task<SubcontractDetailDto> SaveSubcontractAsync(SubcontractCreateDto dto, Guid userId)
    {
        var project = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var subcontract = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.Subcontracts.ForCompany(Tenant).Include(s => s.BoqLines).FirstOrDefaultAsync(s => s.Id == dto.Id)
            : null;

        if (subcontract is null)
        {
            subcontract = new Subcontract
            {
                Reference = await numbering.NextSubcontractNumberAsync(DateTime.UtcNow),
                ConstructionProjectId = dto.ConstructionProjectId,
                ContractorId = dto.ContractorId,
                TenderId = dto.TenderId,
            }.StampNew(Tenant, userId);

            Db.Subcontracts.Add(subcontract);
        }
        else
        {
            // Once anything has been certified, the contract value is settled. Changing it
            // retrospectively changes what a contractor has already been paid against.
            if (subcontract.CertifiedToDate > 0m && subcontract.ContractValue != dto.ContractValue)
                throw new InvalidOperationException("Work has already been certified against this subcontract. Raise a variation rather than changing the value.");

            subcontract.StampUpdated(userId);
        }

        if (dto.FinishDate < dto.StartDate)
            throw new InvalidOperationException("The subcontract cannot finish before it starts.");

        subcontract.Name = dto.Name;
        subcontract.WbsNodeId = dto.WbsNodeId;
        subcontract.Kind = dto.Kind;
        subcontract.ContractValue = dto.ContractValue;
        subcontract.RevisedValue = RealEstateMapper.Money(dto.ContractValue + subcontract.ApprovedVariations);
        subcontract.CurrencyCode = dto.CurrencyCode ?? project.CurrencyCode;
        subcontract.StartDate = dto.StartDate;
        subcontract.FinishDate = dto.FinishDate;
        subcontract.RetentionPercent = dto.RetentionPercent;
        subcontract.RetentionCapPercent = dto.RetentionCapPercent;
        subcontract.AdvancePercent = dto.AdvancePercent;
        subcontract.PaymentTermDays = dto.PaymentTermDays;
        subcontract.LiquidatedDamagesPerDay = dto.LiquidatedDamagesPerDay;
        subcontract.LiquidatedDamagesCapPercent = dto.LiquidatedDamagesCapPercent;
        subcontract.DefectsPeriodMonths = dto.DefectsPeriodMonths;
        subcontract.BillOfQuantitiesId = dto.BillOfQuantitiesId;
        // The signed contract is filed as a generated document rather than a bare URL.
        subcontract.DefectsPeriodEndsOn = dto.FinishDate.AddMonths(dto.DefectsPeriodMonths);

        // Insurance and licence are checked against the contractor's live compliance rather than
        // being ticked here — a box somebody ticked two years ago proves nothing.
        var today = Today;

        var compliance = await Db.ContractorCompliances.ForCompany(Tenant)
            .Where(c => c.ContractorId == dto.ContractorId)
            .Select(c => new { c.ComplianceType, c.ExpiresOn, c.IsVerified })
            .ToListAsync();

        subcontract.InsuranceVerified = compliance.Any(c =>
            c.ComplianceType.Contains("Insurance", StringComparison.OrdinalIgnoreCase) && c.IsVerified && c.ExpiresOn >= today);

        subcontract.LicenceVerified = compliance.Any(c =>
            c.ComplianceType.Contains("Licence", StringComparison.OrdinalIgnoreCase) && c.IsVerified && c.ExpiresOn >= today);

        await Db.SaveChangesAsync();

        if (dto.BoqLines.Count > 0)
        {
            Db.SubcontractBoqLines.RemoveRange(subcontract.BoqLines);

            foreach (var l in dto.BoqLines)
            {
                subcontract.BoqLines.Add(new SubcontractBoqLine
                {
                    SubcontractId = subcontract.Id,
                    BoqLineId = l.BoqLineId,
                    AwardedQuantity = l.AwardedQuantity,
                    AwardedRate = l.AwardedRate,
                    AwardedAmount = RealEstateMapper.Money(l.AwardedQuantity * l.AwardedRate),
                }.StampNew(Tenant, userId));
            }

            await Db.SaveChangesAsync();
        }

        // The WBS node's committed cost is the sum of what has been awarded against it.
        if (subcontract.WbsNodeId is not null)
        {
            var node = await Db.WbsNodes.ForCompany(Tenant).FirstOrDefaultAsync(n => n.Id == subcontract.WbsNodeId);

            if (node is not null)
            {
                node.CommittedAmount = await Db.Subcontracts.ForCompany(Tenant)
                    .Where(s => s.WbsNodeId == node.Id && s.Status != SubcontractStatus.Terminated)
                    .SumAsync(s => s.RevisedValue);

                node.SubcontractId ??= subcontract.Id;
                node.StampUpdated(userId);

                await Db.SaveChangesAsync();
            }
        }

        return (await GetSubcontractAsync(subcontract.Id))!;
    }

    public async Task<SubcontractDetailDto> ChangeSubcontractStatusAsync(
        Guid id, SubcontractStatus status, string? reason, Guid userId)
    {
        var subcontract = await RequireAsync<Subcontract>(id, "That subcontract does not exist.");

        if (status == SubcontractStatus.Terminated && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Terminating a subcontract has to record the grounds.");

        if (status == SubcontractStatus.Active && !subcontract.InsuranceVerified)
            throw new InvalidOperationException("This contractor's insurance has not been verified. Work cannot start on site.");

        var today = Today;

        subcontract.Status = status;

        if (status is SubcontractStatus.Completed && subcontract.ActualFinishDate is null)
        {
            subcontract.ActualFinishDate = today;
            subcontract.DefectsPeriodEndsOn = today.AddMonths(subcontract.DefectsPeriodMonths);
        }

        if (status == SubcontractStatus.Terminated)
        {
            subcontract.TerminationReason = reason;
            subcontract.ActualFinishDate = today;

            await WriteAuditNoteAsync(
                "Subcontract", id, "SubcontractTerminated", Guid.Empty, userId,
                amountImpact: subcontract.RevisedValue - subcontract.CertifiedToDate,
                note: reason,
                entityReference: subcontract.Reference,
                highRisk: true);
        }

        subcontract.StampUpdated(userId);
        await Db.SaveChangesAsync();

        return (await GetSubcontractAsync(id))!;
    }

    // ═══ Claims ══════════════════════════════════════════════════════════════

    public async Task<SubcontractorClaimDto> SubmitClaimAsync(SubcontractorClaimDto dto, Guid userId)
    {
        var subcontract = await RequireAsync<Subcontract>(dto.SubcontractId, "That subcontract does not exist.");

        if (subcontract.Status is SubcontractStatus.Draft or SubcontractStatus.Terminated)
            throw new InvalidOperationException($"This subcontract is {subcontract.Status} and cannot be claimed against.");

        var previous = await Db.SubcontractorClaims.ForCompany(Tenant)
            .Where(c => c.SubcontractId == dto.SubcontractId && c.Status != CertificateStatus.Rejected)
            .ToListAsync();

        // Two claims for one period is either a mistake or an attempt at double payment.
        var overlapping = previous.FirstOrDefault(c => c.PeriodFrom <= dto.PeriodTo && c.PeriodTo >= dto.PeriodFrom);

        if (overlapping is not null)
        {
            throw new InvalidOperationException(
                $"Claim {overlapping.Reference} already covers {overlapping.PeriodFrom:dd MMM} to {overlapping.PeriodTo:dd MMM yyyy}.");
        }

        var claim = new SubcontractorClaim
        {
            Reference = await numbering.NextMasterCodeAsync(Db.SubcontractorClaims, "SCL"),
            SubcontractId = dto.SubcontractId,
            ContractorId = subcontract.ContractorId,
            SequenceNumber = previous.Count == 0 ? 1 : previous.Max(c => c.SequenceNumber) + 1,
            PeriodFrom = dto.PeriodFrom,
            PeriodTo = dto.PeriodTo,
            SubmittedOn = dto.SubmittedOn == default ? Today : dto.SubmittedOn,
            ClaimedGross = dto.ClaimedGross,
            PreviouslyCertified = RealEstateMapper.Money(previous.Sum(c => c.ThisPeriodCertified)),
            Status = CertificateStatus.SubmittedForCertification,
            ClaimDocumentUrl = dto.ClaimDocumentUrl,
            DueDate = (dto.SubmittedOn == default ? Today : dto.SubmittedOn).AddDays(subcontract.PaymentTermDays),
        }.StampNew(Tenant, userId);

        Db.SubcontractorClaims.Add(claim);
        await Db.SaveChangesAsync();

        return (await MapClaimsAsync([claim], true))[0];
    }

    /// <summary>
    /// Certifies a claim line by line. What is disallowed carries a reason, because the
    /// contractor will ask and "we cut it" is not an answer that survives adjudication.
    /// </summary>
    public async Task<SubcontractorClaimDto> CertifyClaimAsync(Guid id, List<ClaimCertificationDto> certifications, Guid userId)
    {
        var claim = await RequireAsync<SubcontractorClaim>(id, "That claim does not exist.");

        if (claim.Status is CertificateStatus.Approved or CertificateStatus.Paid)
            throw new InvalidOperationException("This claim has already been certified and approved.");

        var subcontract = await RequireAsync<Subcontract>(claim.SubcontractId, "The subcontract is missing.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var existing = await Db.ClaimCertifications.ForCompany(Tenant)
            .Where(c => c.SubcontractorClaimId == id)
            .ToListAsync();

        Db.ClaimCertifications.RemoveRange(existing);

        var certified = 0m;
        var disallowed = 0m;
        var reasons = new List<string>();

        foreach (var c in certifications)
        {
            if (c.CertifiedQuantity > c.ClaimedQuantity)
                throw new InvalidOperationException($"{c.Description}: certifying {c.CertifiedQuantity:N2} against a claim of {c.ClaimedQuantity:N2} is not possible.");

            var claimedValue = RealEstateMapper.Money(c.ClaimedQuantity * c.Rate);
            var certifiedValue = RealEstateMapper.Money(c.CertifiedQuantity * c.Rate);
            var cut = claimedValue - certifiedValue;

            if (cut > 0.01m && string.IsNullOrWhiteSpace(c.Reason))
                throw new InvalidOperationException($"{c.Description}: {cut:N0} was disallowed without a reason. Record why.");

            Db.ClaimCertifications.Add(new ClaimCertification
            {
                SubcontractorClaimId = id,
                BoqLineId = c.BoqLineId,
                Description = c.Description,
                ClaimedQuantity = c.ClaimedQuantity,
                CertifiedQuantity = c.CertifiedQuantity,
                Rate = c.Rate,
                ClaimedValue = claimedValue,
                CertifiedValue = certifiedValue,
                Reason = c.Reason,
                CertifiedByUserId = userId,
            }.StampNew(Tenant, userId));

            certified += certifiedValue;
            disallowed += cut;

            if (cut > 0.01m && !string.IsNullOrWhiteSpace(c.Reason)) reasons.Add($"{c.Description}: {c.Reason}");
        }

        claim.CertifiedGross = RealEstateMapper.Money(certified);
        claim.DisallowedAmount = RealEstateMapper.Money(disallowed);
        claim.ThisPeriodCertified = RealEstateMapper.Money(certified - claim.PreviouslyCertified);
        claim.DisallowanceReason = reasons.Count == 0 ? null : string.Join(" · ", reasons);

        // Deductions, in contract order.
        claim.RetentionDeducted = RealEstateMapper.Money(claim.ThisPeriodCertified * subcontract.RetentionPercent / 100m);

        var cap = subcontract.RetentionCapPercent > 0m
            ? RealEstateMapper.Money(subcontract.RevisedValue * subcontract.RetentionCapPercent / 100m)
            : decimal.MaxValue;

        if (subcontract.RetentionHeld + claim.RetentionDeducted > cap)
            claim.RetentionDeducted = RealEstateMapper.Money(Math.Max(0m, cap - subcontract.RetentionHeld));

        var advance = await Db.AdvancePayments.ForCompany(Tenant)
            .FirstOrDefaultAsync(a => a.SubcontractId == claim.SubcontractId && a.OutstandingAmount > 0m);

        if (advance is not null)
        {
            claim.AdvanceRecovered = RealEstateMapper.Money(
                Math.Min(advance.OutstandingAmount, claim.ThisPeriodCertified * advance.RecoveryPercent / 100m));
        }

        var contra = await Db.ContraCharges.ForCompany(Tenant)
            .Where(c => c.SubcontractId == claim.SubcontractId && !c.IsRecovered && c.IsAgreed && !c.IsDisputed)
            .ToListAsync();

        claim.ContraChargesDeducted = RealEstateMapper.Money(contra.Sum(c => c.Amount));

        claim.NetPayable = RealEstateMapper.Money(
            claim.ThisPeriodCertified
            - claim.RetentionDeducted
            - claim.AdvanceRecovered
            - claim.ContraChargesDeducted
            - claim.PenaltiesDeducted
            + claim.TaxAmount
            - claim.WithholdingTax);

        claim.Status = CertificateStatus.Certified;
        claim.StampUpdated(userId);

        foreach (var charge in contra)
        {
            charge.SubcontractorClaimId = id;
            charge.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await MapClaimsAsync([claim], true))[0];
    }

    public async Task<PaginatedResponse<SubcontractorClaimDto>> GetClaimsAsync(
        ListQueryDto query, Guid? subcontractId, CertificateStatus? status)
    {
        var q = Db.SubcontractorClaims.ForCompany(Tenant)
            .WhereIf(subcontractId.HasValue, c => c.SubcontractId == subcontractId)
            .WhereIf(status.HasValue, c => c.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), c => c.Reference.Contains(query.Search!))
            .OrderByDescending(c => c.SubmittedOn);

        return await PageAsync(q, query, list => MapClaimsAsync(list, false));
    }

    private async Task<List<SubcontractorClaimDto>> MapClaimsAsync(List<SubcontractorClaim> claims, bool includeCertifications)
    {
        if (claims.Count == 0) return [];

        var today = Today;
        var currency = await CurrencyAsync();
        var ids = claims.Select(c => c.Id).ToList();

        var subIds = claims.Select(c => c.SubcontractId).Distinct().ToList();

        var subcontracts = await Db.Subcontracts.ForCompany(Tenant)
            .Where(s => subIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Reference);

        var contractorIds = claims.Select(c => c.ContractorId).Distinct().ToList();

        var contractors = await Db.Contractors.ForCompany(Tenant)
            .Where(c => contractorIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var certifications = includeCertifications
            ? await Db.ClaimCertifications.ForCompany(Tenant)
                .Where(c => ids.Contains(c.SubcontractorClaimId))
                .ToListAsync()
            : [];

        var certifiers = await AgentUserNamesAsync(certifications.Select(c => c.CertifiedByUserId));

        return claims.Select(c => new SubcontractorClaimDto
        {
            Id = c.Id,
            Reference = c.Reference,
            SubcontractId = c.SubcontractId,
            SubcontractReference = subcontracts.GetValueOrDefault(c.SubcontractId, "—"),
            ContractorId = c.ContractorId,
            ContractorName = contractors.GetValueOrDefault(c.ContractorId, "—"),
            SequenceNumber = c.SequenceNumber,
            PeriodFrom = c.PeriodFrom,
            PeriodTo = c.PeriodTo,
            SubmittedOn = c.SubmittedOn,
            ClaimedGross = c.ClaimedGross,
            CertifiedGross = c.CertifiedGross,
            DisallowedAmount = c.DisallowedAmount,

            // What proportion of the claim was cut. A contractor consistently claiming 20% more
            // than they get is a pattern worth raising before the next tender.
            DisallowedPercent = RealEstateMapper.Percent(c.DisallowedAmount, c.ClaimedGross),

            PreviouslyCertified = c.PreviouslyCertified,
            ThisPeriodCertified = c.ThisPeriodCertified,
            RetentionDeducted = c.RetentionDeducted,
            AdvanceRecovered = c.AdvanceRecovered,
            ContraChargesDeducted = c.ContraChargesDeducted,
            PenaltiesDeducted = c.PenaltiesDeducted,
            TaxAmount = c.TaxAmount,
            WithholdingTax = c.WithholdingTax,
            NetPayable = c.NetPayable,
            PaidAmount = c.PaidAmount,
            CurrencyCode = currency,
            Status = c.Status,
            InterimPaymentCertificateId = c.InterimPaymentCertificateId,
            DueDate = c.DueDate,
            PaidOn = c.PaidOn,
            IsOverdue = c.PaidAmount < c.NetPayable && c.DueDate is not null && c.DueDate < today,
            ClaimDocumentUrl = c.ClaimDocumentUrl,
            DisallowanceReason = c.DisallowanceReason,
            IsDisputed = c.IsDisputed,

            Certifications = certifications.Where(x => x.SubcontractorClaimId == c.Id).Select(x => new ClaimCertificationDto
            {
                Id = x.Id,
                BoqLineId = x.BoqLineId,
                Description = x.Description ?? string.Empty,
                ClaimedQuantity = x.ClaimedQuantity,
                CertifiedQuantity = x.CertifiedQuantity,
                Rate = x.Rate,
                ClaimedValue = x.ClaimedValue,
                CertifiedValue = x.CertifiedValue,
                DisallowedValue = RealEstateMapper.Money(x.ClaimedValue - x.CertifiedValue),
                Reason = x.Reason,
                CertifiedByName = x.CertifiedByUserId is null ? null : certifiers.GetValueOrDefault(x.CertifiedByUserId.Value),
            }).ToList(),
        }).ToList();
    }

    /// <summary>
    /// Records something the main contractor supplied or had to put right, to be recovered from
    /// the subcontractor's next payment. Every one carries its evidence, because a contra-charge
    /// without proof is the fastest route to a stopped job.
    /// </summary>
    public async Task<ContraChargeDto> SaveContraChargeAsync(ContraChargeDto dto, Guid userId)
    {
        var subcontract = await RequireAsync<Subcontract>(dto.SubcontractId, "That subcontract does not exist.");

        var charge = dto.Id != Guid.Empty
            ? await Db.ContraCharges.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
            : null;

        if (charge is null)
        {
            charge = new ContraCharge
            {
                Reference = await numbering.NextMasterCodeAsync(Db.ContraCharges, "CTR"),
                SubcontractId = dto.SubcontractId,
                ContractorId = subcontract.ContractorId,
            }.StampNew(Tenant, userId);

            Db.ContraCharges.Add(charge);
        }
        else
        {
            if (charge.IsRecovered)
                throw new InvalidOperationException("This contra-charge has already been recovered and cannot be edited.");

            charge.StampUpdated(userId);
        }

        if (string.IsNullOrWhiteSpace(dto.EvidenceUrl) && dto.MaterialIssueId is null
            && dto.PlantAllocationId is null && dto.WorkOrderId is null)
        {
            throw new InvalidOperationException(
                "A contra-charge needs evidence — a material issue, a plant allocation, a work order, or a document.");
        }

        charge.Kind = dto.Kind;
        charge.Description = dto.Description;
        charge.IncurredOn = dto.IncurredOn == default ? Today : dto.IncurredOn;
        charge.Quantity = dto.Quantity <= 0m ? 1m : dto.Quantity;
        charge.Uom = dto.Uom;
        charge.Rate = dto.Rate;
        charge.Amount = RealEstateMapper.Money(dto.Amount > 0m ? dto.Amount : charge.Quantity * dto.Rate);
        charge.MaterialIssueId = dto.MaterialIssueId;
        charge.PlantAllocationId = dto.PlantAllocationId;
        charge.WorkOrderId = dto.WorkOrderId;
        charge.IsAgreed = dto.IsAgreed;
        charge.IsDisputed = dto.IsDisputed;
        charge.DisputeNote = dto.DisputeNote;
        charge.EvidenceUrl = dto.EvidenceUrl;

        if (dto.IsDisputed && string.IsNullOrWhiteSpace(dto.DisputeNote))
            throw new InvalidOperationException("A disputed contra-charge has to record what is being disputed.");

        await Db.SaveChangesAsync();
        return (await MapContraChargesAsync([charge]))[0];
    }

    private async Task<List<ContraChargeDto>> MapContraChargesAsync(List<ContraCharge> charges)
    {
        if (charges.Count == 0) return [];

        var currency = await CurrencyAsync();

        var subIds = charges.Select(c => c.SubcontractId).Distinct().ToList();

        var subcontracts = await Db.Subcontracts.ForCompany(Tenant)
            .Where(s => subIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Reference);

        var contractorIds = charges.Select(c => c.ContractorId).Distinct().ToList();

        var contractors = await Db.Contractors.ForCompany(Tenant)
            .Where(c => contractorIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        return charges.Select(c => new ContraChargeDto
        {
            Id = c.Id,
            Reference = c.Reference,
            SubcontractId = c.SubcontractId,
            SubcontractReference = subcontracts.GetValueOrDefault(c.SubcontractId),
            ContractorId = c.ContractorId,
            ContractorName = contractors.GetValueOrDefault(c.ContractorId, "—"),
            Kind = c.Kind,
            Description = c.Description ?? string.Empty,
            IncurredOn = c.IncurredOn,
            Quantity = c.Quantity,
            Uom = c.Uom,
            Rate = c.Rate,
            Amount = c.Amount,
            CurrencyCode = currency,
            MaterialIssueId = c.MaterialIssueId,
            PlantAllocationId = c.PlantAllocationId,
            WorkOrderId = c.WorkOrderId,
            IsAgreed = c.IsAgreed,
            IsDisputed = c.IsDisputed,
            DisputeNote = c.DisputeNote,
            IsRecovered = c.IsRecovered,
            EvidenceUrl = c.EvidenceUrl,
        }).ToList();
    }
}
