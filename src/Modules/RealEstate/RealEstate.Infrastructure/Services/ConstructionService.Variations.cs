using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Variations, site instructions, delay and extensions of time, and site operations.
///
/// A variation instructed and never priced is money the contractor spends and cannot recover, or
/// money the employer owes and never sees coming. So a site instruction with cost impact must
/// produce a variation, an approved variation is what moves the contract value and nothing else
/// does, and a delay records who caused it before anybody argues about who pays for it.
/// </summary>
public partial class ConstructionService
{
    // ═══ Variations ══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<VariationOrderListItemDto>> GetVariationsAsync(
        ListQueryDto query, Guid? constructionProjectId, VariationStatus? status)
    {
        var q = Db.VariationOrders.ForCompany(Tenant)
            .WhereIf(constructionProjectId.HasValue, v => v.ConstructionProjectId == constructionProjectId)
            .WhereIf(status.HasValue, v => v.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                v => v.VariationNumber.Contains(query.Search!) || v.Title.Contains(query.Search!))
            .OrderByDescending(v => v.RaisedOn);

        return await PageAsync(q, query, MapVariationListAsync);
    }

    private async Task<List<VariationOrderListItemDto>> MapVariationListAsync(List<VariationOrder> variations)
    {
        if (variations.Count == 0) return [];

        var today = Today;
        var currency = await CurrencyAsync();

        var projectIds = variations.Select(v => v.ConstructionProjectId).Distinct().ToList();

        var projects = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var subIds = variations.Where(v => v.SubcontractId.HasValue).Select(v => v.SubcontractId!.Value).Distinct().ToList();

        var subcontractors = subIds.Count == 0
            ? []
            : await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => subIds.Contains(s.Id))
                .Join(Db.Contractors.ForCompany(Tenant), s => s.ContractorId, c => c.Id, (s, c) => new { s.Id, c.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        var contractIds = variations.Where(v => v.ClientBuildContractId.HasValue)
            .Select(v => v.ClientBuildContractId!.Value).Distinct().ToList();

        var clients = contractIds.Count == 0
            ? []
            : await Db.ClientBuildContracts.ForCompany(Tenant)
                .Where(c => contractIds.Contains(c.Id))
                .Join(Db.Parties.ForCompany(Tenant), c => c.ClientPartyId, p => p.Id, (c, p) => new { c.Id, p.DisplayName })
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName);

        var raisers = await AgentUserNamesAsync(variations.Select(v => v.RaisedByUserId));

        return variations.Select(v => new VariationOrderListItemDto
        {
            Id = v.Id,
            VariationNumber = v.VariationNumber,
            ConstructionProjectId = v.ConstructionProjectId,
            ProjectName = projects.GetValueOrDefault(v.ConstructionProjectId, "—"),
            SubcontractId = v.SubcontractId,
            SubcontractorName = v.SubcontractId is null ? null : subcontractors.GetValueOrDefault(v.SubcontractId.Value),
            ClientBuildContractId = v.ClientBuildContractId,
            ClientName = v.ClientBuildContractId is null ? null : clients.GetValueOrDefault(v.ClientBuildContractId.Value),
            Origin = v.Origin,
            Status = v.Status,
            Title = v.Title,
            RaisedOn = v.RaisedOn,
            RaisedByName = v.RaisedByUserId is null ? null : raisers.GetValueOrDefault(v.RaisedByUserId.Value),
            AdditionAmount = v.AdditionAmount,
            OmissionAmount = v.OmissionAmount,
            NetAmount = v.NetAmount,
            TimeImpactDays = v.TimeImpactDays,
            RevisedContractValue = v.RevisedContractValue,
            CurrencyCode = currency,
            QuotedOn = v.QuotedOn,
            ApprovedOn = v.ApprovedOn,
            ClientApproved = v.ClientApproved,
            IsMeasured = v.IsMeasured,

            // Days a variation has sat unpriced or unapproved. Work is usually already being done
            // on it, so this number is exposure rather than admin.
            DaysOpen = (v.ApprovedOn ?? today).DayNumber - v.RaisedOn.DayNumber,

            IsAwaitingApproval = v.Status is VariationStatus.Proposed or VariationStatus.Quoted or VariationStatus.Instructed,
        }).ToList();
    }

    public async Task<VariationOrderDetailDto?> GetVariationAsync(Guid id)
    {
        var variation = await Db.VariationOrders.ForCompany(Tenant)
            .Include(v => v.Lines)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (variation is null) return null;

        var head = (await MapVariationListAsync([variation]))[0];

        var detail = new VariationOrderDetailDto
        {
            Id = head.Id,
            VariationNumber = head.VariationNumber,
            ConstructionProjectId = head.ConstructionProjectId,
            ProjectName = head.ProjectName,
            SubcontractId = head.SubcontractId,
            SubcontractorName = head.SubcontractorName,
            ClientBuildContractId = head.ClientBuildContractId,
            ClientName = head.ClientName,
            Origin = head.Origin,
            Status = head.Status,
            Title = head.Title,
            RaisedOn = head.RaisedOn,
            RaisedByName = head.RaisedByName,
            AdditionAmount = head.AdditionAmount,
            OmissionAmount = head.OmissionAmount,
            NetAmount = head.NetAmount,
            TimeImpactDays = head.TimeImpactDays,
            RevisedContractValue = head.RevisedContractValue,
            CurrencyCode = head.CurrencyCode,
            QuotedOn = head.QuotedOn,
            ApprovedOn = head.ApprovedOn,
            ClientApproved = head.ClientApproved,
            IsMeasured = head.IsMeasured,
            DaysOpen = head.DaysOpen,
            IsAwaitingApproval = head.IsAwaitingApproval,

            WbsNodeId = variation.WbsNodeId,
            Description = variation.Description ?? string.Empty,
            Justification = variation.Justification,
            SiteInstructionId = variation.SiteInstructionId,
            RejectionReason = variation.RejectionReason,
            DocumentUrl = variation.DocumentUrl,
            SignatureSessionId = variation.SignatureSessionId,

            Lines = variation.Lines.OrderBy(l => l.SortOrder).Select(l => new VariationLineDto
            {
                Id = l.Id,
                BoqLineId = l.BoqLineId,
                Description = l.Description ?? string.Empty,
                Uom = l.Uom,
                Quantity = l.Quantity,
                Rate = l.Rate,
                Amount = l.Amount,
                IsOmission = l.IsOmission,
                RateAnalysisId = l.RateAnalysisId,
                IsNewRate = l.IsNewRate,
                SortOrder = l.SortOrder,
            }).ToList(),
        };

        if (variation.SiteInstructionId is not null)
        {
            detail.SiteInstructionNumber = await Db.SiteInstructions.ForCompany(Tenant)
                .Where(i => i.Id == variation.SiteInstructionId)
                .Select(i => i.InstructionNumber)
                .FirstOrDefaultAsync();
        }

        if (variation.ApprovedByPartyId is not null)
        {
            var names = await PartyNamesAsync([variation.ApprovedByPartyId.Value]);
            detail.ApprovedByName = names.GetValueOrDefault(variation.ApprovedByPartyId.Value);
        }

        return detail;
    }

    public async Task<VariationOrderDetailDto> SaveVariationAsync(VariationOrderUpsertDto dto, Guid userId)
    {
        var project = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var variation = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.VariationOrders.ForCompany(Tenant).Include(v => v.Lines).FirstOrDefaultAsync(v => v.Id == dto.Id)
            : null;

        if (variation is null)
        {
            variation = new VariationOrder
            {
                VariationNumber = await numbering.NextVariationNumberAsync(DateTime.UtcNow),
                ConstructionProjectId = dto.ConstructionProjectId,
                SubcontractId = dto.SubcontractId,
                ClientBuildContractId = dto.ClientBuildContractId,
                RaisedOn = dto.RaisedOn == default ? Today : dto.RaisedOn,
                RaisedByUserId = userId,
            }.StampNew(Tenant, userId);

            Db.VariationOrders.Add(variation);
        }
        else
        {
            // An approved variation has moved the contract value and may already have been
            // certified against. Changing it is a second variation, not an edit.
            if (variation.Status == VariationStatus.Approved)
                throw new InvalidOperationException("This variation is approved. Raise another to change it.");

            variation.StampUpdated(userId);
        }

        variation.WbsNodeId = dto.WbsNodeId;
        variation.Origin = dto.Origin;
        variation.Title = dto.Title;
        variation.Description = dto.Description;
        variation.Justification = dto.Justification;
        variation.SiteInstructionId = dto.SiteInstructionId;
        variation.TimeImpactDays = dto.TimeImpactDays;

        if (dto.Lines.Count > 0)
        {
            Db.VariationLines.RemoveRange(variation.Lines);

            var order = 0;

            foreach (var l in dto.Lines)
            {
                variation.Lines.Add(new VariationLine
                {
                    BoqLineId = l.BoqLineId,
                    Description = l.Description,
                    Uom = l.Uom,
                    Quantity = l.Quantity,
                    Rate = l.Rate,
                    Amount = RealEstateMapper.Money(l.Quantity * l.Rate),
                    IsOmission = l.IsOmission,
                    RateAnalysisId = l.RateAnalysisId,

                    // A rate not in the contract has to be agreed separately, so it is flagged
                    // rather than slipped through at whatever the contractor put on the sheet.
                    IsNewRate = l.BoqLineId is null,

                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        variation.AdditionAmount = RealEstateMapper.Money(variation.Lines.Where(l => !l.IsOmission).Sum(l => l.Amount));
        variation.OmissionAmount = RealEstateMapper.Money(variation.Lines.Where(l => l.IsOmission).Sum(l => l.Amount));
        variation.NetAmount = RealEstateMapper.Money(variation.AdditionAmount - variation.OmissionAmount);
        variation.RevisedContractValue = RealEstateMapper.Money(project.RevisedContractValue + variation.NetAmount);

        if (variation.Lines.Count > 0 && variation.Status == VariationStatus.Proposed)
        {
            variation.Status = VariationStatus.Quoted;
            variation.QuotedOn = Today;
        }

        await Db.SaveChangesAsync();
        return (await GetVariationAsync(variation.Id))!;
    }

    /// <summary>
    /// Approving a variation is the only thing that moves a contract value. Its lines become BOQ
    /// lines so the extra work is measurable and certifiable like everything else — a variation
    /// approved but never added to the bill is work that gets done and never gets paid for.
    /// </summary>
    public async Task<VariationOrderDetailDto> DecideVariationAsync(
        Guid id, VariationStatus status, string? reason, Guid userId)
    {
        var variation = await Db.VariationOrders.ForCompany(Tenant)
            .Include(v => v.Lines)
            .FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new InvalidOperationException("That variation does not exist.");

        if (variation.Status == VariationStatus.Approved)
            throw new InvalidOperationException($"This variation was approved on {variation.ApprovedOn:dd MMM yyyy}.");

        if (status == VariationStatus.Rejected && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Rejecting a variation has to say why — the work may already be in hand.");

        var today = Today;

        if (status != VariationStatus.Approved)
        {
            variation.Status = status;
            variation.RejectionReason = reason;
            variation.StampUpdated(userId);

            await Db.SaveChangesAsync();
            return (await GetVariationAsync(id))!;
        }

        if (variation.Lines.Count == 0)
            throw new InvalidOperationException("This variation has no priced lines. It cannot be approved at an unknown value.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        variation.Status = VariationStatus.Approved;
        variation.ApprovedOn = today;
        variation.StampUpdated(userId);

        var project = await RequireAsync<ConstructionProject>(variation.ConstructionProjectId, "The project is missing.");

        project.ApprovedVariations = RealEstateMapper.Money(project.ApprovedVariations + variation.NetAmount);
        project.RevisedContractValue = RealEstateMapper.Money(project.ContractValue + project.ApprovedVariations);

        // Time impact extends the programme. A variation that adds four weeks of work without
        // moving the completion date manufactures a delay the contractor did not cause.
        if (variation.TimeImpactDays > 0)
        {
            project.ExtensionDaysGranted += variation.TimeImpactDays;

            if (project.ForecastCompletionDate is not null)
                project.ForecastCompletionDate = project.ForecastCompletionDate.Value.AddDays(variation.TimeImpactDays);
        }

        project.StampUpdated(userId);

        // The variation's work becomes measurable BOQ lines on the current contract bill.
        var boq = await Db.BillsOfQuantities.ForCompany(Tenant)
            .Where(b => b.ConstructionProjectId == variation.ConstructionProjectId
                     && b.IsCurrent
                     && b.SubcontractId == variation.SubcontractId)
            .OrderByDescending(b => b.Version)
            .FirstOrDefaultAsync();

        if (boq is not null)
        {
            var maxSort = await Db.BoqLines.ForCompany(Tenant)
                .Where(l => l.BillOfQuantitiesId == boq.Id)
                .MaxAsync(l => (int?)l.SortOrder) ?? 0;

            var sequence = 1;

            foreach (var line in variation.Lines.OrderBy(l => l.SortOrder))
            {
                Db.BoqLines.Add(new BoqLine
                {
                    BillOfQuantitiesId = boq.Id,
                    WbsNodeId = variation.WbsNodeId,
                    ItemCode = $"{variation.VariationNumber}-{sequence++:D2}",
                    Description = line.Description,
                    Kind = BoqLineKind.Measured,
                    Uom = line.Uom ?? "Item",

                    // An omission is a negative quantity against the bill, so the measured total
                    // falls rather than the omission being tracked in a parallel document.
                    Quantity = line.IsOmission ? -line.Quantity : line.Quantity,
                    Rate = line.Rate,
                    Amount = RealEstateMapper.Money((line.IsOmission ? -line.Quantity : line.Quantity) * line.Rate),

                    RateAnalysisId = line.RateAnalysisId,
                    IsVariation = true,
                    VariationOrderId = variation.Id,
                    SortOrder = maxSort + (sequence * 10),
                }.StampNew(Tenant, userId));
            }

            await Db.SaveChangesAsync();
            await RecalculateBoqTotalsAsync(boq.Id, userId);
        }

        if (variation.SubcontractId is not null)
        {
            var subcontract = await Db.Subcontracts.ForCompany(Tenant)
                .FirstOrDefaultAsync(s => s.Id == variation.SubcontractId);

            if (subcontract is not null)
            {
                subcontract.ApprovedVariations = RealEstateMapper.Money(subcontract.ApprovedVariations + variation.NetAmount);
                subcontract.RevisedValue = RealEstateMapper.Money(subcontract.ContractValue + subcontract.ApprovedVariations);
                subcontract.ExtensionDaysGranted += variation.TimeImpactDays;
                subcontract.StampUpdated(userId);
            }
        }

        // The instruction that caused it is closed off, so nobody raises a second variation for
        // the same instruction next month.
        if (variation.SiteInstructionId is not null)
        {
            var instruction = await Db.SiteInstructions.ForCompany(Tenant)
                .FirstOrDefaultAsync(i => i.Id == variation.SiteInstructionId);

            if (instruction is not null)
            {
                instruction.VariationOrderId = variation.Id;
                instruction.StampUpdated(userId);
            }
        }

        await WriteAuditNoteAsync(
            "VariationOrder", id, "VariationApproved", Guid.Empty, userId,
            amountImpact: variation.NetAmount,
            before: project.ContractValue.ToString("N0"),
            after: project.RevisedContractValue.ToString("N0"),
            note: $"{variation.Title}. {variation.TimeImpactDays} days added to the programme.",
            entityReference: variation.VariationNumber,
            highRisk: true);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await GetVariationAsync(id))!;
    }

    // ═══ Site instructions ═══════════════════════════════════════════════════

    /// <summary>
    /// An instruction issued on site. Where it has a cost impact a variation is opened at once —
    /// an instruction carried out and priced six months later is the single commonest source of a
    /// disputed final account.
    /// </summary>
    public async Task<SiteInstructionDto> IssueSiteInstructionAsync(SiteInstructionDto dto, Guid userId)
    {
        _ = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var instruction = new SiteInstruction
        {
            InstructionNumber = await numbering.NextSiteInstructionNumberAsync(DateTime.UtcNow),
            ConstructionProjectId = dto.ConstructionProjectId,
            SubcontractId = dto.SubcontractId,
            IssuedOn = dto.IssuedOn == default ? Today : dto.IssuedOn,
            IssuedByUserId = userId,
            Instruction = dto.Instruction,
            Location = dto.Location,
            ComplyBy = dto.ComplyBy,
            HasCostImpact = dto.HasCostImpact,
            HasTimeImpact = dto.HasTimeImpact,
            PhotoUrls = dto.PhotoUrls.Count == 0 ? null : string.Join('\n', dto.PhotoUrls),
            DocumentUrl = dto.DocumentUrl,
        }.StampNew(Tenant, userId);

        if (dto.SubcontractId is not null)
        {
            instruction.ContractorId = await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => s.Id == dto.SubcontractId)
                .Select(s => (Guid?)s.ContractorId)
                .FirstOrDefaultAsync();
        }

        Db.SiteInstructions.Add(instruction);
        await Db.SaveChangesAsync();

        if (dto.HasCostImpact || dto.HasTimeImpact)
        {
            var variation = new VariationOrder
            {
                VariationNumber = await numbering.NextVariationNumberAsync(DateTime.UtcNow),
                ConstructionProjectId = dto.ConstructionProjectId,
                SubcontractId = dto.SubcontractId,
                Origin = VariationOrigin.DesignChange,
                Status = VariationStatus.Instructed,
                Title = dto.Instruction.Length > 120 ? dto.Instruction[..120] : dto.Instruction,
                Description = dto.Instruction,
                Justification = $"Raised automatically from site instruction {instruction.InstructionNumber}.",
                RaisedOn = instruction.IssuedOn,
                RaisedByUserId = userId,
                SiteInstructionId = instruction.Id,
            }.StampNew(Tenant, userId);

            Db.VariationOrders.Add(variation);
            await Db.SaveChangesAsync();

            instruction.VariationOrderId = variation.Id;
            await Db.SaveChangesAsync();
        }

        return (await MapInstructionsAsync([instruction]))[0];
    }

    public async Task<PaginatedResponse<SiteInstructionDto>> GetSiteInstructionsAsync(
        ListQueryDto query, Guid? constructionProjectId)
    {
        var q = Db.SiteInstructions.ForCompany(Tenant)
            .WhereIf(constructionProjectId.HasValue, i => i.ConstructionProjectId == constructionProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), i => i.InstructionNumber.Contains(query.Search!))
            .OrderByDescending(i => i.IssuedOn);

        return await PageAsync(q, query, MapInstructionsAsync);
    }

    private async Task<List<SiteInstructionDto>> MapInstructionsAsync(List<SiteInstruction> instructions)
    {
        if (instructions.Count == 0) return [];

        var today = Today;

        var projectIds = instructions.Select(i => i.ConstructionProjectId).Distinct().ToList();

        var projects = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var contractorIds = instructions.Where(i => i.ContractorId.HasValue).Select(i => i.ContractorId!.Value).Distinct().ToList();

        var contractors = contractorIds.Count == 0
            ? []
            : await Db.Contractors.ForCompany(Tenant).Where(c => contractorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

        var variationIds = instructions.Where(i => i.VariationOrderId.HasValue)
            .Select(i => i.VariationOrderId!.Value).Distinct().ToList();

        var variations = variationIds.Count == 0
            ? []
            : await Db.VariationOrders.ForCompany(Tenant)
                .Where(v => variationIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.VariationNumber);

        var issuers = await AgentUserNamesAsync(instructions.Select(i => (Guid?)i.IssuedByUserId));

        return instructions.Select(i => new SiteInstructionDto
        {
            Id = i.Id,
            InstructionNumber = i.InstructionNumber,
            ConstructionProjectId = i.ConstructionProjectId,
            ProjectName = projects.GetValueOrDefault(i.ConstructionProjectId),
            SubcontractId = i.SubcontractId,
            ContractorName = i.ContractorId is null ? null : contractors.GetValueOrDefault(i.ContractorId.Value),
            IssuedOn = i.IssuedOn,
            IssuedByName = issuers.GetValueOrDefault(i.IssuedByUserId, "—"),
            Instruction = i.Instruction,
            Location = i.Location,
            ComplyBy = i.ComplyBy,
            IsAcknowledged = i.IsAcknowledged,
            AcknowledgedOn = i.AcknowledgedOn,
            HasCostImpact = i.HasCostImpact,
            HasTimeImpact = i.HasTimeImpact,
            VariationOrderId = i.VariationOrderId,
            VariationNumber = i.VariationOrderId is null ? null : variations.GetValueOrDefault(i.VariationOrderId.Value),
            PhotoUrls = string.IsNullOrWhiteSpace(i.PhotoUrls)
                ? []
                : i.PhotoUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
            DocumentUrl = i.DocumentUrl,
            IsComplied = i.IsComplied,
            IsOverdue = !i.IsComplied && i.ComplyBy is not null && i.ComplyBy < today,
        }).ToList();
    }

    // ═══ Delay and extensions of time ════════════════════════════════════════

    /// <summary>
    /// A delay event. Whether it is excusable and whether it is compensable are two different
    /// questions with two different answers — weather usually buys time but not money — and
    /// recording them separately is what makes an extension-of-time claim assessable.
    /// </summary>
    public async Task<DelayEventDto> SaveDelayAsync(DelayEventDto dto, Guid userId)
    {
        _ = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var delay = dto.Id != Guid.Empty
            ? await Db.DelayEvents.ForCompany(Tenant).FirstOrDefaultAsync(d => d.Id == dto.Id)
            : null;

        if (delay is null)
        {
            delay = new DelayEvent
            {
                Reference = await numbering.NextMasterCodeAsync(Db.DelayEvents, "DLY"),
                ConstructionProjectId = dto.ConstructionProjectId,
                SubcontractId = dto.SubcontractId,
            }.StampNew(Tenant, userId);

            Db.DelayEvents.Add(delay);
        }
        else delay.StampUpdated(userId);

        delay.ProgrammeActivityId = dto.ProgrammeActivityId;
        delay.Cause = dto.Cause;
        delay.Description = dto.Description;
        delay.StartedOn = dto.StartedOn;
        delay.EndedOn = dto.EndedOn;
        delay.IsExcusable = dto.IsExcusable;
        delay.IsCompensable = dto.IsCompensable;
        delay.ResponsibleParty = dto.ResponsibleParty;
        delay.CostImpact = dto.CostImpact;
        delay.EvidenceUrl = dto.EvidenceUrl;
        delay.IsNotified = dto.IsNotified;
        delay.NotifiedOn = dto.IsNotified ? dto.NotifiedOn ?? Today : null;

        delay.DelayDays = (dto.EndedOn ?? Today).DayNumber - dto.StartedOn.DayNumber + 1;

        // Only a delay on the critical path moves the completion date. One on an activity with
        // float is absorbed, and treating the two the same overstates every claim.
        if (dto.ProgrammeActivityId is not null)
        {
            delay.AffectsCriticalPath = await Db.ProgrammeActivities.ForCompany(Tenant)
                .Where(a => a.Id == dto.ProgrammeActivityId)
                .Select(a => a.IsCritical)
                .FirstOrDefaultAsync();
        }
        else delay.AffectsCriticalPath = dto.AffectsCriticalPath;

        // Most contracts require notice within a fixed window or the claim is lost. Recording the
        // failure to notify is as important as recording the delay.
        if (!delay.IsNotified && delay.IsExcusable && (Today.DayNumber - delay.StartedOn.DayNumber) > 28)
        {
            await WriteAuditNoteAsync(
                "DelayEvent", delay.Id, "DelayNotNotifiedInTime", Guid.Empty, userId,
                note: $"Delay started {delay.StartedOn:dd MMM yyyy} and has not been notified. " +
                      "Most contracts bar an extension-of-time claim after the notice period.",
                entityReference: delay.Reference,
                highRisk: true);
        }

        await Db.SaveChangesAsync();
        return (await MapDelaysAsync([delay]))[0];
    }

    public async Task<PaginatedResponse<DelayEventDto>> GetDelaysAsync(ListQueryDto query, Guid? constructionProjectId)
    {
        var q = Db.DelayEvents.ForCompany(Tenant)
            .WhereIf(constructionProjectId.HasValue, d => d.ConstructionProjectId == constructionProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), d => d.Reference.Contains(query.Search!))
            .OrderByDescending(d => d.StartedOn);

        return await PageAsync(q, query, MapDelaysAsync);
    }

    private async Task<List<DelayEventDto>> MapDelaysAsync(List<DelayEvent> delays)
    {
        if (delays.Count == 0) return [];

        var projectIds = delays.Select(d => d.ConstructionProjectId).Distinct().ToList();

        var projects = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var subIds = delays.Where(d => d.SubcontractId.HasValue).Select(d => d.SubcontractId!.Value).Distinct().ToList();

        var subcontractors = subIds.Count == 0
            ? []
            : await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => subIds.Contains(s.Id))
                .Join(Db.Contractors.ForCompany(Tenant), s => s.ContractorId, c => c.Id, (s, c) => new { s.Id, c.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        var activityIds = delays.Where(d => d.ProgrammeActivityId.HasValue)
            .Select(d => d.ProgrammeActivityId!.Value).Distinct().ToList();

        var activities = activityIds.Count == 0
            ? []
            : await Db.ProgrammeActivities.ForCompany(Tenant)
                .Where(a => activityIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Name);

        return delays.Select(d => new DelayEventDto
        {
            Id = d.Id,
            Reference = d.Reference,
            ConstructionProjectId = d.ConstructionProjectId,
            ProjectName = projects.GetValueOrDefault(d.ConstructionProjectId),
            SubcontractId = d.SubcontractId,
            SubcontractorName = d.SubcontractId is null ? null : subcontractors.GetValueOrDefault(d.SubcontractId.Value),
            ProgrammeActivityId = d.ProgrammeActivityId,
            ActivityName = d.ProgrammeActivityId is null ? null : activities.GetValueOrDefault(d.ProgrammeActivityId.Value),
            Cause = d.Cause,
            Description = d.Description ?? string.Empty,
            StartedOn = d.StartedOn,
            EndedOn = d.EndedOn,
            DelayDays = d.DelayDays,
            IsExcusable = d.IsExcusable,
            IsCompensable = d.IsCompensable,
            ResponsibleParty = d.ResponsibleParty,
            CostImpact = d.CostImpact,
            AffectsCriticalPath = d.AffectsCriticalPath,
            ExtensionOfTimeId = d.ExtensionOfTimeId,
            EvidenceUrl = d.EvidenceUrl,
            IsNotified = d.IsNotified,
            NotifiedOn = d.NotifiedOn,
            IsOngoing = d.EndedOn is null,
        }).ToList();
    }

    public async Task<ExtensionOfTimeDto> SaveExtensionOfTimeAsync(ExtensionOfTimeDto dto, Guid userId)
    {
        var project = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var extension = dto.Id != Guid.Empty
            ? await Db.ExtensionOfTimes.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == dto.Id)
            : null;

        if (extension is null)
        {
            extension = new ExtensionOfTime
            {
                Reference = await numbering.NextMasterCodeAsync(Db.ExtensionOfTimes, "EOT"),
                ConstructionProjectId = dto.ConstructionProjectId,
                SubcontractId = dto.SubcontractId,
                ClientBuildContractId = dto.ClientBuildContractId,
                OriginalCompletionDate = dto.OriginalCompletionDate == default
                    ? project.PlannedCompletionDate ?? Today
                    : dto.OriginalCompletionDate,
            }.StampNew(Tenant, userId);

            Db.ExtensionOfTimes.Add(extension);
        }
        else
        {
            if (extension.Status is "Granted" or "Rejected")
                throw new InvalidOperationException($"This claim was already {extension.Status.ToLowerInvariant()}.");

            extension.StampUpdated(userId);
        }

        extension.ClaimedOn = dto.ClaimedOn == default ? Today : dto.ClaimedOn;
        extension.DaysClaimed = dto.DaysClaimed;
        extension.Grounds = dto.Grounds;
        extension.ProlongationCost = dto.ProlongationCost;
        extension.DocumentUrl = dto.DocumentUrl;
        extension.Status = dto.Status;

        await Db.SaveChangesAsync();
        return (await MapExtensionsAsync([extension]))[0];
    }

    /// <summary>
    /// Assesses an extension-of-time claim. Time and money are decided separately: granting days
    /// without prolongation cost is the normal outcome for a neutral event, and conflating them
    /// either overpays the contractor or denies them relief they are entitled to.
    /// </summary>
    public async Task<ExtensionOfTimeDto> DecideExtensionAsync(
        Guid id, int daysGranted, bool prolongationGranted, string? note, Guid userId)
    {
        var extension = await RequireAsync<ExtensionOfTime>(id, "That claim does not exist.");

        if (extension.Status is "Granted" or "Rejected")
            throw new InvalidOperationException($"This claim was already {extension.Status.ToLowerInvariant()}.");

        if (daysGranted < extension.DaysClaimed && string.IsNullOrWhiteSpace(note))
            throw new InvalidOperationException("Granting fewer days than claimed has to record the assessment.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        extension.DaysGranted = daysGranted;
        extension.ProlongationCostGranted = prolongationGranted;
        extension.AssessedByUserId = userId;
        extension.DecidedOn = Today;
        extension.DecisionNote = note;
        extension.Status = daysGranted > 0 ? "Granted" : "Rejected";
        extension.RevisedCompletionDate = extension.OriginalCompletionDate.AddDays(daysGranted);

        if (daysGranted > 0)
        {
            var project = await RequireAsync<ConstructionProject>(extension.ConstructionProjectId, "The project is missing.");

            project.ExtensionDaysGranted += daysGranted;
            project.PlannedCompletionDate = extension.RevisedCompletionDate;

            if (project.ForecastCompletionDate is not null && project.ForecastCompletionDate < extension.RevisedCompletionDate)
                project.ForecastCompletionDate = extension.RevisedCompletionDate;

            project.StampUpdated(userId);

            if (extension.SubcontractId is not null)
            {
                var subcontract = await Db.Subcontracts.ForCompany(Tenant)
                    .FirstOrDefaultAsync(s => s.Id == extension.SubcontractId);

                if (subcontract is not null)
                {
                    subcontract.ExtensionDaysGranted += daysGranted;
                    subcontract.StampUpdated(userId);
                }
            }

            // The extension resets what delay is measured against, so the programme is
            // re-baselined here rather than leaving every activity showing a stale variance.
            var activities = await Db.ProgrammeActivities.ForCompany(Tenant)
                .Where(a => a.ConstructionProjectId == extension.ConstructionProjectId && a.ActualFinish == null)
                .ToListAsync();

            foreach (var activity in activities)
            {
                activity.PlannedStart = activity.PlannedStart.AddDays(daysGranted);
                activity.PlannedFinish = activity.PlannedFinish.AddDays(daysGranted);
                activity.StampUpdated(userId);
            }
        }

        await WriteAuditNoteAsync(
            "ExtensionOfTime", id, "ExtensionOfTimeDecided", Guid.Empty, userId,
            amountImpact: prolongationGranted ? extension.ProlongationCost : null,
            before: extension.DaysClaimed.ToString(),
            after: daysGranted.ToString(),
            note: note,
            entityReference: extension.Reference,
            highRisk: true);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await MapExtensionsAsync([extension]))[0];
    }

    private async Task<List<ExtensionOfTimeDto>> MapExtensionsAsync(List<ExtensionOfTime> extensions)
    {
        if (extensions.Count == 0) return [];

        var assessors = await AgentUserNamesAsync(extensions.Select(e => e.AssessedByUserId));

        return extensions.Select(e => new ExtensionOfTimeDto
        {
            Id = e.Id,
            Reference = e.Reference,
            ConstructionProjectId = e.ConstructionProjectId,
            SubcontractId = e.SubcontractId,
            ClientBuildContractId = e.ClientBuildContractId,
            ClaimedOn = e.ClaimedOn,
            DaysClaimed = e.DaysClaimed,
            DaysGranted = e.DaysGranted,
            Grounds = e.Grounds,
            OriginalCompletionDate = e.OriginalCompletionDate,
            RevisedCompletionDate = e.RevisedCompletionDate,
            Status = e.Status,
            ProlongationCost = e.ProlongationCost,
            ProlongationCostGranted = e.ProlongationCostGranted,
            AssessedByName = e.AssessedByUserId is null ? null : assessors.GetValueOrDefault(e.AssessedByUserId.Value),
            DecidedOn = e.DecidedOn,
            DecisionNote = e.DecisionNote,
            DocumentUrl = e.DocumentUrl,
        }).ToList();
    }
}
