using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Service-charge budgets, on-account billing, the year-end reconciliation, and turnover rent.
///
/// The reconciliation applies three lease protections in the order the lease reads them —
/// exclusions, then gross-up, then caps — and shows its working, because a service-charge
/// statement a tenant's surveyor cannot follow is a service-charge statement they will dispute.
/// </summary>
public partial class LeasingService
{
    // ═══ Budgets ═════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ServiceChargeBudgetDto>> GetBudgetsAsync(ListQueryDto query, Guid? propertyId)
    {
        var q = Db.ServiceChargeBudgets.ForCompany(Tenant)
            .Include(b => b.Lines)
            .WhereIf(propertyId.HasValue, b => b.PropertyId == propertyId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), b => b.Reference.Contains(query.Search!))
            .OrderByDescending(b => b.FinancialYear);

        return await PageAsync(q, query, MapBudgetsAsync);
    }

    public async Task<ServiceChargeBudgetDto?> GetBudgetAsync(Guid id)
    {
        var budget = await Db.ServiceChargeBudgets.ForCompany(Tenant)
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (budget is null) return null;
        return (await MapBudgetsAsync([budget]))[0];
    }

    private async Task<List<ServiceChargeBudgetDto>> MapBudgetsAsync(List<ServiceChargeBudget> budgets)
    {
        if (budgets.Count == 0) return [];

        var unit = await AreaUnitAsync();
        var currency = await CurrencyAsync();

        var propertyIds = budgets.Where(b => b.PropertyId.HasValue).Select(b => b.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.ServiceChargeBudgets.ForCompany(Tenant)
                .Where(b => propertyIds.Contains(b.PropertyId!.Value))
                .Join(Db.Properties.ForCompany(Tenant), b => b.PropertyId, p => p.Id, (b, p) => new { p.Id, Name = p.Name ?? p.Reference })
                .Distinct()
                .ToDictionaryAsync(x => x.Id, x => x.Name);

        return budgets.Select(b => new ServiceChargeBudgetDto
        {
            Id = b.Id,
            Reference = b.Reference,
            PropertyId = b.PropertyId,
            PropertyName = b.PropertyId is null ? null : properties.GetValueOrDefault(b.PropertyId.Value),
            SocietyId = b.SocietyId,
            FinancialYear = b.FinancialYear,
            PeriodFrom = b.PeriodFrom,
            PeriodTo = b.PeriodTo,
            TotalBudget = b.TotalBudget,
            TotalActual = b.TotalActual,
            TotalBilled = b.TotalBilled,
            Variance = RealEstateMapper.Money(b.TotalActual - b.TotalBudget),
            VariancePercent = RealEstateMapper.Percent(b.TotalActual - b.TotalBudget, b.TotalBudget),
            CurrencyCode = currency,
            ManagementFeePercent = b.ManagementFeePercent,
            TotalGrossLettableArea = RealEstateMapper.Area(b.TotalGrossLettableAreaSqFt, unit),
            OccupiedArea = RealEstateMapper.Area(b.OccupiedAreaSqFt, unit),
            OccupancyPercent = RealEstateMapper.Percent(b.OccupiedAreaSqFt, b.TotalGrossLettableAreaSqFt),
            BaseYear = b.BaseYear,
            GrossUpEnabled = b.GrossUpEnabled,
            GrossUpToOccupancyPercent = b.GrossUpToOccupancyPercent,
            AnnualCapPercent = b.AnnualCapPercent,
            CumulativeCapPercent = b.CumulativeCapPercent,
            IsApproved = b.IsApproved,
            ApprovedOn = b.ApprovedOn,
            IsReconciled = b.IsReconciled,
            DocumentUrl = b.DocumentUrl,

            Lines = b.Lines.OrderBy(l => l.SortOrder).ThenBy(l => l.Head).Select(l => MapBudgetLine(l, b)).ToList(),
        }).ToList();
    }

    private static ServiceChargeBudgetLineDto MapBudgetLine(ServiceChargeBudgetLine l, ServiceChargeBudget b)
    {
        var actual = l.ActualAmount;
        var grossedUp = actual;

        // Gross-up applies only to costs that vary with occupancy. Grossing up a fixed cost —
        // insurance, a management fee — inflates the recovery and is what a surveyor challenges.
        if (b.GrossUpEnabled && l.IsVariableCost && b.TotalGrossLettableAreaSqFt > 0m)
        {
            var occupancy = RealEstateMapper.Percent(b.OccupiedAreaSqFt, b.TotalGrossLettableAreaSqFt);

            if (occupancy > 0m && occupancy < b.GrossUpToOccupancyPercent)
                grossedUp = RealEstateMapper.Money(actual / occupancy * b.GrossUpToOccupancyPercent);
        }

        var recoverable = l.IsExcludedFromRecovery ? 0m : grossedUp;

        if (l.IsCapped && l.CapAmount is > 0m && recoverable > l.CapAmount)
            recoverable = l.CapAmount.Value;

        return new ServiceChargeBudgetLineDto
        {
            Id = l.Id,
            Head = l.Head,
            Label = l.Label,
            BudgetAmount = l.BudgetAmount,
            ActualAmount = l.ActualAmount,
            VarianceAmount = RealEstateMapper.Money(l.ActualAmount - l.BudgetAmount),
            VariancePercent = RealEstateMapper.Percent(l.ActualAmount - l.BudgetAmount, l.BudgetAmount),
            Basis = l.Basis,
            IsVariableCost = l.IsVariableCost,
            IsExcludedFromRecovery = l.IsExcludedFromRecovery,
            ExclusionReason = l.ExclusionReason,
            IsCapped = l.IsCapped,
            CapAmount = l.CapAmount,
            GrossedUpAmount = RealEstateMapper.Money(grossedUp),
            RecoverableAmount = RealEstateMapper.Money(recoverable),
            SortOrder = l.SortOrder,
        };
    }

    public async Task<ServiceChargeBudgetDto> SaveBudgetAsync(ServiceChargeBudgetDto dto, Guid userId)
    {
        var budget = dto.Id != Guid.Empty
            ? await Db.ServiceChargeBudgets.ForCompany(Tenant).Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == dto.Id)
            : null;

        if (budget is null)
        {
            budget = new ServiceChargeBudget
            {
                Reference = await numbering.NextMasterCodeAsync(Db.ServiceChargeBudgets, "SCB"),
                PropertyId = dto.PropertyId,
                SocietyId = dto.SocietyId,
            }.StampNew(Tenant, userId);

            Db.ServiceChargeBudgets.Add(budget);
        }
        else
        {
            if (budget.IsReconciled)
                throw new InvalidOperationException("This year has been reconciled and its budget can no longer be edited.");

            budget.StampUpdated(userId);
        }

        budget.FinancialYear = dto.FinancialYear;
        budget.PeriodFrom = dto.PeriodFrom;
        budget.PeriodTo = dto.PeriodTo;
        budget.ManagementFeePercent = dto.ManagementFeePercent;
        budget.BaseYear = dto.BaseYear;
        budget.GrossUpEnabled = dto.GrossUpEnabled;
        budget.GrossUpToOccupancyPercent = dto.GrossUpToOccupancyPercent <= 0m ? 95m : dto.GrossUpToOccupancyPercent;
        budget.AnnualCapPercent = dto.AnnualCapPercent;
        budget.CumulativeCapPercent = dto.CumulativeCapPercent;

        // Areas are measured from the property tree rather than typed, because the apportionment
        // divides by them and a typo silently mis-bills every tenant in the building.
        if (dto.PropertyId is not null)
        {
            var units = await Db.Properties.ForCompany(Tenant)
                .Where(p => p.Id == dto.PropertyId || p.MasterPropertyId == dto.PropertyId)
                .Select(p => new { p.Id, Area = p.SaleableAreaSqFt ?? 0m })
                .ToListAsync();

            budget.TotalGrossLettableAreaSqFt = units.Sum(u => u.Area);

            var occupiedIds = await Db.Tenancies.ForCompany(Tenant)
                .Where(t => t.Status == TenancyStatus.Active)
                .Select(t => t.PropertyId)
                .ToListAsync();

            budget.OccupiedAreaSqFt = units.Where(u => occupiedIds.Contains(u.Id)).Sum(u => u.Area);
        }

        if (dto.Lines.Count > 0)
        {
            Db.ServiceChargeBudgetLines.RemoveRange(budget.Lines);

            var order = 0;

            foreach (var l in dto.Lines)
            {
                if (l.IsExcludedFromRecovery && string.IsNullOrWhiteSpace(l.ExclusionReason))
                    throw new InvalidOperationException($"\"{l.Label}\" is excluded from recovery but does not say why. The lease clause has to be named.");

                budget.Lines.Add(new ServiceChargeBudgetLine
                {
                    Head = l.Head,
                    Label = l.Label,
                    BudgetAmount = l.BudgetAmount,
                    ActualAmount = l.ActualAmount,
                    VarianceAmount = RealEstateMapper.Money(l.ActualAmount - l.BudgetAmount),
                    Basis = l.Basis,
                    IsVariableCost = l.IsVariableCost,
                    IsExcludedFromRecovery = l.IsExcludedFromRecovery,
                    ExclusionReason = l.ExclusionReason,
                    IsCapped = l.IsCapped,
                    CapAmount = l.CapAmount,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        budget.TotalBudget = RealEstateMapper.Money(budget.Lines.Sum(l => l.BudgetAmount));
        budget.TotalActual = RealEstateMapper.Money(budget.Lines.Sum(l => l.ActualAmount));

        await Db.SaveChangesAsync();
        return (await GetBudgetAsync(budget.Id))!;
    }

    public async Task<ServiceChargeBudgetDto> ApproveBudgetAsync(Guid id, Guid userId)
    {
        var budget = await Db.ServiceChargeBudgets.ForCompany(Tenant)
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new InvalidOperationException("That budget does not exist.");

        if (budget.IsApproved)
            throw new InvalidOperationException($"This budget was approved on {budget.ApprovedOn:dd MMM yyyy}.");

        if (budget.Lines.Count == 0)
            throw new InvalidOperationException("A budget with no lines cannot be approved.");

        // Tenants are entitled to be consulted on the estimate before it is billed, so the
        // apportionment has to exist and balance before the budget can go live.
        if (budget.PropertyId is not null)
        {
            var schedule = await Db.ApportionmentSchedules.ForCompany(Tenant)
                .Include(s => s.Lines)
                .Where(s => s.PropertyId == budget.PropertyId && s.EffectiveFrom <= budget.PeriodFrom)
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (schedule is null)
                throw new InvalidOperationException("No apportionment schedule covers this period. The charge cannot be divided without one.");

            var total = schedule.Lines.Where(l => !l.IsExempt).Sum(l => l.SharePercent);

            if (Math.Abs(total - 100m) > 0.5m)
                throw new InvalidOperationException($"The apportionment adds up to {total:N2}%, not 100%. Fix it before approving.");
        }

        budget.IsApproved = true;
        budget.ApprovedOn = Today;
        budget.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetBudgetAsync(id))!;
    }

    /// <summary>
    /// Raises the on-account instalments for a period. Each tenant's share comes from the
    /// apportionment schedule, so what they are billed can always be traced to a percentage
    /// somebody signed off rather than a number somebody typed.
    /// </summary>
    public async Task<List<ServiceChargeInvoiceDto>> RaiseOnAccountAsync(
        Guid budgetId, DateOnly periodFrom, DateOnly periodTo, bool dryRun, Guid userId)
    {
        var budget = await Db.ServiceChargeBudgets.ForCompany(Tenant)
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == budgetId)
            ?? throw new InvalidOperationException("That budget does not exist.");

        if (!budget.IsApproved)
            throw new InvalidOperationException("This budget has not been approved. Nothing can be billed against it yet.");

        var schedule = await Db.ApportionmentSchedules.ForCompany(Tenant)
            .Include(s => s.Lines)
            .Where(s => s.PropertyId == budget.PropertyId && s.EffectiveFrom <= periodFrom)
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No apportionment schedule covers this period.");

        // The share of the year this instalment covers.
        var yearDays = Math.Max(1, budget.PeriodTo.DayNumber - budget.PeriodFrom.DayNumber + 1);
        var periodDays = Math.Max(1, periodTo.DayNumber - periodFrom.DayNumber + 1);
        var fraction = (decimal)periodDays / yearDays;

        var recoverable = budget.Lines.Sum(l => MapBudgetLine(l, budget).RecoverableAmount);
        var withFee = RealEstateMapper.Money(recoverable * (1m + budget.ManagementFeePercent / 100m));
        var periodTotal = RealEstateMapper.Money(withFee * fraction);

        var already = await Db.ServiceChargeInvoices.ForCompany(Tenant)
            .Where(i => i.ServiceChargeBudgetId == budgetId && i.PeriodFrom == periodFrom && i.PeriodTo == periodTo)
            .Select(i => i.TenancyId)
            .ToListAsync();

        var tenancyIds = schedule.Lines.Where(l => l.TenancyId.HasValue).Select(l => l.TenancyId!.Value).ToList();

        var tenancies = tenancyIds.Count == 0
            ? []
            : await Db.Tenancies.ForCompany(Tenant)
                .Where(t => tenancyIds.Contains(t.Id))
                .ToListAsync();

        var leads = await Db.TenancyParties.ForCompany(Tenant)
            .Where(p => tenancyIds.Contains(p.TenancyId) && p.IsLeadTenant)
            .Select(p => new { p.TenancyId, p.PartyId })
            .ToListAsync();

        var invoices = new List<ServiceChargeInvoice>();

        foreach (var line in schedule.Lines.Where(l => !l.IsExempt && l.TenancyId.HasValue))
        {
            if (already.Contains(line.TenancyId)) continue;

            var tenancy = tenancies.FirstOrDefault(t => t.Id == line.TenancyId);
            if (tenancy is null || tenancy.Status is TenancyStatus.Ended or TenancyStatus.Abandoned) continue;

            var lead = leads.FirstOrDefault(l => l.TenancyId == line.TenancyId);
            if (lead is null) continue;

            var share = line.FixedAmount > 0m
                ? RealEstateMapper.Money(line.FixedAmount * fraction)
                : RealEstateMapper.Money(periodTotal * line.SharePercent / 100m);

            invoices.Add(new ServiceChargeInvoice
            {
                InvoiceNumber = dryRun ? "(preview)" : await numbering.NextServiceChargeInvoiceNumberAsync(DateTime.UtcNow),
                TenancyId = tenancy.Id,
                UnitId = line.UnitId,
                PartyId = lead.PartyId,
                ServiceChargeBudgetId = budgetId,
                InvoiceType = "OnAccount",
                IssuedOn = Today,
                DueDate = periodFrom,
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                Amount = share,
                TotalAmount = share,
                Balance = share,
                SharePercent = line.SharePercent,
                AreaSqFt = line.AreaSqFt,
                Status = InstalmentStatus.NotDue,
            }.StampNew(Tenant, userId));
        }

        if (!dryRun)
        {
            Db.ServiceChargeInvoices.AddRange(invoices);
            budget.TotalBilled = RealEstateMapper.Money(budget.TotalBilled + invoices.Sum(i => i.TotalAmount));
            budget.StampUpdated(userId);
            await Db.SaveChangesAsync();
        }

        var names = await PartyNamesAsync(invoices.Select(i => i.PartyId));

        return invoices.Select(i => new ServiceChargeInvoiceDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            TenancyId = i.TenancyId,
            TenantName = names.GetValueOrDefault(i.PartyId),
            UnitId = i.UnitId,
            InvoiceType = i.InvoiceType,
            IssuedOn = i.IssuedOn,
            DueDate = i.DueDate,
            PeriodFrom = i.PeriodFrom,
            PeriodTo = i.PeriodTo,
            Amount = i.Amount,
            TaxAmount = i.TaxAmount,
            TotalAmount = i.TotalAmount,
            PaidAmount = i.PaidAmount,
            Balance = i.Balance,
            SharePercent = i.SharePercent,
            Status = i.Status,
        }).ToList();
    }

    /// <summary>
    /// The year-end reconciliation. Exclusions, then gross-up, then caps — in that order, because
    /// capping a grossed-up figure and grossing up a capped one give different answers and the
    /// lease says which. Every step is written out so the statement can be checked, not argued.
    /// </summary>
    public async Task<ServiceChargeReconciliationDto> ReconcileAsync(Guid budgetId, bool dryRun, Guid userId)
    {
        var budget = await Db.ServiceChargeBudgets.ForCompany(Tenant)
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == budgetId)
            ?? throw new InvalidOperationException("That budget does not exist.");

        if (budget.IsReconciled && !dryRun)
            throw new InvalidOperationException("This year has already been reconciled.");

        var currency = await CurrencyAsync();
        var today = Today;

        var result = new ServiceChargeReconciliationDto
        {
            ServiceChargeBudgetId = budgetId,
            FinancialYear = budget.FinancialYear,
            ReconciledOn = today,
            CurrencyCode = currency,
            TotalBudget = budget.TotalBudget,
            TotalActual = budget.TotalActual,
        };

        var occupancy = RealEstateMapper.Percent(budget.OccupiedAreaSqFt, budget.TotalGrossLettableAreaSqFt);

        result.Workings.Add($"Actual expenditure for {budget.FinancialYear}: {budget.TotalActual:N0}.");
        result.Workings.Add($"Budget was {budget.TotalBudget:N0}, a variance of {budget.TotalActual - budget.TotalBudget:N0}.");
        result.Workings.Add($"Occupancy over the year: {occupancy:N1}% of {budget.TotalGrossLettableAreaSqFt:N0} sq ft.");

        // Step 1 — exclusions.
        var excluded = budget.Lines.Where(l => l.IsExcludedFromRecovery).ToList();
        result.TotalExcluded = RealEstateMapper.Money(excluded.Sum(l => l.ActualAmount));

        if (excluded.Count > 0)
        {
            result.Workings.Add($"Excluded from recovery: {result.TotalExcluded:N0} across {excluded.Count} heads.");

            foreach (var line in excluded)
                result.Workings.Add($"  · {line.Label} ({line.ActualAmount:N0}) — {line.ExclusionReason}");
        }

        // Step 2 — gross-up on the variable heads only.
        var mapped = budget.Lines.Select(l => MapBudgetLine(l, budget)).ToList();

        result.TotalGrossedUp = RealEstateMapper.Money(mapped.Sum(l => l.GrossedUpAmount));

        if (budget.GrossUpEnabled && occupancy < budget.GrossUpToOccupancyPercent)
        {
            var uplift = result.TotalGrossedUp - budget.TotalActual;

            result.Workings.Add(
                $"Variable costs grossed up from {occupancy:N1}% to {budget.GrossUpToOccupancyPercent:N0}% occupancy, " +
                $"adding {uplift:N0}. Fixed costs are not grossed up.");
        }

        var beforeCap = RealEstateMapper.Money(mapped.Where(l => !l.IsExcludedFromRecovery).Sum(l => l.GrossedUpAmount));

        // Step 3 — the caps. Line caps have already been applied; this is the scheme-wide one.
        var capped = beforeCap;

        if (budget.AnnualCapPercent is > 0m && budget.BaseYear is not null)
        {
            var baseline = await Db.ServiceChargeBudgets.ForCompany(Tenant)
                .Where(b => b.PropertyId == budget.PropertyId && b.FinancialYear == budget.BaseYear)
                .Select(b => (decimal?)b.TotalActual)
                .FirstOrDefaultAsync();

            if (baseline is > 0m)
            {
                var years = Math.Max(1, budget.FinancialYear - budget.BaseYear.Value);

                // A cumulative cap compounds from the base year; an annual cap applies to one
                // year's movement. Where a lease grants both, the lower ceiling governs.
                var annualCeiling = baseline.Value * (decimal)Math.Pow(
                    (double)(1m + budget.AnnualCapPercent.Value / 100m), years);

                var ceiling = annualCeiling;

                if (budget.CumulativeCapPercent is > 0m)
                {
                    var cumulativeCeiling = baseline.Value * (1m + budget.CumulativeCapPercent.Value / 100m);
                    ceiling = Math.Min(ceiling, cumulativeCeiling);
                }

                if (capped > ceiling)
                {
                    result.TotalCapAdjustment = RealEstateMapper.Money(capped - ceiling);
                    capped = RealEstateMapper.Money(ceiling);

                    result.Workings.Add(
                        $"Capped at {ceiling:N0}, being the {budget.BaseYear} base of {baseline.Value:N0} " +
                        $"escalated at {budget.AnnualCapPercent:N1}% for {years} years. " +
                        $"{result.TotalCapAdjustment:N0} is irrecoverable and falls on the landlord.");
                }
            }
        }

        var withFee = RealEstateMapper.Money(capped * (1m + budget.ManagementFeePercent / 100m));

        if (budget.ManagementFeePercent > 0m)
            result.Workings.Add($"Management fee at {budget.ManagementFeePercent:N1}% adds {withFee - capped:N0}.");

        result.TotalRecoverable = withFee;
        result.Workings.Add($"Total recoverable: {result.TotalRecoverable:N0}.");

        result.HeadBreakdown = mapped;

        // Now each tenant's share of the recoverable total, against what they were billed.
        var schedule = await Db.ApportionmentSchedules.ForCompany(Tenant)
            .Include(s => s.Lines)
            .Where(s => s.PropertyId == budget.PropertyId && s.EffectiveFrom <= budget.PeriodTo)
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync();

        var billed = await Db.ServiceChargeInvoices.ForCompany(Tenant)
            .Where(i => i.ServiceChargeBudgetId == budgetId && i.InvoiceType == "OnAccount")
            .GroupBy(i => i.TenancyId)
            .Select(g => new { TenancyId = g.Key, Amount = g.Sum(x => x.TotalAmount) })
            .ToListAsync();

        result.TotalBilledOnAccount = RealEstateMapper.Money(billed.Sum(b => b.Amount));
        result.NetDifference = RealEstateMapper.Money(result.TotalRecoverable - result.TotalBilledOnAccount);

        result.Outcome = result.NetDifference > 0.5m ? ReconciliationOutcome.BalancingCharge
            : result.NetDifference < -0.5m ? ReconciliationOutcome.BalancingCredit
            : ReconciliationOutcome.Nil;

        result.Workings.Add(
            $"Billed on account: {result.TotalBilledOnAccount:N0}. " +
            (result.Outcome == ReconciliationOutcome.BalancingCharge
                ? $"A balancing charge of {result.NetDifference:N0} is due."
                : result.Outcome == ReconciliationOutcome.BalancingCredit
                    ? $"A credit of {Math.Abs(result.NetDifference):N0} is owed to tenants."
                    : "Nothing further is due either way."));

        if (schedule is not null)
        {
            var tenancyIds = schedule.Lines.Where(l => l.TenancyId.HasValue).Select(l => l.TenancyId!.Value).ToList();

            var leads = await Db.TenancyParties.ForCompany(Tenant)
                .Where(p => tenancyIds.Contains(p.TenancyId) && p.IsLeadTenant)
                .Select(p => new { p.TenancyId, p.PartyId })
                .ToListAsync();

            var names = await PartyNamesAsync(leads.Select(l => l.PartyId));

            var unitIds = schedule.Lines.Where(l => l.UnitId.HasValue).Select(l => l.UnitId!.Value).Distinct().ToList();

            var units = unitIds.Count == 0
                ? []
                : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

            foreach (var line in schedule.Lines.Where(l => !l.IsExempt))
            {
                var share = RealEstateMapper.Money(result.TotalRecoverable * line.SharePercent / 100m);
                var onAccount = billed.FirstOrDefault(b => b.TenancyId == line.TenancyId)?.Amount ?? 0m;
                var difference = RealEstateMapper.Money(share - onAccount);
                var lead = leads.FirstOrDefault(l => l.TenancyId == line.TenancyId);

                result.TenantLines.Add(new ReconciliationLineDto
                {
                    TenancyId = line.TenancyId,
                    TenantName = lead is null ? null : names.GetValueOrDefault(lead.PartyId),
                    UnitId = line.UnitId,
                    UnitLabel = line.UnitId is null ? null : units.GetValueOrDefault(line.UnitId.Value),
                    SharePercent = line.SharePercent,
                    RecoverableShare = share,
                    BilledOnAccount = onAccount,
                    Difference = difference,
                    CapAdjustment = RealEstateMapper.Money(result.TotalCapAdjustment * line.SharePercent / 100m),
                    Outcome = difference > 0.5m ? ReconciliationOutcome.BalancingCharge
                        : difference < -0.5m ? ReconciliationOutcome.BalancingCredit
                        : ReconciliationOutcome.Nil,
                });
            }
        }

        if (dryRun) return result;

        var reconciliation = new ServiceChargeReconciliation
        {
            Reference = await numbering.NextMasterCodeAsync(Db.ServiceChargeReconciliations, "SCR"),
            ServiceChargeBudgetId = budgetId,
            ReconciledOn = today,
            TotalBudget = result.TotalBudget,
            TotalActual = result.TotalActual,
            TotalGrossedUp = result.TotalGrossedUp,
            TotalExcluded = result.TotalExcluded,
            TotalCapAdjustment = result.TotalCapAdjustment,
            TotalRecoverable = result.TotalRecoverable,
            TotalBilledOnAccount = result.TotalBilledOnAccount,
            NetDifference = result.NetDifference,
            Outcome = result.Outcome,
        }.StampNew(Tenant, userId);

        Db.ServiceChargeReconciliations.Add(reconciliation);

        foreach (var line in result.TenantLines)
        {
            reconciliation.Lines.Add(new ReconciliationLine
            {
                TenancyId = line.TenancyId,
                UnitId = line.UnitId,
                SharePercent = line.SharePercent,
                RecoverableShare = line.RecoverableShare,
                BilledOnAccount = line.BilledOnAccount,
                Difference = line.Difference,
                CapAdjustment = line.CapAdjustment,
                Outcome = line.Outcome,
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();

        result.Id = reconciliation.Id;
        result.Reference = reconciliation.Reference;
        return result;
    }

    public async Task<ServiceChargeReconciliationDto> FinaliseReconciliationAsync(Guid id, Guid userId)
    {
        var reconciliation = await Db.ServiceChargeReconciliations.ForCompany(Tenant)
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("That reconciliation does not exist.");

        if (reconciliation.IsFinalised)
            throw new InvalidOperationException("This reconciliation has already been finalised.");

        await using var transaction = await Db.Database.BeginTransactionAsync();

        var today = Today;

        // Balancing charges and credits are raised as their own documents, not netted into the
        // next on-account demand — a tenant is entitled to see the year settled on its own terms.
        foreach (var line in reconciliation.Lines.Where(l => l.Outcome != ReconciliationOutcome.Nil && l.TenancyId is not null))
        {
            var lead = await Db.TenancyParties.ForCompany(Tenant)
                .Where(p => p.TenancyId == line.TenancyId && p.IsLeadTenant)
                .Select(p => (Guid?)p.PartyId)
                .FirstOrDefaultAsync();

            if (lead is null) continue;

            var budget = await Db.ServiceChargeBudgets.ForCompany(Tenant)
                .FirstOrDefaultAsync(b => b.Id == reconciliation.ServiceChargeBudgetId);

            var invoice = new ServiceChargeInvoice
            {
                InvoiceNumber = await numbering.NextServiceChargeInvoiceNumberAsync(DateTime.UtcNow),
                TenancyId = line.TenancyId,
                UnitId = line.UnitId,
                PartyId = lead.Value,
                ServiceChargeBudgetId = reconciliation.ServiceChargeBudgetId,
                InvoiceType = line.Outcome == ReconciliationOutcome.BalancingCharge ? "Balancing" : "Credit",
                IssuedOn = today,
                DueDate = today.AddDays(30),
                PeriodFrom = budget?.PeriodFrom ?? today,
                PeriodTo = budget?.PeriodTo ?? today,
                Amount = Math.Abs(line.Difference),
                TotalAmount = Math.Abs(line.Difference),
                Balance = line.Outcome == ReconciliationOutcome.BalancingCharge ? Math.Abs(line.Difference) : 0m,
                SharePercent = line.SharePercent,
                Status = InstalmentStatus.Due,
            }.StampNew(Tenant, userId);

            Db.ServiceChargeInvoices.Add(invoice);

            if (line.Outcome == ReconciliationOutcome.BalancingCharge) line.BalancingInvoiceId = invoice.Id;
            else line.CreditNoteId = invoice.Id;

            line.StampUpdated(userId);
        }

        reconciliation.IsFinalised = true;
        reconciliation.IsIssuedToTenants = true;
        reconciliation.StampUpdated(userId);

        var parent = await Db.ServiceChargeBudgets.ForCompany(Tenant)
            .FirstOrDefaultAsync(b => b.Id == reconciliation.ServiceChargeBudgetId);

        if (parent is not null)
        {
            parent.IsReconciled = true;
            parent.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await ReconcileAsync(reconciliation.ServiceChargeBudgetId, true, userId);
    }

    // ═══ Apportionment ═══════════════════════════════════════════════════════

    public async Task<List<ApportionmentScheduleDto>> GetApportionmentsAsync(Guid propertyId)
    {
        var unit = await AreaUnitAsync();

        var schedules = await Db.ApportionmentSchedules.ForCompany(Tenant)
            .Include(s => s.Lines)
            .Where(s => s.PropertyId == propertyId)
            .OrderByDescending(s => s.EffectiveFrom)
            .ToListAsync();

        if (schedules.Count == 0) return [];

        var today = Today;

        var unitIds = schedules.SelectMany(s => s.Lines).Where(l => l.UnitId.HasValue).Select(l => l.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var tenancyIds = schedules.SelectMany(s => s.Lines).Where(l => l.TenancyId.HasValue).Select(l => l.TenancyId!.Value).Distinct().ToList();

        var leads = tenancyIds.Count == 0
            ? []
            : await Db.TenancyParties.ForCompany(Tenant)
                .Where(p => tenancyIds.Contains(p.TenancyId) && p.IsLeadTenant)
                .Select(p => new { p.TenancyId, p.PartyId })
                .ToListAsync();

        var names = await PartyNamesAsync(leads.Select(l => l.PartyId));

        return schedules.Select(s =>
        {
            var total = s.Lines.Where(l => !l.IsExempt).Sum(l => l.SharePercent);

            return new ApportionmentScheduleDto
            {
                Id = s.Id,
                Name = s.Name,
                PropertyId = s.PropertyId,
                Basis = s.Basis,
                AppliesToHead = s.AppliesToHead,
                EffectiveFrom = s.EffectiveFrom,
                EffectiveTo = s.EffectiveTo,
                IsActive = s.EffectiveFrom <= today && (s.EffectiveTo is null || s.EffectiveTo >= today),
                TotalPercent = RealEstateMapper.Money(total),

                // A schedule that does not add to 100 either over-recovers or leaves the landlord
                // short. It is flagged rather than silently divided.
                IsBalanced = Math.Abs(total - 100m) <= 0.5m,

                Lines = s.Lines.Select(l =>
                {
                    var lead = leads.FirstOrDefault(x => x.TenancyId == l.TenancyId);

                    return new ApportionmentLineDto
                    {
                        Id = l.Id,
                        UnitId = l.UnitId,
                        UnitLabel = l.UnitId is null ? null : units.GetValueOrDefault(l.UnitId.Value),
                        TenancyId = l.TenancyId,
                        TenantName = lead is null ? null : names.GetValueOrDefault(lead.PartyId),
                        Area = RealEstateMapper.AreaOrNull(l.AreaSqFt, unit),
                        SharePercent = l.SharePercent,
                        FixedAmount = l.FixedAmount,
                        IsExempt = l.IsExempt,
                        ExemptionReason = l.ExemptionReason,
                    };
                }).ToList(),
            };
        }).ToList();
    }

    public async Task<ApportionmentScheduleDto> SaveApportionmentAsync(ApportionmentScheduleDto dto, Guid userId)
    {
        var schedule = dto.Id != Guid.Empty
            ? await Db.ApportionmentSchedules.ForCompany(Tenant).Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == dto.Id)
            : null;

        if (schedule is null)
        {
            schedule = new ApportionmentSchedule { PropertyId = dto.PropertyId }.StampNew(Tenant, userId);
            Db.ApportionmentSchedules.Add(schedule);
        }
        else schedule.StampUpdated(userId);

        schedule.Name = dto.Name;
        schedule.Basis = dto.Basis;
        schedule.AppliesToHead = dto.AppliesToHead;
        schedule.EffectiveFrom = dto.EffectiveFrom;
        schedule.EffectiveTo = dto.EffectiveTo;

        Db.ApportionmentLines.RemoveRange(schedule.Lines);

        // Pro-rata by area is derived, not typed. Letting an operator type both the area and the
        // percentage guarantees they will disagree by the second year.
        if (dto.Basis == ApportionmentBasis.ProRataByArea)
        {
            var totalArea = dto.Lines.Where(l => !l.IsExempt).Sum(l => l.Area?.SquareFeet ?? 0m);

            foreach (var l in dto.Lines)
            {
                var area = l.Area?.SquareFeet ?? 0m;

                schedule.Lines.Add(new ApportionmentLine
                {
                    UnitId = l.UnitId,
                    PropertyId = dto.PropertyId,
                    TenancyId = l.TenancyId,
                    AreaSqFt = area,
                    SharePercent = l.IsExempt ? 0m : RealEstateMapper.Percent(area, totalArea),
                    FixedAmount = l.FixedAmount,
                    IsExempt = l.IsExempt,
                    ExemptionReason = l.ExemptionReason,
                }.StampNew(Tenant, userId));
            }
        }
        else
        {
            foreach (var l in dto.Lines)
            {
                schedule.Lines.Add(new ApportionmentLine
                {
                    UnitId = l.UnitId,
                    PropertyId = dto.PropertyId,
                    TenancyId = l.TenancyId,
                    AreaSqFt = l.Area?.SquareFeet ?? 0m,
                    SharePercent = l.IsExempt ? 0m : l.SharePercent,
                    FixedAmount = l.FixedAmount,
                    IsExempt = l.IsExempt,
                    ExemptionReason = l.ExemptionReason,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        return (await GetApportionmentsAsync(dto.PropertyId!.Value)).First(s => s.Id == schedule.Id);
    }

    // ═══ Turnover rent ═══════════════════════════════════════════════════════

    public async Task<TurnoverRentTermDto> SaveTurnoverTermAsync(TurnoverRentTermDto dto, Guid userId)
    {
        var term = await SaveTurnoverTermInternalAsync(dto.TenancyId, dto, userId);
        await Db.SaveChangesAsync();
        return MapTurnoverTerm(term);
    }

    private async Task<TurnoverRentTerm> SaveTurnoverTermInternalAsync(Guid tenancyId, TurnoverRentTermDto dto, Guid userId)
    {
        var tenancy = await RequireAsync<Tenancy>(tenancyId, "That tenancy does not exist.");

        var term = await Db.TurnoverRentTerms.ForCompany(Tenant)
            .Include(t => t.Slabs)
            .FirstOrDefaultAsync(t => t.TenancyId == tenancyId);

        if (term is null)
        {
            term = new TurnoverRentTerm { TenancyId = tenancyId }.StampNew(Tenant, userId);
            Db.TurnoverRentTerms.Add(term);
        }
        else term.StampUpdated(userId);

        term.Basis = dto.Basis;
        term.Percent = dto.Percent;
        term.CalculationPeriod = dto.CalculationPeriod;
        term.PeriodStartMonth = dto.PeriodStartMonth == default ? tenancy.StartDate : dto.PeriodStartMonth;
        term.ExcludedSalesCategories = dto.ExcludedSalesCategories;
        term.RequiresAuditedFigures = dto.RequiresAuditedFigures;
        term.DeclarationDueDays = dto.DeclarationDueDays <= 0 ? 15 : dto.DeclarationDueDays;
        term.OffsetBaseRent = dto.OffsetBaseRent;
        term.IsActive = dto.IsActive;

        // A natural breakpoint is the base rent divided by the percentage — the sales level at
        // which turnover rent would exactly equal the rent already being paid. Deriving it stops
        // the two numbers drifting apart when the rent is reviewed.
        term.BreakpointAmount = dto.Basis == TurnoverRentBasis.NaturalBreakpoint && dto.Percent > 0m
            ? RealEstateMapper.Money(AnnualiseRent(tenancy.Rent, tenancy.Frequency) / (dto.Percent / 100m))
            : dto.BreakpointAmount;

        if (dto.Slabs.Count > 0)
        {
            Db.TurnoverRentSlabs.RemoveRange(term.Slabs);

            var order = 0;

            foreach (var s in dto.Slabs.OrderBy(s => s.FromSales))
            {
                term.Slabs.Add(new TurnoverRentSlab
                {
                    FromSales = s.FromSales,
                    ToSales = s.ToSales,
                    Percent = s.Percent,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        tenancy.TurnoverRentApplies = dto.IsActive;
        tenancy.StampUpdated(userId);

        return term;
    }

    private static TurnoverRentTermDto MapTurnoverTerm(TurnoverRentTerm t) => new()
    {
        Id = t.Id,
        TenancyId = t.TenancyId,
        Basis = t.Basis,
        BreakpointAmount = t.BreakpointAmount,
        Percent = t.Percent,
        CalculationPeriod = t.CalculationPeriod,
        PeriodStartMonth = t.PeriodStartMonth,
        ExcludedSalesCategories = t.ExcludedSalesCategories,
        RequiresAuditedFigures = t.RequiresAuditedFigures,
        DeclarationDueDays = t.DeclarationDueDays,
        OffsetBaseRent = t.OffsetBaseRent,
        IsActive = t.IsActive,
        Slabs = t.Slabs.OrderBy(s => s.SortOrder).Select(s => new TurnoverRentSlabDto
        {
            Id = s.Id,
            FromSales = s.FromSales,
            ToSales = s.ToSales,
            Percent = s.Percent,
            SortOrder = s.SortOrder,
        }).ToList(),
    };

    public async Task<TenantSalesDeclarationDto> SaveSalesDeclarationAsync(TenantSalesDeclarationDto dto, Guid userId)
    {
        var tenancy = await RequireAsync<Tenancy>(dto.TenancyId, "That tenancy does not exist.");

        var term = await Db.TurnoverRentTerms.ForCompany(Tenant)
            .FirstOrDefaultAsync(t => t.TenancyId == dto.TenancyId);

        var declaration = dto.Id != Guid.Empty
            ? await Db.TenantSalesDeclarations.ForCompany(Tenant).FirstOrDefaultAsync(d => d.Id == dto.Id)
            : await Db.TenantSalesDeclarations.ForCompany(Tenant)
                .FirstOrDefaultAsync(d => d.TenancyId == dto.TenancyId && d.PeriodFrom == dto.PeriodFrom);

        if (declaration is null)
        {
            declaration = new TenantSalesDeclaration
            {
                TenancyId = dto.TenancyId,
                TurnoverRentTermId = term?.Id,
                PeriodFrom = dto.PeriodFrom,
                PeriodTo = dto.PeriodTo,
                DueOn = dto.PeriodTo.AddDays(term?.DeclarationDueDays ?? 15),
            }.StampNew(Tenant, userId);

            Db.TenantSalesDeclarations.Add(declaration);
        }
        else declaration.StampUpdated(userId);

        declaration.GrossSales = dto.GrossSales;
        declaration.ExcludedSales = dto.ExcludedSales;
        declaration.NetSales = RealEstateMapper.Money(dto.GrossSales - dto.ExcludedSales);
        declaration.TransactionCount = dto.TransactionCount;
        declaration.FootfallCount = dto.FootfallCount;
        declaration.DeclarationSource = dto.DeclarationSource;
        declaration.IsEstimated = dto.IsEstimated;
        declaration.SupportingDocumentUrl = dto.SupportingDocumentUrl;
        declaration.DeclaredOn = dto.DeclaredOn ?? Today;

        // Audited figures replacing a declaration is the normal path, and the variance is what a
        // landlord chases — a pattern of under-declaration is worth more than one month's rent.
        if (dto.AuditedSales is not null)
        {
            declaration.IsAudited = true;
            declaration.AuditedSales = dto.AuditedSales;
            declaration.AuditedOn = Today;
            declaration.Variance = RealEstateMapper.Money(dto.AuditedSales.Value - declaration.NetSales);
        }

        declaration.IsLate = declaration.DeclaredOn > declaration.DueOn;

        await Db.SaveChangesAsync();
        return (await MapDeclarationsAsync([declaration]))[0];
    }

    public async Task<PaginatedResponse<TenantSalesDeclarationDto>> GetSalesDeclarationsAsync(
        ListQueryDto query, Guid? propertyId, bool overdueOnly)
    {
        var today = Today;

        var tenancyIds = propertyId is null
            ? null
            : Db.Tenancies.ForCompany(Tenant).Where(t => t.PropertyId == propertyId).Select(t => t.Id);

        var q = Db.TenantSalesDeclarations.ForCompany(Tenant)
            .WhereIf(tenancyIds is not null, d => tenancyIds!.Contains(d.TenancyId))
            .WhereIf(overdueOnly, d => d.DeclaredOn == null && d.DueOn < today)
            .OrderByDescending(d => d.PeriodTo);

        return await PageAsync(q, query, MapDeclarationsAsync);
    }

    private async Task<List<TenantSalesDeclarationDto>> MapDeclarationsAsync(List<TenantSalesDeclaration> declarations)
    {
        if (declarations.Count == 0) return [];

        var today = Today;
        var tenancyIds = declarations.Select(d => d.TenancyId).Distinct().ToList();

        var tenancies = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => tenancyIds.Contains(t.Id))
            .ToListAsync();

        var propertyIds = tenancies.Select(t => t.PropertyId).Distinct().ToList();

        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var leads = await Db.TenancyParties.ForCompany(Tenant)
            .Where(p => tenancyIds.Contains(p.TenancyId) && p.IsLeadTenant)
            .Select(p => new { p.TenancyId, p.PartyId })
            .ToListAsync();

        var names = await PartyNamesAsync(leads.Select(l => l.PartyId));

        return declarations.Select(d =>
        {
            var tenancy = tenancies.FirstOrDefault(t => t.Id == d.TenancyId);
            var property = tenancy is null ? null : properties.GetValueOrDefault(tenancy.PropertyId);
            var lead = leads.FirstOrDefault(l => l.TenancyId == d.TenancyId);
            var area = property?.SaleableAreaSqFt ?? 0m;

            return new TenantSalesDeclarationDto
            {
                Id = d.Id,
                TenancyId = d.TenancyId,
                TenancyReference = tenancy?.Reference ?? "—",
                TenantName = lead is null ? "—" : names.GetValueOrDefault(lead.PartyId, "—"),
                UnitLabel = property?.UnitNumber,
                PeriodFrom = d.PeriodFrom,
                PeriodTo = d.PeriodTo,
                DueOn = d.DueOn,
                DeclaredOn = d.DeclaredOn,
                GrossSales = d.GrossSales,
                ExcludedSales = d.ExcludedSales,
                NetSales = d.NetSales,
                TransactionCount = d.TransactionCount,
                FootfallCount = d.FootfallCount,
                DeclarationSource = d.DeclarationSource,
                IsAudited = d.IsAudited,
                AuditedSales = d.AuditedSales,
                Variance = d.Variance,
                IsLate = d.IsLate || (d.DeclaredOn is null && d.DueOn < today),
                LatePenalty = d.LatePenalty,
                IsEstimated = d.IsEstimated,
                SupportingDocumentUrl = d.SupportingDocumentUrl,

                // The benchmark a retail landlord actually manages by.
                SalesPerSqFt = area > 0m ? RealEstateMapper.Money(d.NetSales / area) : null,
            };
        }).ToList();
    }

    /// <summary>
    /// Works out turnover rent for the period. Slabs are applied band by band where the lease has
    /// them, and base rent already paid is offset where the lease says so — billing the gross
    /// figure on an offsetting lease double-charges the tenant.
    /// </summary>
    public async Task<List<OverageInvoiceDto>> CalculateOverageAsync(
        Guid? propertyId, DateOnly periodFrom, DateOnly periodTo, bool dryRun, Guid userId)
    {
        var currency = await CurrencyAsync();
        var today = Today;

        var tenancies = await Db.Tenancies.ForCompany(Tenant)
            .Where(t => t.TurnoverRentApplies && t.Status == TenancyStatus.Active)
            .WhereIf(propertyId.HasValue, t => t.PropertyId == propertyId)
            .ToListAsync();

        if (tenancies.Count == 0) return [];

        var tenancyIds = tenancies.Select(t => t.Id).ToList();

        var terms = await Db.TurnoverRentTerms.ForCompany(Tenant)
            .Include(t => t.Slabs)
            .Where(t => tenancyIds.Contains(t.TenancyId) && t.IsActive)
            .ToListAsync();

        var declarations = await Db.TenantSalesDeclarations.ForCompany(Tenant)
            .Where(d => tenancyIds.Contains(d.TenancyId) && d.PeriodFrom >= periodFrom && d.PeriodTo <= periodTo)
            .ToListAsync();

        var already = await Db.OverageInvoices.ForCompany(Tenant)
            .Where(i => tenancyIds.Contains(i.TenancyId) && i.PeriodFrom == periodFrom && i.PeriodTo == periodTo)
            .Select(i => i.TenancyId)
            .ToListAsync();

        var leads = await Db.TenancyParties.ForCompany(Tenant)
            .Where(p => tenancyIds.Contains(p.TenancyId) && p.IsLeadTenant)
            .Select(p => new { p.TenancyId, p.PartyId })
            .ToListAsync();

        var names = await PartyNamesAsync(leads.Select(l => l.PartyId));

        var propertyIds = tenancies.Select(t => t.PropertyId).Distinct().ToList();

        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.UnitNumber);

        var invoices = new List<OverageInvoice>();
        var results = new List<OverageInvoiceDto>();

        foreach (var tenancy in tenancies)
        {
            if (already.Contains(tenancy.Id)) continue;

            var term = terms.FirstOrDefault(t => t.TenancyId == tenancy.Id);
            if (term is null) continue;

            var mine = declarations.Where(d => d.TenancyId == tenancy.Id).ToList();
            if (mine.Count == 0) continue;

            // Audited figures beat declared ones wherever we have them.
            var sales = RealEstateMapper.Money(mine.Sum(d => d.AuditedSales ?? d.NetSales));

            var provisional = term.RequiresAuditedFigures && mine.Any(d => !d.IsAudited);

            if (sales <= term.BreakpointAmount && term.Slabs.Count == 0) continue;

            decimal gross;
            decimal percentApplied;

            if (term.Slabs.Count > 0)
            {
                // Banded: each slab charges only the sales that fall inside it.
                gross = 0m;

                foreach (var slab in term.Slabs.OrderBy(s => s.FromSales))
                {
                    if (sales <= slab.FromSales) continue;

                    var top = slab.ToSales is null ? sales : Math.Min(sales, slab.ToSales.Value);
                    gross += (top - slab.FromSales) * slab.Percent / 100m;
                }

                percentApplied = sales > 0m ? RealEstateMapper.Percent(gross, sales) : 0m;
            }
            else
            {
                gross = (sales - term.BreakpointAmount) * term.Percent / 100m;
                percentApplied = term.Percent;
            }

            gross = RealEstateMapper.Money(Math.Max(0m, gross));

            var baseRent = term.OffsetBaseRent
                ? RealEstateMapper.Money(AnnualiseRent(tenancy.Rent, tenancy.Frequency)
                    * (periodTo.DayNumber - periodFrom.DayNumber + 1) / 365m)
                : 0m;

            var net = RealEstateMapper.Money(Math.Max(0m, gross - baseRent));

            if (net <= 0m) continue;

            var lead = leads.FirstOrDefault(l => l.TenancyId == tenancy.Id);
            if (lead is null) continue;

            var invoice = new OverageInvoice
            {
                InvoiceNumber = dryRun ? "(preview)" : await numbering.NextMasterCodeAsync(Db.OverageInvoices, "OVR"),
                TenancyId = tenancy.Id,
                PartyId = lead.PartyId,
                TurnoverRentTermId = term.Id,
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                IssuedOn = today,
                DueDate = today.AddDays(30),
                DeclaredSales = sales,
                BreakpointApplied = term.BreakpointAmount,
                SalesAboveBreakpoint = RealEstateMapper.Money(Math.Max(0m, sales - term.BreakpointAmount)),
                PercentApplied = percentApplied,
                GrossOverage = gross,
                BaseRentOffset = baseRent,
                NetOverage = net,
                TotalAmount = net,
                Status = InstalmentStatus.Due,

                // Billed on unaudited figures, so it is marked as what it is and re-issued when
                // the audit lands rather than quietly adjusted.
                IsProvisional = provisional,
            }.StampNew(Tenant, userId);

            invoices.Add(invoice);

            results.Add(new OverageInvoiceDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                TenancyId = tenancy.Id,
                TenantName = names.GetValueOrDefault(lead.PartyId, "—"),
                UnitLabel = properties.GetValueOrDefault(tenancy.PropertyId),
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                IssuedOn = invoice.IssuedOn,
                DueDate = invoice.DueDate,
                DeclaredSales = sales,
                BreakpointApplied = invoice.BreakpointApplied,
                SalesAboveBreakpoint = invoice.SalesAboveBreakpoint,
                PercentApplied = percentApplied,
                GrossOverage = gross,
                BaseRentOffset = baseRent,
                NetOverage = net,
                TotalAmount = net,
                Status = invoice.Status,
                IsProvisional = provisional,
                CurrencyCode = currency,
            });
        }

        if (!dryRun && invoices.Count > 0)
        {
            Db.OverageInvoices.AddRange(invoices);
            await Db.SaveChangesAsync();
        }

        return results;
    }
}
