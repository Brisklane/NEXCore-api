using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Retention, advances, cost-to-complete, tendering and subcontracts.
///
/// Tendering is where a main contractor either makes its margin or gives it away. The comparison
/// is rate by rate rather than headline only — a bid that is cheapest overall while being wildly
/// under on one item is a claim waiting to be made — and awarding to anyone other than the lowest
/// bidder demands a written justification, because that is the decision an auditor asks about.
/// </summary>
public partial class ConstructionService
{
    // ═══ Retention ═══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<RetentionLedgerEntryDto>> GetRetentionAsync(
        ListQueryDto query, Guid? subcontractId, bool dueForReleaseOnly)
    {
        var today = Today;

        var q = Db.RetentionLedgerEntries.ForCompany(Tenant)
            .WhereIf(subcontractId.HasValue, r => r.SubcontractId == subcontractId)
            .WhereIf(dueForReleaseOnly, r => r.Movement == RetentionMovement.Held && r.DueForReleaseOn != null && r.DueForReleaseOn <= today)
            .OrderByDescending(r => r.EntryDate);

        return await PageAsync(q, query, MapRetentionAsync);
    }

    private async Task<List<RetentionLedgerEntryDto>> MapRetentionAsync(List<RetentionLedgerEntry> entries)
    {
        if (entries.Count == 0) return [];

        var today = Today;
        var currency = await CurrencyAsync();

        var projectIds = entries.Select(e => e.ConstructionProjectId).Distinct().ToList();

        var projects = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var subIds = entries.Where(e => e.SubcontractId.HasValue).Select(e => e.SubcontractId!.Value).Distinct().ToList();

        var subcontractors = subIds.Count == 0
            ? []
            : await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => subIds.Contains(s.Id))
                .Join(Db.Contractors.ForCompany(Tenant), s => s.ContractorId, c => c.Id, (s, c) => new { s.Id, c.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        var certIds = entries.Where(e => e.InterimPaymentCertificateId.HasValue)
            .Select(e => e.InterimPaymentCertificateId!.Value).Distinct().ToList();

        var certificates = certIds.Count == 0
            ? []
            : await Db.InterimPaymentCertificates.ForCompany(Tenant)
                .Where(c => certIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.CertificateNumber);

        var punchIds = entries.Where(e => e.PunchListId.HasValue).Select(e => e.PunchListId!.Value).Distinct().ToList();

        var punchLists = punchIds.Count == 0
            ? []
            : await Db.PunchLists.ForCompany(Tenant)
                .Where(p => punchIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.IsClosed);

        return entries.Select(e => new RetentionLedgerEntryDto
        {
            Id = e.Id,
            ConstructionProjectId = e.ConstructionProjectId,
            ProjectName = projects.GetValueOrDefault(e.ConstructionProjectId),
            SubcontractId = e.SubcontractId,
            SubcontractorName = e.SubcontractId is null ? null : subcontractors.GetValueOrDefault(e.SubcontractId.Value),
            ClientBuildContractId = e.ClientBuildContractId,
            Direction = e.Direction,
            Movement = e.Movement,
            EntryDate = e.EntryDate,
            InterimPaymentCertificateId = e.InterimPaymentCertificateId,
            CertificateNumber = e.InterimPaymentCertificateId is null
                ? null : certificates.GetValueOrDefault(e.InterimPaymentCertificateId.Value),
            Amount = e.Amount,
            RunningBalance = e.RunningBalance,
            CurrencyCode = currency,
            DueForReleaseOn = e.DueForReleaseOn,

            // Due does not mean releasable. An open punch list is exactly what retention is for.
            IsDueForRelease = e.Movement == RetentionMovement.Held
                              && e.DueForReleaseOn is not null && e.DueForReleaseOn <= today
                              && (e.PunchListId is null || punchLists.GetValueOrDefault(e.PunchListId.Value)),

            PunchListId = e.PunchListId,
            PunchListClosed = e.PunchListId is not null && punchLists.GetValueOrDefault(e.PunchListId.Value),
            BankGuaranteeId = e.BankGuaranteeId,
            Note = e.Note,
        }).ToList();
    }

    /// <summary>
    /// Releases retention. It is refused while a punch list is open, because retention exists to
    /// make outstanding work get done — releasing it early loses that leverage permanently.
    /// </summary>
    public async Task<RetentionLedgerEntryDto> ReleaseRetentionAsync(
        Guid? subcontractId, Guid? clientBuildContractId, decimal amount, RetentionMovement movement, Guid userId)
    {
        if (subcontractId is null && clientBuildContractId is null)
            throw new InvalidOperationException("Name a subcontract or a client contract to release retention against.");

        if (amount <= 0m)
            throw new InvalidOperationException("A release has to be for more than zero.");

        var held = await Db.RetentionLedgerEntries.ForCompany(Tenant)
            .Where(r => r.SubcontractId == subcontractId && r.ClientBuildContractId == clientBuildContractId)
            .OrderByDescending(r => r.EntryDate)
            .ToListAsync();

        var balance = held.Count == 0 ? 0m : held[0].RunningBalance;

        if (amount > balance)
            throw new InvalidOperationException($"Only {balance:N0} of retention is held. {amount:N0} cannot be released.");

        var subcontract = subcontractId is null
            ? null
            : await RequireAsync<Subcontract>(subcontractId.Value, "That subcontract does not exist.");

        if (subcontract is not null && movement != RetentionMovement.ForfeitedForDefects)
        {
            var openPunchLists = await Db.PunchLists.ForCompany(Tenant)
                .Where(p => p.SubcontractId == subcontractId && !p.IsClosed)
                .Select(p => new { p.Reference, Open = p.ItemCount - p.ClosedCount })
                .ToListAsync();

            if (openPunchLists.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Retention cannot be released while punch lists are open: " +
                    string.Join(", ", openPunchLists.Select(p => $"{p.Reference} ({p.Open} items)")) + ".");
            }
        }

        var entry = new RetentionLedgerEntry
        {
            ConstructionProjectId = subcontract?.ConstructionProjectId ?? held.FirstOrDefault()?.ConstructionProjectId ?? Guid.Empty,
            SubcontractId = subcontractId,
            ClientBuildContractId = clientBuildContractId,
            Direction = held.FirstOrDefault()?.Direction ?? "Payable",
            Movement = movement,
            EntryDate = Today,
            Amount = -amount,
            RunningBalance = RealEstateMapper.Money(balance - amount),
        }.StampNew(Tenant, userId);

        Db.RetentionLedgerEntries.Add(entry);

        if (subcontract is not null)
        {
            subcontract.RetentionHeld = RealEstateMapper.Money(subcontract.RetentionHeld - amount);
            subcontract.RetentionReleased = RealEstateMapper.Money(subcontract.RetentionReleased + amount);
            subcontract.StampUpdated(userId);
        }

        await WriteAuditNoteAsync(
            "RetentionLedgerEntry", entry.Id, $"Retention{movement}", Guid.Empty, userId,
            amountImpact: amount,
            note: $"{amount:N0} released, leaving {entry.RunningBalance:N0} held.",
            highRisk: movement == RetentionMovement.ForfeitedForDefects);

        await Db.SaveChangesAsync();
        return (await MapRetentionAsync([entry]))[0];
    }

    public async Task<AdvancePaymentDto> CreateAdvanceAsync(AdvancePaymentDto dto, Guid userId)
    {
        var project = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var subcontract = dto.SubcontractId is null
            ? null
            : await RequireAsync<Subcontract>(dto.SubcontractId.Value, "That subcontract does not exist.");

        var contractValue = subcontract?.RevisedValue ?? project.RevisedContractValue;

        var amount = dto.Amount > 0m
            ? dto.Amount
            : RealEstateMapper.Money(contractValue * dto.PercentOfContract / 100m);

        // An advance above a fifth of the contract with no guarantee behind it is money that
        // walks off site. It is not refused, but it is flagged as the risk it is.
        var guaranteeMissing = dto.BankGuaranteeId is null && amount > contractValue * 0.2m;

        var advance = new AdvancePayment
        {
            Reference = await numbering.NextMasterCodeAsync(Db.AdvancePayments, "ADV"),
            ConstructionProjectId = dto.ConstructionProjectId,
            SubcontractId = dto.SubcontractId,
            ClientBuildContractId = dto.ClientBuildContractId,
            PartyId = dto.SubcontractId is null ? null : subcontract?.PartyId,
            Direction = dto.Direction,
            Amount = amount,
            PercentOfContract = contractValue > 0m ? RealEstateMapper.Percent(amount, contractValue) : dto.PercentOfContract,
            PaidOn = dto.PaidOn == default ? Today : dto.PaidOn,
            RecoveryPercent = dto.RecoveryPercent <= 0m ? 20m : dto.RecoveryPercent,
            RecoveryStartsAtProgressPercent = dto.RecoveryStartsAtProgressPercent,
            OutstandingAmount = amount,
            BankGuaranteeId = dto.BankGuaranteeId,
        }.StampNew(Tenant, userId);

        Db.AdvancePayments.Add(advance);

        if (subcontract is not null)
        {
            subcontract.AdvancePaid = RealEstateMapper.Money(subcontract.AdvancePaid + amount);
            subcontract.StampUpdated(userId);
        }

        if (guaranteeMissing)
        {
            await WriteAuditNoteAsync(
                "AdvancePayment", advance.Id, "AdvancePaidWithoutGuarantee", Guid.Empty, userId,
                amountImpact: amount,
                note: $"{advance.PercentOfContract:N1}% of the contract advanced with no bank guarantee recorded.",
                entityReference: advance.Reference,
                highRisk: true);
        }

        await Db.SaveChangesAsync();

        dto.Id = advance.Id;
        dto.Reference = advance.Reference;
        dto.Amount = advance.Amount;
        dto.PercentOfContract = advance.PercentOfContract;
        dto.OutstandingAmount = advance.OutstandingAmount;
        dto.CurrencyCode = project.CurrencyCode;
        return dto;
    }

    public async Task<MaterialsOnSiteDto> SaveMaterialsOnSiteAsync(MaterialsOnSiteDto dto, Guid userId)
    {
        _ = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var materials = dto.Id != Guid.Empty
            ? await Db.MaterialsOnSite.ForCompany(Tenant).FirstOrDefaultAsync(m => m.Id == dto.Id)
            : null;

        if (materials is null)
        {
            materials = new MaterialsOnSite
            {
                ConstructionProjectId = dto.ConstructionProjectId,
                SubcontractId = dto.SubcontractId,
            }.StampNew(Tenant, userId);

            Db.MaterialsOnSite.Add(materials);
        }
        else materials.StampUpdated(userId);

        // Materials on site are only certifiable if they are insured and secured — otherwise the
        // employer is paying for stock that can be stolen or rained on overnight.
        if (!dto.IsInsured || !dto.IsSecured)
        {
            throw new InvalidOperationException(
                "Materials on site can only be certified once they are both insured and properly secured.");
        }

        materials.ItemId = dto.ItemId;
        materials.Description = dto.Description;
        materials.Uom = dto.Uom;
        materials.Quantity = dto.Quantity;
        materials.Rate = dto.Rate;
        materials.Value = RealEstateMapper.Money(dto.Quantity * dto.Rate);
        materials.AllowedPercent = dto.AllowedPercent <= 0m ? 90m : dto.AllowedPercent;
        materials.DeliveredOn = dto.DeliveredOn == default ? Today : dto.DeliveredOn;
        materials.IsInsured = dto.IsInsured;
        materials.IsSecured = dto.IsSecured;
        materials.ConsumedQuantity = dto.ConsumedQuantity;
        materials.PhotoUrl = dto.PhotoUrl;

        // Never the full value: the standard allowance leaves a margin for damage and shrinkage.
        materials.AllowedValue = RealEstateMapper.Money(materials.Value * materials.AllowedPercent / 100m);

        // As material is built in, its on-site value reverses out — otherwise the employer pays
        // for the same concrete twice, once as stock and once as measured work.
        if (materials.Quantity > 0m && materials.ConsumedQuantity > 0m)
        {
            materials.ReversedValue = RealEstateMapper.Money(
                materials.AllowedValue * Math.Min(1m, materials.ConsumedQuantity / materials.Quantity));

            materials.IsFullyReversed = materials.ConsumedQuantity >= materials.Quantity;
        }

        await Db.SaveChangesAsync();

        dto.Id = materials.Id;
        dto.Value = materials.Value;
        dto.AllowedValue = materials.AllowedValue;
        dto.ReversedValue = materials.ReversedValue;
        dto.IsFullyReversed = materials.IsFullyReversed;
        return dto;
    }

    /// <summary>
    /// Cost to complete, and the two indices that come out of it. The cost-performance index is
    /// the honest early-warning signal on a job: below 1.0 at 30% complete almost always means
    /// below 1.0 at the end, and it is visible months before the final account says so.
    /// </summary>
    public async Task<CostToCompleteDto> RecalculateCostToCompleteAsync(
        Guid constructionProjectId, DateOnly asOf, Guid userId)
    {
        var project = await RequireAsync<ConstructionProject>(constructionProjectId, "That construction project does not exist.");
        var today = asOf == default ? Today : asOf;

        var nodes = await Db.WbsNodes.ForCompany(Tenant)
            .Where(n => n.ConstructionProjectId == constructionProjectId)
            .ToListAsync();

        var budget = nodes.Sum(n => n.BudgetAmount);
        var incurred = nodes.Sum(n => n.ActualAmount);
        var committed = nodes.Sum(n => n.CommittedAmount);
        var earned = nodes.Sum(n => n.EarnedValue);

        if (budget <= 0m) budget = project.BudgetCost;
        if (incurred <= 0m) incurred = project.ActualCost;

        var percentComplete = project.PhysicalProgressPercent;

        // Remaining cost is estimated from performance so far rather than from the untouched
        // budget. A job running 15% over does not suddenly become efficient for the rest of it.
        var cpi = incurred > 0m ? RealEstateMapper.Money(earned / incurred, 3) : 1m;

        var remaining = percentComplete > 0m && cpi > 0m
            ? RealEstateMapper.Money(Math.Max(0m, (budget - earned) / cpi))
            : RealEstateMapper.Money(Math.Max(0m, budget - incurred));

        var forecast = RealEstateMapper.Money(incurred + Math.Max(remaining, committed - incurred));

        // Schedule performance compares earned value against what should have been earned by now.
        var elapsed = project.StartDate is null || project.PlannedCompletionDate is null
            ? 0m
            : RealEstateMapper.Percent(
                Math.Max(0, today.DayNumber - project.StartDate.Value.DayNumber),
                Math.Max(1, project.PlannedCompletionDate.Value.DayNumber - project.StartDate.Value.DayNumber));

        var planned = RealEstateMapper.Money(budget * elapsed / 100m);
        var spi = planned > 0m ? RealEstateMapper.Money(earned / planned, 3) : 1m;

        var record = new CostToComplete
        {
            ConstructionProjectId = constructionProjectId,
            AsOfDate = today,
            BudgetCost = RealEstateMapper.Money(budget),
            CostIncurred = RealEstateMapper.Money(incurred),
            CommittedNotIncurred = RealEstateMapper.Money(Math.Max(0m, committed - incurred)),
            EstimatedRemaining = remaining,
            ForecastFinalCost = forecast,
            VarianceToBudget = RealEstateMapper.Money(forecast - budget),
            PercentComplete = percentComplete,
            EarnedValue = RealEstateMapper.Money(earned),
            CostPerformanceIndex = cpi,
            SchedulePerformanceIndex = spi,
            PreparedByUserId = userId,
        }.StampNew(Tenant, userId);

        Db.CostToCompletes.Add(record);

        project.ForecastFinalCost = forecast;
        project.CommittedCost = RealEstateMapper.Money(committed);
        project.ActualCost = RealEstateMapper.Money(incurred);
        project.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return await MapCostToCompleteAsync(record);
    }

    private async Task<CostToCompleteDto> MapCostToCompleteAsync(CostToComplete c)
    {
        var currency = await CurrencyAsync();
        var users = await AgentUserNamesAsync([c.PreparedByUserId]);

        var wbsName = c.WbsNodeId is null
            ? null
            : await Db.WbsNodes.ForCompany(Tenant).Where(n => n.Id == c.WbsNodeId).Select(n => n.Name).FirstOrDefaultAsync();

        return new CostToCompleteDto
        {
            Id = c.Id,
            ConstructionProjectId = c.ConstructionProjectId,
            WbsNodeId = c.WbsNodeId,
            WbsName = wbsName,
            AsOfDate = c.AsOfDate,
            BudgetCost = c.BudgetCost,
            CostIncurred = c.CostIncurred,
            CommittedNotIncurred = c.CommittedNotIncurred,
            EstimatedRemaining = c.EstimatedRemaining,
            ForecastFinalCost = c.ForecastFinalCost,
            VarianceToBudget = c.VarianceToBudget,
            PercentComplete = c.PercentComplete,
            EarnedValue = c.EarnedValue,
            CostPerformanceIndex = c.CostPerformanceIndex,
            SchedulePerformanceIndex = c.SchedulePerformanceIndex,
            CurrencyCode = currency,
            PreparedByName = c.PreparedByUserId is null ? null : users.GetValueOrDefault(c.PreparedByUserId.Value),
            Assumptions = c.Assumptions,
            IsOverrunning = c.ForecastFinalCost > c.BudgetCost,
        };
    }

    // ═══ Tendering ═══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<TenderDto>> GetTendersAsync(ListQueryDto query, TenderStatus? status)
    {
        var q = Db.Tenders.ForCompany(Tenant)
            .Include(t => t.Bidders)
            .WhereIf(status.HasValue, t => t.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                t => t.Reference.Contains(query.Search!) || t.Name.Contains(query.Search!))
            .OrderByDescending(t => t.IssuedOn);

        return await PageAsync(q, query, list => MapTendersAsync(list, false));
    }

    public async Task<TenderDto?> GetTenderAsync(Guid id)
    {
        var tender = await Db.Tenders.ForCompany(Tenant)
            .Include(t => t.Bidders)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tender is null) return null;
        return (await MapTendersAsync([tender], true))[0];
    }

    private async Task<List<TenderDto>> MapTendersAsync(List<Tender> tenders, bool includeBids)
    {
        if (tenders.Count == 0) return [];

        var now = DateTime.UtcNow;
        var currency = await CurrencyAsync();
        var ids = tenders.Select(t => t.Id).ToList();

        var projectIds = tenders.Select(t => t.ConstructionProjectId).Distinct().ToList();

        var projects = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var nodeIds = tenders.Where(t => t.WbsNodeId.HasValue).Select(t => t.WbsNodeId!.Value).Distinct().ToList();

        var nodes = nodeIds.Count == 0
            ? []
            : await Db.WbsNodes.ForCompany(Tenant).Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        var bids = await Db.TenderBids.ForCompany(Tenant)
            .Where(b => ids.Contains(b.TenderId))
            .ToListAsync();

        var bidderIds = bids.Select(b => b.TenderBidderId).Distinct().ToList();

        var bidders = tenders.SelectMany(t => t.Bidders).ToList();

        var contractorIds = bidders.Where(b => b.ContractorId.HasValue).Select(b => b.ContractorId!.Value).Distinct().ToList();

        var contractors = contractorIds.Count == 0
            ? []
            : await Db.Contractors.ForCompany(Tenant).Where(c => contractorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

        return tenders.Select(t =>
        {
            var mine = bids.Where(b => b.TenderId == t.Id && !b.IsDisqualified).ToList();
            var lowest = mine.Count == 0 ? (decimal?)null : mine.Min(b => b.NegotiatedAmount ?? b.BidAmount);

            var dto = new TenderDto
            {
                Id = t.Id,
                Reference = t.Reference,
                Name = t.Name,
                ConstructionProjectId = t.ConstructionProjectId,
                ProjectName = projects.GetValueOrDefault(t.ConstructionProjectId, "—"),
                WbsNodeId = t.WbsNodeId,
                PackageName = t.WbsNodeId is null ? null : nodes.GetValueOrDefault(t.WbsNodeId.Value),
                Status = t.Status,
                Scope = t.Scope,
                EstimatedValue = t.EstimatedValue,
                CurrencyCode = currency,
                IssuedOn = t.IssuedOn,
                SubmissionDeadline = t.SubmissionDeadline,
                RequiredStartDate = t.RequiredStartDate,
                RequiredFinishDate = t.RequiredFinishDate,
                EarnestMoneyDeposit = t.EarnestMoneyDeposit,
                DocumentUrl = t.DocumentUrl,
                BidderCount = t.Bidders.Count,
                BidCount = bids.Count(b => b.TenderId == t.Id),
                AwardedBidId = t.AwardedBidId,
                AwardedOn = t.AwardedOn,
                SubcontractId = t.SubcontractId,
                AwardJustification = t.AwardJustification,
                IsDeadlinePassed = t.SubmissionDeadline is not null && t.SubmissionDeadline < now,
            };

            if (!includeBids) return dto;

            dto.Bids = mine
                .Concat(bids.Where(b => b.TenderId == t.Id && b.IsDisqualified))
                .OrderBy(b => b.NegotiatedAmount ?? b.BidAmount)
                .Select(b =>
                {
                    var bidder = bidders.FirstOrDefault(x => x.Id == b.TenderBidderId);
                    var amount = b.NegotiatedAmount ?? b.BidAmount;

                    return new TenderBidDto
                    {
                        Id = b.Id,
                        TenderBidderId = b.TenderBidderId,
                        ContractorId = bidder?.ContractorId,
                        BidderName = bidder?.Name
                            ?? (bidder?.ContractorId is null ? "—" : contractors.GetValueOrDefault(bidder.ContractorId.Value, "—")),
                        SubmittedAt = b.SubmittedAt,
                        BidAmount = b.BidAmount,

                        // Against the estimate, which is what tells a QS whether the market has
                        // moved or the estimate was wrong.
                        VarianceFromEstimate = t.EstimatedValue > 0m ? RealEstateMapper.Money(amount - t.EstimatedValue) : null,
                        VariancePercent = t.EstimatedValue > 0m ? RealEstateMapper.Percent(amount - t.EstimatedValue, t.EstimatedValue) : null,

                        ProposedDurationDays = b.ProposedDurationDays,
                        PaymentTerms = b.PaymentTerms,
                        AdvanceRequested = b.AdvanceRequested,
                        RetentionOffered = b.RetentionOffered,
                        TechnicalScore = b.TechnicalScore,
                        CommercialScore = b.CommercialScore,
                        TotalScore = b.TotalScore,
                        Rank = b.Rank,
                        Qualifications = b.Qualifications,
                        Exclusions = b.Exclusions,
                        DocumentUrl = b.DocumentUrl,
                        NegotiatedAmount = b.NegotiatedAmount,
                        IsAwarded = b.IsAwarded,
                        IsDisqualified = b.IsDisqualified,
                        DisqualificationReason = b.DisqualificationReason,
                        IsLowest = !b.IsDisqualified && lowest is not null && amount == lowest,
                    };
                })
                .ToList();

            return dto;
        }).ToList();
    }

    public async Task<TenderDto> SaveTenderAsync(TenderDto dto, Guid userId)
    {
        var tender = dto.Id != Guid.Empty
            ? await Db.Tenders.ForCompany(Tenant).Include(t => t.Bidders).FirstOrDefaultAsync(t => t.Id == dto.Id)
            : null;

        if (tender is null)
        {
            tender = new Tender
            {
                Reference = await numbering.NextTenderNumberAsync(DateTime.UtcNow),
                ConstructionProjectId = dto.ConstructionProjectId,
            }.StampNew(Tenant, userId);

            Db.Tenders.Add(tender);
        }
        else
        {
            if (tender.AwardedBidId is not null)
                throw new InvalidOperationException("This tender has been awarded and cannot be edited.");

            tender.StampUpdated(userId);
        }

        tender.Name = dto.Name;
        tender.WbsNodeId = dto.WbsNodeId;
        tender.BillOfQuantitiesId = dto.Id == Guid.Empty ? tender.BillOfQuantitiesId : tender.BillOfQuantitiesId;
        tender.Status = dto.Status;
        tender.Scope = dto.Scope;
        tender.EstimatedValue = dto.EstimatedValue;
        tender.IssuedOn = dto.IssuedOn;
        tender.SubmissionDeadline = dto.SubmissionDeadline;
        tender.RequiredStartDate = dto.RequiredStartDate;
        tender.RequiredFinishDate = dto.RequiredFinishDate;
        tender.EarnestMoneyDeposit = dto.EarnestMoneyDeposit;
        tender.DocumentUrl = dto.DocumentUrl;

        await Db.SaveChangesAsync();
        return (await GetTenderAsync(tender.Id))!;
    }

    public async Task<TenderDto> SubmitBidAsync(Guid tenderId, TenderBidDto bid, Guid userId)
    {
        var tender = await Db.Tenders.ForCompany(Tenant)
            .Include(t => t.Bidders)
            .FirstOrDefaultAsync(t => t.Id == tenderId)
            ?? throw new InvalidOperationException("That tender does not exist.");

        if (tender.AwardedBidId is not null)
            throw new InvalidOperationException("This tender has already been awarded.");

        var now = DateTime.UtcNow;

        // A bid after the deadline is recorded but disqualified. Silently accepting it is how a
        // tender process loses its integrity, and the other bidders will find out.
        var late = tender.SubmissionDeadline is not null && now > tender.SubmissionDeadline;

        var bidder = tender.Bidders.FirstOrDefault(b => b.Id == bid.TenderBidderId)
                  ?? tender.Bidders.FirstOrDefault(b => b.ContractorId == bid.ContractorId);

        if (bidder is null)
        {
            bidder = new TenderBidder
            {
                TenderId = tenderId,
                ContractorId = bid.ContractorId,
                Name = bid.BidderName,
                InvitedOn = Today,
            }.StampNew(Tenant, userId);

            tender.Bidders.Add(bidder);
            await Db.SaveChangesAsync();
        }

        var existing = await Db.TenderBids.ForCompany(Tenant)
            .FirstOrDefaultAsync(b => b.TenderId == tenderId && b.TenderBidderId == bidder.Id);

        if (existing is not null)
            throw new InvalidOperationException($"{bidder.Name ?? "This bidder"} has already submitted a bid. Withdraw it before submitting another.");

        var record = new TenderBid
        {
            TenderId = tenderId,
            TenderBidderId = bidder.Id,
            SubmittedAt = now,
            BidAmount = bid.BidAmount,
            ProposedDurationDays = bid.ProposedDurationDays,
            PaymentTerms = bid.PaymentTerms,
            AdvanceRequested = bid.AdvanceRequested,
            RetentionOffered = bid.RetentionOffered,
            TechnicalScore = bid.TechnicalScore,
            CommercialScore = bid.CommercialScore,
            Qualifications = bid.Qualifications,
            Exclusions = bid.Exclusions,
            DocumentUrl = bid.DocumentUrl,
            IsDisqualified = late,
            DisqualificationReason = late ? $"Submitted after the deadline of {tender.SubmissionDeadline:dd MMM yyyy HH:mm}." : null,
        }.StampNew(Tenant, userId);

        record.TotalScore = (bid.TechnicalScore ?? 0) + (bid.CommercialScore ?? 0);

        Db.TenderBids.Add(record);

        bidder.HasResponded = true;
        bidder.StampUpdated(userId);

        tender.Status = TenderStatus.BidsOpen;
        tender.StampUpdated(userId);

        await Db.SaveChangesAsync();
        await RankBidsAsync(tenderId, userId);

        return (await GetTenderAsync(tenderId))!;
    }

    private async Task RankBidsAsync(Guid tenderId, Guid userId)
    {
        var bids = await Db.TenderBids.ForCompany(Tenant)
            .Where(b => b.TenderId == tenderId && !b.IsDisqualified)
            .ToListAsync();

        // Ranked on total score where the tender is scored, on price where it is not. A purely
        // lowest-price ranking on a technically-scored tender misrepresents the evaluation.
        var scored = bids.Any(b => b.TotalScore > 0);

        var ordered = scored
            ? bids.OrderByDescending(b => b.TotalScore).ThenBy(b => b.NegotiatedAmount ?? b.BidAmount).ToList()
            : bids.OrderBy(b => b.NegotiatedAmount ?? b.BidAmount).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Rank = i + 1;
            ordered[i].StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
    }

    /// <summary>
    /// Awards the tender and opens the subcontract. Awarding to anything but the lowest compliant
    /// bid demands a written justification — that is the decision an auditor always asks about,
    /// and a system that does not capture the answer leaves the buyer exposed.
    /// </summary>
    public async Task<TenderDto> AwardTenderAsync(Guid tenderId, Guid bidId, string justification, Guid userId)
    {
        var tender = await Db.Tenders.ForCompany(Tenant)
            .Include(t => t.Bidders)
            .FirstOrDefaultAsync(t => t.Id == tenderId)
            ?? throw new InvalidOperationException("That tender does not exist.");

        if (tender.AwardedBidId is not null)
            throw new InvalidOperationException($"This tender was awarded on {tender.AwardedOn:dd MMM yyyy}.");

        var bids = await Db.TenderBids.ForCompany(Tenant)
            .Where(b => b.TenderId == tenderId)
            .ToListAsync();

        var winner = bids.FirstOrDefault(b => b.Id == bidId)
            ?? throw new InvalidOperationException("That bid does not exist on this tender.");

        if (winner.IsDisqualified)
            throw new InvalidOperationException($"That bid was disqualified: {winner.DisqualificationReason}");

        var compliant = bids.Where(b => !b.IsDisqualified).ToList();
        var lowest = compliant.OrderBy(b => b.NegotiatedAmount ?? b.BidAmount).FirstOrDefault();

        var winningAmount = winner.NegotiatedAmount ?? winner.BidAmount;
        var lowestAmount = lowest is null ? winningAmount : lowest.NegotiatedAmount ?? lowest.BidAmount;

        if (winner.Id != lowest?.Id && string.IsNullOrWhiteSpace(justification))
        {
            var bidder = tender.Bidders.FirstOrDefault(b => b.Id == lowest?.TenderBidderId);

            throw new InvalidOperationException(
                $"This is not the lowest compliant bid — {bidder?.Name ?? "another bidder"} offered " +
                $"{lowestAmount:N0} against {winningAmount:N0}. Record why this bid was preferred.");
        }

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var today = Today;

        winner.IsAwarded = true;
        winner.StampUpdated(userId);

        tender.AwardedBidId = bidId;
        tender.AwardedOn = today;
        tender.Status = TenderStatus.Awarded;
        tender.AwardJustification = justification;
        tender.StampUpdated(userId);

        var awardedBidder = tender.Bidders.First(b => b.Id == winner.TenderBidderId);

        if (awardedBidder.ContractorId is null)
            throw new InvalidOperationException("The winning bidder is not linked to a contractor record. Create one before awarding.");

        var subcontract = new Subcontract
        {
            Reference = await numbering.NextSubcontractNumberAsync(DateTime.UtcNow),
            Name = tender.Name,
            ConstructionProjectId = tender.ConstructionProjectId,
            WbsNodeId = tender.WbsNodeId,
            TenderId = tenderId,
            ContractorId = awardedBidder.ContractorId.Value,
            Status = SubcontractStatus.Awarded,
            ContractValue = winningAmount,
            RevisedValue = winningAmount,
            AwardedOn = today,
            StartDate = tender.RequiredStartDate ?? today,
            FinishDate = tender.RequiredFinishDate
                ?? (winner.ProposedDurationDays is > 0
                    ? (tender.RequiredStartDate ?? today).AddDays(winner.ProposedDurationDays.Value)
                    : today.AddMonths(6)),
            AdvancePercent = winner.AdvanceRequested is null || winningAmount <= 0m
                ? 0m
                : RealEstateMapper.Percent(winner.AdvanceRequested.Value, winningAmount),
            RetentionPercent = winner.RetentionOffered ?? 10m,
            BillOfQuantitiesId = tender.BillOfQuantitiesId,
        }.StampNew(Tenant, userId);

        Db.Subcontracts.Add(subcontract);
        await Db.SaveChangesAsync();

        tender.SubcontractId = subcontract.Id;

        // The losing bidders are told. A tender process that never closes the loop stops
        // attracting bids, and a thin bidder list costs more than any single award.
        foreach (var loser in compliant.Where(b => b.Id != bidId))
        {
            var bidder = tender.Bidders.FirstOrDefault(x => x.Id == loser.TenderBidderId);
            if (bidder?.PartyId is null) continue;

            await QueueNotificationAsync(
                "TenderNotAwarded",
                $"{tender.Reference} — outcome",
                $"Thank you for bidding on {tender.Name}. On this occasion the package has been awarded elsewhere.",
                $"/realestate/tenders/{tenderId}",
                recipientPartyId: bidder.PartyId,
                entityType: "Tender",
                entityId: tenderId);
        }

        await WriteAuditNoteAsync(
            "Tender", tenderId, "TenderAwarded", Guid.Empty, userId,
            amountImpact: winningAmount,
            note: winner.Id == lowest?.Id
                ? $"Awarded to the lowest compliant bid at {winningAmount:N0}."
                : $"Awarded at {winningAmount:N0} against a lowest bid of {lowestAmount:N0}. {justification}",
            entityReference: tender.Reference,
            highRisk: winner.Id != lowest?.Id);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await GetTenderAsync(tenderId))!;
    }

    /// <summary>
    /// Rate-by-rate comparison across every bid. Outliers are flagged because a bid that is far
    /// below the others on one item is either a mistake or a deliberate under-price waiting to be
    /// claimed back through variations.
    /// </summary>
    public async Task<List<BidComparisonLineDto>> GetBidComparisonAsync(Guid tenderId)
    {
        var tender = await Db.Tenders.ForCompany(Tenant)
            .Include(t => t.Bidders)
            .FirstOrDefaultAsync(t => t.Id == tenderId)
            ?? throw new InvalidOperationException("That tender does not exist.");

        if (tender.BillOfQuantitiesId is null) return [];

        var boqLines = await Db.BoqLines.ForCompany(Tenant)
            .Where(l => l.BillOfQuantitiesId == tender.BillOfQuantitiesId)
            .OrderBy(l => l.SortOrder)
            .ToListAsync();

        var bids = await Db.TenderBids.ForCompany(Tenant)
            .Where(b => b.TenderId == tenderId && !b.IsDisqualified)
            .ToListAsync();

        var comparisons = await Db.BidComparisonLines.ForCompany(Tenant)
            .Where(c => c.TenderId == tenderId)
            .ToListAsync();

        var contractorIds = tender.Bidders.Where(b => b.ContractorId.HasValue).Select(b => b.ContractorId!.Value).Distinct().ToList();

        var contractors = contractorIds.Count == 0
            ? []
            : await Db.Contractors.ForCompany(Tenant).Where(c => contractorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

        var result = new List<BidComparisonLineDto>();

        foreach (var boqLine in boqLines)
        {
            var cells = comparisons.Where(c => c.BoqLineId == boqLine.Id).ToList();
            if (cells.Count == 0) continue;

            var rates = cells.Select(c => c.BidRate).Where(r => r > 0m).ToList();
            var median = rates.Count == 0 ? 0m : rates.OrderBy(r => r).ElementAt(rates.Count / 2);
            var lowest = rates.Count == 0 ? 0m : rates.Min();

            var line = new BidComparisonLineDto
            {
                BoqLineId = boqLine.Id,
                ItemCode = boqLine.ItemCode,
                Description = boqLine.Description ?? string.Empty,
                Uom = boqLine.Uom,
                Quantity = boqLine.Quantity,
                EstimateRate = boqLine.Rate,
            };

            foreach (var cell in cells)
            {
                var bid = bids.FirstOrDefault(b => b.Id == cell.TenderBidId);
                var bidder = bid is null ? null : tender.Bidders.FirstOrDefault(x => x.Id == bid.TenderBidderId);

                // Half or double the median is not competitive pricing, it is an error or a
                // strategy. Either way somebody should look at it before the award.
                var outlier = median > 0m && (cell.BidRate < median * 0.5m || cell.BidRate > median * 1.5m);

                line.Bids.Add(new BidComparisonCellDto
                {
                    TenderBidId = cell.TenderBidId,
                    BidderName = bidder?.Name
                        ?? (bidder?.ContractorId is null ? "—" : contractors.GetValueOrDefault(bidder.ContractorId.Value, "—")),
                    Rate = cell.BidRate,
                    Amount = cell.BidAmount,
                    VariancePercent = boqLine.Rate > 0m
                        ? RealEstateMapper.Percent(cell.BidRate - boqLine.Rate, boqLine.Rate)
                        : null,
                    IsOutlier = outlier,
                    IsLowest = cell.BidRate == lowest && lowest > 0m,
                    Note = outlier
                        ? $"{(cell.BidRate < median ? "Well below" : "Well above")} the median of {median:N2}."
                        : cell.Note,
                });
            }

            result.Add(line);
        }

        return result;
    }
}
