using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The rent roll, the rent run, arrears, voids, service charges and turnover rent.
///
/// The service-charge reconciliation is the hardest thing in this file and the one nobody else
/// gets right. Three lease-negotiated protections interact — an excluded head, a gross-up to a
/// notional occupancy, and an annual or cumulative cap — and applying them in the wrong order
/// gives a different answer. They are applied in the order the lease reads them, each step is
/// written out in words, and the workings are returned with the number so a tenant's surveyor can
/// check it rather than dispute it.
/// </summary>
public partial class LeasingService
{
    // ═══ Rent roll ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Every lettable unit in one grid, let or not. The vacant rows are the point: a rent roll
    /// that only lists tenancies hides exactly the income the asset is not earning.
    /// </summary>
    public async Task<RentRollDto> GetRentRollAsync(Guid? propertyId, Guid? projectId, DateOnly asOf)
    {
        if (propertyId is null && projectId is null)
            throw new InvalidOperationException("Name a property or a project to build a rent roll.");

        var unit = await AreaUnitAsync();
        var currency = await CurrencyAsync();
        var today = asOf == default ? Today : asOf;

        var properties = await Db.Properties.ForCompany(Tenant)
            .WhereIf(propertyId.HasValue, p => p.Id == propertyId || p.MasterPropertyId == propertyId)
            .WhereIf(projectId.HasValue, p => p.ProjectId == projectId)
            .ToListAsync();

        if (properties.Count == 0)
            return new RentRollDto { PropertyId = propertyId, AsOfDate = today, CurrencyCode = currency };

        var ids = properties.Select(p => p.Id).ToList();

        var tenancies = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => ids.Contains(t.PropertyId)
                     && t.Status != TenancyStatus.Abandoned
                     && t.StartDate <= today
                     && (t.ActualEndDate == null || t.ActualEndDate >= today))
            .ToListAsync();

        var tenancyIds = tenancies.Select(t => t.Id).ToList();

        var leads = await Db.TenancyParties.ForCompany(Tenant)
            .Where(p => tenancyIds.Contains(p.TenancyId) && p.IsLeadTenant)
            .Select(p => new { p.TenancyId, p.PartyId })
            .ToListAsync();

        var names = await PartyNamesAsync(leads.Select(l => l.PartyId));

        var recoveries = tenancyIds.Count == 0
            ? []
            : await Db.RecoveryCharges.ForCompany(Tenant)
                .Where(r => tenancyIds.Contains(r.TenancyId) && r.IsActive)
                .Select(r => new { r.TenancyId, r.ChargeType, r.Amount, r.Frequency })
                .ToListAsync();

        var deposits = tenancyIds.Count == 0
            ? []
            : await Db.SecurityDeposits.ForCompany(Tenant)
                .Where(d => tenancyIds.Contains(d.TenancyId) && d.ReleasedOn == null)
                .Select(d => new { d.TenancyId, d.Amount })
                .ToListAsync();

        var reviews = tenancyIds.Count == 0
            ? []
            : await Db.RentReviews.ForCompany(Tenant)
                .Where(r => tenancyIds.Contains(r.TenancyId) && r.AgreedOn == null && r.ReviewDate >= today)
                .Select(r => new { r.TenancyId, r.ReviewDate })
                .ToListAsync();

        var breaks = tenancyIds.Count == 0
            ? []
            : await Db.LeaseOptions.ForCompany(Tenant)
                .Where(o => tenancyIds.Contains(o.TenancyId) && o.Kind == LeaseOptionKind.Break && !o.IsExercised && !o.IsLapsed)
                .Select(o => new { o.TenancyId, o.OptionDate })
                .ToListAsync();

        var voids = await Db.VoidRecords.ForCompany(Tenant)
            .Where(v => ids.Contains(v.PropertyId) && !v.IsClosed)
            .Select(v => new { v.PropertyId, v.VacantFrom, v.AskingRent })
            .ToListAsync();

