using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Construction: the work breakdown, the bill of quantities, rates and specifications.
///
/// The whole discipline rests on one idea — every quantity on site traces back to a priced BOQ
/// line, and every BOQ line traces to a WBS node that carries a budget. Break that chain and
/// nobody can say whether the job is making money until it is finished, which is exactly when it
/// is too late. So the BOQ is versioned rather than edited, a rate carries its own build-up, and
/// a specification is frozen before it becomes a contract.
/// </summary>
public partial class ConstructionService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), IConstructionService
{
    // ═══ Construction projects ═══════════════════════════════════════════════

    public async Task<PaginatedResponse<ConstructionProjectListItemDto>> GetProjectsAsync(
        ListQueryDto query, ProjectStatus? status)
    {
        var q = Db.ConstructionProjects.ForCompany(Tenant)
            .WhereIf(status.HasValue, p => p.Status == status)
            .WhereIf(query.ProjectId.HasValue, p => p.ProjectId == query.ProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), p => p.Name.Contains(query.Search!))
            .OrderBy(p => p.Name);

        return await PageAsync(q, query, MapProjectListAsync);
    }

    private async Task<List<ConstructionProjectListItemDto>> MapProjectListAsync(List<ConstructionProject> projects)
    {
        if (projects.Count == 0) return [];

        var ids = projects.Select(p => p.Id).ToList();
        var names = await ProjectNamesAsync(projects.Select(p => p.ProjectId));
        var managers = await AgentUserNamesAsync(projects.Select(p => p.ProjectManagerUserId));

        var variations = await Db.VariationOrders.ForCompany(Tenant)
            .Where(v => ids.Contains(v.ConstructionProjectId)
                     && v.Status != VariationStatus.Approved
                     && v.Status != VariationStatus.Rejected)
            .GroupBy(v => v.ConstructionProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

        var delays = await Db.DelayEvents.ForCompany(Tenant)
            .Where(d => ids.Contains(d.ConstructionProjectId) && d.EndedOn == null)
            .GroupBy(d => d.ConstructionProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

        return projects.Select(p =>
        {
            var revised = p.RevisedContractValue > 0m ? p.RevisedContractValue : p.ContractValue + p.ApprovedVariations;
            var forecast = p.ForecastFinalCost > 0m ? p.ForecastFinalCost : p.BudgetCost;
            var margin = revised - forecast;

            var slip = p.PlannedCompletionDate is null || p.ForecastCompletionDate is null
                ? (int?)null
                : p.ForecastCompletionDate.Value.DayNumber - p.PlannedCompletionDate.Value.AddDays(p.ExtensionDaysGranted).DayNumber;

            return new ConstructionProjectListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                ProjectId = p.ProjectId,
                ProjectName = p.ProjectId is null ? null : names.GetValueOrDefault(p.ProjectId.Value),
                ClientBuildContractId = p.ClientBuildContractId,
                Status = p.Status,
                StartDate = p.StartDate,
                PlannedCompletionDate = p.PlannedCompletionDate,
                ForecastCompletionDate = p.ForecastCompletionDate,
                ExtensionDaysGranted = p.ExtensionDaysGranted,
                SlipDays = slip,
                ContractValue = p.ContractValue,
                ApprovedVariations = p.ApprovedVariations,
                RevisedContractValue = revised,
                CurrencyCode = p.CurrencyCode,
                BudgetCost = p.BudgetCost,
                CommittedCost = p.CommittedCost,
                ActualCost = p.ActualCost,
                ForecastFinalCost = forecast,
                ForecastMargin = RealEstateMapper.Money(margin),
                MarginPercent = RealEstateMapper.Percent(margin, revised),
                CertifiedValue = p.CertifiedValue,
                ReceivedValue = p.ReceivedValue,
                RetentionHeld = p.RetentionHeld,
                PhysicalProgressPercent = p.PhysicalProgressPercent,
                FinancialProgressPercent = p.FinancialProgressPercent,
                ProjectManagerName = p.ProjectManagerUserId is null ? null : managers.GetValueOrDefault(p.ProjectManagerUserId.Value),
                OpenVariations = variations.GetValueOrDefault(p.Id),
                OpenDelays = delays.GetValueOrDefault(p.Id),

                // At risk means the money or the programme has already gone wrong, not that it
                // might. A project run at a loss is not "amber", it is losing money now.
                IsAtRisk = margin < 0m || slip > 0 || p.ActualCost > p.BudgetCost,
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

    public async Task<ConstructionProjectDetailDto?> GetProjectAsync(Guid id)
    {
        var project = await Db.ConstructionProjects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return null;

        var head = (await MapProjectListAsync([project]))[0];

        var detail = new ConstructionProjectDetailDto
        {
            Id = head.Id,
            Name = head.Name,
            Code = head.Code,
            ProjectId = head.ProjectId,
            ProjectName = head.ProjectName,
            ClientBuildContractId = head.ClientBuildContractId,
            Status = head.Status,
            StartDate = head.StartDate,
            PlannedCompletionDate = head.PlannedCompletionDate,
            ForecastCompletionDate = head.ForecastCompletionDate,
            ExtensionDaysGranted = head.ExtensionDaysGranted,
            SlipDays = head.SlipDays,
            ContractValue = head.ContractValue,
            ApprovedVariations = head.ApprovedVariations,
            RevisedContractValue = head.RevisedContractValue,
            CurrencyCode = head.CurrencyCode,
            BudgetCost = head.BudgetCost,
            CommittedCost = head.CommittedCost,
            ActualCost = head.ActualCost,
            ForecastFinalCost = head.ForecastFinalCost,
            ForecastMargin = head.ForecastMargin,
            MarginPercent = head.MarginPercent,
            CertifiedValue = head.CertifiedValue,
            ReceivedValue = head.ReceivedValue,
            RetentionHeld = head.RetentionHeld,
            PhysicalProgressPercent = head.PhysicalProgressPercent,
            FinancialProgressPercent = head.FinancialProgressPercent,
            ProjectManagerName = head.ProjectManagerName,
            OpenVariations = head.OpenVariations,
            OpenDelays = head.OpenDelays,
            IsAtRisk = head.IsAtRisk,

            PropertyId = project.PropertyId,
            ProgressMethod = project.ProgressMethod,
            ActualCompletionDate = project.ActualCompletionDate,
            InvoicedValue = project.InvoicedValue,
            SiteWarehouseId = project.SiteWarehouseId,
            DefaultRetentionPercent = project.DefaultRetentionPercent,
            RetentionCapPercent = project.RetentionCapPercent,
            DefectsPeriodMonths = project.DefectsPeriodMonths,
        };

        var staff = await AgentUserNamesAsync([project.QuantitySurveyorUserId, project.SiteEngineerUserId]);

        detail.QuantitySurveyorName = project.QuantitySurveyorUserId is null
            ? null : staff.GetValueOrDefault(project.QuantitySurveyorUserId.Value);

        detail.SiteEngineerName = project.SiteEngineerUserId is null
            ? null : staff.GetValueOrDefault(project.SiteEngineerUserId.Value);

        detail.Wbs = await GetWbsAsync(id);

        detail.BillsOfQuantities = await Db.BillsOfQuantities.ForCompany(Tenant)
            .Where(b => b.ConstructionProjectId == id)
            .OrderByDescending(b => b.Version)
            .Select(b => new LookupDto
            {
                Id = b.Id,
                Label = b.Name,
                SubLabel = $"v{b.Version} · {b.BoqType}",
                Code = b.Reference,
                IsActive = b.IsCurrent,
            })
            .ToListAsync();

        detail.Programme = await GetProgrammeAsync(id);

        detail.Subcontracts = await MapSubcontractListAsync(
            await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => s.ConstructionProjectId == id)
                .OrderBy(s => s.Reference)
                .ToListAsync());

        detail.Variations = await MapVariationListAsync(
            await Db.VariationOrders.ForCompany(Tenant)
                .Where(v => v.ConstructionProjectId == id)
                .OrderByDescending(v => v.RaisedOn)
                .Take(50)
                .ToListAsync());

        detail.Certificates = await MapIpcListAsync(
            await Db.InterimPaymentCertificates.ForCompany(Tenant)
                .Where(c => c.ConstructionProjectId == id)
                .OrderByDescending(c => c.IssuedOn)
                .Take(50)
                .ToListAsync());

        var latest = await Db.CostToCompletes.ForCompany(Tenant)
            .Where(c => c.ConstructionProjectId == id && c.WbsNodeId == null)
            .OrderByDescending(c => c.AsOfDate)
            .FirstOrDefaultAsync();

        if (latest is not null) detail.CostToComplete = await MapCostToCompleteAsync(latest);

        return detail;
    }

    public async Task<ConstructionProjectDetailDto> SaveProjectAsync(ConstructionProjectDetailDto dto, Guid userId)
    {
        var project = dto.Id != Guid.Empty
            ? await Db.ConstructionProjects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (project is null)
        {
            project = new ConstructionProject
            {
                Code = string.IsNullOrWhiteSpace(dto.Code)
                    ? await numbering.NextMasterCodeAsync(Db.ConstructionProjects, "CPR")
                    : dto.Code,
            }.StampNew(Tenant, userId);

            Db.ConstructionProjects.Add(project);
        }
        else project.StampUpdated(userId);

        project.Name = dto.Name;
        project.ProjectId = dto.ProjectId;
        project.ClientBuildContractId = dto.ClientBuildContractId;
        project.PropertyId = dto.PropertyId;
        project.Status = dto.Status;
        project.ProgressMethod = dto.ProgressMethod;
        project.StartDate = dto.StartDate;
        project.PlannedCompletionDate = dto.PlannedCompletionDate;
        project.ForecastCompletionDate = dto.ForecastCompletionDate ?? dto.PlannedCompletionDate;
        project.ActualCompletionDate = dto.ActualCompletionDate;
        project.ContractValue = dto.ContractValue;
        project.CurrencyCode = dto.CurrencyCode;
        project.BudgetCost = dto.BudgetCost;
        project.SiteWarehouseId = dto.SiteWarehouseId;
        project.DefaultRetentionPercent = dto.DefaultRetentionPercent;
        project.RetentionCapPercent = dto.RetentionCapPercent;
        project.DefectsPeriodMonths = dto.DefectsPeriodMonths;

        // Revised value is derived. Letting somebody type it lets the contract value and the sum
        // of approved variations disagree, which is the argument at every final account.
        project.RevisedContractValue = RealEstateMapper.Money(project.ContractValue + project.ApprovedVariations);

        await Db.SaveChangesAsync();
        return (await GetProjectAsync(project.Id))!;
    }

    // ═══ Work breakdown structure ════════════════════════════════════════════

    public async Task<List<WbsNodeDto>> GetWbsAsync(Guid constructionProjectId)
    {
        var nodes = await Db.WbsNodes.ForCompany(Tenant)
            .Where(n => n.ConstructionProjectId == constructionProjectId)
            .OrderBy(n => n.Depth).ThenBy(n => n.SortOrder)
            .ToListAsync();

        if (nodes.Count == 0) return [];

        var responsible = await AgentUserNamesAsync(nodes.Select(n => n.ResponsibleUserId));

        var subIds = nodes.Where(n => n.SubcontractId.HasValue).Select(n => n.SubcontractId!.Value).Distinct().ToList();

        var subcontracts = subIds.Count == 0
            ? []
            : await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => subIds.Contains(s.Id))
                .Join(Db.Contractors.ForCompany(Tenant), s => s.ContractorId, c => c.Id, (s, c) => new { s.Id, c.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        var nodeIds = nodes.Where(n => n.ProjectNodeId.HasValue).Select(n => n.ProjectNodeId!.Value).Distinct().ToList();

        var blocks = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant)
                .Where(p => nodeIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

        var map = nodes.ToDictionary(n => n.Id, n => new WbsNodeDto
        {
            Id = n.Id,
            ParentNodeId = n.ParentNodeId,
            Kind = n.Kind,
            Code = n.Code ?? string.Empty,
            Name = n.Name,
            Depth = n.Depth,
            SortOrder = n.SortOrder,
            ProjectNodeId = n.ProjectNodeId,
            BlockName = n.ProjectNodeId is null ? null : blocks.GetValueOrDefault(n.ProjectNodeId.Value),
            BudgetAmount = n.BudgetAmount,
            CommittedAmount = n.CommittedAmount,
            ActualAmount = n.ActualAmount,
            ForecastAmount = n.ForecastAmount > 0m ? n.ForecastAmount : n.CommittedAmount + n.ActualAmount,
            EarnedValue = n.EarnedValue,
            WeightPercent = n.WeightPercent,
            ProgressPercent = n.ProgressPercent,
            ResponsibleName = n.ResponsibleUserId is null ? null : responsible.GetValueOrDefault(n.ResponsibleUserId.Value),
            SubcontractId = n.SubcontractId,
            SubcontractorName = n.SubcontractId is null ? null : subcontracts.GetValueOrDefault(n.SubcontractId.Value),
        });

        foreach (var dto in map.Values)
            dto.VarianceAmount = RealEstateMapper.Money(dto.ForecastAmount - dto.BudgetAmount);

        // Money rolls up the tree so a package total is the sum of what is under it, never a
        // separately-typed number that drifts.
        foreach (var node in nodes.OrderByDescending(n => n.Depth))
        {
            if (node.ParentNodeId is null) continue;
            if (!map.TryGetValue(node.Id, out var child)) continue;
            if (!map.TryGetValue(node.ParentNodeId.Value, out var parent)) continue;

            parent.Children.Add(child);
        }

        foreach (var dto in map.Values.Where(d => d.Children.Count > 0))
        {
            dto.Children = dto.Children.OrderBy(c => c.SortOrder).ThenBy(c => c.Code).ToList();

            dto.BudgetAmount = RealEstateMapper.Money(dto.BudgetAmount + dto.Children.Sum(c => c.BudgetAmount));
            dto.CommittedAmount = RealEstateMapper.Money(dto.CommittedAmount + dto.Children.Sum(c => c.CommittedAmount));
            dto.ActualAmount = RealEstateMapper.Money(dto.ActualAmount + dto.Children.Sum(c => c.ActualAmount));
            dto.ForecastAmount = RealEstateMapper.Money(dto.ForecastAmount + dto.Children.Sum(c => c.ForecastAmount));
            dto.EarnedValue = RealEstateMapper.Money(dto.EarnedValue + dto.Children.Sum(c => c.EarnedValue));
            dto.VarianceAmount = RealEstateMapper.Money(dto.ForecastAmount - dto.BudgetAmount);

            // A parent's progress is its children weighted by their budgets, because a package
            // worth ten times another should move the parent ten times as much.
            var weight = dto.Children.Sum(c => c.BudgetAmount);

            if (weight > 0m)
            {
                dto.ProgressPercent = RealEstateMapper.Money(
                    dto.Children.Sum(c => c.ProgressPercent * c.BudgetAmount) / weight);
            }
        }

        return map.Values
            .Where(d => d.ParentNodeId is null)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Code)
            .ToList();
    }

    public async Task<WbsNodeDto> SaveWbsNodeAsync(WbsNodeUpsertDto dto, Guid userId)
    {
        var node = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.WbsNodes.ForCompany(Tenant).FirstOrDefaultAsync(n => n.Id == dto.Id)
            : null;

        if (node is null)
        {
            node = new WbsNode { ConstructionProjectId = dto.ConstructionProjectId }.StampNew(Tenant, userId);
            Db.WbsNodes.Add(node);
        }
        else
        {
            if (dto.ParentNodeId is not null && await IsWbsDescendantAsync(dto.ParentNodeId.Value, node.Id))
                throw new InvalidOperationException($"{node.Name} cannot sit under one of its own children.");

            node.StampUpdated(userId);
        }

        node.ParentNodeId = dto.ParentNodeId;
        node.Kind = dto.Kind;
        node.Code = dto.Code;
        node.Name = dto.Name;
        node.SortOrder = dto.SortOrder;
        node.ProjectNodeId = dto.ProjectNodeId;
        node.BudgetAmount = dto.BudgetAmount;
        node.WeightPercent = dto.WeightPercent;
        node.ResponsibleUserId = dto.ResponsibleUserId;

        if (node.ParentNodeId is null)
        {
            node.Depth = 0;
            node.Path = node.Code;
        }
        else
        {
            var parent = await Db.WbsNodes.ForCompany(Tenant).FirstOrDefaultAsync(n => n.Id == node.ParentNodeId);
            node.Depth = (parent?.Depth ?? 0) + 1;
            node.Path = parent is null ? node.Code : $"{parent.Path}.{node.Code}";
        }

        await Db.SaveChangesAsync();

        var tree = await GetWbsAsync(dto.ConstructionProjectId);
        return FindWbs(tree, node.Id) ?? throw new InvalidOperationException("The node was saved but could not be reloaded.");
    }

    private static WbsNodeDto? FindWbs(List<WbsNodeDto> nodes, Guid id)
    {
        foreach (var n in nodes)
        {
            if (n.Id == id) return n;

            var hit = FindWbs(n.Children, id);
            if (hit is not null) return hit;
        }

        return null;
    }

    private async Task<bool> IsWbsDescendantAsync(Guid candidateParentId, Guid nodeId)
    {
        var cursor = candidateParentId;

        for (var guard = 0; guard < 32; guard++)
        {
            if (cursor == nodeId) return true;

            var parent = await Db.WbsNodes.ForCompany(Tenant)
                .Where(n => n.Id == cursor)
                .Select(n => n.ParentNodeId)
                .FirstOrDefaultAsync();

            if (parent is null) return false;
            cursor = parent.Value;
        }

        return false;
    }

    public async Task DeleteWbsNodeAsync(Guid id, Guid userId)
    {
        var node = await RequireAsync<WbsNode>(id, "That work-breakdown node does not exist.");

        if (await Db.WbsNodes.ForCompany(Tenant).AnyAsync(n => n.ParentNodeId == id))
            throw new InvalidOperationException($"{node.Name} still has nodes under it. Remove those first.");

        // Deleting a node that BOQ lines or a subcontract point at would orphan the money.
        if (await Db.BoqLines.ForCompany(Tenant).AnyAsync(l => l.WbsNodeId == id))
            throw new InvalidOperationException($"{node.Name} carries BOQ lines and cannot be removed.");

        if (await Db.Subcontracts.ForCompany(Tenant).AnyAsync(s => s.WbsNodeId == id))
            throw new InvalidOperationException($"{node.Name} is a subcontract package and cannot be removed.");

        node.StampDeleted(userId);
        await Db.SaveChangesAsync();
    }

    // ═══ Bill of quantities ══════════════════════════════════════════════════

    public async Task<List<BillOfQuantitiesDto>> GetBoqsAsync(Guid constructionProjectId)
    {
        var boqs = await Db.BillsOfQuantities.ForCompany(Tenant)
            .Where(b => b.ConstructionProjectId == constructionProjectId)
            .OrderByDescending(b => b.Version)
            .ToListAsync();

        var result = new List<BillOfQuantitiesDto>();

        foreach (var boq in boqs) result.Add(await MapBoqAsync(boq, false));

        return result;
    }

    public async Task<BillOfQuantitiesDto?> GetBoqAsync(Guid id)
    {
        var boq = await Db.BillsOfQuantities.ForCompany(Tenant)
            .Include(b => b.Sections)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (boq is null) return null;
        return await MapBoqAsync(boq, true);
    }

    private async Task<BillOfQuantitiesDto> MapBoqAsync(BillOfQuantities boq, bool includeLines)
    {
        var projectName = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => p.Id == boq.ConstructionProjectId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync();

        var preparers = await AgentUserNamesAsync([boq.PreparedByUserId]);

        var lines = await Db.BoqLines.ForCompany(Tenant)
            .Where(l => l.BillOfQuantitiesId == boq.Id)
            .ToListAsync();

        var dto = new BillOfQuantitiesDto
        {
            Id = boq.Id,
            Reference = boq.Reference,
            Name = boq.Name,
            ConstructionProjectId = boq.ConstructionProjectId,
            ProjectName = projectName,
            SubcontractId = boq.SubcontractId,
            TenderId = boq.TenderId,
            Version = boq.Version,
            IsCurrent = boq.IsCurrent,
            BoqType = boq.BoqType,
            TotalAmount = boq.TotalAmount,
            ProvisionalSumsTotal = boq.ProvisionalSumsTotal,
            ContingencyTotal = boq.ContingencyTotal,
            MeasuredTotal = RealEstateMapper.Money(lines.Where(l => l.Kind == BoqLineKind.Measured).Sum(l => l.Amount)),
            CurrencyCode = boq.CurrencyCode,
            PreparedOn = boq.PreparedOn,
            PreparedByName = boq.PreparedByUserId is null ? null : preparers.GetValueOrDefault(boq.PreparedByUserId.Value),
            IsApproved = boq.IsApproved,
            DocumentUrl = boq.DocumentUrl,
            LineCount = lines.Count,

            // Progress on a BOQ is value-weighted, not line-counted: a hundred small items at 100%
            // and one huge one at nothing is not a project that is nearly finished.
            OverallProgressPercent = boq.TotalAmount > 0m
                ? RealEstateMapper.Percent(lines.Sum(l => l.Amount * l.ProgressPercent / 100m), boq.TotalAmount)
                : 0m,
        };

        if (!includeLines) return dto;

        var variationIds = lines.Where(l => l.VariationOrderId.HasValue).Select(l => l.VariationOrderId!.Value).Distinct().ToList();

        var variations = variationIds.Count == 0
            ? []
            : await Db.VariationOrders.ForCompany(Tenant)
                .Where(v => variationIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.VariationNumber);

        var sections = boq.Sections.OrderBy(s => s.SortOrder).ToList();

        var sectionMap = sections.ToDictionary(s => s.Id, s => new BoqSectionDto
        {
            Id = s.Id,
            ParentSectionId = s.ParentSectionId,
            Code = s.Code ?? string.Empty,
            Name = s.Name,
            TotalAmount = s.TotalAmount,
            SortOrder = s.SortOrder,
            WbsNodeId = s.WbsNodeId,
        });

        foreach (var line in lines.OrderBy(l => l.SortOrder))
        {
            var mapped = MapBoqLine(line, variations);

            if (line.BoqSectionId is not null && sectionMap.TryGetValue(line.BoqSectionId.Value, out var section))
                section.Lines.Add(mapped);
        }

        foreach (var section in sectionMap.Values)
        {
            section.ExecutedAmount = RealEstateMapper.Money(section.Lines.Sum(l => l.ExecutedQuantity * l.Rate));
            section.ProgressPercent = RealEstateMapper.Percent(section.ExecutedAmount, section.TotalAmount);
        }

        foreach (var section in sections.Where(s => s.ParentSectionId is not null))
        {
            if (!sectionMap.TryGetValue(section.Id, out var child)) continue;
            if (!sectionMap.TryGetValue(section.ParentSectionId!.Value, out var parent)) continue;

            parent.Children.Add(child);
        }

        dto.Sections = sectionMap.Values.Where(s => s.ParentSectionId is null).OrderBy(s => s.SortOrder).ToList();
        return dto;
    }

    private static BoqLineDto MapBoqLine(BoqLine l, Dictionary<Guid, string> variations)
    {
        var quantity = l.RemeasuredQuantity ?? l.Quantity;
        var margin = l.Rate - l.BudgetCostRate;

        return new BoqLineDto
        {
            Id = l.Id,
            BoqSectionId = l.BoqSectionId,
            WbsNodeId = l.WbsNodeId,
            ItemCode = l.ItemCode,
            Description = l.Description ?? string.Empty,
            Kind = l.Kind,
            Uom = l.Uom,
            Quantity = l.Quantity,
            Rate = l.Rate,
            Amount = l.Amount,
            RemeasuredQuantity = l.RemeasuredQuantity,
            ExecutedQuantity = l.ExecutedQuantity,
            CertifiedQuantity = l.CertifiedQuantity,
            RemainingQuantity = RealEstateMapper.Money(Math.Max(0m, quantity - l.ExecutedQuantity), 4),
            ProgressPercent = l.ProgressPercent,
            BudgetCostRate = l.BudgetCostRate,
            ActualCost = l.ActualCost,

            // Margin per unit is what tells a QS which items to watch. A negative one on a
            // high-quantity item is where a job quietly loses its profit.
            MarginPerUnit = RealEstateMapper.Money(margin, 4),
            MarginPercent = RealEstateMapper.Percent(margin, l.Rate),

            RateAnalysisId = l.RateAnalysisId,
            IsVariation = l.IsVariation,
            VariationOrderId = l.VariationOrderId,
            VariationNumber = l.VariationOrderId is null ? null : variations.GetValueOrDefault(l.VariationOrderId.Value),
            SortOrder = l.SortOrder,
            Note = l.Note,
        };
    }

    public async Task<BillOfQuantitiesDto> SaveBoqAsync(BillOfQuantitiesDto dto, Guid userId)
    {
        var boq = dto.Id != Guid.Empty
            ? await Db.BillsOfQuantities.ForCompany(Tenant).Include(b => b.Sections).FirstOrDefaultAsync(b => b.Id == dto.Id)
            : null;

        if (boq is null)
        {
            // A new version supersedes rather than replaces. The signed BOQ has to stay readable
            // for the life of the final account, however many times it is re-measured.
            var previous = await Db.BillsOfQuantities.ForCompany(Tenant)
                .Where(b => b.ConstructionProjectId == dto.ConstructionProjectId
                         && b.BoqType == dto.BoqType
                         && b.SubcontractId == dto.SubcontractId
                         && b.IsCurrent)
                .ToListAsync();

            foreach (var old in previous)
            {
                old.IsCurrent = false;
                old.StampUpdated(userId);
            }

            boq = new BillOfQuantities
            {
                Reference = string.IsNullOrWhiteSpace(dto.Reference)
                    ? await numbering.NextMasterCodeAsync(Db.BillsOfQuantities, "BOQ")
                    : dto.Reference,
                ConstructionProjectId = dto.ConstructionProjectId,
                SubcontractId = dto.SubcontractId,
                TenderId = dto.TenderId,
                Version = previous.Count == 0 ? 1 : previous.Max(p => p.Version) + 1,
                SupersedesBoqId = previous.OrderByDescending(p => p.Version).FirstOrDefault()?.Id,
                IsCurrent = true,
                PreparedByUserId = userId,
                PreparedOn = Today,
            }.StampNew(Tenant, userId);

            Db.BillsOfQuantities.Add(boq);
        }
        else
        {
            if (boq.IsApproved)
                throw new InvalidOperationException("This bill of quantities is approved. Create a new version rather than editing it.");

            boq.StampUpdated(userId);
        }

        boq.Name = dto.Name;
        boq.BoqType = dto.BoqType;
        boq.CurrencyCode = dto.CurrencyCode;
        boq.DocumentUrl = dto.DocumentUrl;
        boq.IsApproved = dto.IsApproved;

        if (dto.Sections.Count > 0)
        {
            Db.BoqSections.RemoveRange(boq.Sections);

            var order = 0;

            foreach (var s in Flatten(dto.Sections))
            {
                boq.Sections.Add(new BoqSection
                {
                    Code = s.Code,
                    Name = s.Name,
                    TotalAmount = s.TotalAmount,
                    SortOrder = order += 10,
                    WbsNodeId = s.WbsNodeId,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        await RecalculateBoqTotalsAsync(boq.Id, userId);

        return (await GetBoqAsync(boq.Id))!;
    }

    private static IEnumerable<BoqSectionDto> Flatten(List<BoqSectionDto> sections)
    {
        foreach (var section in sections)
        {
            yield return section;

            foreach (var child in Flatten(section.Children)) yield return child;
        }
    }

    public async Task<BoqLineDto> SaveBoqLineAsync(BoqLineUpsertDto dto, Guid userId)
    {
        var boq = await RequireAsync<BillOfQuantities>(dto.BillOfQuantitiesId, "That bill of quantities does not exist.");

        if (boq.IsApproved)
            throw new InvalidOperationException("This bill of quantities is approved. Raise a variation rather than editing a line.");

        var line = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.BoqLines.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == dto.Id)
            : null;

        if (line is null)
        {
            line = new BoqLine { BillOfQuantitiesId = dto.BillOfQuantitiesId }.StampNew(Tenant, userId);
            Db.BoqLines.Add(line);
        }
        else
        {
            // Once quantity has been certified against a line, its rate is contractually fixed.
            // Changing it retrospectively rewrites what a contractor has already been paid.
            if (line.CertifiedQuantity > 0m && line.Rate != dto.Rate)
                throw new InvalidOperationException($"{line.ItemCode} already has certified quantity. Its rate cannot be changed — raise a variation.");

            line.StampUpdated(userId);
        }

        line.BoqSectionId = dto.BoqSectionId;
        line.WbsNodeId = dto.WbsNodeId;
        line.ItemCode = dto.ItemCode;
        line.Description = dto.Description;
        line.Kind = dto.Kind;
        line.Uom = dto.Uom;
        line.Quantity = dto.Quantity;
        line.Rate = dto.Rate;
        line.Amount = RealEstateMapper.Money(dto.Quantity * dto.Rate);
        line.RateAnalysisId = dto.RateAnalysisId;
        line.ItemId = dto.ItemId;
        line.SortOrder = dto.SortOrder;
        line.Note = dto.Note;

        // The cost rate comes from the rate analysis where there is one, so the margin on the
        // line is real rather than a number somebody guessed at.
        if (dto.RateAnalysisId is not null && dto.BudgetCostRate <= 0m)
        {
            line.BudgetCostRate = await Db.RateAnalysises.ForCompany(Tenant)
                .Where(r => r.Id == dto.RateAnalysisId)
                .Select(r => r.SubtotalCost)
                .FirstOrDefaultAsync();
        }
        else line.BudgetCostRate = dto.BudgetCostRate;

        await Db.SaveChangesAsync();
        await RecalculateBoqTotalsAsync(dto.BillOfQuantitiesId, userId);

        return MapBoqLine(line, []);
    }

    public async Task<BillOfQuantitiesDto> ImportBoqLinesAsync(Guid boqId, List<BoqLineUpsertDto> lines, Guid userId)
    {
        var boq = await RequireAsync<BillOfQuantities>(boqId, "That bill of quantities does not exist.");

        if (boq.IsApproved)
            throw new InvalidOperationException("This bill of quantities is approved and cannot be re-imported.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var existing = await Db.BoqLines.ForCompany(Tenant)
            .Where(l => l.BillOfQuantitiesId == boqId)
            .ToListAsync();

        // An import matches on item code, so re-importing a corrected spreadsheet updates rather
        // than doubling the bill — which is what happens when a QS fixes one row and re-uploads.
        var order = 0;
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dto in lines)
        {
            if (!codes.Add(dto.ItemCode))
                throw new InvalidOperationException($"Item code {dto.ItemCode} appears twice in the import.");

            var line = existing.FirstOrDefault(l => l.ItemCode.Equals(dto.ItemCode, StringComparison.OrdinalIgnoreCase));

            if (line is null)
            {
                line = new BoqLine { BillOfQuantitiesId = boqId, ItemCode = dto.ItemCode }.StampNew(Tenant, userId);
                Db.BoqLines.Add(line);
            }
            else
            {
                if (line.CertifiedQuantity > 0m && line.Rate != dto.Rate)
                    throw new InvalidOperationException($"{line.ItemCode} has certified quantity at {line.Rate:N2}. The import would change its rate.");

                line.StampUpdated(userId);
            }

            line.BoqSectionId = dto.BoqSectionId;
            line.WbsNodeId = dto.WbsNodeId;
            line.Description = dto.Description;
            line.Kind = dto.Kind;
            line.Uom = dto.Uom;
            line.Quantity = dto.Quantity;
            line.Rate = dto.Rate;
            line.Amount = RealEstateMapper.Money(dto.Quantity * dto.Rate);
            line.BudgetCostRate = dto.BudgetCostRate;
            line.RateAnalysisId = dto.RateAnalysisId;
            line.ItemId = dto.ItemId;
            line.SortOrder = order += 10;
            line.Note = dto.Note;
        }

        await Db.SaveChangesAsync();
        await RecalculateBoqTotalsAsync(boqId, userId);
        await transaction.CommitAsync();

        return (await GetBoqAsync(boqId))!;
    }

    private async Task RecalculateBoqTotalsAsync(Guid boqId, Guid userId)
    {
        var boq = await Db.BillsOfQuantities.ForCompany(Tenant)
            .Include(b => b.Sections)
            .FirstOrDefaultAsync(b => b.Id == boqId);

        if (boq is null) return;

        var lines = await Db.BoqLines.ForCompany(Tenant)
            .Where(l => l.BillOfQuantitiesId == boqId)
            .Select(l => new { l.BoqSectionId, l.Kind, l.Amount })
            .ToListAsync();

        boq.TotalAmount = RealEstateMapper.Money(lines.Sum(l => l.Amount));

        // Provisional sums and contingency are not measured work, so they are shown separately —
        // a contract value that quietly includes them overstates what is actually priced.
        boq.ProvisionalSumsTotal = RealEstateMapper.Money(
            lines.Where(l => l.Kind == BoqLineKind.ProvisionalSum).Sum(l => l.Amount));

        boq.ContingencyTotal = RealEstateMapper.Money(
            lines.Where(l => l.Kind == BoqLineKind.Contingency).Sum(l => l.Amount));

        foreach (var section in boq.Sections)
        {
            section.TotalAmount = RealEstateMapper.Money(
                lines.Where(l => l.BoqSectionId == section.Id).Sum(l => l.Amount));

            section.StampUpdated(userId);
        }

        boq.StampUpdated(userId);
        await Db.SaveChangesAsync();
    }

    // ═══ Rate analysis ═══════════════════════════════════════════════════════

    public async Task<List<RateAnalysisDto>> GetRateAnalysesAsync(Guid? constructionProjectId, bool libraryOnly)
    {
        var analyses = await Db.RateAnalysises.ForCompany(Tenant)
            .Include(r => r.Components)
            .WhereIf(libraryOnly, r => r.IsLibraryItem)
            .WhereIf(constructionProjectId.HasValue && !libraryOnly,
                r => r.ConstructionProjectId == constructionProjectId || r.IsLibraryItem)
            .OrderBy(r => r.Code)
            .ToListAsync();

        if (analyses.Count == 0) return [];

        var currency = await CurrencyAsync();
        var ids = analyses.Select(r => r.Id).ToList();

        var usage = await Db.BoqLines.ForCompany(Tenant)
            .Where(l => l.RateAnalysisId != null && ids.Contains(l.RateAnalysisId.Value))
            .GroupBy(l => l.RateAnalysisId!.Value)
            .Select(g => new { RateId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RateId, x => x.Count);

        return analyses.Select(r => new RateAnalysisDto
        {
            Id = r.Id,
            Code = r.Code ?? string.Empty,
            Description = r.Description ?? string.Empty,
            Uom = r.Uom,
            ConstructionProjectId = r.ConstructionProjectId,
            OutputQuantity = r.OutputQuantity,
            MaterialCost = r.MaterialCost,
            LabourCost = r.LabourCost,
            PlantCost = r.PlantCost,
            SubtotalCost = r.SubtotalCost,
            OverheadPercent = r.OverheadPercent,
            ProfitPercent = r.ProfitPercent,
            FinalRate = r.FinalRate,
            CurrencyCode = currency,
            PricedOn = r.PricedOn,
            IsLibraryItem = r.IsLibraryItem,
            IsActive = r.IsActive,
            UsageCount = usage.GetValueOrDefault(r.Id),

            Components = r.Components.OrderBy(c => c.SortOrder).Select(c => new RateComponentDto
            {
                Id = c.Id,
                ComponentType = c.ComponentType,
                Description = c.Description ?? string.Empty,
                ItemId = c.ItemId,
                Uom = c.Uom,
                Quantity = c.Quantity,
                Rate = c.Rate,
                Amount = c.Amount,
                WastagePercent = c.WastagePercent,
                SortOrder = c.SortOrder,
            }).ToList(),
        }).ToList();
    }

    /// <summary>
    /// A rate built from its components rather than typed. The total is always the sum of the
    /// build-up plus overhead and profit, so a rate can be defended line by line in a negotiation
    /// instead of being a number nobody can explain.
    /// </summary>
    public async Task<RateAnalysisDto> SaveRateAnalysisAsync(RateAnalysisDto dto, Guid userId)
    {
        var analysis = dto.Id != Guid.Empty
            ? await Db.RateAnalysises.ForCompany(Tenant).Include(r => r.Components).FirstOrDefaultAsync(r => r.Id == dto.Id)
            : null;

        if (analysis is null)
        {
            analysis = new RateAnalysis
            {
                Code = string.IsNullOrWhiteSpace(dto.Code)
                    ? await numbering.NextMasterCodeAsync(Db.RateAnalysises, "RAT")
                    : dto.Code,
            }.StampNew(Tenant, userId);

            Db.RateAnalysises.Add(analysis);
        }
        else analysis.StampUpdated(userId);

        analysis.Description = dto.Description;
        analysis.Uom = dto.Uom;
        analysis.ConstructionProjectId = dto.ConstructionProjectId;
        analysis.OutputQuantity = dto.OutputQuantity <= 0m ? 1m : dto.OutputQuantity;
        analysis.OverheadPercent = dto.OverheadPercent;
        analysis.ProfitPercent = dto.ProfitPercent;
        analysis.PricedOn = dto.PricedOn ?? Today;
        analysis.IsLibraryItem = dto.IsLibraryItem;
        analysis.IsActive = dto.IsActive;

        if (dto.Components.Count > 0)
        {
            Db.RateComponents.RemoveRange(analysis.Components);

            var order = 0;

            foreach (var c in dto.Components)
            {
                // Wastage is part of the material cost, not a separate line an estimator
                // remembers to add. Ten bags of cement means eleven bags bought.
                var quantity = c.Quantity * (1m + c.WastagePercent / 100m);

                analysis.Components.Add(new RateComponent
                {
                    ComponentType = c.ComponentType,
                    Description = c.Description,
                    ItemId = c.ItemId,
                    Uom = c.Uom,
                    Quantity = c.Quantity,
                    Rate = c.Rate,
                    Amount = RealEstateMapper.Money(quantity * c.Rate, 4),
                    WastagePercent = c.WastagePercent,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        analysis.MaterialCost = RealEstateMapper.Money(analysis.Components.Where(c => c.ComponentType == "Material").Sum(c => c.Amount), 4);
        analysis.LabourCost = RealEstateMapper.Money(analysis.Components.Where(c => c.ComponentType == "Labour").Sum(c => c.Amount), 4);
        analysis.PlantCost = RealEstateMapper.Money(analysis.Components.Where(c => c.ComponentType == "Plant").Sum(c => c.Amount), 4);

        var direct = analysis.Components.Sum(c => c.Amount);

        analysis.SubtotalCost = RealEstateMapper.Money(direct / analysis.OutputQuantity, 4);

        analysis.FinalRate = RealEstateMapper.Money(
            analysis.SubtotalCost * (1m + analysis.OverheadPercent / 100m) * (1m + analysis.ProfitPercent / 100m), 4);

        await Db.SaveChangesAsync();
        return (await GetRateAnalysesAsync(analysis.ConstructionProjectId, false)).First(r => r.Id == analysis.Id);
    }

    // ═══ Estimates ═══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<EstimateDto>> GetEstimatesAsync(ListQueryDto query)
    {
        var q = Db.Estimates.ForCompany(Tenant)
            .Include(e => e.Lines)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                e => e.Reference.Contains(query.Search!) || e.Name.Contains(query.Search!))
            .OrderByDescending(e => e.PreparedOn);

        return await PageAsync(q, query, MapEstimatesAsync);
    }

    private async Task<List<EstimateDto>> MapEstimatesAsync(List<Estimate> estimates)
    {
        if (estimates.Count == 0) return [];

        var today = Today;
        var unit = await AreaUnitAsync();
        var currency = await CurrencyAsync();
        var names = await PartyNamesAsync(estimates.Where(e => e.PartyId.HasValue).Select(e => e.PartyId!.Value));

        return estimates.Select(e => new EstimateDto
        {
            Id = e.Id,
            Reference = e.Reference,
            Name = e.Name,
            ConstructionProjectId = e.ConstructionProjectId,
            ClientBuildContractId = e.ClientBuildContractId,
            EnquiryId = e.EnquiryId,
            PartyId = e.PartyId,
            ClientName = e.PartyId is null ? null : names.GetValueOrDefault(e.PartyId.Value),
            Version = e.Version,
            PreparedOn = e.PreparedOn,
            ValidUntil = e.ValidUntil,
            IsExpired = e.ValidUntil is not null && e.ValidUntil < today,
            DirectCost = e.DirectCost,
            OverheadPercent = e.OverheadPercent,
            ProfitPercent = e.ProfitPercent,
            ContingencyPercent = e.ContingencyPercent,
            TotalAmount = e.TotalAmount,
            Area = RealEstateMapper.AreaOrNull(e.AreaSqFt, unit),
            RatePerSqFt = e.RatePerSqFt,
            CurrencyCode = currency,
            Grade = e.Grade,
            Status = e.Status,
            DocumentUrl = e.DocumentUrl,
            Assumptions = e.Assumptions,
            Exclusions = e.Exclusions,

            Lines = e.Lines.OrderBy(l => l.SortOrder).Select(l => new EstimateLineDto
            {
                Id = l.Id,
                Description = l.Description ?? string.Empty,
                Uom = l.Uom,
                Quantity = l.Quantity,
                Rate = l.Rate,
                Amount = l.Amount,
                RateAnalysisId = l.RateAnalysisId,
                WbsNodeId = l.WbsNodeId,
                SortOrder = l.SortOrder,
            }).ToList(),
        }).ToList();
    }

    public async Task<EstimateDto> SaveEstimateAsync(EstimateDto dto, Guid userId)
    {
        var estimate = dto.Id != Guid.Empty
            ? await Db.Estimates.ForCompany(Tenant).Include(e => e.Lines).FirstOrDefaultAsync(e => e.Id == dto.Id)
            : null;

        if (estimate is null)
        {
            // A revised estimate is a new version, because the client has already seen the old one
            // and will ask what changed.
            var previous = string.IsNullOrWhiteSpace(dto.Reference)
                ? []
                : await Db.Estimates.ForCompany(Tenant)
                    .Where(e => e.Reference == dto.Reference)
                    .ToListAsync();

            estimate = new Estimate
            {
                Reference = string.IsNullOrWhiteSpace(dto.Reference)
                    ? await numbering.NextMasterCodeAsync(Db.Estimates, "EST")
                    : dto.Reference,
                Version = previous.Count == 0 ? 1 : previous.Max(p => p.Version) + 1,
                PartyId = dto.PartyId,
                EnquiryId = dto.EnquiryId,
            }.StampNew(Tenant, userId);

            Db.Estimates.Add(estimate);
        }
        else
        {
            if (estimate.Status is "Accepted" or "Converted")
                throw new InvalidOperationException("This estimate has been accepted. Create a revision instead.");

            estimate.StampUpdated(userId);
        }

        estimate.Name = dto.Name;
        estimate.ConstructionProjectId = dto.ConstructionProjectId;
        estimate.ClientBuildContractId = dto.ClientBuildContractId;
        estimate.PreparedOn = dto.PreparedOn == default ? Today : dto.PreparedOn;
        estimate.ValidUntil = dto.ValidUntil ?? dto.PreparedOn.AddDays(30);
        estimate.OverheadPercent = dto.OverheadPercent;
        estimate.ProfitPercent = dto.ProfitPercent;
        estimate.ContingencyPercent = dto.ContingencyPercent;
        estimate.AreaSqFt = dto.Area is null ? null : RealEstateMapper.ToSquareFeet(dto.Area.DisplayValue, dto.Area.DisplayUnit);
        estimate.Grade = dto.Grade;
        estimate.Status = dto.Status;
        estimate.DocumentUrl = dto.DocumentUrl;
        estimate.Assumptions = dto.Assumptions;
        estimate.Exclusions = dto.Exclusions;

        if (dto.Lines.Count > 0)
        {
            Db.EstimateLines.RemoveRange(estimate.Lines);

            var order = 0;

            foreach (var l in dto.Lines)
            {
                estimate.Lines.Add(new EstimateLine
                {
                    Description = l.Description,
                    Uom = l.Uom,
                    Quantity = l.Quantity,
                    Rate = l.Rate,
                    Amount = RealEstateMapper.Money(l.Quantity * l.Rate),
                    RateAnalysisId = l.RateAnalysisId,
                    WbsNodeId = l.WbsNodeId,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        estimate.DirectCost = RealEstateMapper.Money(estimate.Lines.Sum(l => l.Amount));

        // Overhead, profit and contingency compound in that order — applying them all to the
        // direct cost understates the price by several per cent on a large job.
        var withOverhead = estimate.DirectCost * (1m + estimate.OverheadPercent / 100m);
        var withProfit = withOverhead * (1m + estimate.ProfitPercent / 100m);

        estimate.TotalAmount = RealEstateMapper.Money(withProfit * (1m + estimate.ContingencyPercent / 100m));

        if (estimate.AreaSqFt is > 0m)
            estimate.RatePerSqFt = RealEstateMapper.Money(estimate.TotalAmount / estimate.AreaSqFt.Value);

        await Db.SaveChangesAsync();
        return (await MapEstimatesAsync([estimate]))[0];
    }

    /// <summary>
    /// Turns a won estimate into the project's contract BOQ. The estimate's own lines become
    /// priced items, so what was quoted and what is being built are the same document rather than
    /// two spreadsheets that diverge from week one.
    /// </summary>
    public async Task<BillOfQuantitiesDto> ConvertEstimateToBoqAsync(Guid estimateId, Guid constructionProjectId, Guid userId)
    {
        var estimate = await Db.Estimates.ForCompany(Tenant)
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == estimateId)
            ?? throw new InvalidOperationException("That estimate does not exist.");

        if (estimate.BillOfQuantitiesId is not null)
            throw new InvalidOperationException("This estimate has already been converted.");

        _ = await RequireAsync<ConstructionProject>(constructionProjectId, "That construction project does not exist.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var boq = new BillOfQuantities
        {
            Reference = await numbering.NextMasterCodeAsync(Db.BillsOfQuantities, "BOQ"),
            Name = $"{estimate.Name} — contract bill",
            ConstructionProjectId = constructionProjectId,
            Version = 1,
            IsCurrent = true,
            BoqType = "Contract",
            CurrencyCode = await CurrencyAsync(),
            PreparedOn = Today,
            PreparedByUserId = userId,
        }.StampNew(Tenant, userId);

        Db.BillsOfQuantities.Add(boq);
        await Db.SaveChangesAsync();

        var order = 0;
        var sequence = 1;

        var rateIds = estimate.Lines.Where(l => l.RateAnalysisId.HasValue)
            .Select(l => l.RateAnalysisId!.Value).Distinct().ToList();

        var rates = rateIds.Count == 0
            ? []
            : await Db.RateAnalysises.ForCompany(Tenant)
                .Where(r => rateIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.SubtotalCost);

        foreach (var line in estimate.Lines.OrderBy(l => l.SortOrder))
        {
            Db.BoqLines.Add(new BoqLine
            {
                BillOfQuantitiesId = boq.Id,
                WbsNodeId = line.WbsNodeId,
                ItemCode = $"{sequence++:D4}",
                Description = line.Description,
                Kind = BoqLineKind.Measured,
                Uom = line.Uom ?? "Item",
                Quantity = line.Quantity,
                Rate = line.Rate,
                Amount = line.Amount,
                RateAnalysisId = line.RateAnalysisId,

                // The cost rate carries across so the margin on every item survives the conversion.
                BudgetCostRate = line.RateAnalysisId is null ? 0m : rates.GetValueOrDefault(line.RateAnalysisId.Value),
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));
        }

        estimate.BillOfQuantitiesId = boq.Id;
        estimate.Status = "Converted";
        estimate.ConstructionProjectId = constructionProjectId;
        estimate.StampUpdated(userId);

        await Db.SaveChangesAsync();
        await RecalculateBoqTotalsAsync(boq.Id, userId);
        await transaction.CommitAsync();

        return (await GetBoqAsync(boq.Id))!;
    }

    // ═══ Specifications ══════════════════════════════════════════════════════

    public async Task<List<SpecificationScheduleDto>> GetSpecificationsAsync(
        Guid? constructionProjectId, Guid? clientBuildContractId)
    {
        var schedules = await Db.SpecificationSchedules.ForCompany(Tenant)
            .Include(s => s.Items)
            .WhereIf(constructionProjectId.HasValue, s => s.ConstructionProjectId == constructionProjectId)
            .WhereIf(clientBuildContractId.HasValue, s => s.ClientBuildContractId == clientBuildContractId)
            .OrderByDescending(s => s.Version)
            .ToListAsync();

        if (schedules.Count == 0) return [];

        var names = await PartyNamesAsync(schedules.Where(s => s.ApprovedByPartyId.HasValue).Select(s => s.ApprovedByPartyId!.Value));

        return schedules.Select(s => new SpecificationScheduleDto
        {
            Id = s.Id,
            Name = s.Name,
            ConstructionProjectId = s.ConstructionProjectId,
            ClientBuildContractId = s.ClientBuildContractId,
            ProjectId = s.ProjectId,
            UnitTypeCode = s.UnitTypeCode,
            Grade = s.Grade,
            Version = s.Version,
            IsFrozen = s.IsFrozen,
            FrozenOn = s.FrozenOn,
            ApprovedByName = s.ApprovedByPartyId is null ? null : names.GetValueOrDefault(s.ApprovedByPartyId.Value),
            DocumentUrl = s.DocumentUrl,
            ItemCount = s.Items.Count,
            ClientSelectableCount = s.Items.Count(i => i.IsClientSelectable),

            // What the client still has to choose. Every one of these is a decision the site is
            // waiting on, and the count is the honest measure of how ready the job is to proceed.
            PendingSelectionCount = s.Items.Count(i => i.IsClientSelectable && string.IsNullOrWhiteSpace(i.SelectedOption)),

            Items = s.Items.OrderBy(i => i.SortOrder).Select(i => new SpecificationItemDto
            {
                Id = i.Id,
                Category = i.Category,
                Location = i.Location,
                ItemName = i.ItemName,
                Specification = i.Specification,
                Brand = i.Brand,
                ModelOrCode = i.ModelOrCode,
                AllowanceRate = i.AllowanceRate,
                Uom = i.Uom,
                IsClientSelectable = i.IsClientSelectable,
                SelectedOption = i.SelectedOption,
                UpgradeCost = i.UpgradeCost,
                ClientVariationId = i.ClientVariationId,
                IsClientSupplied = i.IsClientSupplied,
                SortOrder = i.SortOrder,
            }).ToList(),
        }).ToList();
    }

    public async Task<SpecificationScheduleDto> SaveSpecificationAsync(SpecificationScheduleDto dto, Guid userId)
    {
        var schedule = dto.Id != Guid.Empty
            ? await Db.SpecificationSchedules.ForCompany(Tenant).Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == dto.Id)
            : null;

        if (schedule is null)
        {
            schedule = new SpecificationSchedule
            {
                ConstructionProjectId = dto.ConstructionProjectId,
                ClientBuildContractId = dto.ClientBuildContractId,
                ProjectId = dto.ProjectId,
            }.StampNew(Tenant, userId);

            Db.SpecificationSchedules.Add(schedule);
        }
        else
        {
            // A frozen specification is what the contract was priced against. Changing it is a
            // variation, not an edit.
            if (schedule.IsFrozen)
                throw new InvalidOperationException($"This specification was frozen on {schedule.FrozenOn:dd MMM yyyy}. Raise a variation to change it.");

            schedule.StampUpdated(userId);
        }

        schedule.Name = dto.Name;
        schedule.UnitTypeCode = dto.UnitTypeCode;
        schedule.Grade = dto.Grade;
        schedule.DocumentUrl = dto.DocumentUrl;

        if (dto.Items.Count > 0)
        {
            Db.SpecificationItems.RemoveRange(schedule.Items);

            var order = 0;

            foreach (var i in dto.Items)
            {
                schedule.Items.Add(new SpecificationItem
                {
                    Category = i.Category,
                    Location = i.Location,
                    ItemName = i.ItemName,
                    Specification = i.Specification,
                    Brand = i.Brand,
                    ModelOrCode = i.ModelOrCode,
                    AllowanceRate = i.AllowanceRate,
                    Uom = i.Uom,
                    IsClientSelectable = i.IsClientSelectable,
                    SelectedOption = i.SelectedOption,
                    UpgradeCost = i.UpgradeCost,
                    IsClientSupplied = i.IsClientSupplied,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        return (await GetSpecificationsAsync(schedule.ConstructionProjectId, schedule.ClientBuildContractId))
            .First(s => s.Id == schedule.Id);
    }

    /// <summary>
    /// Freezes the specification. After this every change is a priced variation, which is the
    /// only way a turnkey contract keeps its margin — an unfrozen spec is an open invitation to
    /// upgrade everything for free.
    /// </summary>
    public async Task<SpecificationScheduleDto> FreezeSpecificationAsync(Guid id, Guid userId)
    {
        var schedule = await Db.SpecificationSchedules.ForCompany(Tenant)
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new InvalidOperationException("That specification does not exist.");

        if (schedule.IsFrozen)
            throw new InvalidOperationException($"This specification was already frozen on {schedule.FrozenOn:dd MMM yyyy}.");

        var undecided = schedule.Items
            .Where(i => i.IsClientSelectable && string.IsNullOrWhiteSpace(i.SelectedOption))
            .Select(i => i.ItemName)
            .ToList();

        if (undecided.Count > 0)
        {
            throw new InvalidOperationException(
                $"{undecided.Count} client selections are still open: {string.Join(", ", undecided.Take(5))}" +
                (undecided.Count > 5 ? $" and {undecided.Count - 5} more." : "."));
        }

        schedule.IsFrozen = true;
        schedule.FrozenOn = Today;
        schedule.StampUpdated(userId);

        await WriteAuditNoteAsync(
            "SpecificationSchedule", id, "SpecificationFrozen", Guid.Empty, userId,
            note: $"Version {schedule.Version} frozen with {schedule.Items.Count} items. Changes from here are variations.",
            entityReference: schedule.Name);

        await Db.SaveChangesAsync();
        return (await GetSpecificationsAsync(schedule.ConstructionProjectId, schedule.ClientBuildContractId))
            .First(s => s.Id == id);
    }
}
