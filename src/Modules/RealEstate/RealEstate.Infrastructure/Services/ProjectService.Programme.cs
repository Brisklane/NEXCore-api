using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The programme, the budget, the cash-flow forecast, the site plan and the plot-file register.
///
/// Certifying a milestone is the piece that matters most: on a construction-linked plan it is the
/// event that turns a stage of building into an instalment somebody owes. So the certificate is a
/// record in its own right, the demand run it triggers is idempotent, and the screen tells the
/// engineer how much money the signature is about to release *before* they sign.
/// </summary>
public partial class ProjectService
{
    // ═══ Milestones ══════════════════════════════════════════════════════════

    public async Task<List<ProjectMilestoneDto>> GetMilestonesAsync(Guid projectId)
    {
        var milestones = await Db.ProjectMilestones.ForCompany(Tenant)
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

        if (milestones.Count == 0) return [];

        var ids = milestones.Select(m => m.Id).ToList();

        var nodeIds = milestones.Where(m => m.ProjectNodeId.HasValue)
            .Select(m => m.ProjectNodeId!.Value).Distinct().ToList();

        var nodes = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant)
                .Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        // What each milestone is worth in unraised instalments. This is the number the engineer
        // and the finance controller both need before anyone signs anything.
        var linked = await Db.Instalments.ForCompany(Tenant)
            .Where(i => i.ProjectMilestoneId != null
                     && ids.Contains(i.ProjectMilestoneId.Value)
                     && i.Status == InstalmentStatus.NotDue)
            .GroupBy(i => i.ProjectMilestoneId!.Value)
            .Select(g => new { Id = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x);

        var certifiers = await AgentNamesAsync(milestones.Select(m => m.CertifiedByUserId));

        return milestones.Select(m =>
        {
            var link = linked.GetValueOrDefault(m.Id);

            return new ProjectMilestoneDto
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                ProjectNodeId = m.ProjectNodeId,
                BlockName = m.ProjectNodeId is null ? null : nodes.GetValueOrDefault(m.ProjectNodeId.Value),
                Name = m.Name,
                Code = m.Code,
                SortOrder = m.SortOrder,
                Status = m.Status,
                PlannedDate = m.PlannedDate,
                ForecastDate = m.ForecastDate,
                ReachedOn = m.ReachedOn,
                CertifiedOn = m.CertifiedOn,
                CertifiedByName = m.CertifiedByUserId is null ? null : certifiers.GetValueOrDefault(m.CertifiedByUserId.Value),
                CertificateUrl = m.CertificateUrl,
                WeightPercent = m.WeightPercent,
                ProgressPercent = m.ProgressPercent,
                DemandsRaised = m.DemandsRaised,
                LinkedDemandValue = link?.Amount ?? 0m,
                LinkedInstalmentCount = link?.Count ?? 0,
                SlipDays = m.PlannedDate is null
                    ? null
                    : ((m.ReachedOn ?? m.ForecastDate) is { } actual ? actual.DayNumber - m.PlannedDate.Value.DayNumber : null),
            };
        }).ToList();
    }

    public async Task<ProjectMilestoneDto> SaveMilestoneAsync(ProjectMilestoneDto dto, Guid userId)
    {
        var milestone = dto.Id != Guid.Empty
            ? await Db.ProjectMilestones.ForCompany(Tenant).FirstOrDefaultAsync(m => m.Id == dto.Id)
            : null;

        if (milestone is null)
        {
            milestone = new ProjectMilestone { ProjectId = dto.ProjectId }.StampNew(Tenant, userId);
            Db.ProjectMilestones.Add(milestone);
        }
        else milestone.StampUpdated(userId);

        milestone.ProjectNodeId = dto.ProjectNodeId;
        milestone.Name = dto.Name;
        milestone.Code = dto.Code;
        milestone.SortOrder = dto.SortOrder;
        milestone.PlannedDate = dto.PlannedDate;
        milestone.ForecastDate = dto.ForecastDate;
        milestone.WeightPercent = dto.WeightPercent;
        milestone.ProgressPercent = Math.Clamp(dto.ProgressPercent, 0m, 100m);

        // Status and progress are one thing said twice, so they are kept consistent here rather
        // than trusted to whoever last touched the form.
        milestone.Status = dto.Status switch
        {
            MilestoneStatus.Certified => MilestoneStatus.Certified,
            _ when milestone.ProgressPercent >= 100m => MilestoneStatus.Reached,
            _ when milestone.ProgressPercent > 0m => MilestoneStatus.InProgress,
            _ => MilestoneStatus.NotStarted,
        };

        if (milestone.Status == MilestoneStatus.Reached && milestone.ReachedOn is null)
            milestone.ReachedOn = dto.ReachedOn ?? Today;

        await Db.SaveChangesAsync();
        await RollUpProgressAsync(milestone.ProjectId, userId);

        return (await GetMilestonesAsync(milestone.ProjectId)).First(m => m.Id == milestone.Id);
    }

    /// <summary>Weighted physical progress, pushed onto the project's forecast completion date.</summary>
    private async Task RollUpProgressAsync(Guid projectId, Guid userId)
    {
        var milestones = await Db.ProjectMilestones.ForCompany(Tenant)
            .Where(m => m.ProjectId == projectId)
            .Select(m => new { m.WeightPercent, m.ProgressPercent, m.ForecastDate, m.PlannedDate, m.Status })
            .ToListAsync();

        if (milestones.Count == 0) return;

        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) return;

        // The forecast completion date is the last outstanding milestone's forecast, which is the
        // only honest answer — the project finishes when its slowest remaining stage finishes.
        var last = milestones
            .Where(m => m.Status != MilestoneStatus.Certified)
            .Select(m => m.ForecastDate ?? m.PlannedDate)
            .Where(d => d is not null)
            .DefaultIfEmpty(null)
            .Max();

        if (last is not null) project.ForecastPossessionDate = last;

        project.StampUpdated(userId);
        await Db.SaveChangesAsync();
    }

    /// <summary>
    /// Certifies a milestone and, on a construction-linked plan, raises the instalments it
    /// unlocks. The certificate is created first and the demands quote it, so a customer asking
    /// "what am I paying for" gets the engineer's certificate number, date and photographs.
    /// </summary>
    public async Task<MilestoneCertificateDto> CertifyMilestoneAsync(MilestoneCertificateDto dto, Guid userId)
    {
        var milestone = await RequireAsync<ProjectMilestone>(dto.ProjectMilestoneId, "That milestone does not exist.");

        if (milestone.Status == MilestoneStatus.Certified)
            throw new InvalidOperationException($"\"{milestone.Name}\" was already certified on {milestone.CertifiedOn:dd MMM yyyy}.");

        // Certifying out of order is how a developer collects for a stage that has not happened.
        var earlier = await Db.ProjectMilestones.ForCompany(Tenant)
            .Where(m => m.ProjectId == milestone.ProjectId
                     && m.ProjectNodeId == milestone.ProjectNodeId
                     && m.SortOrder < milestone.SortOrder
                     && m.Status != MilestoneStatus.Certified)
            .OrderBy(m => m.SortOrder)
            .Select(m => m.Name)
            .FirstOrDefaultAsync();

        if (earlier is not null)
            throw new InvalidOperationException($"\"{earlier}\" has not been certified yet. Certify the stages in order.");

        // An approval that gates this stage has to be in hand first.
        var blocking = await Db.ApprovalRecords.ForCompany(Tenant)
            .Where(a => a.BlocksMilestoneId == milestone.Id
                     && a.IsBlocking
                     && a.State != ApprovalState.Granted)
            .Select(a => new { a.Kind, a.Authority })
            .FirstOrDefaultAsync();

        if (blocking is not null)
            throw new InvalidOperationException($"The {blocking.Kind} from {blocking.Authority ?? "the authority"} is not granted yet, and it gates this milestone.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var today = Today;

        var certificate = new MilestoneCertificate
        {
            CertificateNumber = await numbering.NextMasterCodeAsync(Db.MilestoneCertificates, "MSC"),
            ProjectMilestoneId = milestone.Id,
            ProjectNodeId = milestone.ProjectNodeId,
            CertifiedOn = dto.CertifiedOn == default ? today : dto.CertifiedOn,
            CertifiedByUserId = userId,
            CertifierName = dto.CertifierName,
            CertifierQualification = dto.CertifierQualification,
            ProgressPercent = dto.ProgressPercent <= 0m ? 100m : dto.ProgressPercent,
            Observations = dto.Observations,
            PhotoUrls = dto.PhotoUrls.Count == 0 ? null : string.Join('\n', dto.PhotoUrls),
            DocumentUrl = dto.DocumentUrl,
            IsCountersigned = dto.IsCountersigned,
        }.StampNew(Tenant, userId);

        Db.MilestoneCertificates.Add(certificate);

        milestone.Status = MilestoneStatus.Certified;
        milestone.CertifiedOn = certificate.CertifiedOn;
        milestone.CertifiedByUserId = userId;
        milestone.CertificateUrl = dto.DocumentUrl;
        milestone.ProgressPercent = certificate.ProgressPercent;
        milestone.ReachedOn ??= certificate.CertifiedOn;
        milestone.StampUpdated(userId);

        await Db.SaveChangesAsync();

        var result = new MilestoneCertificateDto
        {
            Id = certificate.Id,
            CertificateNumber = certificate.CertificateNumber,
            ProjectMilestoneId = milestone.Id,
            MilestoneName = milestone.Name,
            CertifiedOn = certificate.CertifiedOn,
            CertifiedByName = dto.CertifiedByName,
            CertifierName = certificate.CertifierName,
            CertifierQualification = certificate.CertifierQualification,
            ProgressPercent = certificate.ProgressPercent,
            Observations = certificate.Observations,
            PhotoUrls = dto.PhotoUrls,
            DocumentUrl = certificate.DocumentUrl,
            IsCountersigned = certificate.IsCountersigned,
        };

        // The demand run is idempotent and it is the milestone flag, not this call, that stops a
        // second run — so a retry after a timeout cannot bill anyone twice.
        if (!milestone.DemandsRaised)
        {
            var batch = await money.RunDemandsAsync(new DemandRunRequestDto
            {
                ProjectId = milestone.ProjectId,
                ProjectNodeId = milestone.ProjectNodeId,
                ProjectMilestoneId = milestone.Id,
                RunType = "Milestone",
                IsDryRun = false,
                SendImmediately = true,
            }, userId);

            milestone.DemandsRaised = true;
            certificate.DemandsTriggered = true;
            certificate.DemandBatchId = batch.Id;

            result.DemandsTriggered = true;
            result.DemandValueTriggered = batch.TotalAmount;
            result.DemandCountTriggered = batch.GeneratedCount;

            await Db.SaveChangesAsync();
        }

        await RollUpProgressAsync(milestone.ProjectId, userId);
        await transaction.CommitAsync();

        return result;
    }

    // ═══ Budget ══════════════════════════════════════════════════════════════

    public async Task<List<ProjectBudgetLineDto>> GetBudgetAsync(Guid projectId)
    {
        var lines = await Db.ProjectBudgetLines.ForCompany(Tenant)
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.CostHead)
            .ToListAsync();

        return lines.Select(MapBudgetLine).ToList();
    }

    private static ProjectBudgetLineDto MapBudgetLine(ProjectBudgetLine b)
    {
        // Variance is measured against the forecast, not the actual. Comparing budget to actual
        // half-way through a build flatters every line and tells nobody anything.
        var forecast = b.ForecastAmount > 0m ? b.ForecastAmount : b.CommittedAmount + b.ActualAmount;
        var variance = forecast - b.BudgetAmount;

        return new ProjectBudgetLineDto
        {
            Id = b.Id,
            CostHead = b.CostHead,
            Description = b.Description,
            BudgetAmount = b.BudgetAmount,
            CommittedAmount = b.CommittedAmount,
            ActualAmount = b.ActualAmount,
            ForecastAmount = forecast,
            VarianceAmount = RealEstateMapper.Money(variance),
            VariancePercent = RealEstateMapper.Percent(variance, b.BudgetAmount),
            SortOrder = b.SortOrder,
        };
    }

    public async Task<ProjectBudgetLineDto> SaveBudgetLineAsync(Guid projectId, ProjectBudgetLineDto dto, Guid userId)
    {
        var line = dto.Id != Guid.Empty
            ? await Db.ProjectBudgetLines.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == dto.Id)
            : null;

        if (line is null)
        {
            line = new ProjectBudgetLine { ProjectId = projectId }.StampNew(Tenant, userId);
            Db.ProjectBudgetLines.Add(line);
        }
        else line.StampUpdated(userId);

        line.CostHead = dto.CostHead;
        line.Description = dto.Description;
        line.BudgetAmount = dto.BudgetAmount;
        line.CommittedAmount = dto.CommittedAmount;
        line.ActualAmount = dto.ActualAmount;
        line.ForecastAmount = dto.ForecastAmount;
        line.SortOrder = dto.SortOrder;

        await Db.SaveChangesAsync();

        var project = await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == projectId);

        if (project is not null)
        {
            project.TotalBudget = await Db.ProjectBudgetLines.ForCompany(Tenant)
                .Where(b => b.ProjectId == projectId)
                .SumAsync(b => b.BudgetAmount);

            project.StampUpdated(userId);
            await Db.SaveChangesAsync();
        }

        return MapBudgetLine(line);
    }

    // ═══ Cash flow ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Month by month: what the payment plans say will come in, what the budget says will go out,
    /// and the running position. Past months use actuals; future months use the schedule.
    ///
    /// The peak funding gap is the number this exists to produce — the worst point on the curve is
    /// what a developer's bank conversation is actually about.
    /// </summary>
    public async Task<ProjectCashFlowDto> GetCashFlowAsync(Guid projectId, int months)
    {
        var project = await RequireAsync<Project>(projectId, "That project does not exist.");
        var span = Math.Clamp(months <= 0 ? 24 : months, 3, 120);

        var today = Today;
        var from = new DateOnly(today.Year, today.Month, 1).AddMonths(-6);
        var to = from.AddMonths(span);

        var result = new ProjectCashFlowDto
        {
            ProjectId = projectId,
            ProjectName = project.Name,
            CurrencyCode = project.CurrencyCode,
            FromMonth = from,
            ToMonth = to,
        };

        var bookingIds = await Db.Bookings.ForCompany(Tenant)
            .Where(b => b.ProjectId == projectId && b.Status != BookingStatus.Cancelled)
            .Select(b => b.Id)
            .ToListAsync();

        // Scheduled inflow: every unpaid instalment on a live plan, by its due month.
        var scheduled = bookingIds.Count == 0
            ? []
            : await Db.Instalments.ForCompany(Tenant)
                .Where(i => bookingIds.Contains(i.BookingId)
                         && i.Status != InstalmentStatus.Paid
                         && i.Status != InstalmentStatus.Cancelled
                         && i.Status != InstalmentStatus.Waived
                         && i.Status != InstalmentStatus.Restructured
                         && i.DueDate != null && i.DueDate >= from && i.DueDate < to)
                .Select(i => new { Date = i.DueDate!.Value, Outstanding = i.Amount - i.PaidAmount })
                .GroupBy(i => new { i.Date.Year, i.Date.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.Outstanding) })
                .ToListAsync();

        // Actual inflow: allocated receipts, by receipt month.
        var received = bookingIds.Count == 0
            ? []
            : await Db.Receipts.ForCompany(Tenant)
                .Where(r => r.BookingId != null && bookingIds.Contains(r.BookingId.Value)
                         && r.Status == ReceiptStatus.Posted
                         && r.ReceivedOn >= from && r.ReceivedOn < to)
                .GroupBy(r => new { r.ReceivedOn.Year, r.ReceivedOn.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.Amount) })
                .ToListAsync();

        // Outflow. Budget forecast spread across the remaining build, actuals where we have them.
        var budgetTotal = await Db.ProjectBudgetLines.ForCompany(Tenant)
            .Where(b => b.ProjectId == projectId)
            .SumAsync(b => b.ForecastAmount > 0m ? b.ForecastAmount : b.BudgetAmount);

        var spentToDate = await Db.ProjectBudgetLines.ForCompany(Tenant)
            .Where(b => b.ProjectId == projectId)
            .SumAsync(b => b.ActualAmount);

        var constructionIds = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(c => c.ProjectId == projectId)
            .Select(c => c.Id)
            .ToListAsync();

        var certified = constructionIds.Count == 0
            ? []
            : await Db.InterimPaymentCertificates.ForCompany(Tenant)
            .Where(c => constructionIds.Contains(c.ConstructionProjectId)
                     && c.Direction == "Payable"
                     && c.CertifiedOn != null
                     && c.CertifiedOn >= from && c.CertifiedOn < to)
            .Select(c => new { Date = c.CertifiedOn!.Value, c.NetPayable })
            .GroupBy(c => new { c.Date.Year, c.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.NetPayable) })
            .ToListAsync();

        var remainingSpend = Math.Max(0m, budgetTotal - spentToDate);

        var futureMonths = Math.Max(1, project.ForecastPossessionDate is null
            ? span / 2
            : Math.Max(1, ((project.ForecastPossessionDate.Value.Year - today.Year) * 12)
                          + project.ForecastPossessionDate.Value.Month - today.Month));

        var monthlySpend = RealEstateMapper.Money(remainingSpend / futureMonths);

        var position = 0m;

        for (var i = 0; i < span; i++)
        {
            var month = from.AddMonths(i);
            var isPast = month < new DateOnly(today.Year, today.Month, 1);

            var projectedIn = scheduled
                .Where(s => s.Year == month.Year && s.Month == month.Month)
                .Sum(s => s.Amount);

            var actualIn = received
                .Where(r => r.Year == month.Year && r.Month == month.Month)
                .Sum(r => r.Amount);

            var actualOut = certified
                .Where(c => c.Year == month.Year && c.Month == month.Month)
                .Sum(c => c.Amount);

            var projectedOut = isPast ? actualOut : monthlySpend;

            var movement = (isPast ? actualIn : projectedIn) - projectedOut;
            position += movement;

            result.Months.Add(new CashFlowMonthDto
            {
                Month = month,
                Label = month.ToString("MMM yyyy"),
                ProjectedCollections = RealEstateMapper.Money(projectedIn),
                ActualCollections = RealEstateMapper.Money(actualIn),
                ProjectedSpend = RealEstateMapper.Money(projectedOut),
                ActualSpend = RealEstateMapper.Money(actualOut),
                NetMovement = RealEstateMapper.Money(movement),
                ClosingPosition = RealEstateMapper.Money(position),
                IsActual = isPast,
            });
        }

        result.TotalProjectedInflow = RealEstateMapper.Money(result.Months.Sum(m => m.IsActual ? m.ActualCollections : m.ProjectedCollections));
        result.TotalProjectedOutflow = RealEstateMapper.Money(result.Months.Sum(m => m.ProjectedSpend));
        result.NetPosition = RealEstateMapper.Money(result.TotalProjectedInflow - result.TotalProjectedOutflow);

        var worst = result.Months.OrderBy(m => m.ClosingPosition).FirstOrDefault();

        if (worst is not null && worst.ClosingPosition < 0m)
        {
            result.PeakFundingGap = Math.Abs(worst.ClosingPosition);
            result.PeakGapMonth = worst.Month;
        }

        return result;
    }

    // ═══ Site plans ══════════════════════════════════════════════════════════

    public async Task<List<SitePlanDto>> GetSitePlansAsync(Guid projectId)
    {
        var plans = await Db.SitePlans.ForCompany(Tenant)
            .Where(s => s.ProjectId == projectId)
            .Include(s => s.Shapes)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        if (plans.Count == 0) return [];

        // The shapes carry live status so the plan renders as the inventory board itself.
        var unitIds = plans.SelectMany(p => p.Shapes)
            .Where(s => s.UnitId.HasValue)
            .Select(s => s.UnitId!.Value)
            .Distinct()
            .ToList();

        var statuses = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant)
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Status);

        return plans.Select(p => new SitePlanDto
        {
            Id = p.Id,
            Name = p.Name,
            ImageUrl = p.ImageUrl,
            ImageWidthPx = p.ImageWidthPx,
            ImageHeightPx = p.ImageHeightPx,
            IsDefault = p.IsDefault,
            Shapes = p.Shapes.OrderBy(s => s.SortOrder).Select(s => new SitePlanShapeDto
            {
                Id = s.Id,
                UnitId = s.UnitId,
                Label = s.Label,
                ShapeType = s.ShapeType,
                Points = s.Points,
                LabelX = s.LabelX,
                LabelY = s.LabelY,
                IsDecorative = s.IsDecorative,
                FillOverride = s.FillOverride,
                Status = s.UnitId is null ? null : statuses.GetValueOrDefault(s.UnitId.Value),
            }).ToList(),
        }).ToList();
    }

    public async Task<SitePlanDto> SaveSitePlanAsync(Guid projectId, SitePlanDto dto, Guid userId)
    {
        var plan = dto.Id != Guid.Empty
            ? await Db.SitePlans.ForCompany(Tenant).FirstOrDefaultAsync(s => s.Id == dto.Id)
            : null;

        if (plan is null)
        {
            plan = new SitePlan { ProjectId = projectId }.StampNew(Tenant, userId);
            Db.SitePlans.Add(plan);
        }
        else plan.StampUpdated(userId);

        plan.Name = dto.Name;
        plan.ImageUrl = dto.ImageUrl;
        plan.ImageWidthPx = dto.ImageWidthPx;
        plan.ImageHeightPx = dto.ImageHeightPx;

        if (dto.IsDefault)
        {
            var others = await Db.SitePlans.ForCompany(Tenant)
                .Where(s => s.ProjectId == projectId && s.Id != plan.Id && s.IsDefault)
                .ToListAsync();

            foreach (var other in others) { other.IsDefault = false; other.StampUpdated(userId); }
        }

        plan.IsDefault = dto.IsDefault;

        await Db.SaveChangesAsync();
        return (await GetSitePlansAsync(projectId)).First(s => s.Id == plan.Id);
    }

    /// <summary>
    /// Replaces the polygons on a plan. Binding a shape to a unit is what makes the drawing
    /// clickable, so a unit already bound elsewhere on the same plan is rejected by name rather
    /// than silently rebound — two hotspots for one plot is a support call.
    /// </summary>
    public async Task<SitePlanDto> SaveShapesAsync(Guid sitePlanId, List<SitePlanShapeDto> shapes, Guid userId)
    {
        var plan = await Db.SitePlans.ForCompany(Tenant)
            .Include(s => s.Shapes)
            .FirstOrDefaultAsync(s => s.Id == sitePlanId)
            ?? throw new InvalidOperationException("That site plan does not exist.");

        var bound = shapes.Where(s => s.UnitId.HasValue).GroupBy(s => s.UnitId!.Value).Where(g => g.Count() > 1).ToList();

        if (bound.Count > 0)
        {
            var numbers = await Db.Units.ForCompany(Tenant)
                .Where(u => bound.Select(b => b.Key).Contains(u.Id))
                .Select(u => u.UnitNumber)
                .ToListAsync();

            throw new InvalidOperationException($"These units have more than one shape on the plan: {string.Join(", ", numbers)}.");
        }

        Db.SitePlanShapes.RemoveRange(plan.Shapes);

        var order = 0;

        foreach (var s in shapes)
        {
            Db.SitePlanShapes.Add(new SitePlanShape
            {
                SitePlanId = sitePlanId,
                UnitId = s.UnitId,
                Label = s.Label,
                ShapeType = s.ShapeType,
                Points = s.Points,
                LabelX = s.LabelX,
                LabelY = s.LabelY,
                IsDecorative = s.IsDecorative,
                FillOverride = s.FillOverride,
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();

        // Point each unit back at its shape so the board can jump from a list row to the map.
        var saved = await Db.SitePlanShapes.ForCompany(Tenant)
            .Where(s => s.SitePlanId == sitePlanId && s.UnitId != null)
            .ToListAsync();

        var units = await Db.Units.ForCompany(Tenant)
            .Where(u => saved.Select(s => s.UnitId!.Value).Contains(u.Id))
            .ToListAsync();

        foreach (var unit in units)
        {
            unit.SitePlanShapeId = saved.First(s => s.UnitId == unit.Id).Id;
            unit.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        return (await GetSitePlansAsync(plan.ProjectId)).First(s => s.Id == sitePlanId);
    }

    // ═══ Plot files ══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<PlotFileDto>> GetPlotFilesAsync(Guid projectId, ListQueryDto query)
    {
        var q = Db.PlotFiles.ForCompany(Tenant)
            .Where(f => f.ProjectId == projectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                f => f.FileNumber.Contains(query.Search!)
                     || f.CategoryCode.Contains(query.Search!)
                     || (f.AllottedPlotNumber != null && f.AllottedPlotNumber.Contains(query.Search!)))
            .OrderBy(f => f.FileNumber);

        return await PageAsync(q, query, MapPlotFilesAsync);
    }

    private async Task<List<PlotFileDto>> MapPlotFilesAsync(List<PlotFile> files)
    {
        if (files.Count == 0) return [];

        var unit = await AreaUnitAsync();
        var projects = await ProjectNamesAsync(files.Select(f => (Guid?)f.ProjectId));

        var bookingIds = files.Where(f => f.CurrentBookingId.HasValue)
            .Select(f => f.CurrentBookingId!.Value).Distinct().ToList();

        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant)
                .Where(b => bookingIds.Contains(b.Id))
                .Select(b => new { b.Id, b.PrimaryApplicantPartyId })
                .ToListAsync();

        var owners = await PartyNamesAsync(bookings.Select(b => b.PrimaryApplicantPartyId));

        return files.Select(f =>
        {
            var booking = bookings.FirstOrDefault(b => b.Id == f.CurrentBookingId);

            return new PlotFileDto
            {
                Id = f.Id,
                ProjectId = f.ProjectId,
                ProjectName = projects.GetValueOrDefault(f.ProjectId),
                FileNumber = f.FileNumber,
                CategoryCode = f.CategoryCode,
                NominalArea = RealEstateMapper.Area(f.NominalAreaSqFt, unit),
                SubType = f.SubType,
                Status = f.Status,
                Price = f.Price,
                OwnerName = booking is null ? null : owners.GetValueOrDefault(booking.PrimaryApplicantPartyId),
                CurrentBookingId = f.CurrentBookingId,
                IssuedOn = f.IssuedOn,
                BallotId = f.BallotId,
                AllottedUnitId = f.AllottedUnitId,
                AllottedPlotNumber = f.AllottedPlotNumber,
                AllottedOn = f.AllottedOn,
                IsDuplicateIssued = f.IsDuplicateIssued,
                IsCancelled = f.IsCancelled,
            };
        }).ToList();
    }

    /// <summary>
    /// Issues a run of files for a category. File numbers come from a per-project, per-category
    /// series and are never reused, because a file number is the tradeable instrument itself and
    /// a repeat is indistinguishable from a forgery.
    /// </summary>
    public async Task<List<PlotFileDto>> IssuePlotFilesAsync(
        Guid projectId, string categoryCode, int count, decimal price, decimal areaSqFt, Guid userId)
    {
        _ = await RequireAsync<Project>(projectId, "That project does not exist.");

        if (count is <= 0 or > 5000)
            throw new InvalidOperationException("Issue between 1 and 5000 files in one run.");

        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new InvalidOperationException("Give the category a code — for example 5M for five marla.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var today = Today;
        var created = new List<PlotFile>();

        for (var i = 0; i < count; i++)
        {
            var file = new PlotFile
            {
                ProjectId = projectId,
                FileNumber = await numbering.NextPlotFileNumberAsync(projectId, categoryCode),
                CategoryCode = categoryCode,
                NominalAreaSqFt = areaSqFt,
                Price = price,
                Status = PropertyStatus.Available,
                IssuedOn = today,
            }.StampNew(Tenant, userId);

            Db.PlotFiles.Add(file);
            created.Add(file);
        }

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await MapPlotFilesAsync(created);
    }
}