        var declarations = tenancyIds.Count == 0
            ? []
            : await Db.TenantSalesDeclarations.ForCompany(Tenant)
                .Where(d => tenancyIds.Contains(d.TenancyId) && d.DeclaredOn != null)
                .GroupBy(d => d.TenancyId)
                .Select(g => new { TenancyId = g.Key, Sales = g.OrderByDescending(x => x.PeriodTo).First().NetSales })
                .ToListAsync();

        var categories = await Db.TenantCategories.ForCompany(Tenant).ToDictionaryAsync(c => c.Id, c => c.Name);

        var result = new RentRollDto
        {
            PropertyId = propertyId,
            PropertyName = propertyId is null ? null : properties.FirstOrDefault(p => p.Id == propertyId)?.Name,
            CurrencyCode = currency,
            AsOfDate = today,
        };

        foreach (var property in properties.OrderBy(p => p.FloorNumber).ThenBy(p => p.UnitNumber))
        {
            var tenancy = tenancies.FirstOrDefault(t => t.PropertyId == property.Id);
            var area = property.SaleableAreaSqFt ?? property.CoveredAreaSqFt ?? 0m;
            var vacancy = voids.FirstOrDefault(v => v.PropertyId == property.Id);

            var mine = tenancy is null ? [] : recoveries.Where(r => r.TenancyId == tenancy.Id).ToList();

            var serviceCharge = RealEstateMapper.Money(mine
                .Where(r => r.ChargeType == "ServiceCharge")
                .Sum(r => AnnualiseRent(r.Amount, r.Frequency)));

            var otherRecoveries = RealEstateMapper.Money(mine
                .Where(r => r.ChargeType != "ServiceCharge")
                .Sum(r => AnnualiseRent(r.Amount, r.Frequency)));

            var annualRent = tenancy is null ? 0m : tenancy.AnnualRent ?? AnnualiseRent(tenancy.Rent, tenancy.Frequency);
            var lead = tenancy is null ? null : leads.FirstOrDefault(l => l.TenancyId == tenancy.Id);

            result.Rows.Add(new RentRollRowDto
            {
                UnitId = tenancy?.UnitId,
                PropertyId = property.Id,
                UnitLabel = property.UnitNumber ?? property.Name ?? property.Reference,
                FloorLabel = property.FloorLabel,
                Area = RealEstateMapper.Area(area, unit),
                SubType = property.SubType,

                TenancyId = tenancy?.Id,
                TenantName = lead is null ? null : names.GetValueOrDefault(lead.PartyId),
                TenantCategory = property.TenantCategoryId is null ? null : categories.GetValueOrDefault(property.TenantCategoryId.Value),
                Status = tenancy?.Status,
                StartDate = tenancy?.StartDate,
                EndDate = tenancy?.EndDate,
                MonthsRemaining = tenancy?.EndDate is null
                    ? null
                    : Math.Max(0, ((tenancy.EndDate.Value.Year - today.Year) * 12) + tenancy.EndDate.Value.Month - today.Month),

                Rent = tenancy?.Rent ?? 0m,
                Frequency = tenancy?.Frequency,
                AnnualRent = annualRent,
                RentPerSqFt = area > 0m ? RealEstateMapper.Money(annualRent / area) : 0m,
                ServiceCharge = serviceCharge,
                OtherRecoveries = otherRecoveries,
                TotalIncome = RealEstateMapper.Money(annualRent + serviceCharge + otherRecoveries),

                Deposit = tenancy is null ? 0m : deposits.Where(d => d.TenancyId == tenancy.Id).Sum(d => d.Amount),
                Arrears = tenancy?.ArrearsAmount ?? 0m,
                DaysInArrears = tenancy?.DaysInArrears ?? 0,

                Escalation = tenancy?.Escalation,
                NextReviewDate = tenancy is null ? null : reviews.Where(r => r.TenancyId == tenancy.Id).Select(r => (DateOnly?)r.ReviewDate).FirstOrDefault(),
                NextBreakDate = tenancy is null ? null : breaks.Where(b => b.TenancyId == tenancy.Id).Select(b => (DateOnly?)b.OptionDate).FirstOrDefault(),

                IsVacant = tenancy is null,
                DaysVoid = vacancy is null ? null : today.DayNumber - vacancy.VacantFrom.DayNumber,
                AskingRent = tenancy is null ? vacancy?.AskingRent ?? property.MonthlyRent : null,

                TurnoverRentApplies = tenancy?.TurnoverRentApplies ?? false,
                LastDeclaredSales = tenancy is null ? null : declarations.Where(d => d.TenancyId == tenancy.Id).Select(d => (decimal?)d.Sales).FirstOrDefault(),
            });
        }

