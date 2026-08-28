using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Site operations: material requisitions and issues, wastage, labour, plant, the gate and safety.
///
/// Wastage is the one that pays for itself. Material issued against a BOQ line can be compared to
/// what that quantity of work should theoretically have consumed, and the difference — beyond the
/// allowance the trade genuinely needs — is either leaking off site or being over-ordered. Nobody
/// finds it by looking at a stock report; it only shows up when issues are tied to measured work.
/// </summary>
public partial class ConstructionService
{
    // ═══ Material requisitions ═══════════════════════════════════════════════

    public async Task<MaterialRequisitionDto> SaveRequisitionAsync(MaterialRequisitionDto dto, Guid userId)
    {
        _ = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var requisition = dto.Id != Guid.Empty
            ? await Db.MaterialRequisitions.ForCompany(Tenant).Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == dto.Id)
            : null;

        if (requisition is null)
        {
            requisition = new MaterialRequisition
            {
                Reference = await numbering.NextMasterCodeAsync(Db.MaterialRequisitions, "MRQ"),
                ConstructionProjectId = dto.ConstructionProjectId,
                RequestedByUserId = userId,
                RequestedOn = dto.RequestedOn == default ? Today : dto.RequestedOn,
            }.StampNew(Tenant, userId);

            Db.MaterialRequisitions.Add(requisition);
        }
        else
        {
            if (requisition.Status is "Approved" or "Ordered")
                throw new InvalidOperationException($"This requisition is {requisition.Status.ToLowerInvariant()} and cannot be edited.");

            requisition.StampUpdated(userId);
        }

        requisition.WbsNodeId = dto.WbsNodeId;
        requisition.BoqLineId = dto.Id == Guid.Empty ? requisition.BoqLineId : requisition.BoqLineId;
        requisition.RequiredBy = dto.RequiredBy;
        requisition.IsUrgent = dto.IsUrgent;
        requisition.Note = dto.Note;

