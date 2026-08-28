using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Projects: the tree, the area statement, the programme, the budget and the plot files.
///
/// Two things here are load-bearing. The area statement reconciles — the parts have to sum to the
/// whole, and when they do not the discrepancy is shown rather than swallowed, because a regulator
/// will ask. And certifying a milestone is treated as a financial event, not a status change: it
/// is the moment a construction-linked plan turns into money, so it is gated and it is auditable.
/// </summary>
public partial class ProjectService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering,
    IMoneyService money)
    : RealEstateServiceBase(db, tenant), IProjectService
{
    // ═══ Projects ════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ProjectListItemDto>> GetProjectsAsync(ListQueryDto query)
    {
        var q = Db.Projects.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                p => p.Name.Contains(query.Search!)
                     || (p.Code != null && p.Code.Contains(query.Search!))
                     || (p.City != null && p.City.Contains(query.Search!)))
            .WhereIf(query.OfficeId.HasValue, p => p.OfficeId == query.OfficeId)
            .OrderBy(p => p.Name);

        return await PageAsync(q, query, MapProjectListAsync);
    }

    private async Task<List<ProjectListItemDto>> MapProjectListAsync(List<Project> projects)
    {
        if (projects.Count == 0) return [];

        var ids = projects.Select(p => p.Id).ToList();
        var today = Today;

        // One grouped read gives every count on every card.
        var counts = await Db.Units.ForCompany(Tenant)
            .Where(u => ids.Contains(u.ProjectId) && u.IsSaleable)
            .GroupBy(u => new { u.ProjectId, u.Status })
            .Select(g => new { g.Key.ProjectId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        var areaIds = projects.Where(p => p.GeoAreaId.HasValue).Select(p => p.GeoAreaId!.Value).Distinct().ToList();

        var areas = areaIds.Count == 0
            ? []
            : await Db.GeoAreas.ForCompany(Tenant)
                .Where(a => areaIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Name);

        var outstanding = await Db.Bookings.ForCompany(Tenant)
            .Where(b => ids.Contains(b.ProjectId) && b.Status != BookingStatus.Cancelled)
            .GroupBy(b => b.ProjectId)
            .Select(g => new { ProjectId = g.Key, Outstanding = g.Sum(x => x.Outstanding) })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Outstanding);

        var progress = await Db.ProjectMilestones.ForCompany(Tenant)
            .Where(m => ids.Contains(m.ProjectId))
            .GroupBy(m => m.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                Weighted = g.Sum(m => m.WeightPercent * m.ProgressPercent),
                Weight = g.Sum(m => m.WeightPercent),
            })
            .ToDictionaryAsync(x => x.ProjectId, x => x);

        var escrow = await Db.ProjectBankAccounts.ForCompany(Tenant)
            .Where(e => ids.Contains(e.ProjectId) && e.Kind == ProjectAccountKind.Escrow)
            .GroupBy(e => e.ProjectId)
            .Select(g => new { ProjectId = g.Key, Balance = g.Sum(x => x.Balance) })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Balance);

        return projects.Select(p =>
        {
            var mine = counts.Where(c => c.ProjectId == p.Id).ToList();
            var total = mine.Sum(c => c.Count);
            var available = mine.Where(c => c.Status == PropertyStatus.Available).Sum(c => c.Count);
            var booked = mine.Where(c => c.Status is PropertyStatus.Booked or PropertyStatus.Reserved).Sum(c => c.Count);
            var sold = mine.Where(c => c.Status is PropertyStatus.Sold or PropertyStatus.Possessed or PropertyStatus.Registered).Sum(c => c.Count);

            var weights = progress.GetValueOrDefault(p.Id);

            return new ProjectListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                Kind = p.Kind,
                Status = p.Status,
                City = p.City,
                AreaName = p.GeoAreaId is null ? null : areas.GetValueOrDefault(p.GeoAreaId.Value),
                HeroImageUrl = p.HeroImageUrl,
                CurrencyCode = p.CurrencyCode,

                TotalUnits = total,
                UnitsAvailable = available,
                UnitsBooked = booked,
                UnitsSold = sold,
                AbsorptionPercent = RealEstateMapper.Percent(booked + sold, total),

                TotalSalesValue = p.TotalSalesValue,
                TotalCollected = p.TotalCollected,
                Outstanding = outstanding.GetValueOrDefault(p.Id),
                PhysicalProgressPercent = weights is null || weights.Weight <= 0m
                    ? 0m
                    : RealEstateMapper.Money(weights.Weighted / weights.Weight),

                PromisedPossessionDate = p.PromisedPossessionDate,
                ForecastPossessionDate = p.ForecastPossessionDate,
                SlipDays = p.PromisedPossessionDate is null || p.ForecastPossessionDate is null
                    ? null
                    : p.ForecastPossessionDate.Value.DayNumber - p.PromisedPossessionDate.Value.DayNumber,

                HasJointVenture = p.HasJointVenture,
                EscrowBalance = escrow.TryGetValue(p.Id, out var balance) ? balance : null,
            };
        }).ToList();
    }

    public async Task<ProjectDetailDto?> GetProjectAsync(Guid id)
    {
        var project = await Db.Projects.ForCompany(Tenant)
            .Include(p => p.TeamMembers)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project is null) return null;

        var head = (await MapProjectListAsync([project]))[0];
        var unit = await AreaUnitAsync(project.OfficeId);
        var today = Today;

        var detail = new ProjectDetailDto
        {
            Id = head.Id,
            Name = head.Name,
            Code = head.Code,
            Kind = head.Kind,
            Status = head.Status,
            City = head.City,
            AreaName = head.AreaName,
            HeroImageUrl = head.HeroImageUrl,
            CurrencyCode = head.CurrencyCode,
            TotalUnits = head.TotalUnits,
            UnitsAvailable = head.UnitsAvailable,
            UnitsBooked = head.UnitsBooked,
            UnitsSold = head.UnitsSold,
            AbsorptionPercent = head.AbsorptionPercent,
            TotalSalesValue = head.TotalSalesValue,
            TotalCollected = head.TotalCollected,
            Outstanding = head.Outstanding,
            PhysicalProgressPercent = head.PhysicalProgressPercent,
            PromisedPossessionDate = head.PromisedPossessionDate,
            ForecastPossessionDate = head.ForecastPossessionDate,
            SlipDays = head.SlipDays,
            HasJointVenture = head.HasJointVenture,
            EscrowBalance = head.EscrowBalance,

            AddressLine = project.AddressLine,
            Latitude = project.Latitude,
            Longitude = project.Longitude,
            GeoAreaId = project.GeoAreaId,
            OfficeId = project.OfficeId,

            LaunchDate = project.LaunchDate,
            BookingOpenDate = project.BookingOpenDate,
            ConstructionStartDate = project.ConstructionStartDate,
            PlannedCompletionDate = project.PlannedCompletionDate,
            ActualCompletionDate = project.ActualCompletionDate,
            HandoverToSocietyDate = project.HandoverToSocietyDate,

            AreaStatement = BuildAreaStatement(project, unit),

            TotalBudget = project.TotalBudget,
            EscrowPercent = project.EscrowPercent,
            RecognitionBasis = project.RecognitionBasis,
            RecognitionRationale = project.RecognitionRationale,
            CostAllocationBasis = project.CostAllocationBasis,

            DefaultPaymentPlanTemplateId = project.DefaultPaymentPlanTemplateId,
            HoldHours = project.HoldHours,
            TransferFeePerSqFt = project.TransferFeePerSqFt,
            TransferFeeFlat = project.TransferFeeFlat,

            RegistrationNumber = project.RegistrationNumber,
            RegistrationValidUntil = project.RegistrationValidUntil,
            RegulatorName = project.RegulatorName,

            Tagline = project.Tagline,
            LongDescription = project.LongDescription,
            UniqueSellingPoints = project.UniqueSellingPoints,
            LocationAdvantages = project.LocationAdvantages,
            BrochureUrl = project.BrochureUrl,

            Team = project.TeamMembers
                .Where(t => t.ToDate is null || t.ToDate >= today)
                .OrderBy(t => t.Role)
                .Select(t => new ProjectTeamMemberDto
                {
                    Id = t.Id,
                    Role = t.Role,
                    Name = t.Name,
                    UserId = t.UserId,
                    PartyId = t.PartyId,
                    Phone = t.Phone,
                    Email = t.Email,
                    FromDate = t.FromDate,
                    ToDate = t.ToDate,
                })
                .ToList(),
        };

        detail.ProjectManagerName = detail.Team.FirstOrDefault(t => t.Role == "ProjectManager")?.Name;
        detail.SalesHeadName = detail.Team.FirstOrDefault(t => t.Role == "SalesHead")?.Name;

        if (project.DefaultPaymentPlanTemplateId is not null)
        {
            detail.DefaultPaymentPlanName = await Db.PaymentPlanTemplates.ForCompany(Tenant)
                .Where(t => t.Id == project.DefaultPaymentPlanTemplateId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();
        }

        detail.Structure = await GetStructureAsync(id);
        detail.Milestones = await GetMilestonesAsync(id);

        detail.PriceLists = await Db.PriceLists.ForCompany(Tenant)
            .Where(l => l.ProjectId == id)
            .OrderByDescending(l => l.EffectiveFrom)
            .Select(l => new LookupDto
            {
                Id = l.Id,
                Label = l.Name,
                SubLabel = l.EffectiveFrom.ToString("dd MMM yyyy"),
                IsActive = l.IsActive,
            })
            .ToListAsync();

        detail.SitePlans = await Db.SitePlans.ForCompany(Tenant)
            .Where(s => s.ProjectId == id)
            .OrderBy(s => s.SortOrder)
            .Select(s => new LookupDto { Id = s.Id, Label = s.Name, IsActive = true })
            .ToListAsync();

        detail.Approvals = await GetApprovalsAsync(id, today);

        return detail;
    }

    private async Task<List<ApprovalRecordDto>> GetApprovalsAsync(Guid projectId, DateOnly today)
    {
        var approvals = await Db.ApprovalRecords.ForCompany(Tenant)
            .Where(a => a.ProjectId == projectId)
            .OrderBy(a => a.Kind)
            .ToListAsync();

        var milestoneIds = approvals.Where(a => a.BlocksMilestoneId.HasValue)
            .Select(a => a.BlocksMilestoneId!.Value).Distinct().ToList();

        var milestones = milestoneIds.Count == 0
            ? []
            : await Db.ProjectMilestones.ForCompany(Tenant)
                .Where(m => milestoneIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Name);

        var owners = await AgentNamesAsync(approvals.Select(a => a.OwnerUserId));

        return approvals.Select(a => new ApprovalRecordDto
        {
            Id = a.Id,
            Reference = a.Reference,
            Kind = a.Kind,
            State = a.State,
            Authority = a.Authority,
            ApplicationNumber = a.ApplicationNumber,
            ApprovalNumber = a.ApprovalNumber,
            AppliedOn = a.AppliedOn,
            GrantedOn = a.GrantedOn,
            ValidUntil = a.ValidUntil,
            TotalCost = a.TotalCost,
            OwnerName = a.OwnerUserId is null ? null : owners.GetValueOrDefault(a.OwnerUserId.Value),
            Conditions = a.Conditions,
            DocumentUrl = a.DocumentUrl,
            IsBlocking = a.IsBlocking,
            BlocksMilestoneName = a.BlocksMilestoneId is null ? null : milestones.GetValueOrDefault(a.BlocksMilestoneId.Value),
            IsMandatory = a.IsMandatory,
            DaysToExpiry = a.ValidUntil is null ? null : a.ValidUntil.Value.DayNumber - today.DayNumber,
            IsOverdue = a.QueryResponseDue is not null && a.QueryResponseDue < today && a.State == ApprovalState.QueryRaised,
        }).ToList();
    }

    protected async Task<Dictionary<Guid, string>> AgentNamesAsync(IEnumerable<Guid?> userIds)
    {
        var ids = userIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => a.UserId != null && ids.Contains(a.UserId.Value))
            .ToDictionaryAsync(a => a.UserId!.Value, a => a.DisplayName);
    }

    /// <summary>
    /// The area statement, with its own arithmetic checked. If the components do not add up to the
    /// total land area the gap is reported rather than hidden — that gap is exactly what a
    /// regulator's approval query is about.
    /// </summary>
    private static AreaStatementDto BuildAreaStatement(Project p, AreaUnit unit)
    {
        var parts = p.SaleableAreaSqFt + p.CommonAreaSqFt + p.RoadsAreaSqFt + p.ParksAreaSqFt
                  + p.AmenitiesAreaSqFt + p.CommercialReserveSqFt + p.UtilitiesAreaSqFt;

        var gap = p.TotalLandAreaSqFt - parts;

        return new AreaStatementDto
        {
            TotalLandArea = RealEstateMapper.Area(p.TotalLandAreaSqFt, unit),
            SaleableArea = RealEstateMapper.Area(p.SaleableAreaSqFt, unit),
            CommonArea = RealEstateMapper.Area(p.CommonAreaSqFt, unit),
            RoadsArea = RealEstateMapper.Area(p.RoadsAreaSqFt, unit),
            ParksArea = RealEstateMapper.Area(p.ParksAreaSqFt, unit),
            AmenitiesArea = RealEstateMapper.Area(p.AmenitiesAreaSqFt, unit),
            CommercialReserve = RealEstateMapper.Area(p.CommercialReserveSqFt, unit),
            UtilitiesArea = RealEstateMapper.Area(p.UtilitiesAreaSqFt, unit),
            SaleablePercent = RealEstateMapper.Percent(p.SaleableAreaSqFt, p.TotalLandAreaSqFt),

            // A rounding tail of a few feet is noise; anything larger is a real discrepancy.
            UnaccountedSqFt = Math.Abs(gap) > 1m ? RealEstateMapper.Money(gap) : null,
        };
    }

    public async Task<ProjectDetailDto> SaveProjectAsync(ProjectUpsertDto dto, Guid userId)
    {
        var project = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.Projects.ForCompany(Tenant).Include(p => p.Milestones).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        var isNew = project is null;

        if (project is null)
        {
            var code = string.IsNullOrWhiteSpace(dto.Code)
                ? await numbering.NextMasterCodeAsync(Db.Projects, "PRJ")
                : dto.Code;

            if (await Db.Projects.ForCompany(Tenant).AnyAsync(p => p.Code == code))
                throw new InvalidOperationException($"Project code {code} is already in use.");

            project = new Project { Code = code }.StampNew(Tenant, userId);
            Db.Projects.Add(project);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code != project.Code)
            {
                if (await Db.Projects.ForCompany(Tenant).AnyAsync(p => p.Code == dto.Code && p.Id != project.Id))
                    throw new InvalidOperationException($"Project code {dto.Code} is already in use.");

                project.Code = dto.Code;
            }

            project.StampUpdated(userId);
        }

        project.Name = dto.Name;
        project.Kind = dto.Kind;
        project.Status = dto.Status;
        project.OfficeId = dto.OfficeId;
        project.GeoAreaId = dto.GeoAreaId;
        project.AddressLine = dto.AddressLine;
        project.City = dto.City;
        project.Latitude = dto.Latitude;
        project.Longitude = dto.Longitude;
        project.CurrencyCode = dto.CurrencyCode ?? project.CurrencyCode;

        project.LaunchDate = dto.LaunchDate;
        project.BookingOpenDate = dto.BookingOpenDate;
        project.ConstructionStartDate = dto.ConstructionStartDate;
        project.PlannedCompletionDate = dto.PlannedCompletionDate;
        project.PromisedPossessionDate = dto.PromisedPossessionDate;
        project.ForecastPossessionDate = dto.ForecastPossessionDate ?? dto.PlannedCompletionDate;

        var u = dto.InputAreaUnit;
        project.TotalLandAreaSqFt = RealEstateMapper.ToSquareFeet(dto.TotalLandArea, u);
        project.SaleableAreaSqFt = RealEstateMapper.ToSquareFeet(dto.SaleableArea, u);
        project.CommonAreaSqFt = RealEstateMapper.ToSquareFeet(dto.CommonArea, u);
        project.RoadsAreaSqFt = RealEstateMapper.ToSquareFeet(dto.RoadsArea, u);
        project.ParksAreaSqFt = RealEstateMapper.ToSquareFeet(dto.ParksArea, u);
        project.AmenitiesAreaSqFt = RealEstateMapper.ToSquareFeet(dto.AmenitiesArea, u);
        project.CommercialReserveSqFt = RealEstateMapper.ToSquareFeet(dto.CommercialReserve, u);
        project.UtilitiesAreaSqFt = RealEstateMapper.ToSquareFeet(dto.UtilitiesArea, u);

        project.TotalBudget = dto.TotalBudget;
        project.EscrowPercent = dto.EscrowPercent;
        project.RecognitionBasis = dto.RecognitionBasis;
        project.RecognitionRationale = dto.RecognitionRationale;
        project.CostAllocationBasis = dto.CostAllocationBasis;

        // Over-time recognition is an accounting-policy choice an auditor will question. It does
        // not get made by accident, so it has to carry its reasoning.
        if (dto.RecognitionBasis == RecognitionBasis.OverTime && string.IsNullOrWhiteSpace(dto.RecognitionRationale))
        {
            throw new InvalidOperationException(
                "Over-time recognition needs a written rationale — which enforceable right to payment " +
                "or asset with no alternative use supports it.");
        }

        project.DefaultPaymentPlanTemplateId = dto.DefaultPaymentPlanTemplateId;
        project.DefaultSurchargePolicyId = dto.DefaultSurchargePolicyId;
        project.DefaultDunningPolicyId = dto.DefaultDunningPolicyId;
        project.DefaultDeductionPolicyId = dto.DefaultDeductionPolicyId;
        project.HoldHours = dto.HoldHours <= 0 ? 48 : dto.HoldHours;
        project.TransferFeePerSqFt = dto.TransferFeePerSqFt;
        project.TransferFeeFlat = dto.TransferFeeFlat;

        project.RegistrationNumber = dto.RegistrationNumber;
        project.RegistrationValidUntil = dto.RegistrationValidUntil;
        project.RegulatorName = dto.RegulatorName;

        project.Tagline = dto.Tagline;
        project.LongDescription = dto.LongDescription;
        project.UniqueSellingPoints = dto.UniqueSellingPoints;
        project.LocationAdvantages = dto.LocationAdvantages;
        project.HeroImageUrl = dto.HeroImageUrl;
        project.BrochureUrl = dto.BrochureUrl;
        project.ProjectManagerUserId = dto.ProjectManagerUserId;
        project.SalesHeadUserId = dto.SalesHeadUserId;

        await Db.SaveChangesAsync();

        // Attach the land the project sits on, so the land bank stops showing it as unallocated.
        if (dto.LandParcelIds.Count > 0)
        {
            var parcels = await Db.LandParcels.ForCompany(Tenant)
                .Where(p => dto.LandParcelIds.Contains(p.Id))
                .ToListAsync();

            foreach (var parcel in parcels)
            {
                parcel.ProjectId = project.Id;
                parcel.StampUpdated(userId);
            }
        }

        // A brand-new project gets the standard construction programme rather than a blank one.
        if (isNew && dto.Kind != ProjectKind.PlotScheme)
        {
            var order = 0;

            foreach (var (name, weight) in StandardMilestones)
            {
                Db.ProjectMilestones.Add(new ProjectMilestone
                {
                    ProjectId = project.Id,
                    Name = name,
                    WeightPercent = weight,
                    SortOrder = order += 10,
                    Status = MilestoneStatus.NotStarted,
                }.StampNew(Tenant, userId));
            }
        }
        else if (isNew)
        {
            var order = 0;

            foreach (var (name, weight) in PlotSchemeMilestones)
            {
                Db.ProjectMilestones.Add(new ProjectMilestone
                {
                    ProjectId = project.Id,
                    Name = name,
                    WeightPercent = weight,
                    SortOrder = order += 10,
                    Status = MilestoneStatus.NotStarted,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        return (await GetProjectAsync(project.Id))!;
    }

    private static readonly (string Name, decimal Weight)[] StandardMilestones =
    [
        ("Excavation and foundation", 10m),
        ("Plinth level", 8m),
        ("Structure — up to 25%", 12m),
        ("Structure — up to 50%", 12m),
        ("Structure — up to 75%", 12m),
        ("Structure complete", 10m),
        ("Brickwork and plaster", 10m),
        ("Internal finishes", 10m),
        ("External finishes and façade", 6m),
        ("MEP and lifts commissioned", 5m),
        ("Completion certificate", 5m),
    ];

    private static readonly (string Name, decimal Weight)[] PlotSchemeMilestones =
    [
        ("Land acquisition complete", 20m),
        ("Layout approval", 10m),
        ("Demarcation and boundary", 10m),
        ("Earthwork and levelling", 15m),
        ("Roads and carpeting", 20m),
        ("Water and sewerage", 10m),
        ("Electricity and street lighting", 10m),
        ("Possession-ready certificate", 5m),
    ];

    public async Task<List<LookupDto>> GetProjectLookupAsync()
        => await Db.Projects.ForCompany(Tenant)
            .Where(p => p.Status != ProjectStatus.Closed)
            .OrderBy(p => p.Name)
            .Select(p => new LookupDto
            {
                Id = p.Id,
                Label = p.Name,
                SubLabel = p.City,
                Code = p.Code,
                IsActive = p.IsActive,
            })
            .ToListAsync();

    // ═══ Structure ═══════════════════════════════════════════════════════════

    public async Task<List<ProjectNodeDto>> GetStructureAsync(Guid projectId)
    {
        var nodes = await Db.ProjectNodes.ForCompany(Tenant)
            .Where(n => n.ProjectId == projectId)
            .OrderBy(n => n.Depth).ThenBy(n => n.SortOrder)
            .ToListAsync();

        if (nodes.Count == 0) return [];

        var unit = await AreaUnitAsync();

        var counts = await Db.Units.ForCompany(Tenant)
            .Where(u => u.ProjectId == projectId && u.ProjectNodeId != null)
            .GroupBy(u => new { u.ProjectNodeId, u.Status })
            .Select(g => new { g.Key.ProjectNodeId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        var map = nodes.ToDictionary(n => n.Id, n => new ProjectNodeDto
        {
            Id = n.Id,
            ParentNodeId = n.ParentNodeId,
            Kind = n.Kind,
            Name = n.Name,
            Code = n.Code,
            SortOrder = n.SortOrder,
            Depth = n.Depth,
            FloorNumber = n.FloorNumber,
            Area = RealEstateMapper.AreaOrNull(n.AreaSqFt, unit),
            PlannedUnitCount = n.PlannedUnitCount,
            Status = n.Status,
            PlannedCompletionDate = n.PlannedCompletionDate,
            ProgressPercent = n.ProgressPercent,
            FloorPlanUrl = n.FloorPlanUrl,
            SitePlanUrl = n.SitePlanUrl,
        });

        foreach (var c in counts.Where(c => c.ProjectNodeId.HasValue))
        {
            if (!map.TryGetValue(c.ProjectNodeId!.Value, out var dto)) continue;

            dto.ActualUnitCount += c.Count;

            if (c.Status == PropertyStatus.Available) dto.UnitsAvailable += c.Count;
            else if (c.Status is PropertyStatus.Booked or PropertyStatus.Reserved) dto.UnitsBooked += c.Count;
            else if (c.Status is PropertyStatus.Sold or PropertyStatus.Possessed or PropertyStatus.Registered) dto.UnitsSold += c.Count;
        }

        // Counts roll up the tree, so a block shows what its floors hold without a second query.
        foreach (var node in nodes.OrderByDescending(n => n.Depth))
        {
            if (node.ParentNodeId is null) continue;
            if (!map.TryGetValue(node.Id, out var child)) continue;
            if (!map.TryGetValue(node.ParentNodeId.Value, out var parent)) continue;

            parent.ActualUnitCount += child.ActualUnitCount;
            parent.UnitsAvailable += child.UnitsAvailable;
            parent.UnitsBooked += child.UnitsBooked;
            parent.UnitsSold += child.UnitsSold;
            parent.Children.Add(child);
        }

        foreach (var dto in map.Values)
            dto.Children = dto.Children.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();

        return map.Values
            .Where(d => d.ParentNodeId is null)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .ToList();
    }

    public async Task<ProjectNodeDto> SaveNodeAsync(ProjectNodeUpsertDto dto, Guid userId)
    {
        var node = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.ProjectNodes.ForCompany(Tenant).FirstOrDefaultAsync(n => n.Id == dto.Id)
            : null;

        if (node is null)
        {
            node = new ProjectNode { ProjectId = dto.ProjectId }.StampNew(Tenant, userId);
            Db.ProjectNodes.Add(node);
        }
        else
        {
            // Re-parenting a node under its own descendant would make the tree unrenderable.
            if (dto.ParentNodeId is not null && await IsDescendantAsync(dto.ParentNodeId.Value, node.Id))
                throw new InvalidOperationException($"{node.Name} cannot sit under one of its own children.");

            node.StampUpdated(userId);
        }

        node.ParentNodeId = dto.ParentNodeId;
        node.Kind = dto.Kind;
        node.Name = dto.Name;
        node.Code = dto.Code;
        node.SortOrder = dto.SortOrder;
        node.FloorNumber = dto.FloorNumber;
        node.AreaSqFt = dto.Area is null ? null : RealEstateMapper.ToSquareFeet(dto.Area.Value, dto.InputAreaUnit);
        node.PlannedUnitCount = dto.PlannedUnitCount;
        node.Status = dto.Status;
        node.PlannedCompletionDate = dto.PlannedCompletionDate;
        node.FloorPlanUrl = dto.FloorPlanUrl;
        node.SitePlanUrl = dto.SitePlanUrl;

        await StampDepthAndPathAsync(node);
        await Db.SaveChangesAsync();

        var structure = await GetStructureAsync(dto.ProjectId);
        return Find(structure, node.Id) ?? throw new InvalidOperationException("The node was saved but could not be reloaded.");
    }

    private static ProjectNodeDto? Find(List<ProjectNodeDto> nodes, Guid id)
    {
        foreach (var n in nodes)
        {
            if (n.Id == id) return n;

            var hit = Find(n.Children, id);
            if (hit is not null) return hit;
        }

        return null;
    }

    private async Task<bool> IsDescendantAsync(Guid candidateParentId, Guid nodeId)
    {
        var cursor = candidateParentId;

        // Depth is small and bounded; a walk is cheaper and clearer than a recursive CTE here.
        for (var guard = 0; guard < 32; guard++)
        {
            if (cursor == nodeId) return true;

            var parent = await Db.ProjectNodes.ForCompany(Tenant)
                .Where(n => n.Id == cursor)
                .Select(n => n.ParentNodeId)
                .FirstOrDefaultAsync();

            if (parent is null) return false;
            cursor = parent.Value;
        }

        return false;
    }

    private async Task StampDepthAndPathAsync(ProjectNode node)
    {
        if (node.ParentNodeId is null)
        {
            node.Depth = 0;
            node.Path = node.Name;
            return;
        }

        var parent = await Db.ProjectNodes.ForCompany(Tenant)
            .FirstOrDefaultAsync(n => n.Id == node.ParentNodeId);

        node.Depth = (parent?.Depth ?? 0) + 1;
        node.Path = parent is null ? node.Name : $"{parent.Path} / {node.Name}";
    }

    public async Task DeleteNodeAsync(Guid nodeId, Guid userId)
    {
        var node = await RequireAsync<ProjectNode>(nodeId, "That block or floor does not exist.");

        if (await Db.ProjectNodes.ForCompany(Tenant).AnyAsync(n => n.ParentNodeId == nodeId))
            throw new InvalidOperationException($"{node.Name} still has blocks or floors under it. Remove those first.");

        var units = await Db.Units.ForCompany(Tenant).CountAsync(u => u.ProjectNodeId == nodeId);

        if (units > 0)
            throw new InvalidOperationException($"{node.Name} holds {units} units and cannot be removed.");

        node.StampDeleted(userId);
        await Db.SaveChangesAsync();
    }

    // ═══ Bulk unit generation ════════════════════════════════════════════════

    /// <summary>
    /// Creates a tower's worth of units from a pattern. Runs as a dry run by default so the
    /// operator sees the exact numbers and conflicts before four hundred rows appear.
    /// </summary>
    public async Task<UnitGenerationResultDto> GenerateUnitsAsync(UnitGenerationDto dto, Guid userId)
    {
        var project = await RequireAsync<Project>(dto.ProjectId, "That project does not exist.");
        var parent = await RequireAsync<ProjectNode>(dto.ProjectNodeId, "That block does not exist.");

        if (dto.UnitCodes.Count == 0)
            throw new InvalidOperationException("Give at least one unit code per floor — for example A, B, C, D.");

        if (dto.ToFloor < dto.FromFloor)
            throw new InvalidOperationException("The last floor cannot be below the first.");

        var result = new UnitGenerationResultDto();
        var unitOfArea = await AreaUnitAsync(project.OfficeId);

        var saleable = RealEstateMapper.ToSquareFeet(dto.SaleableArea, dto.InputAreaUnit);
        var carpet = dto.CarpetArea is null ? (decimal?)null : RealEstateMapper.ToSquareFeet(dto.CarpetArea.Value, dto.InputAreaUnit);

        var existing = await Db.Units.ForCompany(Tenant)
            .Where(u => u.ProjectId == dto.ProjectId)
            .Select(u => u.UnitNumber)
            .ToListAsync();

        var taken = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        var skip = dto.SkipFloors.ToHashSet();
        var corners = new HashSet<string>(dto.CornerCodes, StringComparer.OrdinalIgnoreCase);

        var priceLines = dto.PriceListId is null
            ? []
            : await Db.PriceListLines.ForCompany(Tenant)
                .Where(l => l.PriceListId == dto.PriceListId)
                .ToListAsync();

        var planned = new List<(int Floor, string Code, string Number)>();

        for (var floor = dto.FromFloor; floor <= dto.ToFloor; floor++)
        {
            if (skip.Contains(floor)) continue;

            foreach (var code in dto.UnitCodes)
            {
                var number = dto.NumberPattern
                    .Replace("{floor}", floor.ToString())
                    .Replace("{code}", code)
                    .Replace("{block}", parent.Code ?? parent.Name);

                if (taken.Contains(number))
                {
                    result.SkippedCount++;
                    result.Conflicts.Add($"{number} already exists and was left alone.");
                    continue;
                }

                taken.Add(number);
                planned.Add((floor, code, number));
            }
        }

        result.WouldCreateCount = planned.Count;

        // The preview is capped: nobody reads four hundred rows, and the count above is the point.
        foreach (var (floor, code, number) in planned.Take(50))
        {
            result.Preview.Add(new InventoryUnitDto
            {
                UnitNumber = number,
                FloorNumber = floor,
                SubType = dto.SubType,
                Status = PropertyStatus.Available,
                AreaSqFt = saleable,
                AreaDisplay = RealEstateMapper.Area(saleable, unitOfArea).DisplayText,
                Bedrooms = dto.Bedrooms,
                IsCorner = corners.Contains(code),
                Facing = dto.Facing,
            });
        }

        if (dto.DryRun) return result;

        await using var transaction = await Db.Database.BeginTransactionAsync();

        // Floor nodes are created once and reused, so the tree stays navigable at 400 units.
        var floorNodes = new Dictionary<int, Guid>();

        if (dto.CreateFloorNodes)
        {
            var already = await Db.ProjectNodes.ForCompany(Tenant)
                .Where(n => n.ParentNodeId == dto.ProjectNodeId && n.FloorNumber != null)
                .ToListAsync();

            foreach (var n in already) floorNodes[n.FloorNumber!.Value] = n.Id;

            foreach (var floor in planned.Select(p => p.Floor).Distinct().OrderBy(f => f))
            {
                if (floorNodes.ContainsKey(floor)) continue;

                var node = new ProjectNode
                {
                    ProjectId = dto.ProjectId,
                    ParentNodeId = dto.ProjectNodeId,
                    Kind = ProjectNodeKind.Floor,
                    Name = FloorName(floor),
                    Code = floor.ToString(),
                    FloorNumber = floor,
                    SortOrder = floor * 10,
                    Depth = parent.Depth + 1,
                    Path = $"{parent.Path} / {FloorName(floor)}",
                    Status = parent.Status,
                }.StampNew(Tenant, userId);

                Db.ProjectNodes.Add(node);
                floorNodes[floor] = node.Id;
            }

            await Db.SaveChangesAsync();
        }

        foreach (var (floor, code, number) in planned)
        {
            var isCorner = corners.Contains(code);
            var nodeId = floorNodes.TryGetValue(floor, out var found) ? found : dto.ProjectNodeId;

            // Every unit is also a property. The physical record is what outlives the sale.
            var property = new Property
            {
                Reference = await numbering.NextPropertyReferenceAsync(),
                Name = $"{project.Name} {number}",
                Category = PropertyCategory.Residential,
                SubType = dto.SubType,
                Status = PropertyStatus.Available,
                Occupancy = OccupancyState.Vacant,
                ProjectId = dto.ProjectId,
                ProjectNodeId = nodeId,
                FloorNumber = floor,
                FloorLabel = FloorName(floor),
                UnitNumber = number,
                StackCode = code,
                Facing = dto.Facing,
                IsCorner = isCorner,
                SaleableAreaSqFt = saleable,
                CarpetAreaSqFt = carpet,
                Bedrooms = dto.Bedrooms,
                Bathrooms = dto.Bathrooms,
                ParkingBays = dto.ParkingBays,
                GeoAreaId = project.GeoAreaId,
                CityId = null,
                Latitude = project.Latitude,
                Longitude = project.Longitude,
                CurrencyCode = project.CurrencyCode,
            }.StampNew(Tenant, userId);

            if (saleable > 0m && carpet is > 0m)
                property.LoadingFactorPercent = RealEstateMapper.Percent(saleable - carpet.Value, carpet.Value);

            Db.Properties.Add(property);

            var rate = ResolveRate(priceLines, dto.SubType, floor, saleable, nodeId);
            var basePrice = RealEstateMapper.Money(rate * saleable);

            var unit = new Unit
            {
                ProjectId = dto.ProjectId,
                ProjectNodeId = nodeId,
                PropertyId = property.Id,
                UnitNumber = number,
                Status = PropertyStatus.Available,
                PriceListId = dto.PriceListId,
                BaseRatePerSqFt = rate,
                BasePrice = basePrice,
                TotalPrice = basePrice,
                CurrencyCode = project.CurrencyCode,
                PositionOnFloor = dto.UnitCodes.IndexOf(code) + 1,
                StackIndex = dto.UnitCodes.IndexOf(code),
                IsSaleable = true,
            }.StampNew(Tenant, userId);

            Db.Units.Add(unit);
            result.CreatedCount++;
        }

        parent.PlannedUnitCount = Math.Max(parent.PlannedUnitCount, planned.Count);
        parent.StampUpdated(userId);

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return result;
    }

    private static string FloorName(int floor) => floor switch
    {
        0 => "Ground floor",
        < 0 => $"Basement {Math.Abs(floor)}",
        1 => "1st floor",
        2 => "2nd floor",
        3 => "3rd floor",
        _ => $"{floor}th floor",
    };

    /// <summary>Picks the most specific matching price line, in the same order the board uses.</summary>
    private static decimal ResolveRate(List<PriceListLine> lines, PropertySubType subType, int floor, decimal areaSqFt, Guid? nodeId)
    {
        if (lines.Count == 0) return 0m;

        var best = lines
            .Where(l => l.SubType is null || l.SubType == subType)
            .Where(l => l.ProjectNodeId is null || l.ProjectNodeId == nodeId)
            .Where(l => l.MinFloor is null || floor >= l.MinFloor)
            .Where(l => l.MaxFloor is null || floor <= l.MaxFloor)
            .Where(l => l.MinAreaSqFt is null || areaSqFt >= l.MinAreaSqFt)
            .Where(l => l.MaxAreaSqFt is null || areaSqFt <= l.MaxAreaSqFt)
            .OrderByDescending(l =>
                (l.ProjectNodeId is null ? 0 : 100)
                + (l.SubType is null ? 0 : 50)
                + (l.MinFloor is null && l.MaxFloor is null ? 0 : 20)
                + (l.MinAreaSqFt is null && l.MaxAreaSqFt is null ? 0 : 10))
            .FirstOrDefault();

        return best?.RatePerSqFt ?? 0m;
    }
}