        result.TotalUnits = result.Rows.Count;
        result.OccupiedUnits = result.Rows.Count(r => !r.IsVacant);
        result.VacantUnits = result.Rows.Count(r => r.IsVacant);
        result.OccupancyPercent = RealEstateMapper.Percent(result.OccupiedUnits, result.TotalUnits);

        var totalArea = result.Rows.Sum(r => r.Area.SquareFeet);
        var occupiedArea = result.Rows.Where(r => !r.IsVacant).Sum(r => r.Area.SquareFeet);

        result.TotalArea = RealEstateMapper.Area(totalArea, unit);
        result.OccupiedArea = RealEstateMapper.Area(occupiedArea, unit);

        result.AnnualRent = RealEstateMapper.Money(result.Rows.Sum(r => r.AnnualRent));
        result.MonthlyRent = RealEstateMapper.Money(result.AnnualRent / 12m);
        result.AverageRentPerSqFt = occupiedArea > 0m ? RealEstateMapper.Money(result.AnnualRent / occupiedArea) : 0m;
        result.TotalArrears = RealEstateMapper.Money(result.Rows.Sum(r => r.Arrears));
        result.TotalDeposits = RealEstateMapper.Money(result.Rows.Sum(r => r.Deposit));

        // Passing rent against what the vacant space is being asked for — the crudest but most
        // useful reversion signal a rent roll can carry without a valuation.
        var asking = result.Rows.Where(r => r.IsVacant && r.AskingRent > 0m && r.Area.SquareFeet > 0m).ToList();

        if (asking.Count > 0 && result.AverageRentPerSqFt > 0m)
        {
            var askingPerSqFt = asking.Sum(r => r.AskingRent!.Value * 12m) / asking.Sum(r => r.Area.SquareFeet);
            result.PassingRentVsMarket = RealEstateMapper.Percent(result.AverageRentPerSqFt - askingPerSqFt, askingPerSqFt);
        }

        // Expiry profile, which is what a lender or a purchaser looks at first.
        var buckets = new (string Label, Func<RentRollRowDto, bool> Test)[]
        {
            ("Vacant", r => r.IsVacant),
            ("Within 12 months", r => r.MonthsRemaining is <= 12 and >= 0),
            ("1 – 3 years", r => r.MonthsRemaining is > 12 and <= 36),
            ("3 – 5 years", r => r.MonthsRemaining is > 36 and <= 60),
            ("Over 5 years", r => r.MonthsRemaining > 60),
            ("Periodic / no end", r => !r.IsVacant && r.MonthsRemaining is null),
        };

        foreach (var (label, test) in buckets)
        {
            var rows = result.Rows.Where(test).ToList();
            if (rows.Count == 0) continue;

            result.ExpiryProfile.Add(new BreakdownSliceDto
            {
                Label = label,
                Value = RealEstateMapper.Money(rows.Sum(r => r.AnnualRent)),
                Percent = RealEstateMapper.Percent(rows.Sum(r => r.AnnualRent), result.AnnualRent),
                Count = rows.Count,
            });
        }

        result.TenantMix = result.Rows
            .Where(r => !r.IsVacant && r.TenantCategory is not null)
            .GroupBy(r => r.TenantCategory!)
            .Select(g => new BreakdownSliceDto
            {
                Label = g.Key,
                Value = RealEstateMapper.Money(g.Sum(r => r.Area.SquareFeet)),
                Percent = RealEstateMapper.Percent(g.Sum(r => r.Area.SquareFeet), occupiedArea),
                Count = g.Count(),
            })
            .OrderByDescending(s => s.Value)
            .ToList();

