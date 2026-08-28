using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The programme, progress measurement, interim payment certificates, retention and advances.
///
/// The interim certificate is the most consequential arithmetic in the whole application: it is
/// what a contractor gets paid, and every deduction on it is a conversation. So it is built in the
/// order the contract reads — gross value to date, less previously certified, less retention up to
/// its cap, less advance recovery, less contra-charges — and it returns its workings in words. A
/// certificate a quantity surveyor cannot follow line by line is one that gets disputed.
/// </summary>
public partial class ConstructionService
{
    // ═══ Programme ═══════════════════════════════════════════════════════════

    public async Task<List<ProgrammeActivityDto>> GetProgrammeAsync(Guid constructionProjectId)
    {
        var activities = await Db.ProgrammeActivities.ForCompany(Tenant)
            .Where(a => a.ConstructionProjectId == constructionProjectId)
            .OrderBy(a => a.SortOrder).ThenBy(a => a.PlannedStart)
            .ToListAsync();

        if (activities.Count == 0) return [];

        var ids = activities.Select(a => a.Id).ToList();

        var dependencies = await Db.ActivityDependencies.ForCompany(Tenant)
            .Where(d => ids.Contains(d.ProgrammeActivityId))
            .Select(d => new { d.ProgrammeActivityId, d.PredecessorActivityId })
            .ToListAsync();

        var responsible = await AgentUserNamesAsync(activities.Select(a => a.ResponsibleUserId));

        var subIds = activities.Where(a => a.SubcontractId.HasValue).Select(a => a.SubcontractId!.Value).Distinct().ToList();

        var subcontractors = subIds.Count == 0
            ? []
            : await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => subIds.Contains(s.Id))
                .Join(Db.Contractors.ForCompany(Tenant), s => s.ContractorId, c => c.Id, (s, c) => new { s.Id, c.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        var today = Today;

        return activities.Select(a =>
        {
            // Variance against the baseline, not against last week's plan. Re-baselining silently
            // is how a programme reports itself on time for eighteen months.
            var variance = a.BaselineFinish is null
                ? (int?)null
                : (a.ActualFinish ?? a.PlannedFinish).DayNumber - a.BaselineFinish.Value.DayNumber;

            return new ProgrammeActivityDto
            {
                Id = a.Id,
                WbsNodeId = a.WbsNodeId,
                Code = a.Code ?? string.Empty,
                Name = a.Name,
                PlannedStart = a.PlannedStart,
                PlannedFinish = a.PlannedFinish,
                DurationDays = a.DurationDays,
                BaselineStart = a.BaselineStart,
                BaselineFinish = a.BaselineFinish,
                ActualStart = a.ActualStart,
                ActualFinish = a.ActualFinish,
                ProgressPercent = a.ProgressPercent,
                TotalFloatDays = a.TotalFloatDays,
                IsCritical = a.IsCritical,
                IsMilestone = a.IsMilestone,
                ProjectMilestoneId = a.ProjectMilestoneId,
                ResponsibleName = a.ResponsibleUserId is null ? null : responsible.GetValueOrDefault(a.ResponsibleUserId.Value),
                SubcontractId = a.SubcontractId,
                SubcontractorName = a.SubcontractId is null ? null : subcontractors.GetValueOrDefault(a.SubcontractId.Value),
                VarianceDays = variance,

                // Behind means the elapsed share of the duration is ahead of the reported
                // progress. It catches the activity that has been "80% done" for a month.
                IsBehindSchedule = a.ActualFinish is null
                                   && a.PlannedStart <= today
                                   && a.ProgressPercent < ElapsedPercent(a, today),

                SortOrder = a.SortOrder,
                PredecessorIds = dependencies.Where(d => d.ProgrammeActivityId == a.Id)
                    .Select(d => d.PredecessorActivityId).ToList(),
            };
        }).ToList();
    }

    private static decimal ElapsedPercent(ProgrammeActivity a, DateOnly today)
    {
        var total = Math.Max(1, a.PlannedFinish.DayNumber - a.PlannedStart.DayNumber + 1);
        var elapsed = Math.Clamp(today.DayNumber - a.PlannedStart.DayNumber + 1, 0, total);

        return RealEstateMapper.Percent(elapsed, total);
    }

    public async Task<ProgrammeActivityDto> SaveActivityAsync(Guid constructionProjectId, ProgrammeActivityDto dto, Guid userId)
    {
        var activity = dto.Id != Guid.Empty
            ? await Db.ProgrammeActivities.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.Id)
            : null;

        if (activity is null)
        {
            activity = new ProgrammeActivity { ConstructionProjectId = constructionProjectId }.StampNew(Tenant, userId);
            Db.ProgrammeActivities.Add(activity);
        }
        else activity.StampUpdated(userId);

        if (dto.PlannedFinish < dto.PlannedStart)
            throw new InvalidOperationException("An activity cannot finish before it starts.");

        activity.WbsNodeId = dto.WbsNodeId;
        activity.Code = dto.Code;
        activity.Name = dto.Name;
        activity.PlannedStart = dto.PlannedStart;
        activity.PlannedFinish = dto.PlannedFinish;
        activity.DurationDays = dto.PlannedFinish.DayNumber - dto.PlannedStart.DayNumber + 1;
        activity.ActualStart = dto.ActualStart;
        activity.ActualFinish = dto.ActualFinish;
        activity.ProgressPercent = Math.Clamp(dto.ProgressPercent, 0m, 100m);
        activity.IsMilestone = dto.IsMilestone;
        activity.ProjectMilestoneId = dto.ProjectMilestoneId;
        activity.SubcontractId = dto.SubcontractId;
        activity.SortOrder = dto.SortOrder;

        // An activity reported complete is complete. Leaving it at 99% with an actual finish is
        // the state that makes every progress report meaningless.
        if (dto.ActualFinish is not null) activity.ProgressPercent = 100m;
        if (dto.ActualStart is not null && activity.ProgressPercent <= 0m) activity.ProgressPercent = 1m;

        await Db.SaveChangesAsync();

        if (dto.PredecessorIds.Count > 0)
        {
            var existing = await Db.ActivityDependencies.ForCompany(Tenant)
                .Where(d => d.ProgrammeActivityId == activity.Id)
                .ToListAsync();

            Db.ActivityDependencies.RemoveRange(existing);

            foreach (var predecessor in dto.PredecessorIds.Where(p => p != activity.Id))
            {
                Db.ActivityDependencies.Add(new ActivityDependency
                {
                    ProgrammeActivityId = activity.Id,
                    PredecessorActivityId = predecessor,
                }.StampNew(Tenant, userId));
            }

            await Db.SaveChangesAsync();
        }

        return (await GetProgrammeAsync(constructionProjectId)).First(a => a.Id == activity.Id);
    }

