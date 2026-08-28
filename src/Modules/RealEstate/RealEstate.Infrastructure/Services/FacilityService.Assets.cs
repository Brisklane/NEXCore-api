using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Contractors, planned maintenance, assets, service contracts and inspection rounds.
///
/// Planned maintenance is where a building either stays compliant or quietly does not. A statutory
/// task that is skipped is not the same as one that is late, so the two are distinguished, the
/// compliance percentage is computed from what actually happened, and generating the next task
/// never produces a duplicate no matter how often the job runs.
/// </summary>
public partial class FacilityService
{
    // ═══ Contractors ═════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ContractorListItemDto>> GetContractorsAsync(
        ListQueryDto query, string? trade, bool? approvedOnly)
    {
        var tradeIds = string.IsNullOrWhiteSpace(trade)
            ? null
            : Db.ContractorTrades.ForCompany(Tenant).Where(t => t.Trade == trade).Select(t => t.ContractorId);

        var q = Db.Contractors.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                c => c.Name.Contains(query.Search!) || c.Reference.Contains(query.Search!))
            .WhereIf(approvedOnly == true, c => c.IsApproved && !c.IsSuspended)
            .WhereIf(tradeIds is not null, c => tradeIds!.Contains(c.Id))
            .OrderBy(c => c.Name);

        return await PageAsync(q, query, MapContractorListAsync);
    }

    private async Task<List<ContractorListItemDto>> MapContractorListAsync(List<Contractor> contractors)
    {
        if (contractors.Count == 0) return [];

        var today = Today;
        var soon = today.AddDays(30);
        var currency = await CurrencyAsync();
        var ids = contractors.Select(c => c.Id).ToList();

        var trades = await Db.ContractorTrades.ForCompany(Tenant)
            .Where(t => ids.Contains(t.ContractorId))
            .Select(t => new { t.ContractorId, t.Trade })
            .ToListAsync();

        var compliance = await Db.ContractorCompliances.ForCompany(Tenant)
            .Where(c => ids.Contains(c.ContractorId))
            .Select(c => new { c.ContractorId, c.ExpiresOn, c.BlocksAssignmentWhenExpired })
            .ToListAsync();

        var openJobs = await Db.WorkOrders.ForCompany(Tenant)
            .Where(w => w.ContractorId != null && ids.Contains(w.ContractorId.Value)
                     && w.Status != WorkOrderStatus.SignedOff
                     && w.Status != WorkOrderStatus.Cancelled)
            .GroupBy(w => w.ContractorId!.Value)
            .Select(g => new { ContractorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ContractorId, x => x.Count);

        return contractors.Select(c => new ContractorListItemDto
        {
            Id = c.Id,
            Reference = c.Reference,
            Name = c.Name,
            ContactName = c.ContactName,
            Phone = c.Phone,
            EmergencyPhone = c.EmergencyPhone,
            Email = c.Email,
            Trades = trades.Where(t => t.ContractorId == c.Id).Select(t => t.Trade).ToList(),
            IsApproved = c.IsApproved,
            IsSuspended = c.IsSuspended,
            IsEmergencyContractor = c.IsEmergencyContractor,
            ResponseSlaHours = c.ResponseSlaHours,
            JobsCompleted = c.JobsCompleted,
            OpenJobs = openJobs.GetValueOrDefault(c.Id),
            AverageCost = c.AverageCost,
            AverageDaysToComplete = c.AverageDaysToComplete,
            AverageRating = c.AverageRating,
            ReworkCount = c.ReworkCount,
            SlaBreachCount = c.SlaBreachCount,
            CurrencyCode = currency,

            // An expired certificate blocks assignment outright, so it belongs on the list row
            // rather than three clicks away.
            HasExpiredCompliance = compliance.Any(x => x.ContractorId == c.Id && x.BlocksAssignmentWhenExpired && x.ExpiresOn < today),
            ComplianceExpiringCount = compliance.Count(x => x.ContractorId == c.Id && x.ExpiresOn >= today && x.ExpiresOn <= soon),
        }).ToList();
    }

    public async Task<ContractorDetailDto?> GetContractorAsync(Guid id)
    {
        var contractor = await Db.Contractors.ForCompany(Tenant)
            .Include(c => c.Trades)
            .Include(c => c.Compliance)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contractor is null) return null;

        var head = (await MapContractorListAsync([contractor]))[0];
        var today = Today;

        var detail = new ContractorDetailDto
        {
            Id = head.Id,
            Reference = head.Reference,
            Name = head.Name,
            ContactName = head.ContactName,
            Phone = head.Phone,
            EmergencyPhone = head.EmergencyPhone,
            Email = head.Email,
            Trades = head.Trades,
            IsApproved = head.IsApproved,
            IsSuspended = head.IsSuspended,
            IsEmergencyContractor = head.IsEmergencyContractor,
            ResponseSlaHours = head.ResponseSlaHours,
            JobsCompleted = head.JobsCompleted,
            OpenJobs = head.OpenJobs,
            AverageCost = head.AverageCost,
            AverageDaysToComplete = head.AverageDaysToComplete,
            AverageRating = head.AverageRating,
            ReworkCount = head.ReworkCount,
            SlaBreachCount = head.SlaBreachCount,
            CurrencyCode = head.CurrencyCode,
            HasExpiredCompliance = head.HasExpiredCompliance,
            ComplianceExpiringCount = head.ComplianceExpiringCount,

            PartyId = contractor.PartyId,
            SupplierId = contractor.SupplierId,
            AddressLine = contractor.AddressLine,
            RegistrationNumber = contractor.RegistrationNumber,
            TaxNumber = contractor.TaxNumber,
            ApprovedOn = contractor.ApprovedOn,
            SuspensionReason = contractor.SuspensionReason,

            TradeDetails = contractor.Trades.Select(t => new ContractorTradeDto
            {
                Id = t.Id,
                Trade = t.Trade,
                IsPrimary = t.IsPrimary,
                Certification = t.Certification,
            }).ToList(),

            Compliance = contractor.Compliance.OrderBy(c => c.ExpiresOn).Select(c => MapCompliance(c, today)).ToList(),
        };

        detail.Rates = await Db.ContractorRates.ForCompany(Tenant)
            .Where(r => r.ContractorId == id)
            .OrderBy(r => r.Trade).ThenByDescending(r => r.EffectiveFrom)
            .Select(r => new ContractorRateDto
            {
                Id = r.Id,
                Trade = r.Trade,
                Description = r.Description ?? r.RateType,
                RateType = r.RateType,
                Rate = r.Rate,
                OutOfHoursRate = r.OutOfHoursRate,
                CalloutCharge = r.CalloutCharge,
                Uom = r.Uom,
                EffectiveFrom = r.EffectiveFrom,
                EffectiveTo = r.EffectiveTo,
                IsActive = r.EffectiveFrom <= today && (r.EffectiveTo == null || r.EffectiveTo >= today),
            })
            .ToListAsync();

        detail.RecentJobs = await MapWorkOrderListAsync(
            await Db.WorkOrders.ForCompany(Tenant)
                .Where(w => w.ContractorId == id)
                .OrderByDescending(w => w.RaisedAt)
                .Take(25)
                .ToListAsync());

        return detail;
    }

    private static ContractorComplianceDto MapCompliance(ContractorCompliance c, DateOnly today) => new()
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
    };

    public async Task<ContractorDetailDto> SaveContractorAsync(ContractorDetailDto dto, Guid userId)
    {
        var contractor = dto.Id != Guid.Empty
            ? await Db.Contractors.ForCompany(Tenant).Include(c => c.Trades).FirstOrDefaultAsync(c => c.Id == dto.Id)
            : null;

        if (contractor is null)
        {
            contractor = new Contractor
            {
                Reference = string.IsNullOrWhiteSpace(dto.Reference)
                    ? await numbering.NextContractorReferenceAsync()
                    : dto.Reference,
            }.StampNew(Tenant, userId);

            Db.Contractors.Add(contractor);
        }
        else contractor.StampUpdated(userId);

        contractor.Name = dto.Name;
        contractor.PartyId = dto.PartyId;
        contractor.SupplierId = dto.SupplierId;
        contractor.ContactName = dto.ContactName;
        contractor.Phone = dto.Phone;
        contractor.EmergencyPhone = dto.EmergencyPhone;
        contractor.Email = dto.Email;
        contractor.AddressLine = dto.AddressLine;
        contractor.RegistrationNumber = dto.RegistrationNumber;
        contractor.TaxNumber = dto.TaxNumber;
        contractor.IsEmergencyContractor = dto.IsEmergencyContractor;
        contractor.ResponseSlaHours = dto.ResponseSlaHours <= 0 ? 24 : dto.ResponseSlaHours;
        contractor.IsSuspended = dto.IsSuspended;
        contractor.SuspensionReason = dto.SuspensionReason;

        if (dto.IsSuspended && string.IsNullOrWhiteSpace(dto.SuspensionReason))
            throw new InvalidOperationException("Suspending a contractor has to say why — it stops them being assigned anywhere.");

        // Approval is a decision with a date on it, so it is stamped once rather than toggled.
        if (dto.IsApproved && !contractor.IsApproved)
        {
            contractor.IsApproved = true;
            contractor.ApprovedOn = Today;
        }
        else if (!dto.IsApproved)
        {
            contractor.IsApproved = false;
            contractor.ApprovedOn = null;
        }

        if (dto.TradeDetails.Count > 0)
        {
            Db.ContractorTrades.RemoveRange(contractor.Trades);

            foreach (var t in dto.TradeDetails)
            {
                contractor.Trades.Add(new ContractorTrade
                {
                    Trade = t.Trade,
                    IsPrimary = t.IsPrimary,
                    Certification = t.Certification,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        return (await GetContractorAsync(contractor.Id))!;
    }

    public async Task<ContractorComplianceDto> SaveContractorComplianceAsync(
        Guid contractorId, ContractorComplianceDto dto, Guid userId)
    {
        _ = await RequireAsync<Contractor>(contractorId, "That contractor does not exist.");

        var compliance = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.ContractorCompliances.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
            : null;

        if (compliance is null)
        {
            compliance = new ContractorCompliance { ContractorId = contractorId }.StampNew(Tenant, userId);
            Db.ContractorCompliances.Add(compliance);
        }
        else compliance.StampUpdated(userId);

        compliance.ComplianceType = dto.ComplianceType;
        compliance.ReferenceNumber = dto.ReferenceNumber;
        compliance.Provider = dto.Provider;
        compliance.CoverAmount = dto.CoverAmount;
        compliance.IssuedOn = dto.IssuedOn;
        compliance.ExpiresOn = dto.ExpiresOn;
        compliance.DocumentUrl = dto.DocumentUrl;
        compliance.IsMandatory = dto.IsMandatory;
        compliance.BlocksAssignmentWhenExpired = dto.BlocksAssignmentWhenExpired;

        // Verified means somebody looked at the certificate. It cannot be true without one.
        if (dto.IsVerified && string.IsNullOrWhiteSpace(dto.DocumentUrl))
            throw new InvalidOperationException("Attach the certificate before marking it verified.");

        if (dto.IsVerified && !compliance.IsVerified)
        {
            compliance.IsVerified = true;
            compliance.VerifiedOn = Today;
        }

        await Db.SaveChangesAsync();
        return MapCompliance(compliance, Today);
    }

    /// <summary>
    /// Recomputes a contractor's scorecard from what they actually did. Derived every time rather
    /// than incremented, because an incremented average that drifts is worse than none.
    /// </summary>
    private async Task RefreshContractorStatsAsync(Guid contractorId, Guid userId)
    {
        var contractor = await Db.Contractors.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == contractorId);
        if (contractor is null) return;

        var jobs = await Db.WorkOrders.ForCompany(Tenant)
            .Where(w => w.ContractorId == contractorId && w.CompletedAt != null)
            .Select(w => new { w.TotalCost, w.RaisedAt, w.CompletedAt, w.SatisfactionRating })
            .ToListAsync();

        contractor.JobsCompleted = jobs.Count;

        if (jobs.Count > 0)
        {
            contractor.AverageCost = RealEstateMapper.Money(jobs.Average(j => j.TotalCost));

            contractor.AverageDaysToComplete = RealEstateMapper.Money(
                (decimal)jobs.Average(j => (j.CompletedAt!.Value - j.RaisedAt).TotalDays), 1);

            var rated = jobs.Where(j => j.SatisfactionRating is not null).ToList();

            contractor.AverageRating = rated.Count == 0
                ? null
                : RealEstateMapper.Money((decimal)rated.Average(j => j.SatisfactionRating!.Value), 1);
        }

        contractor.StampUpdated(userId);
        await Db.SaveChangesAsync();
    }

    // ═══ Planned maintenance ═════════════════════════════════════════════════

    public async Task<PaginatedResponse<PpmScheduleDto>> GetPpmSchedulesAsync(
        ListQueryDto query, Guid? propertyId, bool? overdueOnly)
    {
        var today = Today;

        var q = Db.PpmSchedules.ForCompany(Tenant)
            .WhereIf(propertyId.HasValue, s => s.PropertyId == propertyId)
            .WhereIf(overdueOnly == true, s => s.NextDueOn < today)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), s => s.Name.Contains(query.Search!))
            .OrderBy(s => s.NextDueOn);

        return await PageAsync(q, query, MapSchedulesAsync);
    }

    private async Task<List<PpmScheduleDto>> MapSchedulesAsync(List<PpmSchedule> schedules)
    {
        if (schedules.Count == 0) return [];

        var today = Today;
        var ids = schedules.Select(s => s.Id).ToList();

        var assetIds = schedules.Where(s => s.FacilityAssetId.HasValue).Select(s => s.FacilityAssetId!.Value).Distinct().ToList();

        var assets = assetIds.Count == 0
            ? []
            : await Db.FacilityAssets.ForCompany(Tenant).Where(a => assetIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Name);

        var propertyIds = schedules.Where(s => s.PropertyId.HasValue).Select(s => s.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        var societyIds = schedules.Where(s => s.SocietyId.HasValue).Select(s => s.SocietyId!.Value).Distinct().ToList();

        var societies = societyIds.Count == 0
            ? []
            : await Db.Societies.ForCompany(Tenant).Where(s => societyIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

        var contractorIds = schedules.Where(s => s.ContractorId.HasValue).Select(s => s.ContractorId!.Value).Distinct().ToList();

        var contractors = contractorIds.Count == 0
            ? []
            : await Db.Contractors.ForCompany(Tenant).Where(c => contractorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

        var tasks = await Db.PpmTasks.ForCompany(Tenant)
            .Where(t => ids.Contains(t.PpmScheduleId))
            .Select(t => new { t.PpmScheduleId, t.CompletedOn, t.DueDate, t.Status })
            .ToListAsync();

        return schedules.Select(s =>
        {
            var mine = tasks.Where(t => t.PpmScheduleId == s.Id).ToList();
            var completed = mine.Count(t => t.CompletedOn is not null);

            // Missed means the window closed and nobody did it. Late-but-done is not a miss, and
            // conflating the two makes a compliance figure that flatters the operator.
            var missed = mine.Count(t => t.CompletedOn is null && t.DueDate < today);

            return new PpmScheduleDto
            {
                Id = s.Id,
                Name = s.Name,
                FacilityAssetId = s.FacilityAssetId,
                AssetName = s.FacilityAssetId is null ? null : assets.GetValueOrDefault(s.FacilityAssetId.Value),
                PropertyId = s.PropertyId,
                AddressOneLine = s.PropertyId is null ? null : properties.GetValueOrDefault(s.PropertyId.Value),
                SocietyName = s.SocietyId is null ? null : societies.GetValueOrDefault(s.SocietyId.Value),
                Trade = s.Trade,
                TaskDescription = s.TaskDescription,
                Frequency = s.Frequency,
                IntervalDays = s.IntervalDays,
                LastCompletedOn = s.LastCompletedOn,
                NextDueOn = s.NextDueOn,
                DaysToDue = s.NextDueOn.DayNumber - today.DayNumber,
                IsOverdue = s.NextDueOn < today,
                GenerateDaysBefore = s.GenerateDaysBefore,
                ContractorName = s.ContractorId is null ? null : contractors.GetValueOrDefault(s.ContractorId.Value),
                EstimatedCost = s.EstimatedCost,
                Priority = s.Priority,
                IsStatutory = s.IsStatutory,
                IsActive = s.IsActive,
                CompletedCount = completed,
                MissedCount = missed,
                CompliancePercent = RealEstateMapper.Percent(completed, completed + missed),
            };
        }).ToList();
    }

    public async Task<PpmScheduleDto> SavePpmScheduleAsync(PpmScheduleDto dto, Guid userId)
    {
        var schedule = dto.Id != Guid.Empty
            ? await Db.PpmSchedules.ForCompany(Tenant).FirstOrDefaultAsync(s => s.Id == dto.Id)
            : null;

        if (schedule is null)
        {
            schedule = new PpmSchedule().StampNew(Tenant, userId);
            Db.PpmSchedules.Add(schedule);
        }
        else schedule.StampUpdated(userId);

        schedule.Name = dto.Name;
        schedule.FacilityAssetId = dto.FacilityAssetId;
        schedule.PropertyId = dto.PropertyId;
        schedule.Trade = dto.Trade;
        schedule.TaskDescription = dto.TaskDescription;
        schedule.Frequency = dto.Frequency;

        // The interval is derived from the frequency word so the two can never disagree, which
        // they always do when both are typed.
        schedule.IntervalDays = dto.IntervalDays > 0 ? dto.IntervalDays : dto.Frequency switch
        {
            "Daily" => 1,
            "Weekly" => 7,
            "Fortnightly" => 14,
            "Monthly" => 30,
            "Quarterly" => 91,
            "HalfYearly" => 182,
            "Yearly" => 365,
            "TwoYearly" => 730,
            "FiveYearly" => 1826,
            _ => 30,
        };

        schedule.NextDueOn = dto.NextDueOn == default ? Today.AddDays(schedule.IntervalDays) : dto.NextDueOn;
        schedule.GenerateDaysBefore = dto.GenerateDaysBefore <= 0 ? 7 : dto.GenerateDaysBefore;
        schedule.EstimatedCost = dto.EstimatedCost;
        schedule.Priority = dto.Priority;
        schedule.IsStatutory = dto.IsStatutory;
        schedule.IsActive = dto.IsActive;

        await Db.SaveChangesAsync();
        return (await MapSchedulesAsync([schedule]))[0];
    }

    public async Task<PaginatedResponse<PpmTaskDto>> GetPpmTasksAsync(ListQueryDto query, string? status)
    {
        var today = Today;

        var q = Db.PpmTasks.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(status), t => t.Status == status)
            .WhereIf(query.FromDate.HasValue, t => t.DueDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, t => t.DueDate <= query.ToDate)
            .OrderBy(t => t.DueDate);

        return await PageAsync(q, query, MapTasksAsync);
    }

    private async Task<List<PpmTaskDto>> MapTasksAsync(List<PpmTask> tasks)
    {
        if (tasks.Count == 0) return [];

        var today = Today;
        var scheduleIds = tasks.Select(t => t.PpmScheduleId).Distinct().ToList();

        var schedules = await Db.PpmSchedules.ForCompany(Tenant)
            .Where(s => scheduleIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, s.FacilityAssetId, s.PropertyId, s.IsStatutory })
            .ToListAsync();

        var assetIds = schedules.Where(s => s.FacilityAssetId.HasValue).Select(s => s.FacilityAssetId!.Value).Distinct().ToList();

        var assets = assetIds.Count == 0
            ? []
            : await Db.FacilityAssets.ForCompany(Tenant).Where(a => assetIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Name);

        var propertyIds = schedules.Where(s => s.PropertyId.HasValue).Select(s => s.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        var contractorIds = tasks.Where(t => t.ContractorId.HasValue).Select(t => t.ContractorId!.Value).Distinct().ToList();

        var contractors = contractorIds.Count == 0
            ? []
            : await Db.Contractors.ForCompany(Tenant).Where(c => contractorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

        var workOrderIds = tasks.Where(t => t.WorkOrderId.HasValue).Select(t => t.WorkOrderId!.Value).Distinct().ToList();

        var workOrders = workOrderIds.Count == 0
            ? []
            : await Db.WorkOrders.ForCompany(Tenant).Where(w => workOrderIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.OrderNumber);

        return tasks.Select(t =>
        {
            var schedule = schedules.FirstOrDefault(s => s.Id == t.PpmScheduleId);

            return new PpmTaskDto
            {
                Id = t.Id,
                PpmScheduleId = t.PpmScheduleId,
                ScheduleName = schedule?.Name ?? "—",
                AssetName = schedule?.FacilityAssetId is null ? null : assets.GetValueOrDefault(schedule.FacilityAssetId.Value),
                AddressOneLine = schedule?.PropertyId is null ? null : properties.GetValueOrDefault(schedule.PropertyId.Value),
                DueDate = t.DueDate,
                CompletedOn = t.CompletedOn,
                WorkOrderId = t.WorkOrderId,
                WorkOrderNumber = t.WorkOrderId is null ? null : workOrders.GetValueOrDefault(t.WorkOrderId.Value),
                ContractorName = t.ContractorId is null ? null : contractors.GetValueOrDefault(t.ContractorId.Value),
                Cost = t.Cost,
                Status = t.Status,
                IsOverdue = t.CompletedOn is null && t.DueDate < today,
                IsStatutory = schedule?.IsStatutory ?? false,
                CompletionNote = t.CompletionNote,
                CertificateUrl = t.CertificateUrl,
                SkipReason = t.SkipReason,
            };
        }).ToList();
    }

    /// <summary>
    /// Creates the tasks that fall due in the lead-in window. Idempotent: a schedule already
    /// carrying an open task for its next date is skipped, so running this hourly is harmless.
    /// </summary>
    public async Task<int> GeneratePpmTasksAsync(DateOnly asOf)
    {
        var today = asOf == default ? Today : asOf;

        var schedules = await Db.PpmSchedules.ForCompany(Tenant)
            .Where(s => s.IsActive)
            .ToListAsync();

        var due = schedules.Where(s => s.NextDueOn <= today.AddDays(s.GenerateDaysBefore)).ToList();
        if (due.Count == 0) return 0;

        var ids = due.Select(s => s.Id).ToList();

        var existing = await Db.PpmTasks.ForCompany(Tenant)
            .Where(t => ids.Contains(t.PpmScheduleId) && t.CompletedOn == null)
            .Select(t => new { t.PpmScheduleId, t.DueDate })
            .ToListAsync();

        var created = 0;

        foreach (var schedule in due)
        {
            if (existing.Any(e => e.PpmScheduleId == schedule.Id && e.DueDate == schedule.NextDueOn)) continue;

            var task = new PpmTask
            {
                PpmScheduleId = schedule.Id,
                DueDate = schedule.NextDueOn,
                ContractorId = schedule.ContractorId,
                Status = "Pending",
            }.StampNew(Tenant, Guid.Empty);

            Db.PpmTasks.Add(task);
            created++;

            // A statutory task becomes a work order immediately rather than waiting for somebody
            // to notice it. Missing a statutory inspection is an offence, not an oversight.
            if (schedule.IsStatutory)
            {
                var order = new WorkOrder
                {
                    OrderNumber = await numbering.NextWorkOrderNumberAsync(DateTime.UtcNow),
                    Source = WorkOrderSource.PlannedMaintenance,
                    PropertyId = schedule.PropertyId,
                    SocietyId = schedule.SocietyId,
                    ProjectId = schedule.ProjectId,
                    FacilityAssetId = schedule.FacilityAssetId,
                    Title = schedule.Name,
                    Description = schedule.TaskDescription,
                    Trade = schedule.Trade,
                    Priority = schedule.Priority,
                    Status = WorkOrderStatus.Raised,
                    ContractorId = schedule.ContractorId,
                    PpmTaskId = task.Id,
                    RaisedAt = DateTime.UtcNow,
                    EstimatedCost = schedule.EstimatedCost ?? 0m,
                    CompletionDueAt = schedule.NextDueOn.ToDateTime(TimeOnly.MaxValue),
                    IsAuthorised = true,
                    CostBearer = schedule.SocietyId is not null ? CostBearer.Society : CostBearer.Landlord,
                }.StampNew(Tenant, Guid.Empty);

                Db.WorkOrders.Add(order);
                task.WorkOrderId = order.Id;
            }
        }

        await Db.SaveChangesAsync();
        return created;
    }

    public async Task<PpmTaskDto> CompletePpmTaskAsync(
        Guid id, DateOnly completedOn, decimal? cost, string? note, string? certificateUrl, Guid userId)
    {
        var task = await RequireAsync<PpmTask>(id, "That maintenance task does not exist.");

        if (task.CompletedOn is not null)
            throw new InvalidOperationException($"This task was completed on {task.CompletedOn:dd MMM yyyy}.");

        var schedule = await RequireAsync<PpmSchedule>(task.PpmScheduleId, "The schedule behind this task is missing.");

        // A statutory task closes on a certificate. Without one there is no evidence the
        // inspection happened, which is the only thing that matters when it is questioned.
        if (schedule.IsStatutory && string.IsNullOrWhiteSpace(certificateUrl))
            throw new InvalidOperationException($"\"{schedule.Name}\" is a statutory task. Attach the certificate before closing it.");

        task.CompletedOn = completedOn;
        task.Cost = cost;
        task.CompletionNote = note;
        task.CertificateUrl = certificateUrl;
        task.Status = "Completed";
        task.StampUpdated(userId);

        schedule.LastCompletedOn = completedOn;

        // The next date runs from when it was actually done, not from when it was due — otherwise
        // a task completed three weeks late immediately falls due again.
        schedule.NextDueOn = completedOn.AddDays(schedule.IntervalDays);
        schedule.StampUpdated(userId);

        if (schedule.FacilityAssetId is not null)
        {
            var asset = await Db.FacilityAssets.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == schedule.FacilityAssetId);

            if (asset is not null)
            {
                asset.LastServicedOn = completedOn;
                asset.NextServiceDue = schedule.NextDueOn;
                if (cost is not null) asset.LifetimeMaintenanceCost = RealEstateMapper.Money(asset.LifetimeMaintenanceCost + cost.Value);
                asset.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();
        return (await MapTasksAsync([task]))[0];
    }

    // ═══ Assets ══════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<FacilityAssetDto>> GetAssetsAsync(ListQueryDto query, Guid? propertyId, AssetKind? kind)
    {
        var q = Db.FacilityAssets.ForCompany(Tenant)
            .WhereIf(propertyId.HasValue, a => a.PropertyId == propertyId)
            .WhereIf(kind.HasValue, a => a.Kind == kind)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                a => a.Name.Contains(query.Search!) || a.AssetCode.Contains(query.Search!)
                  || (a.SerialNumber != null && a.SerialNumber.Contains(query.Search!)))
            .OrderBy(a => a.Name);

        return await PageAsync(q, query, MapAssetsAsync);
    }

    public async Task<FacilityAssetDto?> GetAssetAsync(Guid id)
    {
        var asset = await Db.FacilityAssets.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == id);
        if (asset is null) return null;

        return (await MapAssetsAsync([asset]))[0];
    }

    private async Task<List<FacilityAssetDto>> MapAssetsAsync(List<FacilityAsset> assets)
    {
        if (assets.Count == 0) return [];

        var today = Today;
        var currency = await CurrencyAsync();

        var propertyIds = assets.Where(a => a.PropertyId.HasValue).Select(a => a.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        var societyIds = assets.Where(a => a.SocietyId.HasValue).Select(a => a.SocietyId!.Value).Distinct().ToList();

        var societies = societyIds.Count == 0
            ? []
            : await Db.Societies.ForCompany(Tenant).Where(s => societyIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

        var contractIds = assets.Where(a => a.ServiceContractId.HasValue).Select(a => a.ServiceContractId!.Value).Distinct().ToList();

        var contracts = contractIds.Count == 0
            ? []
            : await Db.ServiceContracts.ForCompany(Tenant).Where(c => contractIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

        return assets.Select(a => new FacilityAssetDto
        {
            Id = a.Id,
            Name = a.Name,
            AssetCode = a.AssetCode,
            Kind = a.Kind,
            PropertyId = a.PropertyId,
            AddressOneLine = a.PropertyId is null ? null : properties.GetValueOrDefault(a.PropertyId.Value),
            SocietyName = a.SocietyId is null ? null : societies.GetValueOrDefault(a.SocietyId.Value),
            Location = a.Location,
            Make = a.Make,
            Model = a.Model,
            SerialNumber = a.SerialNumber,
            Capacity = a.Capacity,
            InstalledOn = a.InstalledOn,
            WarrantyExpiresOn = a.WarrantyExpiresOn,
            WarrantyActive = a.WarrantyExpiresOn is not null && a.WarrantyExpiresOn >= today,
            ExpectedLifeYears = a.ExpectedLifeYears,
            AgeYears = a.InstalledOn is null ? null : (today.DayNumber - a.InstalledOn.Value.DayNumber) / 365,
            PurchaseCost = a.PurchaseCost,
            ReplacementCost = a.ReplacementCost,
            ServiceContractId = a.ServiceContractId,
            ServiceContractName = a.ServiceContractId is null ? null : contracts.GetValueOrDefault(a.ServiceContractId.Value),
            LastServicedOn = a.LastServicedOn,
            NextServiceDue = a.NextServiceDue,
            ServiceOverdue = a.NextServiceDue is not null && a.NextServiceDue < today,
            OperationalStatus = a.OperationalStatus,
            BreakdownCount = a.BreakdownCount,
            LifetimeMaintenanceCost = a.LifetimeMaintenanceCost,
            CurrencyCode = currency,
            ManualUrl = a.ManualUrl,
            IsCritical = a.IsCritical,
            IsActive = a.IsActive,
        }).ToList();
    }

    public async Task<FacilityAssetDto> SaveAssetAsync(FacilityAssetDto dto, Guid userId)
    {
        var asset = dto.Id != Guid.Empty
            ? await Db.FacilityAssets.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.Id)
            : null;

        if (asset is null)
        {
            asset = new FacilityAsset
            {
                AssetCode = string.IsNullOrWhiteSpace(dto.AssetCode)
                    ? await numbering.NextMasterCodeAsync(Db.FacilityAssets, "AST")
                    : dto.AssetCode,
            }.StampNew(Tenant, userId);

            Db.FacilityAssets.Add(asset);
        }
        else asset.StampUpdated(userId);

        asset.Name = dto.Name;
        asset.Kind = dto.Kind;
        asset.PropertyId = dto.PropertyId;
        asset.Location = dto.Location;
        asset.Make = dto.Make;
        asset.Model = dto.Model;
        asset.SerialNumber = dto.SerialNumber;
        asset.Capacity = dto.Capacity;
        asset.InstalledOn = dto.InstalledOn;
        asset.WarrantyExpiresOn = dto.WarrantyExpiresOn;
        asset.ExpectedLifeYears = dto.ExpectedLifeYears;
        asset.PurchaseCost = dto.PurchaseCost;
        asset.ReplacementCost = dto.ReplacementCost;
        asset.ServiceContractId = dto.ServiceContractId;
        asset.OperationalStatus = dto.OperationalStatus;
        asset.ManualUrl = dto.ManualUrl;
        asset.IsCritical = dto.IsCritical;
        asset.IsActive = dto.IsActive;

        await Db.SaveChangesAsync();
        return (await MapAssetsAsync([asset]))[0];
    }

    public async Task<AssetServiceRecordDto> RecordServiceAsync(Guid assetId, AssetServiceRecordDto dto, Guid userId)
    {
        var asset = await RequireAsync<FacilityAsset>(assetId, "That asset does not exist.");

        var record = new AssetServiceRecord
        {
            FacilityAssetId = assetId,
            ServicedOn = dto.ServicedOn == default ? Today : dto.ServicedOn,
            ServiceType = dto.ServiceType,
            WorkOrderId = dto.WorkOrderId,
            WorkDone = dto.WorkDone,
            PartsReplaced = dto.PartsReplaced,
            Cost = dto.Cost,
            DowntimeHours = dto.DowntimeHours,
            NextServiceDue = dto.NextServiceDue,
            CertificateUrl = dto.CertificateUrl,
            Findings = dto.Findings,
        }.StampNew(Tenant, userId);

        Db.AssetServiceRecords.Add(record);

        asset.LastServicedOn = record.ServicedOn;
        asset.NextServiceDue = dto.NextServiceDue ?? asset.NextServiceDue;
        asset.LifetimeMaintenanceCost = RealEstateMapper.Money(asset.LifetimeMaintenanceCost + dto.Cost);

        if (dto.ServiceType == "Breakdown") asset.BreakdownCount++;

        asset.StampUpdated(userId);

        await Db.SaveChangesAsync();

        dto.Id = record.Id;
        dto.ServicedOn = record.ServicedOn;
        return dto;
    }

    public async Task<List<ServiceContractDto>> GetServiceContractsAsync(Guid? propertyId, bool expiringOnly)
    {
        var today = Today;
        var currency = await CurrencyAsync();

        var contracts = await Db.ServiceContracts.ForCompany(Tenant)
            .WhereIf(propertyId.HasValue, c => c.PropertyId == propertyId)
            .WhereIf(expiringOnly, c => c.EndDate <= today.AddDays(90))
            .OrderBy(c => c.EndDate)
            .ToListAsync();

        if (contracts.Count == 0) return [];

        var ids = contracts.Select(c => c.Id).ToList();

        var assetCounts = await Db.FacilityAssets.ForCompany(Tenant)
            .Where(a => a.ServiceContractId != null && ids.Contains(a.ServiceContractId.Value))
            .GroupBy(a => a.ServiceContractId!.Value)
            .Select(g => new { ContractId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ContractId, x => x.Count);

        var contractorIds = contracts.Where(c => c.ContractorId.HasValue).Select(c => c.ContractorId!.Value).Distinct().ToList();

        var contractors = contractorIds.Count == 0
            ? []
            : await Db.Contractors.ForCompany(Tenant).Where(c => contractorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

        var propertyIds = contracts.Where(c => c.PropertyId.HasValue).Select(c => c.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        return contracts.Select(c => new ServiceContractDto
        {
            Id = c.Id,
            Reference = c.Reference,
            Name = c.Name,
            ContractorName = c.ContractorId is null ? null : contractors.GetValueOrDefault(c.ContractorId.Value),
            AddressOneLine = c.PropertyId is null ? null : properties.GetValueOrDefault(c.PropertyId.Value),
            Scope = c.Scope,
            AnnualValue = c.AnnualValue,
            CurrencyCode = currency,
            PaymentFrequency = c.PaymentFrequency,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            DaysToExpiry = c.EndDate.DayNumber - today.DayNumber,

            // The alert has to fire before the notice period runs out, not on the expiry date —
            // otherwise the contract auto-renews for another year while somebody is still deciding.
            IsExpiringSoon = c.EndDate.AddDays(-c.NoticePeriodDays).AddDays(-c.AlertDaysBefore) <= today && c.EndDate >= today,

            NoticePeriodDays = c.NoticePeriodDays,
            CoverType = c.CoverType,
            ResponseSlaHours = c.ResponseSlaHours,
            VisitsPerYear = c.VisitsPerYear,
            VisitsCompleted = c.VisitsCompleted,
            DocumentUrl = c.DocumentUrl,
            AutoRenews = c.AutoRenews,
            IsActive = c.IsActive && c.StartDate <= today && c.EndDate >= today,
            AssetCount = assetCounts.GetValueOrDefault(c.Id),
        }).ToList();
    }

    public async Task<ServiceContractDto> SaveServiceContractAsync(ServiceContractDto dto, Guid userId)
    {
        var contract = dto.Id != Guid.Empty
            ? await Db.ServiceContracts.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
            : null;

        if (contract is null)
        {
            contract = new ServiceContract
            {
                Reference = string.IsNullOrWhiteSpace(dto.Reference)
                    ? await numbering.NextMasterCodeAsync(Db.ServiceContracts, "SVC")
                    : dto.Reference,
            }.StampNew(Tenant, userId);

            Db.ServiceContracts.Add(contract);
        }
        else contract.StampUpdated(userId);

        if (dto.EndDate <= dto.StartDate)
            throw new InvalidOperationException("The contract has to end after it starts.");

        contract.Name = dto.Name;
        contract.Scope = dto.Scope;
        contract.AnnualValue = dto.AnnualValue;
        contract.PaymentFrequency = dto.PaymentFrequency;
        contract.StartDate = dto.StartDate;
        contract.EndDate = dto.EndDate;
        contract.NoticePeriodDays = dto.NoticePeriodDays;
        contract.CoverType = dto.CoverType;
        contract.ResponseSlaHours = dto.ResponseSlaHours;
        contract.VisitsPerYear = dto.VisitsPerYear;
        contract.DocumentUrl = dto.DocumentUrl;
        contract.AutoRenews = dto.AutoRenews;
        contract.IsActive = dto.IsActive;

        await Db.SaveChangesAsync();

        Db.CriticalDates.Add(new CriticalDate
        {
            PropertyId = contract.PropertyId,
            Title = $"{contract.Name} — decide before the notice period closes",
            DateType = "ServiceContractRenewal",
            DueDate = contract.EndDate.AddDays(-contract.NoticePeriodDays),
            AlertDaysBefore = contract.AlertDaysBefore,
            Severity = contract.AutoRenews ? AlertSeverity.Warning : AlertSeverity.Info,
        }.StampNew(Tenant, userId));

        await Db.SaveChangesAsync();
        return (await GetServiceContractsAsync(contract.PropertyId, false)).First(c => c.Id == contract.Id);
    }

    // ═══ Inspection rounds ═══════════════════════════════════════════════════

    /// <summary>
    /// A walk of the common parts. Findings become work orders — an inspection that produces a
    /// list nobody acts on is a document, not a control.
    /// </summary>
    public async Task<InspectionRoundDto> SaveInspectionRoundAsync(InspectionRoundDto dto, Guid userId)
    {
        var round = dto.Id != Guid.Empty
            ? await Db.InspectionRounds.ForCompany(Tenant).Include(r => r.Findings).FirstOrDefaultAsync(r => r.Id == dto.Id)
            : null;

        if (round is null)
        {
            round = new InspectionRound
            {
                Reference = await numbering.NextMasterCodeAsync(Db.InspectionRounds, "RND"),
                PropertyId = dto.PropertyId,
            }.StampNew(Tenant, userId);

            Db.InspectionRounds.Add(round);
        }
        else round.StampUpdated(userId);

        round.Kind = dto.Kind;
        round.InspectedAt = dto.InspectedAt == default ? DateTime.UtcNow : dto.InspectedAt;
        round.InspectorUserId = userId;
        round.Areas = dto.Areas;
        round.Score = dto.Score;
        round.Summary = dto.Summary;
        round.ReportUrl = dto.ReportUrl;
        round.NextRoundDue = dto.NextRoundDue;

        await Db.SaveChangesAsync();

        foreach (var f in dto.Findings)
        {
            var finding = f.Id is not null && f.Id != Guid.Empty
                ? round.Findings.FirstOrDefault(x => x.Id == f.Id)
                : null;

            if (finding is null)
            {
                finding = new InspectionFinding { InspectionRoundId = round.Id }.StampNew(Tenant, userId);
                round.Findings.Add(finding);
            }
            else finding.StampUpdated(userId);

            finding.Area = f.Area;
            finding.Description = f.Description;
            finding.Severity = f.Severity;
            finding.PhotoUrls = f.PhotoUrls.Count == 0 ? null : string.Join('\n', f.PhotoUrls);
            // The assignee is set through the work order rather than the finding row itself.
            finding.TargetDate = f.TargetDate;
            finding.IsSafetyIssue = f.IsSafetyIssue;
            finding.IsResolved = f.IsResolved;
            finding.ResolvedOn = f.IsResolved ? f.ResolvedOn ?? Today : null;

            // A safety finding becomes a work order there and then. It is the one category where
            // waiting for somebody to read the report is not acceptable.
            if (f.IsSafetyIssue && finding.WorkOrderId is null && !f.IsResolved)
            {
                var order = new WorkOrder
                {
                    OrderNumber = await numbering.NextWorkOrderNumberAsync(DateTime.UtcNow),
                    Source = WorkOrderSource.Inspection,
                    PropertyId = round.PropertyId,
                    SocietyId = round.SocietyId,
                    LocationDetail = f.Area,
                    Title = $"Safety finding — {f.Area}",
                    Description = f.Description,
                    Priority = TicketPriority.Emergency,
                    Status = WorkOrderStatus.Raised,
                    InspectionFindingId = finding.Id,
                    RaisedAt = DateTime.UtcNow,
                    RaisedByUserId = userId,
                    IsAuthorised = true,
                    CompletionDueAt = DateTime.UtcNow.AddHours(24),
                    CostBearer = round.SocietyId is not null ? CostBearer.Society : CostBearer.Landlord,
                }.StampNew(Tenant, userId);

                Db.WorkOrders.Add(order);
                await Db.SaveChangesAsync();

                finding.WorkOrderId = order.Id;
            }
        }

        round.FindingCount = round.Findings.Count;
        round.OpenFindingCount = round.Findings.Count(f => !f.IsResolved);

        await Db.SaveChangesAsync();
        return (await MapRoundsAsync([round], true))[0];
    }

    public async Task<PaginatedResponse<InspectionRoundDto>> GetInspectionRoundsAsync(ListQueryDto query, Guid? propertyId)
    {
        var q = Db.InspectionRounds.ForCompany(Tenant)
            .Include(r => r.Findings)
            .WhereIf(propertyId.HasValue, r => r.PropertyId == propertyId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), r => r.Reference.Contains(query.Search!))
            .OrderByDescending(r => r.InspectedAt);

        return await PageAsync(q, query, list => MapRoundsAsync(list, false));
    }

    private async Task<List<InspectionRoundDto>> MapRoundsAsync(List<InspectionRound> rounds, bool includeFindings)
    {
        if (rounds.Count == 0) return [];

        var today = Today;

        var propertyIds = rounds.Where(r => r.PropertyId.HasValue).Select(r => r.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        var societyIds = rounds.Where(r => r.SocietyId.HasValue).Select(r => r.SocietyId!.Value).Distinct().ToList();

        var societies = societyIds.Count == 0
            ? []
            : await Db.Societies.ForCompany(Tenant).Where(s => societyIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

        var inspectors = await AgentUserNamesAsync(rounds.Select(r => r.InspectorUserId));

        var assignees = await AgentUserNamesAsync(rounds.SelectMany(r => r.Findings).Select(f => f.AssignedToUserId));

        var workOrderIds = rounds.SelectMany(r => r.Findings).Where(f => f.WorkOrderId.HasValue)
            .Select(f => f.WorkOrderId!.Value).Distinct().ToList();

        var workOrders = workOrderIds.Count == 0
            ? []
            : await Db.WorkOrders.ForCompany(Tenant).Where(w => workOrderIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.OrderNumber);

        return rounds.Select(r => new InspectionRoundDto
        {
            Id = r.Id,
            Reference = r.Reference,
            PropertyId = r.PropertyId,
            AddressOneLine = r.PropertyId is null ? null : properties.GetValueOrDefault(r.PropertyId.Value),
            SocietyName = r.SocietyId is null ? null : societies.GetValueOrDefault(r.SocietyId.Value),
            Kind = r.Kind,
            InspectedAt = r.InspectedAt,
            InspectorName = r.InspectorUserId is null ? null : inspectors.GetValueOrDefault(r.InspectorUserId.Value),
            Areas = r.Areas,
            Score = r.Score,
            FindingCount = r.FindingCount,
            OpenFindingCount = r.OpenFindingCount,
            SafetyIssueCount = r.Findings.Count(f => f.IsSafetyIssue && !f.IsResolved),
            Summary = r.Summary,
            ReportUrl = r.ReportUrl,
            NextRoundDue = r.NextRoundDue,

            Findings = !includeFindings ? [] : r.Findings.Select(f => new InspectionFindingDto
            {
                Id = f.Id,
                Area = f.Area,
                Description = f.Description ?? string.Empty,
                Severity = f.Severity,
                PhotoUrls = string.IsNullOrWhiteSpace(f.PhotoUrls)
                    ? []
                    : f.PhotoUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                WorkOrderId = f.WorkOrderId,
                WorkOrderNumber = f.WorkOrderId is null ? null : workOrders.GetValueOrDefault(f.WorkOrderId.Value),
                AssignedToName = f.AssignedToUserId is null ? null : assignees.GetValueOrDefault(f.AssignedToUserId.Value),
                TargetDate = f.TargetDate,
                IsResolved = f.IsResolved,
                ResolvedOn = f.ResolvedOn,
                IsSafetyIssue = f.IsSafetyIssue,
                IsOverdue = !f.IsResolved && f.TargetDate is not null && f.TargetDate < today,
            }).ToList(),
        }).ToList();
    }
}