        var arrears = result.Rows.Where(r => r.Arrears > 0m).ToList();

        result.Ageing = new AgeingBucketsDto
        {
            Current = RealEstateMapper.Money(arrears.Where(r => r.DaysInArrears <= 0).Sum(r => r.Arrears)),
            Days1To30 = RealEstateMapper.Money(arrears.Where(r => r.DaysInArrears is > 0 and <= 30).Sum(r => r.Arrears)),
            Days31To60 = RealEstateMapper.Money(arrears.Where(r => r.DaysInArrears is > 30 and <= 60).Sum(r => r.Arrears)),
            Days61To90 = RealEstateMapper.Money(arrears.Where(r => r.DaysInArrears is > 60 and <= 90).Sum(r => r.Arrears)),
            Days90Plus = RealEstateMapper.Money(arrears.Where(r => r.DaysInArrears > 90).Sum(r => r.Arrears)),
            Total = result.TotalArrears,
        };

        return result;
    }

    // ═══ The rent run ════════════════════════════════════════════════════════

    /// <summary>
    /// Raises the period's rent across a portfolio. Dry-runnable, idempotent, and it says why it
    /// skipped anything — a run that silently misses ten tenancies is worse than one that fails.
    /// </summary>
    public async Task<RentRunDto> RunRentAsync(RentRunRequestDto dto, Guid userId)
    {
        if (dto.PeriodTo < dto.PeriodFrom)
            throw new InvalidOperationException("The period has to end after it starts.");

        var today = Today;

        var run = new RentRun
        {
            Reference = await numbering.NextMasterCodeAsync(Db.RentRuns, "RNT"),
            PropertyId = dto.PropertyId,
            ProjectId = dto.ProjectId,
            OfficeId = dto.OfficeId,
            RunDate = today,
            PeriodFrom = dto.PeriodFrom,
            PeriodTo = dto.PeriodTo,
            IsDryRun = dto.IsDryRun,
            RunByUserId = userId,
            StartedAt = DateTime.UtcNow,
        }.StampNew(Tenant, userId);

        Db.RentRuns.Add(run);

        var candidates = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => t.Status == TenancyStatus.Active || t.Status == TenancyStatus.NoticeGiven)
            .Where(t => t.StartDate <= dto.PeriodTo)
            .WhereIf(dto.PropertyId.HasValue, t => t.PropertyId == dto.PropertyId)
            .WhereIf(dto.OfficeId.HasValue, t => t.OfficeId == dto.OfficeId)
            .ToListAsync();

        run.CandidateCount = candidates.Count;

        var tenancyIds = candidates.Select(t => t.Id).ToList();

        // Anything already charged for this period is left alone. That is what makes a re-run
        // after a partial failure safe.
        var charged = await Db.RentCharges.ForCompany(Tenant)
            .Where(c => tenancyIds.Contains(c.TenancyId)
                     && c.PeriodFrom <= dto.PeriodTo
                     && c.PeriodTo >= dto.PeriodFrom
                     && c.RentRunId != null)
            .Select(c => c.TenancyId)
            .ToListAsync();

        var recoveries = dto.IncludeRecoveries && tenancyIds.Count > 0
            ? await Db.RecoveryCharges.ForCompany(Tenant)
                .Where(r => tenancyIds.Contains(r.TenancyId)
                         && r.IsActive && r.IncludeInRentRun
                         && r.EffectiveFrom <= dto.PeriodTo
                         && (r.EffectiveTo == null || r.EffectiveTo >= dto.PeriodFrom))
                .ToListAsync()
            : [];

        foreach (var tenancy in candidates)
        {
            var line = new RentRunLine { RentRunId = run.Id, TenancyId = tenancy.Id }.StampNew(Tenant, userId);
            run.Lines.Add(line);

            if (dto.ExcludeTenancyIds.Contains(tenancy.Id))
            {
                line.WasSkipped = true;
                line.SkipReason = "Excluded from this run.";
                run.SkippedCount++;
                continue;
            }

            if (charged.Contains(tenancy.Id))
            {
                line.WasSkipped = true;
                line.SkipReason = "Already charged for this period.";
                run.SkippedCount++;
                continue;
            }

            if (tenancy.ActualEndDate is not null && tenancy.ActualEndDate < dto.PeriodFrom)
            {
                line.WasSkipped = true;
                line.SkipReason = $"Tenancy ended {tenancy.ActualEndDate:dd MMM yyyy}.";
                run.SkippedCount++;
                continue;
            }

            var charge = await Db.RentCharges.ForCompany(Tenant)
                .Where(c => c.TenancyId == tenancy.Id && c.PeriodFrom <= dto.PeriodTo && c.PeriodTo >= dto.PeriodFrom)
                .OrderBy(c => c.SequenceNumber)
                .FirstOrDefaultAsync();

            if (charge is null)
            {
                line.Failed = true;
                line.FailureReason = "No scheduled rent covers this period. Regenerate the schedule.";
                run.FailedCount++;
                continue;
            }

            var amount = charge.TotalAmount;

            var extras = recoveries.Where(r => r.TenancyId == tenancy.Id).ToList();

            foreach (var recovery in extras)
            {
                var recoveryAmount = recovery.Amount;
                if (recovery.IsTaxable) recoveryAmount += recoveryAmount * recovery.TaxPercent / 100m;
                amount += recoveryAmount;
            }

            line.Amount = RealEstateMapper.Money(amount);
            line.RentChargeId = charge.Id;

            if (!dto.IsDryRun)
            {
                charge.RentRunId = run.Id;
                charge.Status = charge.DueDate <= today ? InstalmentStatus.Due : InstalmentStatus.NotDue;
                charge.StampUpdated(userId);

                tenancy.TotalCharged = RealEstateMapper.Money(tenancy.TotalCharged + line.Amount);
                tenancy.NextDueDate = charge.DueDate;
                tenancy.StampUpdated(userId);
            }

            run.ChargedCount++;
            run.TotalAmount += line.Amount;
        }

        run.TotalAmount = RealEstateMapper.Money(run.TotalAmount);
        run.CompletedAt = DateTime.UtcNow;
        run.IsCompleted = true;

        if (run.FailedCount > 0)
            run.ErrorSummary = $"{run.FailedCount} tenancies could not be charged. See the lines for detail.";

        await Db.SaveChangesAsync();
        return (await MapRentRunsAsync([run]))[0];
    }

    public async Task<PaginatedResponse<RentRunDto>> GetRentRunsAsync(ListQueryDto query)
    {
        var q = Db.RentRuns.ForCompany(Tenant)
            .Include(r => r.Lines)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), r => r.Reference.Contains(query.Search!))
            .WhereIf(query.FromDate.HasValue, r => r.RunDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, r => r.RunDate <= query.ToDate)
            .OrderByDescending(r => r.RunDate);

        return await PageAsync(q, query, MapRentRunsAsync);
    }

    private async Task<List<RentRunDto>> MapRentRunsAsync(List<RentRun> runs)
    {
        if (runs.Count == 0) return [];

        var currency = await CurrencyAsync();
        var users = await AgentUserNamesAsync(runs.Select(r => r.RunByUserId));

        var propertyIds = runs.Where(r => r.PropertyId.HasValue).Select(r => r.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name ?? p.Reference);

        var tenancyIds = runs.SelectMany(r => r.Lines).Select(l => l.TenancyId).Distinct().ToList();

        var tenancies = tenancyIds.Count == 0
            ? []
            : await Db.Tenancies.ForCompany(Tenant)
                .Where(t => tenancyIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Reference, t.PropertyId })
                .ToListAsync();

        var linePropertyIds = tenancies.Select(t => t.PropertyId).Distinct().ToList();

        var lineProperties = linePropertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => linePropertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        return runs.Select(r => new RentRunDto
        {
            Id = r.Id,
            Reference = r.Reference,
            PropertyName = r.PropertyId is null ? null : properties.GetValueOrDefault(r.PropertyId.Value),
            RunDate = r.RunDate,
            PeriodFrom = r.PeriodFrom,
            PeriodTo = r.PeriodTo,
            IsDryRun = r.IsDryRun,
            CandidateCount = r.CandidateCount,
            ChargedCount = r.ChargedCount,
            SkippedCount = r.SkippedCount,
            FailedCount = r.FailedCount,
            TotalAmount = r.TotalAmount,
            CurrencyCode = currency,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            RunByName = r.RunByUserId is null ? null : users.GetValueOrDefault(r.RunByUserId.Value),
            ErrorSummary = r.ErrorSummary,
            IsCompleted = r.IsCompleted,

            Lines = r.Lines.Select(l =>
            {
                var tenancy = tenancies.FirstOrDefault(t => t.Id == l.TenancyId);

                return new RentRunLineDto
                {
                    Id = l.Id,
                    TenancyId = l.TenancyId,
                    TenancyReference = tenancy?.Reference ?? "—",
                    AddressOneLine = tenancy is null ? null : lineProperties.GetValueOrDefault(tenancy.PropertyId),
                    Amount = l.Amount,
                    WasSkipped = l.WasSkipped,
                    SkipReason = l.SkipReason,
                    Failed = l.Failed,
                    FailureReason = l.FailureReason,
                };
            }).ToList(),
        }).ToList();
    }

    public async Task<PaginatedResponse<ArrearsCaseDto>> GetArrearsAsync(ListQueryDto query, int? minDays)
    {
        var q = Db.ArrearsCases.ForCompany(Tenant)
            .Where(a => !a.IsClosed)
            .WhereIf(minDays.HasValue, a => a.DaysInArrears >= minDays)
            .OrderByDescending(a => a.ArrearsAmount);

        return await PageAsync(q, query, async cases =>
        {
            if (cases.Count == 0) return [];

            var currency = await CurrencyAsync();
            var tenancyIds = cases.Select(c => c.TenancyId).Distinct().ToList();

            var tenancies = await Db.Tenancies.ForCompany(Tenant)
                .Where(t => tenancyIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Reference, t.PropertyId })
                .ToListAsync();

            var propertyIds = tenancies.Select(t => t.PropertyId).Distinct().ToList();

            var properties = await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

            var people = await Db.Parties.ForCompany(Tenant)
                .Where(p => cases.Select(c => c.PartyId).Contains(p.Id))
                .ToListAsync();

            var assignees = await AgentUserNamesAsync(cases.Select(c => c.AssignedToUserId));

            var promises = await Db.PromiseToPays.ForCompany(Tenant)
                .Where(p => cases.Select(c => c.ActivePromiseId).Contains(p.Id))
                .Select(p => new { p.Id, p.PromisedDate })
                .ToListAsync();

            return cases.Select(c =>
            {
                var tenancy = tenancies.FirstOrDefault(t => t.Id == c.TenancyId);
                var person = people.FirstOrDefault(p => p.Id == c.PartyId);

                return new ArrearsCaseDto
                {
                    Id = c.Id,
                    Reference = c.Reference,
                    TenancyId = c.TenancyId,
                    TenancyReference = tenancy?.Reference ?? "—",
                    AddressOneLine = tenancy is null ? null : properties.GetValueOrDefault(tenancy.PropertyId),
                    PartyId = c.PartyId,
                    TenantName = person is null ? "—" : RealEstateMapper.DisplayName(person),
                    Phone = person?.PrimaryPhone,
                    OpenedOn = c.OpenedOn,
                    ArrearsAmount = c.ArrearsAmount,
                    LateFeeAmount = c.LateFeeAmount,
                    DaysInArrears = c.DaysInArrears,
                    MonthsInArrears = c.MonthsInArrears,
                    CurrencyCode = currency,
                    AssignedToName = c.AssignedToUserId is null ? null : assignees.GetValueOrDefault(c.AssignedToUserId.Value),
                    ActivePromiseId = c.ActivePromiseId,
                    PromisedDate = promises.FirstOrDefault(p => p.Id == c.ActivePromiseId)?.PromisedDate,
                    LandlordNotified = c.LandlordNotified,
                    NoticeId = c.NoticeId,
                    ReferredToLegal = c.ReferredToLegal,
                    IsClosed = c.IsClosed,
                    Outcome = c.Outcome,
                };
            }).ToList();
        });
    }

    public async Task<List<VoidRecordDto>> GetVoidsAsync(Guid? propertyId, bool openOnly)
    {
        var today = Today;
        var unit = await AreaUnitAsync();
        var currency = await CurrencyAsync();

        var voids = await Db.VoidRecords.ForCompany(Tenant)
            .WhereIf(propertyId.HasValue, v => v.PropertyId == propertyId)
            .WhereIf(openOnly, v => !v.IsClosed)
            .OrderByDescending(v => v.VacantFrom)
            .ToListAsync();

        if (voids.Count == 0) return [];

        var propertyIds = voids.Select(v => v.PropertyId).Distinct().ToList();

        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var reasons = await ReasonLabelsAsync(voids.Select(v => v.VoidReasonCodeId));

        return voids.Select(v =>
        {
            properties.TryGetValue(v.PropertyId, out var property);
            var days = v.IsClosed ? v.DaysVoid : today.DayNumber - v.VacantFrom.DayNumber;

            return new VoidRecordDto
            {
                Id = v.Id,
                PropertyId = v.PropertyId,
                AddressOneLine = property is null ? "—" : RealEstateMapper.OneLineAddress(property),
                UnitId = v.UnitId,
                UnitLabel = property?.UnitNumber,
                Area = RealEstateMapper.AreaOrNull(property?.SaleableAreaSqFt, unit),
                VacantFrom = v.VacantFrom,
                LetFrom = v.LetFrom,
                DaysVoid = days,

                // Lost rent accrues while the void is open, so an open void's cost is live rather
                // than frozen at whatever it was when somebody last saved the row.
                AskingRent = v.AskingRent,
                LostRent = v.IsClosed ? v.LostRent : RealEstateMapper.Money(v.AskingRent / 30m * days),
                HoldingCost = v.HoldingCost,
                CurrencyCode = currency,
                EnquiryCount = v.EnquiryCount,
                ViewingCount = v.ViewingCount,
                VoidReason = v.VoidReasonCodeId is null ? null : reasons.GetValueOrDefault(v.VoidReasonCodeId.Value),
                RefurbishmentRequired = v.RefurbishmentRequired,
                IsClosed = v.IsClosed,
            };
        }).ToList();
    }

    public async Task<List<TenantCategoryDto>> GetTenantMixAsync(Guid propertyId)
    {
        var unit = await AreaUnitAsync();

        var categories = await Db.TenantCategories.ForCompany(Tenant)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync();

        if (categories.Count == 0) return [];

        var units = await Db.Properties.ForCompany(Tenant)
            .Where(p => p.Id == propertyId || p.MasterPropertyId == propertyId)
            .Select(p => new { p.Id, p.TenantCategoryId, Area = p.SaleableAreaSqFt ?? 0m })
            .ToListAsync();

        var totalArea = units.Sum(u => u.Area);

        return categories.Select(c =>
        {
            var mine = units.Where(u => u.TenantCategoryId == c.Id).ToList();
            var area = mine.Sum(u => u.Area);
            var current = RealEstateMapper.Percent(area, totalArea);

            return new TenantCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                ParentCategoryId = c.ParentCategoryId,
                TargetMixPercent = c.TargetMixPercent,
                CurrentMixPercent = current,

                // Positive means over-represented against the target. It is what a leasing team
                // uses to decide which enquiry to chase.
                Variance = RealEstateMapper.Money(current - c.TargetMixPercent),
                UnitCount = mine.Count,
                Area = RealEstateMapper.Area(area, unit),
                ColourHex = c.ColourHex,
                SortOrder = c.SortOrder,
            };
        }).ToList();
    }
}