        if (dto.Lines.Count > 0)
        {
            Db.MaterialRequisitionLines.RemoveRange(requisition.Lines);

            var order = 0;

            foreach (var l in dto.Lines)
            {
                requisition.Lines.Add(new MaterialRequisitionLine
                {
                    ItemId = l.ItemId,
                    Description = l.Description,
                    Uom = l.Uom,
                    RequestedQuantity = l.RequestedQuantity,
                    ApprovedQuantity = l.ApprovedQuantity,
                    EstimatedRate = l.EstimatedRate,
                    WbsNodeId = l.WbsNodeId ?? dto.WbsNodeId,
                    BoqLineId = l.BoqLineId,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        requisition.EstimatedValue = RealEstateMapper.Money(
            requisition.Lines.Sum(l => l.RequestedQuantity * l.EstimatedRate));

        if (requisition.Status == "Draft" && requisition.Lines.Count > 0)
            requisition.Status = "Submitted";

        await Db.SaveChangesAsync();
        return (await MapRequisitionsAsync([requisition]))[0];
    }

    /// <summary>
    /// Approves a requisition against what the BOQ says should be needed. Requesting three times
    /// the theoretical quantity is either a measurement error or an over-order, and either way it
    /// is worth someone looking before the material lands on site.
    /// </summary>
    public async Task<MaterialRequisitionDto> ApproveRequisitionAsync(
        Guid id, ApprovalOutcome outcome, string? comment, Guid userId)
    {
        var requisition = await Db.MaterialRequisitions.ForCompany(Tenant)
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("That requisition does not exist.");

        if (requisition.Status is "Approved" or "Ordered")
            throw new InvalidOperationException("This requisition has already been approved.");

        if (outcome is not (ApprovalOutcome.Approved or ApprovalOutcome.AutoApproved))
        {
            requisition.Status = "Rejected";
            requisition.Note = comment ?? requisition.Note;
            requisition.StampUpdated(userId);

            await Db.SaveChangesAsync();
            return (await MapRequisitionsAsync([requisition]))[0];
        }

        var boqLineIds = requisition.Lines.Where(l => l.BoqLineId.HasValue).Select(l => l.BoqLineId!.Value).Distinct().ToList();

        var norms = boqLineIds.Count == 0
            ? []
            : await Db.MaterialConsumptionNorms.ForCompany(Tenant)
                .Where(n => n.BoqLineId != null && boqLineIds.Contains(n.BoqLineId.Value))
                .ToListAsync();

        var boqLines = boqLineIds.Count == 0
            ? []
            : await Db.BoqLines.ForCompany(Tenant)
                .Where(l => boqLineIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => l);

        var warnings = new List<string>();

        foreach (var line in requisition.Lines)
        {
            if (line.ApprovedQuantity <= 0m) line.ApprovedQuantity = line.RequestedQuantity;

            if (line.BoqLineId is null || !boqLines.TryGetValue(line.BoqLineId.Value, out var boqLine)) continue;

            var norm = norms.FirstOrDefault(n => n.BoqLineId == line.BoqLineId && n.ItemId == line.ItemId);
            if (norm is null || norm.NormQuantity <= 0m) continue;

            var remaining = Math.Max(0m, (boqLine.RemeasuredQuantity ?? boqLine.Quantity) - boqLine.ExecutedQuantity);
            var theoretical = remaining * norm.NormQuantity * (1m + norm.AllowedWastagePercent / 100m);

            if (theoretical > 0m && line.ApprovedQuantity > theoretical * 1.25m)
            {
                warnings.Add(
                    $"{line.Description}: {line.ApprovedQuantity:N2} {line.Uom} requested against a theoretical " +
                    $"{theoretical:N2} for the work remaining on {boqLine.ItemCode}.");
            }

            line.StampUpdated(userId);
        }

        requisition.Status = "Approved";
        requisition.Note = comment ?? requisition.Note;
        requisition.StampUpdated(userId);

        if (warnings.Count > 0)
        {
            await WriteAuditNoteAsync(
                "MaterialRequisition", id, "RequisitionAboveNorm", Guid.Empty, userId,
                amountImpact: requisition.EstimatedValue,
                note: string.Join(" · ", warnings),
                entityReference: requisition.Reference,
                highRisk: true);
        }

        await Db.SaveChangesAsync();
        return (await MapRequisitionsAsync([requisition]))[0];
    }

    public async Task<PaginatedResponse<MaterialRequisitionDto>> GetRequisitionsAsync(
        ListQueryDto query, Guid? constructionProjectId, string? status)
    {
        var q = Db.MaterialRequisitions.ForCompany(Tenant)
            .Include(r => r.Lines)
            .WhereIf(constructionProjectId.HasValue, r => r.ConstructionProjectId == constructionProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(status), r => r.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), r => r.Reference.Contains(query.Search!))
            .OrderByDescending(r => r.IsUrgent).ThenBy(r => r.RequiredBy);

        return await PageAsync(q, query, MapRequisitionsAsync);
    }

    private async Task<List<MaterialRequisitionDto>> MapRequisitionsAsync(List<MaterialRequisition> requisitions)
    {
        if (requisitions.Count == 0) return [];

        var today = Today;
        var currency = await CurrencyAsync();

        var projectIds = requisitions.Select(r => r.ConstructionProjectId).Distinct().ToList();

        var projects = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var nodeIds = requisitions.Where(r => r.WbsNodeId.HasValue).Select(r => r.WbsNodeId!.Value).Distinct().ToList();

        var nodes = nodeIds.Count == 0
            ? []
            : await Db.WbsNodes.ForCompany(Tenant).Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        var requesters = await AgentUserNamesAsync(requisitions.Select(r => (Guid?)r.RequestedByUserId));

        return requisitions.Select(r => new MaterialRequisitionDto
        {
            Id = r.Id,
            Reference = r.Reference,
            ConstructionProjectId = r.ConstructionProjectId,
            ProjectName = projects.GetValueOrDefault(r.ConstructionProjectId, "—"),
            WbsNodeId = r.WbsNodeId,
            WbsName = r.WbsNodeId is null ? null : nodes.GetValueOrDefault(r.WbsNodeId.Value),
            RequestedOn = r.RequestedOn,
            RequiredBy = r.RequiredBy,
            RequestedByName = requesters.GetValueOrDefault(r.RequestedByUserId, "—"),
            Status = r.Status,
            PurchaseRequisitionId = r.PurchaseRequisitionId,
            PurchaseOrderId = r.PurchaseOrderId,
            EstimatedValue = r.EstimatedValue,
            CurrencyCode = currency,
            IsUrgent = r.IsUrgent,

            // Overdue means the site needed it before now, which is a stoppage rather than an
            // admin lapse.
            IsOverdue = r.Status is not ("Received" or "Rejected") && r.RequiredBy < today,

            Note = r.Note,

            Lines = r.Lines.OrderBy(l => l.SortOrder).Select(l => new MaterialRequisitionLineDto
            {
                Id = l.Id,
                ItemId = l.ItemId,
                Description = l.Description ?? string.Empty,
                Uom = l.Uom,
                RequestedQuantity = l.RequestedQuantity,
                ApprovedQuantity = l.ApprovedQuantity,
                ReceivedQuantity = l.ReceivedQuantity,
                EstimatedRate = l.EstimatedRate,
                WbsNodeId = l.WbsNodeId,
                BoqLineId = l.BoqLineId,
                SortOrder = l.SortOrder,
            }).ToList(),
        }).ToList();
    }

    // ═══ Material issues ═════════════════════════════════════════════════════

    /// <summary>
    /// Issues material to the works. Tying the issue to a BOQ line is what makes wastage
    /// measurable later; issuing to a subcontractor who was supposed to supply their own creates
    /// the contra-charge at the same moment, rather than three months later when nobody remembers.
    /// </summary>
    public async Task<MaterialIssueDto> IssueMaterialAsync(MaterialIssueDto dto, Guid userId)
    {
        _ = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var issue = new MaterialIssue
        {
            Reference = await numbering.NextMasterCodeAsync(Db.MaterialIssues, "MIS"),
            ConstructionProjectId = dto.ConstructionProjectId,
            WbsNodeId = dto.WbsNodeId,
            BoqLineId = dto.BoqLineId,
            SubcontractId = dto.SubcontractId,
            IssuedOn = dto.IssuedOn == default ? Today : dto.IssuedOn,
            ItemId = dto.ItemId,
            Description = dto.Description,
            Uom = dto.Uom,
            Quantity = dto.IsReturn ? -Math.Abs(dto.Quantity) : dto.Quantity,
            Rate = dto.Rate,
            IsContraChargeable = dto.IsContraChargeable,
            IsReturn = dto.IsReturn,
            Note = dto.Note,
        }.StampNew(Tenant, userId);

        issue.Value = RealEstateMapper.Money(issue.Quantity * issue.Rate);

        Db.MaterialIssues.Add(issue);
        await Db.SaveChangesAsync();

        if (dto.IsContraChargeable && dto.SubcontractId is not null && !dto.IsReturn)
        {
            var subcontract = await Db.Subcontracts.ForCompany(Tenant)
                .FirstOrDefaultAsync(s => s.Id == dto.SubcontractId);

            if (subcontract is not null)
            {
                var charge = new ContraCharge
                {
                    Reference = await numbering.NextMasterCodeAsync(Db.ContraCharges, "CTR"),
                    SubcontractId = dto.SubcontractId.Value,
                    ContractorId = subcontract.ContractorId,
                    Kind = ContraChargeKind.MaterialIssued,
                    Description = $"{issue.Description} issued on {issue.IssuedOn:dd MMM yyyy} — {issue.Reference}",
                    IncurredOn = issue.IssuedOn,
                    Quantity = issue.Quantity,
                    Uom = issue.Uom,
                    Rate = issue.Rate,
                    Amount = issue.Value,
                    MaterialIssueId = issue.Id,
                    IsAgreed = true,
                }.StampNew(Tenant, userId);

                Db.ContraCharges.Add(charge);
                await Db.SaveChangesAsync();

                issue.ContraChargeId = charge.Id;
            }
        }

        // The WBS node's actual cost moves with the issue, which is what keeps cost-to-complete
        // honest between certificates.
        if (dto.WbsNodeId is not null)
        {
            var node = await Db.WbsNodes.ForCompany(Tenant).FirstOrDefaultAsync(n => n.Id == dto.WbsNodeId);

            if (node is not null)
            {
                node.ActualAmount = RealEstateMapper.Money(node.ActualAmount + issue.Value);
                node.StampUpdated(userId);
            }
        }

        if (dto.BoqLineId is not null)
        {
            var boqLine = await Db.BoqLines.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == dto.BoqLineId);

            if (boqLine is not null)
            {
                boqLine.ActualCost = RealEstateMapper.Money(boqLine.ActualCost + issue.Value);
                boqLine.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();
        return (await MapIssuesAsync([issue]))[0];
    }

    public async Task<PaginatedResponse<MaterialIssueDto>> GetIssuesAsync(ListQueryDto query, Guid? constructionProjectId)
    {
        var q = Db.MaterialIssues.ForCompany(Tenant)
            .WhereIf(constructionProjectId.HasValue, i => i.ConstructionProjectId == constructionProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), i => i.Reference.Contains(query.Search!))
            .WhereIf(query.FromDate.HasValue, i => i.IssuedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, i => i.IssuedOn <= query.ToDate)
            .OrderByDescending(i => i.IssuedOn);

        return await PageAsync(q, query, MapIssuesAsync);
    }

    private async Task<List<MaterialIssueDto>> MapIssuesAsync(List<MaterialIssue> issues)
    {
        if (issues.Count == 0) return [];

        var currency = await CurrencyAsync();

        var nodeIds = issues.Where(i => i.WbsNodeId.HasValue).Select(i => i.WbsNodeId!.Value).Distinct().ToList();

        var nodes = nodeIds.Count == 0
            ? []
            : await Db.WbsNodes.ForCompany(Tenant).Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        var boqLineIds = issues.Where(i => i.BoqLineId.HasValue).Select(i => i.BoqLineId!.Value).Distinct().ToList();

        var boqLines = boqLineIds.Count == 0
            ? []
            : await Db.BoqLines.ForCompany(Tenant).Where(l => boqLineIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => l.ItemCode);

        var subIds = issues.Where(i => i.SubcontractId.HasValue).Select(i => i.SubcontractId!.Value).Distinct().ToList();

        var subcontractors = subIds.Count == 0
            ? []
            : await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => subIds.Contains(s.Id))
                .Join(Db.Contractors.ForCompany(Tenant), s => s.ContractorId, c => c.Id, (s, c) => new { s.Id, c.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        var issuedTo = await AgentUserNamesAsync(issues.Select(i => i.IssuedToUserId));
        var receivedBy = await PartyNamesAsync(issues.Where(i => i.ReceivedByPartyId.HasValue).Select(i => i.ReceivedByPartyId!.Value));

        return issues.Select(i => new MaterialIssueDto
        {
            Id = i.Id,
            Reference = i.Reference,
            ConstructionProjectId = i.ConstructionProjectId,
            WbsNodeId = i.WbsNodeId,
            WbsName = i.WbsNodeId is null ? null : nodes.GetValueOrDefault(i.WbsNodeId.Value),
            BoqLineId = i.BoqLineId,
            BoqItemCode = i.BoqLineId is null ? null : boqLines.GetValueOrDefault(i.BoqLineId.Value),
            SubcontractId = i.SubcontractId,
            ContractorName = i.SubcontractId is null ? null : subcontractors.GetValueOrDefault(i.SubcontractId.Value),
            IssuedOn = i.IssuedOn,
            ItemId = i.ItemId,
            Description = i.Description ?? string.Empty,
            Uom = i.Uom,
            Quantity = i.Quantity,
            Rate = i.Rate,
            Value = i.Value,
            CurrencyCode = currency,
            IssuedToName = i.IssuedToUserId is null ? null : issuedTo.GetValueOrDefault(i.IssuedToUserId.Value),
            ReceivedByName = i.ReceivedByPartyId is null ? null : receivedBy.GetValueOrDefault(i.ReceivedByPartyId.Value),
            IsContraChargeable = i.IsContraChargeable,
            ContraChargeId = i.ContraChargeId,
            IsReturn = i.IsReturn,
            Note = i.Note,
        }).ToList();
    }

    /// <summary>
    /// Theoretical against actual consumption, per material, for the period. The excess above the
    /// trade's allowed wastage is the number worth chasing: on a large concrete frame a two-point
    /// swing is a lot of money that nobody has otherwise accounted for.
    /// </summary>
    public async Task<List<WastageRecordDto>> CalculateWastageAsync(
        Guid constructionProjectId, DateOnly from, DateOnly to, Guid userId)
    {
        var currency = await CurrencyAsync();

        var issues = await Db.MaterialIssues.ForCompany(Tenant)
            .Where(i => i.ConstructionProjectId == constructionProjectId
                     && i.IssuedOn >= from && i.IssuedOn <= to
                     && i.BoqLineId != null)
            .ToListAsync();

        if (issues.Count == 0) return [];

        var boqLineIds = issues.Select(i => i.BoqLineId!.Value).Distinct().ToList();

        var boqLines = await Db.BoqLines.ForCompany(Tenant)
            .Where(l => boqLineIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l);

        var norms = await Db.MaterialConsumptionNorms.ForCompany(Tenant)
            .Where(n => n.BoqLineId != null && boqLineIds.Contains(n.BoqLineId.Value))
            .ToListAsync();

        // Executed quantity for the period, from certified progress rather than from a claim.
        var progress = await Db.ProgressMeasurementLines.ForCompany(Tenant)
            .Where(l => boqLineIds.Contains(l.BoqLineId))
            .Join(Db.ProgressMeasurements.ForCompany(Tenant), l => l.ProgressMeasurementId, m => m.Id, (l, m) => new { l, m })
            .Where(x => x.m.ConstructionProjectId == constructionProjectId
                     && x.m.IsCertified
                     && x.m.PeriodFrom >= from && x.m.PeriodTo <= to)
            .Select(x => new { x.l.BoqLineId, x.l.CertifiedQuantity })
            .ToListAsync();

        var results = new List<WastageRecordDto>();

        var groups = issues
            .Where(i => !i.IsReturn)
            .GroupBy(i => new { i.BoqLineId, i.ItemId, i.Description, i.Uom });

        foreach (var group in groups)
        {
            if (group.Key.BoqLineId is null) continue;
            if (!boqLines.TryGetValue(group.Key.BoqLineId.Value, out var boqLine)) continue;

            var norm = norms.FirstOrDefault(n => n.BoqLineId == group.Key.BoqLineId && n.ItemId == group.Key.ItemId);
            if (norm is null || norm.NormQuantity <= 0m) continue;

            var executed = progress.Where(p => p.BoqLineId == group.Key.BoqLineId).Sum(p => p.CertifiedQuantity);
            if (executed <= 0m) continue;

            var issued = group.Sum(i => i.Quantity);

            var returned = issues
                .Where(i => i.IsReturn && i.BoqLineId == group.Key.BoqLineId && i.ItemId == group.Key.ItemId)
                .Sum(i => Math.Abs(i.Quantity));

            var consumed = issued - returned;
            var theoretical = RealEstateMapper.Money(executed * norm.NormQuantity, 4);

            var wastage = consumed - theoretical;
            var wastagePercent = theoretical > 0m ? RealEstateMapper.Percent(wastage, theoretical) : 0m;
            var excessPercent = Math.Max(0m, wastagePercent - norm.AllowedWastagePercent);

            var rate = group.Average(i => i.Rate);

            var record = new WastageRecord
            {
                ConstructionProjectId = constructionProjectId,
                WbsNodeId = boqLine.WbsNodeId,
                BoqLineId = group.Key.BoqLineId,
                ItemId = group.Key.ItemId,
                MaterialName = group.Key.Description ?? "—",
                Uom = group.Key.Uom,
                PeriodFrom = from,
                PeriodTo = to,
                ExecutedQuantity = executed,
                TheoreticalConsumption = theoretical,
                ActualIssued = issued,
                ReturnedQuantity = returned,
                NetConsumed = consumed,
                WastageQuantity = RealEstateMapper.Money(wastage, 4),
                WastagePercent = wastagePercent,
                AllowedWastagePercent = norm.AllowedWastagePercent,
                ExcessWastagePercent = excessPercent,

                // Only the excess is a loss. The allowance is a real cost of building.
                ExcessValue = RealEstateMapper.Money(theoretical * excessPercent / 100m * rate),
            }.StampNew(Tenant, userId);

            Db.WastageRecords.Add(record);

            results.Add(new WastageRecordDto
            {
                Id = record.Id,
                ConstructionProjectId = constructionProjectId,
                WbsNodeId = record.WbsNodeId,
                BoqLineId = record.BoqLineId,
                BoqItemCode = boqLine.ItemCode,
                MaterialName = record.MaterialName,
                Uom = record.Uom,
                PeriodFrom = from,
                PeriodTo = to,
                ExecutedQuantity = record.ExecutedQuantity,
                TheoreticalConsumption = record.TheoreticalConsumption,
                ActualIssued = record.ActualIssued,
                ReturnedQuantity = record.ReturnedQuantity,
                NetConsumed = record.NetConsumed,
                WastageQuantity = record.WastageQuantity,
                WastagePercent = record.WastagePercent,
                AllowedWastagePercent = record.AllowedWastagePercent,
                ExcessWastagePercent = record.ExcessWastagePercent,
                ExcessValue = record.ExcessValue,
                CurrencyCode = currency,
                IsExcessive = excessPercent > 0m,
            });
        }

        await Db.SaveChangesAsync();
        return results.OrderByDescending(r => r.ExcessValue).ToList();
    }

    // ═══ Labour ══════════════════════════════════════════════════════════════

    public async Task<LabourRecordDto> SaveLabourAsync(LabourRecordDto dto, Guid userId)
    {
        _ = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var record = dto.Id != Guid.Empty
            ? await Db.LabourRecords.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == dto.Id)
            : null;

        if (record is null)
        {
            record = new LabourRecord { ConstructionProjectId = dto.ConstructionProjectId }.StampNew(Tenant, userId);
            Db.LabourRecords.Add(record);
        }
        else record.StampUpdated(userId);

        record.WbsNodeId = dto.WbsNodeId;
        record.SubcontractId = dto.SubcontractId;
        record.WorkDate = dto.WorkDate == default ? Today : dto.WorkDate;
        record.Trade = dto.Trade;
        record.LabourType = dto.LabourType;
        record.HeadCount = dto.HeadCount;
        record.Hours = dto.Hours;
        record.OvertimeHours = dto.OvertimeHours;
        record.DailyRate = dto.DailyRate;
        record.WorkDescription = dto.WorkDescription;
        record.RecordedByUserId = userId;
        record.FromGateAttendance = dto.FromGateAttendance;

        // Overtime at time and a half is the near-universal norm, and paying it flat is one of the
        // commonest ways a labour cost silently understates itself.
        var normalCost = dto.HeadCount * dto.DailyRate;
        var overtimeCost = dto.OvertimeHours > 0m && dto.Hours > 0m
            ? dto.DailyRate / dto.Hours * dto.OvertimeHours * 1.5m * dto.HeadCount
            : 0m;

        record.TotalCost = RealEstateMapper.Money(normalCost + overtimeCost);

        if (dto.SubcontractId is not null) record.ContractorId = await Db.Subcontracts.ForCompany(Tenant)
            .Where(s => s.Id == dto.SubcontractId)
            .Select(s => (Guid?)s.ContractorId)
            .FirstOrDefaultAsync();

        await Db.SaveChangesAsync();

        // Own labour is a project cost. Subcontracted labour is already in the subcontract value,
        // so counting it again would double the forecast.
        if (dto.WbsNodeId is not null && dto.LabourType != "Contractor")
        {
            var node = await Db.WbsNodes.ForCompany(Tenant).FirstOrDefaultAsync(n => n.Id == dto.WbsNodeId);

            if (node is not null)
            {
                node.ActualAmount = RealEstateMapper.Money(node.ActualAmount + record.TotalCost);
                node.StampUpdated(userId);

                await Db.SaveChangesAsync();
            }
        }

        return (await MapLabourAsync([record]))[0];
    }

    public async Task<PaginatedResponse<LabourRecordDto>> GetLabourAsync(ListQueryDto query, Guid? constructionProjectId)
    {
        var q = Db.LabourRecords.ForCompany(Tenant)
            .WhereIf(constructionProjectId.HasValue, l => l.ConstructionProjectId == constructionProjectId)
            .WhereIf(query.FromDate.HasValue, l => l.WorkDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, l => l.WorkDate <= query.ToDate)
            .OrderByDescending(l => l.WorkDate);

        return await PageAsync(q, query, MapLabourAsync);
    }

    private async Task<List<LabourRecordDto>> MapLabourAsync(List<LabourRecord> records)
    {
        if (records.Count == 0) return [];

        var currency = await CurrencyAsync();

        var nodeIds = records.Where(r => r.WbsNodeId.HasValue).Select(r => r.WbsNodeId!.Value).Distinct().ToList();

        var nodes = nodeIds.Count == 0
            ? []
            : await Db.WbsNodes.ForCompany(Tenant).Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        var contractorIds = records.Where(r => r.ContractorId.HasValue).Select(r => r.ContractorId!.Value).Distinct().ToList();

        var contractors = contractorIds.Count == 0
            ? []
            : await Db.Contractors.ForCompany(Tenant).Where(c => contractorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

        var recorders = await AgentUserNamesAsync(records.Select(r => r.RecordedByUserId));

        return records.Select(r => new LabourRecordDto
        {
            Id = r.Id,
            ConstructionProjectId = r.ConstructionProjectId,
            WbsNodeId = r.WbsNodeId,
            WbsName = r.WbsNodeId is null ? null : nodes.GetValueOrDefault(r.WbsNodeId.Value),
            SubcontractId = r.SubcontractId,
            ContractorName = r.ContractorId is null ? null : contractors.GetValueOrDefault(r.ContractorId.Value),
            WorkDate = r.WorkDate,
            Trade = r.Trade,
            LabourType = r.LabourType,
            HeadCount = r.HeadCount,
            Hours = r.Hours,
            OvertimeHours = r.OvertimeHours,
            DailyRate = r.DailyRate,
            TotalCost = r.TotalCost,
            CurrencyCode = currency,
            WorkDescription = r.WorkDescription,
            FromGateAttendance = r.FromGateAttendance,
            RecordedByName = r.RecordedByUserId is null ? null : recorders.GetValueOrDefault(r.RecordedByUserId.Value),
        }).ToList();
    }

    // ═══ Plant ═══════════════════════════════════════════════════════════════

    public async Task<List<PlantItemDto>> GetPlantAsync(string? status)
    {
        var today = Today;

        var plant = await Db.PlantItems.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(status), p => p.Status == status)
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (plant.Count == 0) return [];

        var ids = plant.Select(p => p.Id).ToList();

        var allocations = await Db.PlantAllocations.ForCompany(Tenant)
            .Where(a => ids.Contains(a.PlantItemId) && (a.ToDate == null || a.ToDate >= today))
            .Select(a => new { a.PlantItemId, a.ConstructionProjectId })
            .ToListAsync();

        var projectIds = allocations.Select(a => a.ConstructionProjectId).Distinct().ToList();

        var projects = projectIds.Count == 0
            ? []
            : await Db.ConstructionProjects.ForCompany(Tenant)
                .Where(p => projectIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

        var suppliers = await PartyNamesAsync(plant.Where(p => p.SupplierPartyId.HasValue).Select(p => p.SupplierPartyId!.Value));

        return plant.Select(p =>
        {
            var current = allocations.FirstOrDefault(a => a.PlantItemId == p.Id);

            return new PlantItemDto
            {
                Id = p.Id,
                Name = p.Name,
                AssetCode = p.AssetCode,
                PlantType = p.PlantType,
                Ownership = p.Ownership,
                SupplierName = p.SupplierPartyId is null ? null : suppliers.GetValueOrDefault(p.SupplierPartyId.Value),
                Make = p.Make,
                RegistrationNumber = p.RegistrationNumber,
                Capacity = p.Capacity,
                HourlyRate = p.HourlyRate,
                DailyRate = p.DailyRate,
                MonthlyRate = p.MonthlyRate,
                FuelConsumptionPerHour = p.FuelConsumptionPerHour,
                TotalHoursRun = p.TotalHoursRun,
                UtilisationPercent = p.UtilisationPercent,
                NextServiceDue = p.NextServiceDue,
                Status = p.Status,
                CurrentProjectName = current is null ? null : projects.GetValueOrDefault(current.ConstructionProjectId),
                IsActive = p.IsActive,
            };
        }).ToList();
    }

    public async Task<PlantItemDto> SavePlantAsync(PlantItemDto dto, Guid userId)
    {
        var plant = dto.Id != Guid.Empty
            ? await Db.PlantItems.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (plant is null)
        {
            plant = new PlantItem
            {
                AssetCode = string.IsNullOrWhiteSpace(dto.AssetCode)
                    ? await numbering.NextMasterCodeAsync(Db.PlantItems, "PLT")
                    : dto.AssetCode,
            }.StampNew(Tenant, userId);

            Db.PlantItems.Add(plant);
        }
        else plant.StampUpdated(userId);

        plant.Name = dto.Name;
        plant.PlantType = dto.PlantType;
        plant.Ownership = dto.Ownership;
        plant.Make = dto.Make;
        plant.RegistrationNumber = dto.RegistrationNumber;
        plant.Capacity = dto.Capacity;
        plant.HourlyRate = dto.HourlyRate;
        plant.DailyRate = dto.DailyRate;
        plant.MonthlyRate = dto.MonthlyRate;
        plant.FuelConsumptionPerHour = dto.FuelConsumptionPerHour;
        plant.NextServiceDue = dto.NextServiceDue;
        plant.Status = dto.Status;
        plant.IsActive = dto.IsActive;

        await Db.SaveChangesAsync();
        return (await GetPlantAsync(null)).First(p => p.Id == plant.Id);
    }

    /// <summary>
    /// Allocates plant to a project. Idle hours are recorded separately from working hours because
    /// a machine standing on site is a cost with no output, and the utilisation figure is what
    /// tells a plant manager whether to move it or off-hire it.
    /// </summary>
    public async Task<PlantAllocationDto> AllocatePlantAsync(PlantAllocationDto dto, Guid userId)
    {
        var plant = await RequireAsync<PlantItem>(dto.PlantItemId, "That plant item does not exist.");
        _ = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var today = Today;

        var allocation = dto.Id != Guid.Empty
            ? await Db.PlantAllocations.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.Id)
            : null;

        if (allocation is null)
        {
            // A machine cannot be on two sites at once. The previous allocation is closed rather
            // than overlapped, or two projects each think they have the excavator.
            var current = await Db.PlantAllocations.ForCompany(Tenant)
                .Where(a => a.PlantItemId == dto.PlantItemId && (a.ToDate == null || a.ToDate >= dto.FromDate))
                .ToListAsync();

            foreach (var previous in current.Where(p => p.ConstructionProjectId != dto.ConstructionProjectId))
            {
                previous.ToDate = dto.FromDate.AddDays(-1);
                previous.StampUpdated(userId);
            }

            allocation = new PlantAllocation
            {
                PlantItemId = dto.PlantItemId,
                ConstructionProjectId = dto.ConstructionProjectId,
            }.StampNew(Tenant, userId);

            Db.PlantAllocations.Add(allocation);
        }
        else allocation.StampUpdated(userId);

        allocation.WbsNodeId = dto.WbsNodeId;
        allocation.SubcontractId = dto.SubcontractId;
        allocation.FromDate = dto.FromDate == default ? today : dto.FromDate;
        allocation.ToDate = dto.ToDate;
        allocation.HoursUsed = dto.HoursUsed;
        allocation.IdleHours = dto.IdleHours;
        allocation.Rate = dto.Rate > 0m ? dto.Rate : plant.HourlyRate;
        // The operator is assigned through the HR roster rather than typed on the allocation.
        allocation.IsChargeable = dto.IsChargeable;

        // Idle time is charged too — the machine was there and could not be elsewhere.
        allocation.Cost = RealEstateMapper.Money((dto.HoursUsed + dto.IdleHours) * allocation.Rate);

        allocation.FuelCost = plant.FuelConsumptionPerHour is > 0m
            ? RealEstateMapper.Money(dto.HoursUsed * plant.FuelConsumptionPerHour.Value * dto.FuelCost)
            : dto.FuelCost;

        plant.TotalHoursRun = RealEstateMapper.Money(plant.TotalHoursRun + dto.HoursUsed, 2);

        var totalHours = dto.HoursUsed + dto.IdleHours;
        if (totalHours > 0m) plant.UtilisationPercent = RealEstateMapper.Percent(dto.HoursUsed, totalHours);

        plant.Status = allocation.ToDate is null || allocation.ToDate >= today ? "OnSite" : "Available";
        plant.StampUpdated(userId);

        await Db.SaveChangesAsync();

        // Plant lent to a subcontractor is recovered from their next payment.
        if (dto.IsChargeable && dto.SubcontractId is not null && allocation.ContraChargeId is null && allocation.Cost > 0m)
        {
            var subcontract = await Db.Subcontracts.ForCompany(Tenant)
                .FirstOrDefaultAsync(s => s.Id == dto.SubcontractId);

            if (subcontract is not null)
            {
                var charge = new ContraCharge
                {
                    Reference = await numbering.NextMasterCodeAsync(Db.ContraCharges, "CTR"),
                    SubcontractId = dto.SubcontractId.Value,
                    ContractorId = subcontract.ContractorId,
                    Kind = ContraChargeKind.PlantHire,
                    Description = $"{plant.Name} — {dto.HoursUsed:N1} hours worked, {dto.IdleHours:N1} idle",
                    IncurredOn = allocation.ToDate ?? today,
                    Quantity = totalHours,
                    Uom = "Hours",
                    Rate = allocation.Rate,
                    Amount = allocation.Cost,
                    PlantAllocationId = allocation.Id,
                    IsAgreed = true,
                }.StampNew(Tenant, userId);

                Db.ContraCharges.Add(charge);
                await Db.SaveChangesAsync();

                allocation.ContraChargeId = charge.Id;
                await Db.SaveChangesAsync();
            }
        }

        var projectName = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => p.Id == dto.ConstructionProjectId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync();

        dto.Id = allocation.Id;
        dto.PlantName = plant.Name;
        dto.ProjectName = projectName ?? "—";
        dto.Cost = allocation.Cost;
        dto.FuelCost = allocation.FuelCost;
        dto.ContraChargeId = allocation.ContraChargeId;
        dto.UtilisationPercent = totalHours > 0m ? RealEstateMapper.Percent(dto.HoursUsed, totalHours) : 0m;
        dto.CurrencyCode = await CurrencyAsync();
        return dto;
    }

    // ═══ Site gate ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Material arriving at the site gate. The weighbridge figures are the check: a challan saying
    /// thirty tonnes and a net weight of twenty-six is a short delivery somebody would otherwise
    /// sign for and pay in full.
    /// </summary>
    public async Task<SiteGateEntryDto> RecordSiteGateEntryAsync(SiteGateEntryDto dto, Guid userId)
    {
        _ = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var entry = new SiteGateEntry
        {
            ConstructionProjectId = dto.ConstructionProjectId,
            EnteredAt = dto.EnteredAt == default ? DateTime.UtcNow : dto.EnteredAt,
            EntryType = dto.EntryType,
            VehicleNumber = dto.VehicleNumber?.Trim().ToUpperInvariant(),
            DriverName = dto.DriverName,
            SupplierName = dto.SupplierName,
            ChallanNumber = dto.ChallanNumber,
            PurchaseOrderId = dto.PurchaseOrderId,
            GoodsReceiptId = dto.GoodsReceiptId,
            MaterialDescription = dto.MaterialDescription,
            Quantity = dto.Quantity,
            Uom = dto.Uom,
            GrossWeight = dto.GrossWeight,
            TareWeight = dto.TareWeight,
            RecordedByUserId = userId,
            PhotoUrl = dto.PhotoUrl,
        }.StampNew(Tenant, userId);

        if (dto.GrossWeight is > 0m && dto.TareWeight is > 0m)
        {
            entry.NetWeight = RealEstateMapper.Money(dto.GrossWeight.Value - dto.TareWeight.Value, 3);

            // More than a percent out on a weighbridge is a short delivery, not a rounding
            // difference, and it has to be flagged before the goods receipt is signed.
            if (dto.Quantity is > 0m && Math.Abs(entry.NetWeight.Value - dto.Quantity.Value) > dto.Quantity.Value * 0.01m)
            {
                entry.Discrepancy =
                    $"Challan says {dto.Quantity:N3} {dto.Uom}, weighbridge net is {entry.NetWeight:N3}. " +
                    $"Short by {dto.Quantity.Value - entry.NetWeight.Value:N3}.";
            }
        }

        entry.IsVerified = string.IsNullOrWhiteSpace(entry.Discrepancy);

        Db.SiteGateEntries.Add(entry);
        await Db.SaveChangesAsync();

        var recorders = await AgentUserNamesAsync([userId]);

        dto.Id = entry.Id;
        dto.EnteredAt = entry.EnteredAt;
        dto.NetWeight = entry.NetWeight;
        dto.IsVerified = entry.IsVerified;
        dto.Discrepancy = entry.Discrepancy;
        dto.RecordedByName = recorders.GetValueOrDefault(userId);
        return dto;
    }

    // ═══ Safety ══════════════════════════════════════════════════════════════

    /// <summary>
    /// A safety incident. A reportable one is flagged as reportable whether or not anybody wants
    /// it to be — the threshold is set by law, not by preference, and failing to report is a
    /// separate and worse offence than the incident.
    /// </summary>
    public async Task<SafetyIncidentDto> SaveSafetyIncidentAsync(SafetyIncidentDto dto, Guid userId)
    {
        _ = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var incident = dto.Id != Guid.Empty
            ? await Db.SafetyIncidents.ForCompany(Tenant).FirstOrDefaultAsync(i => i.Id == dto.Id)
            : null;

        if (incident is null)
        {
            incident = new SafetyIncident
            {
                Reference = await numbering.NextMasterCodeAsync(Db.SafetyIncidents, "SAF"),
                ConstructionProjectId = dto.ConstructionProjectId,
                SubcontractId = dto.SubcontractId,
                ReportedByUserId = userId,
            }.StampNew(Tenant, userId);

            Db.SafetyIncidents.Add(incident);
        }
        else incident.StampUpdated(userId);

        incident.OccurredAt = dto.OccurredAt == default ? DateTime.UtcNow : dto.OccurredAt;
        incident.Severity = dto.Severity;
        incident.Description = dto.Description;
        incident.Location = dto.Location;
        incident.PersonsAffected = dto.PersonsAffected;
        incident.InjuredPersonName = dto.InjuredPersonName;
        incident.LostTimeDays = dto.LostTimeDays;
        incident.ImmediateAction = dto.ImmediateAction;
        incident.RootCause = dto.RootCause;
        incident.CorrectiveAction = dto.CorrectiveAction;
        incident.PhotoUrls = dto.PhotoUrls.Count == 0 ? null : string.Join('\n', dto.PhotoUrls);
        incident.Cost = dto.Cost;
        incident.IsReported = dto.IsReported;
        incident.ReportedOn = dto.ReportedOn;
        incident.AuthorityReference = dto.AuthorityReference;

        // Fatality, major injury, or lost time beyond the statutory threshold. Not a judgement
        // call, and not something the operator gets to switch off.
        incident.IsReportableToAuthority = dto.Severity is SafetySeverity.Fatal or SafetySeverity.Major
                                        || dto.LostTimeDays >= 3;

        // Closing an incident without a root cause guarantees it happens again.
        if (dto.IsClosed && string.IsNullOrWhiteSpace(dto.RootCause))
            throw new InvalidOperationException("An incident cannot be closed without a root cause and a corrective action.");

        if (dto.IsClosed && incident.IsReportableToAuthority && !incident.IsReported)
            throw new InvalidOperationException("This incident is reportable to the authority and has not been reported. It cannot be closed.");

        incident.IsClosed = dto.IsClosed;

        await Db.SaveChangesAsync();

        if (incident.IsReportableToAuthority && !incident.IsReported)
        {
            await WriteAuditNoteAsync(
                "SafetyIncident", incident.Id, "ReportableIncidentUnreported", Guid.Empty, userId,
                note: $"{incident.Severity} incident on {incident.OccurredAt:dd MMM yyyy} is reportable and has not yet been reported.",
                entityReference: incident.Reference,
                highRisk: true);

            await Db.SaveChangesAsync();
        }

        return (await MapIncidentsAsync([incident]))[0];
    }

    public async Task<PaginatedResponse<SafetyIncidentDto>> GetSafetyIncidentsAsync(
        ListQueryDto query, Guid? constructionProjectId)
    {
        var q = Db.SafetyIncidents.ForCompany(Tenant)
            .WhereIf(constructionProjectId.HasValue, i => i.ConstructionProjectId == constructionProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), i => i.Reference.Contains(query.Search!))
            .OrderByDescending(i => i.OccurredAt);

        return await PageAsync(q, query, MapIncidentsAsync);
    }

    private async Task<List<SafetyIncidentDto>> MapIncidentsAsync(List<SafetyIncident> incidents)
    {
        if (incidents.Count == 0) return [];

        var projectIds = incidents.Select(i => i.ConstructionProjectId).Distinct().ToList();

        var projects = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var subIds = incidents.Where(i => i.SubcontractId.HasValue).Select(i => i.SubcontractId!.Value).Distinct().ToList();

        var contractors = subIds.Count == 0
            ? []
            : await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => subIds.Contains(s.Id))
                .Join(Db.Contractors.ForCompany(Tenant), s => s.ContractorId, c => c.Id, (s, c) => new { s.Id, c.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        var users = await AgentUserNamesAsync(
            incidents.Select(i => i.ReportedByUserId).Concat(incidents.Select(i => i.InvestigatedByUserId)));

        return incidents.Select(i => new SafetyIncidentDto
        {
            Id = i.Id,
            Reference = i.Reference,
            ConstructionProjectId = i.ConstructionProjectId,
            ProjectName = projects.GetValueOrDefault(i.ConstructionProjectId, "—"),
            SubcontractId = i.SubcontractId,
            ContractorName = i.SubcontractId is null ? null : contractors.GetValueOrDefault(i.SubcontractId.Value),
            OccurredAt = i.OccurredAt,
            Severity = i.Severity,
            Description = i.Description ?? string.Empty,
            Location = i.Location,
            PersonsAffected = i.PersonsAffected,
            InjuredPersonName = i.InjuredPersonName,
            LostTimeDays = i.LostTimeDays,
            ImmediateAction = i.ImmediateAction,
            RootCause = i.RootCause,
            CorrectiveAction = i.CorrectiveAction,
            ReportedByName = i.ReportedByUserId is null ? null : users.GetValueOrDefault(i.ReportedByUserId.Value),
            InvestigatedByName = i.InvestigatedByUserId is null ? null : users.GetValueOrDefault(i.InvestigatedByUserId.Value),
            IsReportableToAuthority = i.IsReportableToAuthority,
            IsReported = i.IsReported,
            ReportedOn = i.ReportedOn,
            AuthorityReference = i.AuthorityReference,
            PhotoUrls = string.IsNullOrWhiteSpace(i.PhotoUrls)
                ? []
                : i.PhotoUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Cost = i.Cost,
            IsClosed = i.IsClosed,
        }).ToList();
    }
}