    /// <summary>
    /// Forward and backward pass over the activity network. Float is finish-to-start with lag,
    /// and anything with no float is critical — which is the set of activities where a day lost is
    /// a day lost on the whole job rather than absorbed.
    /// </summary>
    public async Task<List<ProgrammeActivityDto>> RecalculateCriticalPathAsync(Guid constructionProjectId, Guid userId)
    {
        var activities = await Db.ProgrammeActivities.ForCompany(Tenant)
            .Where(a => a.ConstructionProjectId == constructionProjectId)
            .ToListAsync();

        if (activities.Count == 0) return [];

        var ids = activities.Select(a => a.Id).ToList();

        var dependencies = await Db.ActivityDependencies.ForCompany(Tenant)
            .Where(d => ids.Contains(d.ProgrammeActivityId))
            .Select(d => new { d.ProgrammeActivityId, d.PredecessorActivityId, d.LagDays })
            .ToListAsync();

        var byId = activities.ToDictionary(a => a.Id, a => a);

        var earlyStart = new Dictionary<Guid, int>();
        var earlyFinish = new Dictionary<Guid, int>();

        // Forward pass, iterated rather than recursive so a badly-formed network cannot blow the
        // stack. Convergence is bounded by the activity count.
        for (var pass = 0; pass < activities.Count + 1; pass++)
        {
            var changed = false;

            foreach (var activity in activities)
            {
                var predecessors = dependencies.Where(d => d.ProgrammeActivityId == activity.Id).ToList();

                var start = predecessors.Count == 0
                    ? activity.PlannedStart.DayNumber
                    : predecessors
                        .Where(p => earlyFinish.ContainsKey(p.PredecessorActivityId))
                        .Select(p => earlyFinish[p.PredecessorActivityId] + 1 + p.LagDays)
                        .DefaultIfEmpty(activity.PlannedStart.DayNumber)
                        .Max();

                if (!earlyStart.TryGetValue(activity.Id, out var current) || current != start)
                {
                    earlyStart[activity.Id] = start;
                    earlyFinish[activity.Id] = start + Math.Max(0, activity.DurationDays - 1);
                    changed = true;
                }
            }

            if (!changed) break;
        }

        var projectFinish = earlyFinish.Count == 0 ? 0 : earlyFinish.Values.Max();

        var lateFinish = new Dictionary<Guid, int>();

        // Backward pass. An activity with no successors can finish as late as the project does.
        for (var pass = 0; pass < activities.Count + 1; pass++)
        {
            var changed = false;

            foreach (var activity in activities)
            {
                var successors = dependencies.Where(d => d.PredecessorActivityId == activity.Id).ToList();

                var finish = successors.Count == 0
                    ? projectFinish
                    : successors
                        .Where(s => lateFinish.ContainsKey(s.ProgrammeActivityId) && byId.ContainsKey(s.ProgrammeActivityId))
                        .Select(s => lateFinish[s.ProgrammeActivityId] - Math.Max(0, byId[s.ProgrammeActivityId].DurationDays - 1) - 1 - s.LagDays)
                        .DefaultIfEmpty(projectFinish)
                        .Min();

                if (!lateFinish.TryGetValue(activity.Id, out var current) || current != finish)
                {
                    lateFinish[activity.Id] = finish;
                    changed = true;
                }
            }

            if (!changed) break;
        }

        foreach (var activity in activities)
        {
            var slack = lateFinish.GetValueOrDefault(activity.Id, projectFinish)
                      - earlyFinish.GetValueOrDefault(activity.Id, projectFinish);

            activity.TotalFloatDays = slack;
            activity.IsCritical = slack <= 0;
            activity.StampUpdated(userId);
        }

        // The project's forecast completion is the last early finish across the network.
        var project = await Db.ConstructionProjects.ForCompany(Tenant)
            .FirstOrDefaultAsync(p => p.Id == constructionProjectId);

        if (project is not null && projectFinish > 0)
        {
            project.ForecastCompletionDate = DateOnly.FromDayNumber(projectFinish);
            project.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        return await GetProgrammeAsync(constructionProjectId);
    }

    /// <summary>
    /// Stamps the current plan as the baseline. Done once at contract award, and again only when
    /// an extension of time is granted — because the baseline is what delay is measured against.
    /// </summary>
    public async Task<List<ProgrammeActivityDto>> BaselineAsync(Guid constructionProjectId, Guid userId)
    {
        var activities = await Db.ProgrammeActivities.ForCompany(Tenant)
            .Where(a => a.ConstructionProjectId == constructionProjectId)
            .ToListAsync();

        if (activities.Count == 0)
            throw new InvalidOperationException("There is no programme to baseline.");

        var rebaseline = activities.Any(a => a.BaselineStart is not null);

        foreach (var activity in activities)
        {
            activity.BaselineStart = activity.PlannedStart;
            activity.BaselineFinish = activity.PlannedFinish;
            activity.StampUpdated(userId);
        }

        await WriteAuditNoteAsync(
            "ConstructionProject", constructionProjectId,
            rebaseline ? "ProgrammeRebaselined" : "ProgrammeBaselined", Guid.Empty, userId,
            note: rebaseline
                ? $"{activities.Count} activities re-baselined. Prior delay measurement no longer applies."
                : $"{activities.Count} activities baselined.",
            highRisk: rebaseline);

        await Db.SaveChangesAsync();
        return await GetProgrammeAsync(constructionProjectId);
    }

    // ═══ Progress measurement ════════════════════════════════════════════════

    public async Task<ProgressMeasurementDto> SaveProgressAsync(ProgressMeasurementDto dto, Guid userId)
    {
        var project = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var measurement = dto.Id != Guid.Empty
            ? await Db.ProgressMeasurements.ForCompany(Tenant).Include(m => m.Lines).FirstOrDefaultAsync(m => m.Id == dto.Id)
            : null;

        if (measurement is null)
        {
            measurement = new ProgressMeasurement
            {
                Reference = await numbering.NextMasterCodeAsync(Db.ProgressMeasurements, "PRG"),
                ConstructionProjectId = dto.ConstructionProjectId,
                SubcontractId = dto.SubcontractId,
                WbsNodeId = dto.WbsNodeId,
            }.StampNew(Tenant, userId);

            Db.ProgressMeasurements.Add(measurement);
        }
        else
        {
            // Once certified, a measurement is the basis of a payment. It is superseded by a new
            // one rather than edited.
            if (measurement.IsCertified)
                throw new InvalidOperationException("This measurement has been certified. Record a new one for the next period.");

            measurement.StampUpdated(userId);
        }

        measurement.PeriodFrom = dto.PeriodFrom;
        measurement.PeriodTo = dto.PeriodTo;
        measurement.MeasuredOn = dto.MeasuredOn == default ? Today : dto.MeasuredOn;
        measurement.MeasuredByUserId = userId;
        measurement.Method = dto.Method;
        measurement.PhotoUrls = dto.PhotoUrls.Count == 0 ? null : string.Join('\n', dto.PhotoUrls);
        measurement.Note = dto.Note;

        await Db.SaveChangesAsync();
        await SaveProgressLinesAsync(measurement, dto.Lines, userId);

        return (await MapProgressAsync([measurement]))[0];
    }

    private async Task SaveProgressLinesAsync(ProgressMeasurement measurement, List<ProgressMeasurementLineDto> lines, Guid userId)
    {
        if (lines.Count == 0) return;

        var boqLineIds = lines.Select(l => l.BoqLineId).Distinct().ToList();

        var boqLines = await Db.BoqLines.ForCompany(Tenant)
            .Where(l => boqLineIds.Contains(l.Id))
            .ToListAsync();

        var existing = await Db.ProgressMeasurementLines.ForCompany(Tenant)
            .Where(l => l.ProgressMeasurementId == measurement.Id)
            .ToListAsync();

        Db.ProgressMeasurementLines.RemoveRange(existing);

        var total = 0m;

        foreach (var dto in lines)
        {
            var boqLine = boqLines.FirstOrDefault(l => l.Id == dto.BoqLineId);
            if (boqLine is null) continue;

            var contractQuantity = boqLine.RemeasuredQuantity ?? boqLine.Quantity;
            var cumulative = dto.PreviousQuantity + dto.ThisPeriodQuantity;

            // Over-measurement is the commonest way a contractor gets paid for work that does not
            // exist. Beyond the contract quantity it needs a variation, not a bigger number.
            if (cumulative > contractQuantity * 1.001m && !boqLine.IsVariation)
            {
                throw new InvalidOperationException(
                    $"{boqLine.ItemCode}: cumulative {cumulative:N2} {boqLine.Uom} exceeds the contract quantity of " +
                    $"{contractQuantity:N2}. Remeasure the item or raise a variation.");
            }

            var value = RealEstateMapper.Money(dto.ThisPeriodQuantity * boqLine.Rate);

            measurement.Lines.Add(new ProgressMeasurementLine
            {
                ProgressMeasurementId = measurement.Id,
                BoqLineId = dto.BoqLineId,
                PreviousQuantity = dto.PreviousQuantity,
                ThisPeriodQuantity = dto.ThisPeriodQuantity,
                CumulativeQuantity = cumulative,
                Rate = boqLine.Rate,
                ThisPeriodValue = value,
                CumulativeValue = RealEstateMapper.Money(cumulative * boqLine.Rate),
                MeasurementNote = dto.MeasurementNote,
                Location = dto.Location,
            }.StampNew(Tenant, userId));

            total += value;
        }

        measurement.PeriodValue = RealEstateMapper.Money(total);

        measurement.CumulativeValue = RealEstateMapper.Money(
            measurement.Lines.Sum(l => l.CumulativeValue));

        var contractTotal = boqLines.Sum(l => l.Amount);

        measurement.ProgressPercent = RealEstateMapper.Percent(measurement.CumulativeValue, contractTotal);

        await Db.SaveChangesAsync();
    }

    /// <summary>
    /// Replays a measurement captured on site without a connection. Idempotent by client
    /// reference, because a surveyor's tablet on a half-built floor loses signal constantly.
    /// </summary>
    public async Task<ProgressMeasurementDto> SyncProgressAsync(ProgressSyncBatchDto batch, Guid userId)
    {
        if (!string.IsNullOrWhiteSpace(batch.ClientReference))
        {
            var existing = await Db.ProgressMeasurements.ForCompany(Tenant)
                .Include(m => m.Lines)
                .FirstOrDefaultAsync(m => m.Code == batch.ClientReference);

            if (existing is not null) return (await MapProgressAsync([existing]))[0];
        }

        var measurement = new ProgressMeasurement
        {
            Reference = await numbering.NextMasterCodeAsync(Db.ProgressMeasurements, "PRG"),
            ConstructionProjectId = batch.ConstructionProjectId,
            SubcontractId = batch.SubcontractId,
            PeriodFrom = batch.PeriodFrom,
            PeriodTo = batch.PeriodTo,
            MeasuredOn = batch.MeasuredOn == default ? Today : batch.MeasuredOn,
            MeasuredByUserId = userId,
            WasOffline = true,
            OfflineSyncedAt = DateTime.UtcNow,
            PhotoUrls = batch.PhotoUrls.Count == 0 ? null : string.Join('\n', batch.PhotoUrls),
            Note = batch.Note,
            Code = batch.ClientReference,
        }.StampNew(Tenant, userId);

        Db.ProgressMeasurements.Add(measurement);
        await Db.SaveChangesAsync();

        await SaveProgressLinesAsync(measurement, batch.Lines, userId);

        return (await MapProgressAsync([measurement]))[0];
    }

    /// <summary>
    /// Certifies a measurement, which is the moment it becomes payable. The BOQ's executed and
    /// certified quantities move here and nowhere else, so what has been certified can never
    /// exceed what was measured.
    /// </summary>
    public async Task<ProgressMeasurementDto> CertifyProgressAsync(Guid id, Guid userId)
    {
        var measurement = await Db.ProgressMeasurements.ForCompany(Tenant)
            .Include(m => m.Lines)
            .FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new InvalidOperationException("That measurement does not exist.");

        if (measurement.IsCertified)
            throw new InvalidOperationException($"This measurement was certified on {measurement.CertifiedOn:dd MMM yyyy}.");

        if (measurement.Lines.Count == 0)
            throw new InvalidOperationException("There is nothing to certify — this measurement has no lines.");

        // The person who measured cannot be the person who certifies. That separation is the
        // whole control on quantity fraud.
        if (measurement.MeasuredByUserId == userId)
            throw new InvalidOperationException("The person who took a measurement cannot also certify it.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var boqLineIds = measurement.Lines.Select(l => l.BoqLineId).ToList();

        var boqLines = await Db.BoqLines.ForCompany(Tenant)
            .Where(l => boqLineIds.Contains(l.Id))
            .ToListAsync();

        foreach (var line in measurement.Lines)
        {
            var boqLine = boqLines.FirstOrDefault(l => l.Id == line.BoqLineId);
            if (boqLine is null) continue;

            line.CertifiedQuantity = line.ThisPeriodQuantity;
            line.CertifiedValue = line.ThisPeriodValue;
            line.StampUpdated(userId);

            boqLine.ExecutedQuantity = line.CumulativeQuantity;
            boqLine.CertifiedQuantity = RealEstateMapper.Money(boqLine.CertifiedQuantity + line.ThisPeriodQuantity, 4);

            var contractQuantity = boqLine.RemeasuredQuantity ?? boqLine.Quantity;
            boqLine.ProgressPercent = RealEstateMapper.Percent(boqLine.ExecutedQuantity, contractQuantity);

            boqLine.StampUpdated(userId);
        }

        measurement.IsCertified = true;
        measurement.CertifiedByUserId = userId;
        measurement.CertifiedOn = Today;
        measurement.StampUpdated(userId);

        await Db.SaveChangesAsync();
        await RollUpProjectProgressAsync(measurement.ConstructionProjectId, userId);
        await transaction.CommitAsync();

        return (await MapProgressAsync([measurement]))[0];
    }

    private async Task RollUpProjectProgressAsync(Guid constructionProjectId, Guid userId)
    {
        var project = await Db.ConstructionProjects.ForCompany(Tenant)
            .FirstOrDefaultAsync(p => p.Id == constructionProjectId);

        if (project is null) return;

        var boqIds = await Db.BillsOfQuantities.ForCompany(Tenant)
            .Where(b => b.ConstructionProjectId == constructionProjectId && b.IsCurrent && b.BoqType == "Contract")
            .Select(b => b.Id)
            .ToListAsync();

        if (boqIds.Count == 0) return;

        var lines = await Db.BoqLines.ForCompany(Tenant)
            .Where(l => boqIds.Contains(l.BillOfQuantitiesId))
            .Select(l => new { l.Amount, l.ExecutedQuantity, l.Quantity, l.RemeasuredQuantity, l.Rate, l.WbsNodeId })
            .ToListAsync();

        var contractTotal = lines.Sum(l => l.Amount);
        var executedValue = lines.Sum(l => l.ExecutedQuantity * l.Rate);

        project.PhysicalProgressPercent = RealEstateMapper.Percent(executedValue, contractTotal);

        project.FinancialProgressPercent = project.RevisedContractValue > 0m
            ? RealEstateMapper.Percent(project.CertifiedValue, project.RevisedContractValue)
            : 0m;

        project.StampUpdated(userId);

        // Earned value on each WBS node, which is what makes the cost-performance index real.
        var nodes = await Db.WbsNodes.ForCompany(Tenant)
            .Where(n => n.ConstructionProjectId == constructionProjectId)
            .ToListAsync();

        foreach (var node in nodes)
        {
            var mine = lines.Where(l => l.WbsNodeId == node.Id).ToList();
            if (mine.Count == 0) continue;

            var nodeContract = mine.Sum(l => l.Amount);
            var nodeExecuted = mine.Sum(l => l.ExecutedQuantity * l.Rate);

            node.ProgressPercent = RealEstateMapper.Percent(nodeExecuted, nodeContract);
            node.EarnedValue = RealEstateMapper.Money(node.BudgetAmount * node.ProgressPercent / 100m);
            node.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<ProgressMeasurementDto>> GetProgressAsync(
        ListQueryDto query, Guid? constructionProjectId, Guid? subcontractId)
    {
        var q = Db.ProgressMeasurements.ForCompany(Tenant)
            .Include(m => m.Lines)
            .WhereIf(constructionProjectId.HasValue, m => m.ConstructionProjectId == constructionProjectId)
            .WhereIf(subcontractId.HasValue, m => m.SubcontractId == subcontractId)
            .WhereIf(query.FromDate.HasValue, m => m.PeriodFrom >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, m => m.PeriodTo <= query.ToDate)
            .OrderByDescending(m => m.PeriodTo);

        return await PageAsync(q, query, MapProgressAsync);
    }

    private async Task<List<ProgressMeasurementDto>> MapProgressAsync(List<ProgressMeasurement> measurements)
    {
        if (measurements.Count == 0) return [];

        var currency = await CurrencyAsync();

        var projectIds = measurements.Select(m => m.ConstructionProjectId).Distinct().ToList();

        var projects = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var subIds = measurements.Where(m => m.SubcontractId.HasValue).Select(m => m.SubcontractId!.Value).Distinct().ToList();

        var subcontractors = subIds.Count == 0
            ? []
            : await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => subIds.Contains(s.Id))
                .Join(Db.Contractors.ForCompany(Tenant), s => s.ContractorId, c => c.Id, (s, c) => new { s.Id, c.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        var nodeIds = measurements.Where(m => m.WbsNodeId.HasValue).Select(m => m.WbsNodeId!.Value).Distinct().ToList();

        var nodes = nodeIds.Count == 0
            ? []
            : await Db.WbsNodes.ForCompany(Tenant).Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        var users = await AgentUserNamesAsync(
            measurements.Select(m => m.MeasuredByUserId).Concat(measurements.Select(m => m.CertifiedByUserId)));

        var boqLineIds = measurements.SelectMany(m => m.Lines).Select(l => l.BoqLineId).Distinct().ToList();

        var boqLines = boqLineIds.Count == 0
            ? []
            : await Db.BoqLines.ForCompany(Tenant)
                .Where(l => boqLineIds.Contains(l.Id))
                .Select(l => new { l.Id, l.ItemCode, l.Description, l.Uom, l.Quantity, l.RemeasuredQuantity })
                .ToListAsync();

        return measurements.Select(m => new ProgressMeasurementDto
        {
            Id = m.Id,
            Reference = m.Reference,
            ConstructionProjectId = m.ConstructionProjectId,
            ProjectName = projects.GetValueOrDefault(m.ConstructionProjectId),
            SubcontractId = m.SubcontractId,
            SubcontractorName = m.SubcontractId is null ? null : subcontractors.GetValueOrDefault(m.SubcontractId.Value),
            WbsNodeId = m.WbsNodeId,
            WbsName = m.WbsNodeId is null ? null : nodes.GetValueOrDefault(m.WbsNodeId.Value),
            PeriodFrom = m.PeriodFrom,
            PeriodTo = m.PeriodTo,
            MeasuredOn = m.MeasuredOn,
            MeasuredByName = m.MeasuredByUserId is null ? null : users.GetValueOrDefault(m.MeasuredByUserId.Value),
            PeriodValue = m.PeriodValue,
            CumulativeValue = m.CumulativeValue,
            ProgressPercent = m.ProgressPercent,
            CurrencyCode = currency,
            Method = m.Method,
            IsCertified = m.IsCertified,
            CertifiedByName = m.CertifiedByUserId is null ? null : users.GetValueOrDefault(m.CertifiedByUserId.Value),
            CertifiedOn = m.CertifiedOn,
            InterimPaymentCertificateId = m.InterimPaymentCertificateId,
            WasOffline = m.WasOffline,
            PhotoUrls = string.IsNullOrWhiteSpace(m.PhotoUrls)
                ? []
                : m.PhotoUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Note = m.Note,

            Lines = m.Lines.Select(l =>
            {
                var boq = boqLines.FirstOrDefault(b => b.Id == l.BoqLineId);

                return new ProgressMeasurementLineDto
                {
                    Id = l.Id,
                    BoqLineId = l.BoqLineId,
                    ItemCode = boq?.ItemCode ?? "—",
                    Description = boq?.Description ?? string.Empty,
                    Uom = boq?.Uom ?? string.Empty,
                    ContractQuantity = boq is null ? 0m : boq.RemeasuredQuantity ?? boq.Quantity,
                    PreviousQuantity = l.PreviousQuantity,
                    ThisPeriodQuantity = l.ThisPeriodQuantity,
                    CumulativeQuantity = l.CumulativeQuantity,
                    Rate = l.Rate,
                    ThisPeriodValue = l.ThisPeriodValue,
                    CumulativeValue = l.CumulativeValue,
                    CertifiedQuantity = l.CertifiedQuantity,
                    CertifiedValue = l.CertifiedValue,
                    MeasurementNote = l.MeasurementNote,
                    Location = l.Location,
                };
            }).ToList(),
        }).ToList();
    }

    // ═══ Interim payment certificates ════════════════════════════════════════

    /// <summary>
    /// Builds the certificate. This is the arithmetic every contractor checks line by line, so it
    /// is done in the order the contract reads it and every step is written out.
    /// </summary>
    public async Task<InterimPaymentCertificateDetailDto> PrepareIpcAsync(IpcCreateDto dto, Guid userId)
    {
        var project = await RequireAsync<ConstructionProject>(dto.ConstructionProjectId, "That construction project does not exist.");

        var subcontract = dto.SubcontractId is null
            ? null
            : await RequireAsync<Subcontract>(dto.SubcontractId.Value, "That subcontract does not exist.");

        var currency = project.CurrencyCode;
        var workings = new List<string>();

        // Everything certified before this one, on the same contract and in the same direction.
        var previous = await Db.InterimPaymentCertificates.ForCompany(Tenant)
            .Where(c => c.ConstructionProjectId == dto.ConstructionProjectId
                     && c.Direction == dto.Direction
                     && c.SubcontractId == dto.SubcontractId
                     && c.ClientBuildContractId == dto.ClientBuildContractId
                     && c.Status != CertificateStatus.Rejected)
            .ToListAsync();

        var sequence = previous.Count == 0 ? 1 : previous.Max(p => p.SequenceNumber) + 1;
        var previouslyCertified = RealEstateMapper.Money(previous.Sum(p => p.ThisCertificateGross));

        var certificate = new InterimPaymentCertificate
        {
            CertificateNumber = dto.DryRun ? "(preview)" : await numbering.NextIpcNumberAsync(DateTime.UtcNow),
            ConstructionProjectId = dto.ConstructionProjectId,
            SubcontractId = dto.SubcontractId,
            ClientBuildContractId = dto.ClientBuildContractId,
            Direction = dto.Direction,
            SequenceNumber = sequence,
            PeriodFrom = dto.PeriodFrom,
            PeriodTo = dto.PeriodTo,
            IssuedOn = dto.IssuedOn == default ? Today : dto.IssuedOn,
            Status = CertificateStatus.Draft,
            CurrencyCode = currency,
            MeasuredByUserId = userId,
            PreviouslyCertified = previouslyCertified,
            MaterialsOnSite = dto.MaterialsOnSite,
            Penalties = dto.Penalties,
            OtherDeductions = dto.OtherDeductions,
            Note = dto.Note,
        }.StampNew(Tenant, userId);

        certificate.DueDate = certificate.IssuedOn.AddDays(subcontract?.PaymentTermDays ?? 30);

        // Step 1 — work done to date, from the certified quantities on the BOQ.
        var order = 0;
        var workDone = 0m;
        var variations = 0m;

        var boqLineIds = dto.Lines.Where(l => l.BoqLineId.HasValue).Select(l => l.BoqLineId!.Value).Distinct().ToList();

        var boqLines = boqLineIds.Count == 0
            ? []
            : await Db.BoqLines.ForCompany(Tenant).Where(l => boqLineIds.Contains(l.Id)).ToListAsync();

        foreach (var l in dto.Lines)
        {
            var boqLine = l.BoqLineId is null ? null : boqLines.FirstOrDefault(b => b.Id == l.BoqLineId);
            var rate = boqLine?.Rate ?? l.Rate;
            var certifiedValue = RealEstateMapper.Money(l.CertifiedQuantity * rate);

            certificate.Lines.Add(new IpcLine
            {
                BoqLineId = l.BoqLineId,
                VariationOrderId = l.VariationOrderId,
                WbsNodeId = l.WbsNodeId,
                Description = l.Description,
                Uom = l.Uom ?? boqLine?.Uom,
                ContractQuantity = boqLine is null ? l.ContractQuantity : boqLine.RemeasuredQuantity ?? boqLine.Quantity,
                Rate = rate,
                PreviousQuantity = l.PreviousQuantity,
                ClaimedQuantity = l.ClaimedQuantity,
                CertifiedQuantity = l.CertifiedQuantity,
                CertifiedValue = certifiedValue,
                CertificationNote = l.CertificationNote,
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));

            if (l.VariationOrderId is not null || boqLine?.IsVariation == true) variations += certifiedValue;
            else workDone += certifiedValue;
        }

        certificate.WorkDoneToDate = RealEstateMapper.Money(workDone);
        certificate.VariationsToDate = RealEstateMapper.Money(variations);

        workings.Add($"Measured work to date: {certificate.WorkDoneToDate:N0}.");
        if (variations > 0m) workings.Add($"Approved variations to date: {certificate.VariationsToDate:N0}.");
        if (dto.MaterialsOnSite > 0m) workings.Add($"Materials on site: {dto.MaterialsOnSite:N0}.");

        certificate.GrossValueToDate = RealEstateMapper.Money(
            certificate.WorkDoneToDate + certificate.VariationsToDate + certificate.MaterialsOnSite);

        // Step 2 — this certificate is the movement since the last one.
        certificate.ThisCertificateGross = RealEstateMapper.Money(
            certificate.GrossValueToDate - previouslyCertified);

        workings.Add($"Gross to date {certificate.GrossValueToDate:N0} less previously certified {previouslyCertified:N0} " +
                     $"gives {certificate.ThisCertificateGross:N0} for this certificate.");

        if (certificate.ThisCertificateGross < 0m)
        {
            workings.Add("This certificate is negative — the measured value has fallen since the last one. " +
                         "That is legitimate on remeasurement but should be explained.");
        }

        // Step 3 — retention, capped. The cap is the point most systems get wrong: retention stops
        // accruing once the cumulative held reaches the cap percentage of the contract value.
        var retentionPercent = subcontract?.RetentionPercent ?? project.DefaultRetentionPercent;
        var capPercent = subcontract?.RetentionCapPercent ?? project.RetentionCapPercent;
        var contractValue = subcontract?.RevisedValue ?? project.RevisedContractValue;

        certificate.RetentionPercent = retentionPercent;

        var retentionCumulativeBefore = RealEstateMapper.Money(previous.Sum(p => p.RetentionThisCertificate));
        var cap = capPercent > 0m ? RealEstateMapper.Money(contractValue * capPercent / 100m) : decimal.MaxValue;

        var retentionThis = RealEstateMapper.Money(certificate.ThisCertificateGross * retentionPercent / 100m);

        if (retentionCumulativeBefore + retentionThis > cap)
        {
            var capped = Math.Max(0m, cap - retentionCumulativeBefore);

            workings.Add(
                $"Retention at {retentionPercent:N1}% would be {retentionThis:N0}, but the cumulative cap of " +
                $"{capPercent:N1}% ({cap:N0}) limits it to {capped:N0}.");

            retentionThis = capped;
        }
        else if (retentionThis > 0m)
        {
            workings.Add($"Retention at {retentionPercent:N1}% of {certificate.ThisCertificateGross:N0} is {retentionThis:N0}.");
        }

        certificate.RetentionThisCertificate = retentionThis;
        certificate.RetentionCumulative = RealEstateMapper.Money(retentionCumulativeBefore + retentionThis);

        // Step 4 — advance recovery, which starts only once progress passes the agreed threshold.
        var advance = await Db.AdvancePayments.ForCompany(Tenant)
            .Where(a => a.ConstructionProjectId == dto.ConstructionProjectId
                     && a.SubcontractId == dto.SubcontractId
                     && a.OutstandingAmount > 0m)
            .FirstOrDefaultAsync();

        if (advance is not null)
        {
            var progress = contractValue > 0m
                ? RealEstateMapper.Percent(certificate.GrossValueToDate, contractValue)
                : 0m;

            if (progress >= advance.RecoveryStartsAtProgressPercent)
            {
                var recovery = RealEstateMapper.Money(
                    Math.Min(advance.OutstandingAmount, certificate.ThisCertificateGross * advance.RecoveryPercent / 100m));

                certificate.AdvanceRecovery = Math.Max(0m, recovery);

                workings.Add(
                    $"Advance recovery at {advance.RecoveryPercent:N1}% of this certificate: {certificate.AdvanceRecovery:N0}. " +
                    $"{advance.OutstandingAmount - certificate.AdvanceRecovery:N0} of the advance remains outstanding.");
            }
            else
            {
                workings.Add(
                    $"Advance recovery has not started — progress is {progress:N1}% against a threshold of " +
                    $"{advance.RecoveryStartsAtProgressPercent:N1}%.");
            }
        }

        // Step 5 — contra-charges. Materials the main contractor supplied, plant they lent, or
        // work they had to put right. Every one carries its own evidence.
        if (dto.ContraChargeIds.Count > 0)
        {
            var charges = await Db.ContraCharges.ForCompany(Tenant)
                .Where(c => dto.ContraChargeIds.Contains(c.Id) && !c.IsRecovered)
                .ToListAsync();

            var disputed = charges.Where(c => c.IsDisputed).Select(c => c.Reference).ToList();

            if (disputed.Count > 0)
                throw new InvalidOperationException($"These contra-charges are disputed and cannot be deducted yet: {string.Join(", ", disputed)}.");

            certificate.ContraCharges = RealEstateMapper.Money(charges.Sum(c => c.Amount));

            if (certificate.ContraCharges > 0m)
                workings.Add($"Contra-charges deducted: {certificate.ContraCharges:N0} across {charges.Count} items.");
        }

        certificate.NetBeforeTax = RealEstateMapper.Money(
            certificate.ThisCertificateGross
            - certificate.RetentionThisCertificate
            - certificate.AdvanceRecovery
            - certificate.ContraCharges
            - certificate.Penalties
            - certificate.OtherDeductions);

        if (certificate.Penalties > 0m) workings.Add($"Penalties: {certificate.Penalties:N0}.");
        if (certificate.OtherDeductions > 0m) workings.Add($"Other deductions: {certificate.OtherDeductions:N0}.");

        certificate.NetPayable = RealEstateMapper.Money(
            certificate.NetBeforeTax + certificate.TaxAmount - certificate.WithholdingTax);

        workings.Add($"Net payable: {certificate.NetPayable:N0}, due {certificate.DueDate:dd MMM yyyy}.");

        if (dto.DryRun)
        {
            var preview = await MapIpcDetailAsync(certificate, workings);
            return preview;
        }

        Db.InterimPaymentCertificates.Add(certificate);
        await Db.SaveChangesAsync();

        return await MapIpcDetailAsync(certificate, workings);
    }

    public async Task<InterimPaymentCertificateDetailDto> CertifyIpcAsync(Guid id, Guid userId)
    {
        var certificate = await Db.InterimPaymentCertificates.ForCompany(Tenant)
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("That certificate does not exist.");

        if (certificate.Status != CertificateStatus.Draft)
            throw new InvalidOperationException($"This certificate is {certificate.Status} and cannot be certified again.");

        // The measurer cannot certify their own measurement. Same control as progress.
        if (certificate.MeasuredByUserId == userId)
            throw new InvalidOperationException("The person who measured a certificate cannot also certify it.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        certificate.Status = CertificateStatus.Certified;
        certificate.CertifiedByUserId = userId;
        certificate.CertifiedOn = Today;
        certificate.StampUpdated(userId);

        // Retention moves into the ledger the moment it is certified, so the balance a contractor
        // is owed at the end of the job is a running total rather than a reconstruction.
        if (certificate.RetentionThisCertificate > 0m)
        {
            var balance = await Db.RetentionLedgerEntries.ForCompany(Tenant)
                .Where(r => r.ConstructionProjectId == certificate.ConstructionProjectId
                         && r.SubcontractId == certificate.SubcontractId)
                .OrderByDescending(r => r.EntryDate)
                .Select(r => r.RunningBalance)
                .FirstOrDefaultAsync();

            var subcontract = certificate.SubcontractId is null
                ? null
                : await Db.Subcontracts.ForCompany(Tenant).FirstOrDefaultAsync(s => s.Id == certificate.SubcontractId);

            Db.RetentionLedgerEntries.Add(new RetentionLedgerEntry
            {
                ConstructionProjectId = certificate.ConstructionProjectId,
                SubcontractId = certificate.SubcontractId,
                ClientBuildContractId = certificate.ClientBuildContractId,
                Direction = certificate.Direction,
                Movement = RetentionMovement.Held,
                EntryDate = certificate.CertifiedOn.Value,
                InterimPaymentCertificateId = certificate.Id,
                Amount = certificate.RetentionThisCertificate,
                RunningBalance = RealEstateMapper.Money(balance + certificate.RetentionThisCertificate),

                // Half is normally released at practical completion and half at the end of the
                // defects period. The due date is set now so the release is diarised, not chased.
                DueForReleaseOn = subcontract?.FinishDate.AddMonths(subcontract.DefectsPeriodMonths),
            }.StampNew(Tenant, userId));

            if (subcontract is not null)
            {
                subcontract.RetentionHeld = RealEstateMapper.Money(subcontract.RetentionHeld + certificate.RetentionThisCertificate);
                subcontract.StampUpdated(userId);
            }
        }

        if (certificate.AdvanceRecovery > 0m)
        {
            var advance = await Db.AdvancePayments.ForCompany(Tenant)
                .Where(a => a.ConstructionProjectId == certificate.ConstructionProjectId
                         && a.SubcontractId == certificate.SubcontractId
                         && a.OutstandingAmount > 0m)
                .FirstOrDefaultAsync();

            if (advance is not null)
            {
                advance.RecoveredAmount = RealEstateMapper.Money(advance.RecoveredAmount + certificate.AdvanceRecovery);
                advance.OutstandingAmount = RealEstateMapper.Money(advance.Amount - advance.RecoveredAmount);

                if (advance.OutstandingAmount <= 0.01m) advance.FullyRecoveredOn = Today;

                advance.StampUpdated(userId);

                Db.AdvanceRecoveries.Add(new AdvanceRecovery
                {
                    AdvancePaymentId = advance.Id,
                    InterimPaymentCertificateId = certificate.Id,
                    RecoveredOn = Today,
                    CertificateGross = certificate.ThisCertificateGross,
                    RecoveryPercent = advance.RecoveryPercent,
                    Amount = certificate.AdvanceRecovery,
                    BalanceAfter = advance.OutstandingAmount,
                }.StampNew(Tenant, userId));
            }
        }

        // Contra-charges are spent once deducted, so they cannot be recovered twice.
        if (certificate.ContraCharges > 0m && certificate.SubcontractId is not null)
        {
            var charges = await Db.ContraCharges.ForCompany(Tenant)
                .Where(c => c.SubcontractId == certificate.SubcontractId && !c.IsRecovered)
                .ToListAsync();

            foreach (var charge in charges)
            {
                charge.IsRecovered = true;
                charge.StampUpdated(userId);
            }
        }

        var project = await Db.ConstructionProjects.ForCompany(Tenant)
            .FirstOrDefaultAsync(p => p.Id == certificate.ConstructionProjectId);

        if (project is not null)
        {
            project.CertifiedValue = RealEstateMapper.Money(project.CertifiedValue + certificate.ThisCertificateGross);
            project.RetentionHeld = RealEstateMapper.Money(project.RetentionHeld + certificate.RetentionThisCertificate);
            project.StampUpdated(userId);
        }

        if (certificate.SubcontractId is not null)
        {
            var subcontract = await Db.Subcontracts.ForCompany(Tenant)
                .FirstOrDefaultAsync(s => s.Id == certificate.SubcontractId);

            if (subcontract is not null)
            {
                subcontract.CertifiedToDate = RealEstateMapper.Money(subcontract.CertifiedToDate + certificate.ThisCertificateGross);

                subcontract.ProgressPercent = subcontract.RevisedValue > 0m
                    ? RealEstateMapper.Percent(subcontract.CertifiedToDate, subcontract.RevisedValue)
                    : 0m;

                subcontract.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await GetIpcAsync(id))!;
    }

    public async Task<InterimPaymentCertificateDetailDto> ApproveIpcAsync(
        Guid id, ApprovalOutcome outcome, string? comment, Guid userId)
    {
        var certificate = await Db.InterimPaymentCertificates.ForCompany(Tenant)
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("That certificate does not exist.");

        if (certificate.Status != CertificateStatus.Certified)
            throw new InvalidOperationException("Only a certified certificate can be approved for payment.");

        certificate.Status = outcome is ApprovalOutcome.Approved or ApprovalOutcome.AutoApproved
            ? CertificateStatus.Approved
            : CertificateStatus.Rejected;

        certificate.Note = comment ?? certificate.Note;
        certificate.StampUpdated(userId);

        await WriteAuditNoteAsync(
            "InterimPaymentCertificate", id, $"Ipc{outcome}", Guid.Empty, userId,
            amountImpact: certificate.NetPayable,
            note: comment,
            entityReference: certificate.CertificateNumber,
            highRisk: true);

        await Db.SaveChangesAsync();
        return (await GetIpcAsync(id))!;
    }

    public async Task<PaginatedResponse<InterimPaymentCertificateListItemDto>> GetIpcsAsync(
        ListQueryDto query, Guid? constructionProjectId, string? direction, CertificateStatus? status)
    {
        var q = Db.InterimPaymentCertificates.ForCompany(Tenant)
            .WhereIf(constructionProjectId.HasValue, c => c.ConstructionProjectId == constructionProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(direction), c => c.Direction == direction)
            .WhereIf(status.HasValue, c => c.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), c => c.CertificateNumber.Contains(query.Search!))
            .OrderByDescending(c => c.IssuedOn);

        return await PageAsync(q, query, MapIpcListAsync);
    }

    private async Task<List<InterimPaymentCertificateListItemDto>> MapIpcListAsync(List<InterimPaymentCertificate> certificates)
    {
        if (certificates.Count == 0) return [];

        var today = Today;

        var projectIds = certificates.Select(c => c.ConstructionProjectId).Distinct().ToList();

        var projects = await Db.ConstructionProjects.ForCompany(Tenant)
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var subIds = certificates.Where(c => c.SubcontractId.HasValue).Select(c => c.SubcontractId!.Value).Distinct().ToList();

        var subcontractors = subIds.Count == 0
            ? []
            : await Db.Subcontracts.ForCompany(Tenant)
                .Where(s => subIds.Contains(s.Id))
                .Join(Db.Contractors.ForCompany(Tenant), s => s.ContractorId, c => c.Id, (s, c) => new { s.Id, c.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        var contractIds = certificates.Where(c => c.ClientBuildContractId.HasValue)
            .Select(c => c.ClientBuildContractId!.Value).Distinct().ToList();

        var clients = contractIds.Count == 0
            ? []
            : await Db.ClientBuildContracts.ForCompany(Tenant)
                .Where(c => contractIds.Contains(c.Id))
                .Join(Db.Parties.ForCompany(Tenant), c => c.ClientPartyId, p => p.Id, (c, p) => new { c.Id, p.DisplayName })
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName);

        var certifiers = await AgentUserNamesAsync(certificates.Select(c => c.CertifiedByUserId));

        return certificates.Select(c => new InterimPaymentCertificateListItemDto
        {
            Id = c.Id,
            CertificateNumber = c.CertificateNumber,
            Direction = c.Direction,
            SequenceNumber = c.SequenceNumber,
            ConstructionProjectId = c.ConstructionProjectId,
            ProjectName = projects.GetValueOrDefault(c.ConstructionProjectId, "—"),
            SubcontractId = c.SubcontractId,
            SubcontractorName = c.SubcontractId is null ? null : subcontractors.GetValueOrDefault(c.SubcontractId.Value),
            ClientBuildContractId = c.ClientBuildContractId,
            ClientName = c.ClientBuildContractId is null ? null : clients.GetValueOrDefault(c.ClientBuildContractId.Value),
            PeriodFrom = c.PeriodFrom,
            PeriodTo = c.PeriodTo,
            IssuedOn = c.IssuedOn,
            DueDate = c.DueDate,
            Status = c.Status,
            GrossValueToDate = c.GrossValueToDate,
            ThisCertificateGross = c.ThisCertificateGross,
            RetentionThisCertificate = c.RetentionThisCertificate,
            AdvanceRecovery = c.AdvanceRecovery,
            ContraCharges = c.ContraCharges,
            NetPayable = c.NetPayable,
            PaidAmount = c.PaidAmount,
            CurrencyCode = c.CurrencyCode,

            // Late payment on a certificate carries statutory interest in most jurisdictions, so
            // it is worth surfacing rather than discovering in a claim.
            IsOverdue = c.PaidAmount < c.NetPayable && c.DueDate is not null && c.DueDate < today,

            CertifiedByName = c.CertifiedByUserId is null ? null : certifiers.GetValueOrDefault(c.CertifiedByUserId.Value),
        }).ToList();
    }

    public async Task<InterimPaymentCertificateDetailDto?> GetIpcAsync(Guid id)
    {
        var certificate = await Db.InterimPaymentCertificates.ForCompany(Tenant)
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (certificate is null) return null;
        return await MapIpcDetailAsync(certificate, []);
    }

    private async Task<InterimPaymentCertificateDetailDto> MapIpcDetailAsync(
        InterimPaymentCertificate c, List<string> workings)
    {
        var head = (await MapIpcListAsync([c]))[0];
        var measurers = await AgentUserNamesAsync([c.MeasuredByUserId]);

        var detail = new InterimPaymentCertificateDetailDto
        {
            Id = head.Id,
            CertificateNumber = head.CertificateNumber,
            Direction = head.Direction,
            SequenceNumber = head.SequenceNumber,
            ConstructionProjectId = head.ConstructionProjectId,
            ProjectName = head.ProjectName,
            SubcontractId = head.SubcontractId,
            SubcontractorName = head.SubcontractorName,
            ClientBuildContractId = head.ClientBuildContractId,
            ClientName = head.ClientName,
            PeriodFrom = head.PeriodFrom,
            PeriodTo = head.PeriodTo,
            IssuedOn = head.IssuedOn,
            DueDate = head.DueDate,
            Status = head.Status,
            GrossValueToDate = head.GrossValueToDate,
            ThisCertificateGross = head.ThisCertificateGross,
            RetentionThisCertificate = head.RetentionThisCertificate,
            AdvanceRecovery = head.AdvanceRecovery,
            ContraCharges = head.ContraCharges,
            NetPayable = head.NetPayable,
            PaidAmount = head.PaidAmount,
            CurrencyCode = head.CurrencyCode,
            IsOverdue = head.IsOverdue,
            CertifiedByName = head.CertifiedByName,

            WorkDoneToDate = c.WorkDoneToDate,
            VariationsToDate = c.VariationsToDate,
            MaterialsOnSite = c.MaterialsOnSite,
            PreviouslyCertified = c.PreviouslyCertified,
            RetentionPercent = c.RetentionPercent,
            RetentionCumulative = c.RetentionCumulative,
            Penalties = c.Penalties,
            OtherDeductions = c.OtherDeductions,
            NetBeforeTax = c.NetBeforeTax,
            TaxAmount = c.TaxAmount,
            WithholdingTax = c.WithholdingTax,
            MeasuredByName = c.MeasuredByUserId is null ? null : measurers.GetValueOrDefault(c.MeasuredByUserId.Value),
            CertifiedOn = c.CertifiedOn,
            Note = c.Note,
            Workings = workings,

            Lines = c.Lines.OrderBy(l => l.SortOrder).Select(l => new IpcLineDto
            {
                Id = l.Id,
                BoqLineId = l.BoqLineId,
                VariationOrderId = l.VariationOrderId,
                WbsNodeId = l.WbsNodeId,
                Description = l.Description ?? string.Empty,
                Uom = l.Uom,
                ContractQuantity = l.ContractQuantity,
                Rate = l.Rate,
                PreviousQuantity = l.PreviousQuantity,
                ClaimedQuantity = l.ClaimedQuantity,
                CertifiedQuantity = l.CertifiedQuantity,
                CertifiedValue = l.CertifiedValue,

                // What was claimed but not allowed. The contractor will ask about exactly this.
                DisallowedValue = RealEstateMapper.Money(Math.Max(0m, (l.ClaimedQuantity - l.CertifiedQuantity) * l.Rate)),

                CertificationNote = l.CertificationNote,
                SortOrder = l.SortOrder,
            }).ToList(),
        };

        if (c.SubcontractId is not null)
        {
            var charges = await Db.ContraCharges.ForCompany(Tenant)
                .Where(x => x.SubcontractId == c.SubcontractId && x.SubcontractorClaimId == null)
                .ToListAsync();

            detail.ContraChargeLines = await MapContraChargesAsync(charges);
        }

        return detail;
    }
}
