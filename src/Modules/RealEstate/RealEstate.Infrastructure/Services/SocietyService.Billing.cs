using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Maintenance charges, the monthly bill run, ad-hoc charges and penalties.
///
/// The bill run is the society's single most repeated operation and the one it is judged on. It is
/// dry-runnable, it never bills the same period twice, and it carries arrears forward on the face
/// of the bill rather than in a separate letter — because a resident who sees one number pays it
/// and a resident who receives two documents pays neither.
/// </summary>
public partial class SocietyService
{
    // ═══ Charge schemes ══════════════════════════════════════════════════════

    public async Task<List<MaintenanceChargeSchemeDto>> GetChargeSchemesAsync(Guid societyId)
    {
        var today = Today;

        var schemes = await Db.MaintenanceChargeSchemes.ForCompany(Tenant)
            .Include(s => s.Slabs)
            .Where(s => s.SocietyId == societyId)
            .OrderByDescending(s => s.EffectiveFrom)
            .ToListAsync();

        if (schemes.Count == 0) return [];

        var units = await UnitsForSocietyAsync(societyId);

        return schemes.Select(s =>
        {
            var covered = units.Where(u => Matches(s, u)).ToList();

            return new MaintenanceChargeSchemeDto
            {
                Id = s.Id,
                SocietyId = s.SocietyId,
                Name = s.Name,
                Basis = s.Basis,
                RatePerSqFt = s.RatePerSqFt,
                FlatAmount = s.FlatAmount,
                Frequency = s.Frequency,
                AppliesToSubType = s.AppliesToSubType,
                MinAreaSqFt = s.MinAreaSqFt,
                MaxAreaSqFt = s.MaxAreaSqFt,
                VacantUnitPercent = s.VacantUnitPercent,
                IsTaxable = s.IsTaxable,
                TaxPercent = s.TaxPercent,
                LateFeePercent = s.LateFeePercent,
                LateFeeFlat = s.LateFeeFlat,
                GraceDays = s.GraceDays,
                EarlyPaymentDiscountPercent = s.EarlyPaymentDiscountPercent,
                AllowAnnualPrepayment = s.AllowAnnualPrepayment,
                EffectiveFrom = s.EffectiveFrom,
                EffectiveTo = s.EffectiveTo,
                IsActive = s.IsActive && s.EffectiveFrom <= today && (s.EffectiveTo is null || s.EffectiveTo >= today),
                UnitsCovered = covered.Count,

                // What this scheme is worth a month across the units it covers. It is the number a
                // committee needs before it votes on a rate change.
                MonthlyValue = RealEstateMapper.Money(covered.Sum(u => ComputeCharge(s, u, false)) * MonthlyFactor(s.Frequency)),

                Slabs = s.Slabs.OrderBy(x => x.SortOrder).ThenBy(x => x.FromAreaSqFt).Select(x => new MaintenanceChargeSlabDto
                {
                    Id = x.Id,
                    FromAreaSqFt = x.FromAreaSqFt,
                    ToAreaSqFt = x.ToAreaSqFt,
                    Amount = x.Amount,
                    RatePerSqFt = x.RatePerSqFt,
                    SortOrder = x.SortOrder,
                }).ToList(),
            };
        }).ToList();
    }

    private static decimal MonthlyFactor(RentFrequency frequency) => frequency switch
    {
        RentFrequency.Monthly => 1m,
        RentFrequency.Quarterly => 1m / 3m,
        RentFrequency.HalfYearly => 1m / 6m,
        RentFrequency.Yearly => 1m / 12m,
        _ => 1m,
    };

    /// <summary>One unit as the billing run needs to see it. Kept small; a run reads thousands.</summary>
    private sealed record BillableUnit(
        Guid UnitId, Guid PropertyId, string UnitNumber, decimal AreaSqFt,
        PropertySubType SubType, bool IsOccupied, Guid? PartyId, ResidentKind BilledTo, Guid? ResidentId);

    private async Task<List<BillableUnit>> UnitsForSocietyAsync(Guid societyId)
    {
        var society = await Db.Societies.ForCompany(Tenant).FirstOrDefaultAsync(s => s.Id == societyId);
        if (society is null) return [];

        var units = await Db.Units.ForCompany(Tenant)
            .WhereIf(society.ProjectId.HasValue, u => u.ProjectId == society.ProjectId)
            .Join(Db.Properties.ForCompany(Tenant), u => u.PropertyId, p => p.Id, (u, p) => new
            {
                u.Id,
                u.PropertyId,
                u.UnitNumber,
                Area = p.SaleableAreaSqFt ?? p.CoveredAreaSqFt ?? 0m,
                p.SubType,
                p.Occupancy,
            })
            .ToListAsync();

        var unitIds = units.Select(u => u.Id).ToList();

        // Who the bill goes to. An owner-occupier gets it; a let unit's bill goes to whoever the
        // constitution says, which is why the resident kind is on the record and not assumed.
        var residents = await Db.Residents.ForCompany(Tenant)
            .Where(r => r.SocietyId == societyId && r.MovedOutOn == null)
            .Select(r => new { r.Id, r.UnitId, r.PartyId, r.Kind, r.IsPrimaryContact })
            .ToListAsync();

        return units.Select(u =>
        {
            var mine = residents.Where(r => r.UnitId == u.Id).ToList();

            var billTo = mine.FirstOrDefault(r => r.Kind == ResidentKind.Owner && r.IsPrimaryContact)
                      ?? mine.FirstOrDefault(r => r.Kind == ResidentKind.Owner)
                      ?? mine.FirstOrDefault(r => r.IsPrimaryContact)
                      ?? mine.FirstOrDefault();

            return new BillableUnit(
                u.Id, u.PropertyId, u.UnitNumber, u.Area, u.SubType,
                u.Occupancy != OccupancyState.Vacant,
                billTo?.PartyId, billTo?.Kind ?? ResidentKind.Owner, billTo?.Id);
        }).ToList();
    }

    private static bool Matches(MaintenanceChargeScheme scheme, BillableUnit unit)
        => (scheme.AppliesToSubType is null || scheme.AppliesToSubType == unit.SubType)
        && (scheme.MinAreaSqFt is null || unit.AreaSqFt >= scheme.MinAreaSqFt)
        && (scheme.MaxAreaSqFt is null || unit.AreaSqFt <= scheme.MaxAreaSqFt);

    /// <summary>
    /// The maintenance charge for one unit under one scheme. A pure function, so a resident
    /// querying their bill gets the same number the run produced.
    /// </summary>
    private static decimal ComputeCharge(MaintenanceChargeScheme scheme, BillableUnit unit, bool applyVacancy)
    {
        var amount = scheme.Basis switch
        {
            MaintenanceBasis.PerAreaUnit => scheme.RatePerSqFt * unit.AreaSqFt,
            MaintenanceBasis.FlatRatePerUnit => scheme.FlatAmount,
            MaintenanceBasis.SlabBySize => SlabAmount(scheme, unit.AreaSqFt),
            _ => scheme.FlatAmount,
        };

        // A vacant unit still pays — a share of the lift, the guard and the lighting does not stop
        // because nobody is home — but many constitutions discount it, so the rate is configurable.
        if (applyVacancy && !unit.IsOccupied && scheme.VacantUnitPercent != 100m)
            amount = amount * scheme.VacantUnitPercent / 100m;

        return RealEstateMapper.Money(amount);
    }

    private static decimal SlabAmount(MaintenanceChargeScheme scheme, decimal areaSqFt)
    {
        var slab = scheme.Slabs
            .Where(s => areaSqFt >= s.FromAreaSqFt && (s.ToAreaSqFt is null || areaSqFt <= s.ToAreaSqFt))
            .OrderByDescending(s => s.FromAreaSqFt)
            .FirstOrDefault();

        if (slab is null) return scheme.FlatAmount;

        return slab.Amount > 0m ? slab.Amount : slab.RatePerSqFt * areaSqFt;
    }

    public async Task<MaintenanceChargeSchemeDto> SaveChargeSchemeAsync(MaintenanceChargeSchemeDto dto, Guid userId)
    {
        var scheme = dto.Id != Guid.Empty
            ? await Db.MaintenanceChargeSchemes.ForCompany(Tenant).Include(s => s.Slabs).FirstOrDefaultAsync(s => s.Id == dto.Id)
            : null;

        if (scheme is null)
        {
            scheme = new MaintenanceChargeScheme { SocietyId = dto.SocietyId }.StampNew(Tenant, userId);
            Db.MaintenanceChargeSchemes.Add(scheme);
        }
        else scheme.StampUpdated(userId);

        scheme.Name = dto.Name;
        scheme.Basis = dto.Basis;
        scheme.RatePerSqFt = dto.RatePerSqFt;
        scheme.FlatAmount = dto.FlatAmount;
        scheme.Frequency = dto.Frequency;
        scheme.AppliesToSubType = dto.AppliesToSubType;
        scheme.MinAreaSqFt = dto.MinAreaSqFt;
        scheme.MaxAreaSqFt = dto.MaxAreaSqFt;
        scheme.VacantUnitPercent = dto.VacantUnitPercent <= 0m ? 100m : dto.VacantUnitPercent;
        scheme.IsTaxable = dto.IsTaxable;
        scheme.TaxPercent = dto.TaxPercent;
        scheme.LateFeePercent = dto.LateFeePercent;
        scheme.LateFeeFlat = dto.LateFeeFlat;
        scheme.GraceDays = dto.GraceDays;
        scheme.EarlyPaymentDiscountPercent = dto.EarlyPaymentDiscountPercent;
        scheme.AllowAnnualPrepayment = dto.AllowAnnualPrepayment;
        scheme.EffectiveFrom = dto.EffectiveFrom == default ? Today : dto.EffectiveFrom;
        scheme.EffectiveTo = dto.EffectiveTo;
        scheme.IsActive = dto.IsActive;

        if (dto.Basis == MaintenanceBasis.SlabBySize)
        {
            if (dto.Slabs.Count == 0)
                throw new InvalidOperationException("A slab scheme needs at least one slab.");

            Db.MaintenanceChargeSlabs.RemoveRange(scheme.Slabs);

            var order = 0;

            foreach (var s in dto.Slabs.OrderBy(s => s.FromAreaSqFt))
            {
                scheme.Slabs.Add(new MaintenanceChargeSlab
                {
                    FromAreaSqFt = s.FromAreaSqFt,
                    ToAreaSqFt = s.ToAreaSqFt,
                    Amount = s.Amount,
                    RatePerSqFt = s.RatePerSqFt,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        return (await GetChargeSchemesAsync(scheme.SocietyId)).First(s => s.Id == scheme.Id);
    }

    // ═══ The bill run ════════════════════════════════════════════════════════

    /// <summary>
    /// Bills the whole scheme for a period. Everything a resident owes lands on one document:
    /// maintenance, utilities, ad-hoc charges, penalties, arrears brought forward and any late
    /// fee — because a society that sends four pieces of paper collects less than one that sends one.
    /// </summary>
    public async Task<MaintenanceBillRunResultDto> RunBillingAsync(MaintenanceBillRunDto dto, Guid userId)
    {
        var society = await RequireAsync<Society>(dto.SocietyId, "That society does not exist.");
        var currency = await CurrencyAsync();
        var today = Today;

        var result = new MaintenanceBillRunResultDto { IsDryRun = dto.IsDryRun, CurrencyCode = currency };

        var schemes = await Db.MaintenanceChargeSchemes.ForCompany(Tenant)
            .Include(s => s.Slabs)
            .Where(s => s.SocietyId == dto.SocietyId
                     && s.IsActive
                     && s.EffectiveFrom <= dto.PeriodTo
                     && (s.EffectiveTo == null || s.EffectiveTo >= dto.PeriodFrom))
            .ToListAsync();

        if (schemes.Count == 0)
            throw new InvalidOperationException("No maintenance charge scheme is in force for this period.");

        var units = await UnitsForSocietyAsync(dto.SocietyId);
        result.CandidateCount = units.Count;

        // Already billed for this exact period is skipped, not duplicated. That is what makes a
        // re-run after a crash safe.
        var already = await Db.MaintenanceBills.ForCompany(Tenant)
            .Where(b => b.SocietyId == dto.SocietyId && b.PeriodFrom == dto.PeriodFrom && b.PeriodTo == dto.PeriodTo)
            .Select(b => b.UnitId)
            .ToListAsync();

        var unitIds = units.Select(u => u.UnitId).ToList();

        var charges = await Db.SocietyCharges.ForCompany(Tenant)
            .Where(c => c.SocietyId == dto.SocietyId && c.IsActive
                     && c.EffectiveFrom <= dto.PeriodTo
                     && (c.EffectiveTo == null || c.EffectiveTo >= dto.PeriodFrom))
            .ToListAsync();

        var utilities = dto.IncludeUtilities
            ? await Db.UtilityBills.ForCompany(Tenant)
                .Where(b => b.UnitId != null && unitIds.Contains(b.UnitId.Value)
                         && b.MaintenanceBillId == null
                         && b.PeriodTo <= dto.PeriodTo)
                .ToListAsync()
            : [];

        var penalties = await Db.SocietyPenalties.ForCompany(Tenant)
            .Where(p => p.SocietyId == dto.SocietyId && !p.IsWaived && !p.IsPaid && p.MaintenanceBillId == null)
            .ToListAsync();

        var arrears = dto.IncludeArrears
            ? await Db.MaintenanceBills.ForCompany(Tenant)
                .Where(b => b.SocietyId == dto.SocietyId && b.Balance > 0m && b.PeriodTo < dto.PeriodFrom)
                .GroupBy(b => b.UnitId)
                .Select(g => new { UnitId = g.Key, Amount = g.Sum(x => x.Balance), Oldest = g.Min(x => x.DueDate) })
                .ToListAsync()
            : [];

        var bills = new List<MaintenanceBill>();

        foreach (var unit in units)
        {
            if (dto.ExcludeUnitIds.Contains(unit.UnitId)) { result.SkippedCount++; continue; }
            if (already.Contains(unit.UnitId)) { result.SkippedCount++; continue; }

            if (unit.PartyId is null)
            {
                result.FailedCount++;
                result.Warnings.Add($"{unit.UnitNumber}: nobody is registered as resident, so there is no-one to bill.");
                continue;
            }

            var scheme = schemes.FirstOrDefault(s => Matches(s, unit));

            if (scheme is null)
            {
                result.FailedCount++;
                result.Warnings.Add($"{unit.UnitNumber}: no charge scheme covers a {unit.SubType} of {unit.AreaSqFt:N0} sq ft.");
                continue;
            }

            var bill = new MaintenanceBill
            {
                BillNumber = dto.IsDryRun ? "(preview)" : await numbering.NextMaintenanceBillNumberAsync(DateTime.UtcNow),
                SocietyId = dto.SocietyId,
                UnitId = unit.UnitId,
                PropertyId = unit.PropertyId,
                PartyId = unit.PartyId.Value,
                BilledTo = unit.BilledTo,
                PeriodFrom = dto.PeriodFrom,
                PeriodTo = dto.PeriodTo,
                IssuedOn = today,
                DueDate = dto.DueDate,
            }.StampNew(Tenant, userId);

            var order = 0;

            var maintenance = ComputeCharge(scheme, unit, true);

            bill.MaintenanceAmount = maintenance;

            bill.Lines.Add(new MaintenanceBillLine
            {
                ChargeType = "Maintenance",
                Description = unit.IsOccupied
                    ? $"{scheme.Name} — {dto.PeriodFrom:dd MMM} to {dto.PeriodTo:dd MMM yyyy}"
                    : $"{scheme.Name} (vacant unit at {scheme.VacantUnitPercent:N0}%) — {dto.PeriodFrom:dd MMM} to {dto.PeriodTo:dd MMM yyyy}",
                Quantity = scheme.Basis == MaintenanceBasis.PerAreaUnit ? unit.AreaSqFt : 1m,
                Rate = scheme.Basis == MaintenanceBasis.PerAreaUnit ? scheme.RatePerSqFt : maintenance,
                Amount = maintenance,
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));

            // Ad-hoc society charges, scheme-wide or against this unit.
            foreach (var charge in charges.Where(c => c.UnitId is null || c.UnitId == unit.UnitId))
            {
                bill.OtherChargesAmount += charge.Amount;

                bill.Lines.Add(new MaintenanceBillLine
                {
                    ChargeType = charge.ChargeType,
                    Description = charge.Label,
                    Rate = charge.Amount,
                    Amount = charge.Amount,
                    TaxAmount = charge.IsTaxable ? RealEstateMapper.Money(charge.Amount * scheme.TaxPercent / 100m) : 0m,
                    SocietyChargeId = charge.Id,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }

            foreach (var utility in utilities.Where(u => u.UnitId == unit.UnitId))
            {
                bill.UtilityAmount += utility.TotalAmount;

                bill.Lines.Add(new MaintenanceBillLine
                {
                    ChargeType = "Utility",
                    Description = $"{utility.BillNumber} — {utility.Consumption:N0} units, {utility.PeriodFrom:dd MMM} to {utility.PeriodTo:dd MMM}",
                    Quantity = utility.Consumption,
                    Amount = utility.TotalAmount,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));

                if (!dto.IsDryRun) utility.MaintenanceBillId = bill.Id;
            }

            foreach (var penalty in penalties.Where(p => p.UnitId == unit.UnitId))
            {
                bill.PenaltyAmount += penalty.Amount;

                bill.Lines.Add(new MaintenanceBillLine
                {
                    ChargeType = "Penalty",
                    Description = $"{penalty.ViolationType} observed {penalty.ObservedOn:dd MMM yyyy}",
                    Amount = penalty.Amount,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));

                if (!dto.IsDryRun) penalty.MaintenanceBillId = bill.Id;
            }

            var owing = arrears.FirstOrDefault(a => a.UnitId == unit.UnitId);

            if (owing is not null)
            {
                bill.ArrearsBroughtForward = owing.Amount;

                bill.Lines.Add(new MaintenanceBillLine
                {
                    ChargeType = "Arrears",
                    Description = $"Brought forward, oldest due {owing.Oldest:dd MMM yyyy}",
                    Amount = owing.Amount,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));

                // Late fee on the arrears, not on the new charge — charging a late fee on money
                // that is not yet due is the mistake that makes a bill indefensible.
                if (dto.ApplyLateFees && owing.Oldest.AddDays(scheme.GraceDays) < today)
                {
                    var fee = scheme.LateFeeFlat > 0m
                        ? scheme.LateFeeFlat
                        : RealEstateMapper.Money(owing.Amount * scheme.LateFeePercent / 100m);

                    if (fee > 0m)
                    {
                        bill.LateFeeAmount = fee;

                        bill.Lines.Add(new MaintenanceBillLine
                        {
                            ChargeType = "LateFee",
                            Description = scheme.LateFeeFlat > 0m
                                ? "Late payment charge"
                                : $"Late payment charge at {scheme.LateFeePercent:N1}% of arrears",
                            Amount = fee,
                            SortOrder = order += 10,
                        }.StampNew(Tenant, userId));
                    }
                }
            }

            if (scheme.IsTaxable && scheme.TaxPercent > 0m)
                bill.TaxAmount = RealEstateMapper.Money((bill.MaintenanceAmount + bill.OtherChargesAmount) * scheme.TaxPercent / 100m);

            bill.TotalAmount = RealEstateMapper.Money(
                bill.MaintenanceAmount + bill.UtilityAmount + bill.OtherChargesAmount
                + bill.PenaltyAmount + bill.ArrearsBroughtForward + bill.LateFeeAmount
                + bill.TaxAmount - bill.DiscountAmount);

            bill.Balance = bill.TotalAmount;
            bill.Status = dto.DueDate <= today ? InstalmentStatus.Due : InstalmentStatus.NotDue;

            bills.Add(bill);

            result.GeneratedCount++;
            result.TotalAmount += bill.TotalAmount;
            result.MaintenanceTotal += bill.MaintenanceAmount;
            result.UtilityTotal += bill.UtilityAmount;
            result.ArrearsTotal += bill.ArrearsBroughtForward;
            result.LateFeeTotal += bill.LateFeeAmount;
        }

        result.TotalAmount = RealEstateMapper.Money(result.TotalAmount);
        result.MaintenanceTotal = RealEstateMapper.Money(result.MaintenanceTotal);
        result.UtilityTotal = RealEstateMapper.Money(result.UtilityTotal);
        result.ArrearsTotal = RealEstateMapper.Money(result.ArrearsTotal);
        result.LateFeeTotal = RealEstateMapper.Money(result.LateFeeTotal);

        if (dto.IsDryRun)
        {
            result.Preview = await MapBillsAsync(bills.Take(25).ToList());
            return result;
        }

        await using var transaction = await Db.Database.BeginTransactionAsync();

        Db.MaintenanceBills.AddRange(bills);

        society.MonthlyBillingTotal = RealEstateMapper.Money(result.TotalAmount * MonthlyFactor(schemes[0].Frequency));
        society.StampUpdated(userId);

        await Db.SaveChangesAsync();
        await RefreshDefaultersAsync(dto.SocietyId, userId);
        await transaction.CommitAsync();

        if (dto.SendImmediately)
        {
            foreach (var bill in bills)
            {
                await QueueNotificationAsync(
                    "MaintenanceBillIssued",
                    $"Maintenance bill {bill.BillNumber}",
                    $"{bill.TotalAmount:N0} due by {bill.DueDate:dd MMM yyyy}" +
                    (bill.ArrearsBroughtForward > 0m ? $", including {bill.ArrearsBroughtForward:N0} brought forward." : "."),
                    $"/realestate/maintenance-bills/{bill.Id}",
                    recipientPartyId: bill.PartyId,
                    entityType: "MaintenanceBill",
                    entityId: bill.Id);
            }

            await Db.SaveChangesAsync();
        }

        result.Preview = await MapBillsAsync(bills.Take(25).ToList());
        return result;
    }

    /// <summary>
    /// Recomputes who is a defaulter and whether their amenity access stops. Derived from the
    /// bills every time rather than toggled, so nobody stays suspended after they have paid.
    /// </summary>
    private async Task RefreshDefaultersAsync(Guid societyId, Guid userId)
    {
        var society = await Db.Societies.ForCompany(Tenant).FirstOrDefaultAsync(s => s.Id == societyId);
        if (society is null) return;

        var outstanding = await Db.MaintenanceBills.ForCompany(Tenant)
            .Where(b => b.SocietyId == societyId && b.Balance > 0m)
            .GroupBy(b => b.PartyId)
            .Select(g => new { PartyId = g.Key, Amount = g.Sum(x => x.Balance), Oldest = g.Min(x => x.DueDate) })
            .ToListAsync();

        var residents = await Db.Residents.ForCompany(Tenant)
            .Where(r => r.SocietyId == societyId && r.MovedOutOn == null)
            .ToListAsync();

        var today = Today;

        foreach (var resident in residents)
        {
            var owing = outstanding.FirstOrDefault(o => o.PartyId == resident.PartyId);

            resident.OutstandingDues = RealEstateMapper.Money(owing?.Amount ?? 0m);
            resident.IsDefaulter = owing is not null && owing.Oldest < today;

            resident.AmenitiesSuspended = society.SuspendAmenitiesOnDefault
                && resident.IsDefaulter
                && (society.AmenitySuspensionThreshold <= 0m || resident.OutstandingDues >= society.AmenitySuspensionThreshold);

            resident.StampUpdated(userId);
        }

        society.OutstandingTotal = RealEstateMapper.Money(outstanding.Sum(o => o.Amount));
        society.StampUpdated(userId);

        await Db.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<MaintenanceBillDto>> GetBillsAsync(
        ListQueryDto query, Guid societyId, InstalmentStatus? status)
    {
        var q = Db.MaintenanceBills.ForCompany(Tenant)
            .Include(b => b.Lines)
            .Where(b => b.SocietyId == societyId)
            .WhereIf(status.HasValue, b => b.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), b => b.BillNumber.Contains(query.Search!))
            .WhereIf(query.FromDate.HasValue, b => b.IssuedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, b => b.IssuedOn <= query.ToDate)
            .OrderByDescending(b => b.IssuedOn);

        return await PageAsync(q, query, MapBillsAsync);
    }

    public async Task<MaintenanceBillDto?> GetBillAsync(Guid id)
    {
        var bill = await Db.MaintenanceBills.ForCompany(Tenant)
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bill is null) return null;
        return (await MapBillsAsync([bill]))[0];
    }

    private async Task<List<MaintenanceBillDto>> MapBillsAsync(List<MaintenanceBill> bills)
    {
        if (bills.Count == 0) return [];

        var today = Today;
        var currency = await CurrencyAsync();

        var people = await Db.Parties.ForCompany(Tenant)
            .Where(p => bills.Select(b => b.PartyId).Contains(p.Id))
            .ToListAsync();

        var unitIds = bills.Where(b => b.UnitId.HasValue).Select(b => b.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant)
                .Where(u => unitIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UnitNumber, u.ProjectNodeId })
                .ToListAsync();

        var nodeIds = units.Where(u => u.ProjectNodeId.HasValue).Select(u => u.ProjectNodeId!.Value).Distinct().ToList();

        var nodes = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant)
                .Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        var societies = await Db.Societies.ForCompany(Tenant)
            .Where(s => bills.Select(b => b.SocietyId).Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return bills.Select(b =>
        {
            var person = people.FirstOrDefault(p => p.Id == b.PartyId);
            var unit = b.UnitId is null ? null : units.FirstOrDefault(u => u.Id == b.UnitId);

            return new MaintenanceBillDto
            {
                Id = b.Id,
                BillNumber = b.BillNumber,
                SocietyId = b.SocietyId,
                SocietyName = societies.GetValueOrDefault(b.SocietyId),
                UnitId = b.UnitId,
                UnitLabel = unit?.UnitNumber ?? "—",
                BlockName = unit?.ProjectNodeId is null ? null : nodes.GetValueOrDefault(unit.ProjectNodeId.Value),
                PartyId = b.PartyId,
                PartyName = person is null ? "—" : RealEstateMapper.DisplayName(person),
                PartyPhone = person?.PrimaryPhone,
                BilledTo = b.BilledTo,
                PeriodFrom = b.PeriodFrom,
                PeriodTo = b.PeriodTo,
                IssuedOn = b.IssuedOn,
                DueDate = b.DueDate,
                MaintenanceAmount = b.MaintenanceAmount,
                UtilityAmount = b.UtilityAmount,
                OtherChargesAmount = b.OtherChargesAmount,
                PenaltyAmount = b.PenaltyAmount,
                ArrearsBroughtForward = b.ArrearsBroughtForward,
                LateFeeAmount = b.LateFeeAmount,
                DiscountAmount = b.DiscountAmount,
                TaxAmount = b.TaxAmount,
                TotalAmount = b.TotalAmount,
                PaidAmount = b.PaidAmount,
                Balance = b.Balance,
                CurrencyCode = currency,
                Status = b.Status,
                DaysOverdue = b.Balance > 0m && b.DueDate < today ? today.DayNumber - b.DueDate.DayNumber : 0,
                IsSent = b.IsSent,
                IsDisputed = b.IsDisputed,

                Lines = b.Lines.OrderBy(l => l.SortOrder).Select(l => new MaintenanceBillLineDto
                {
                    Id = l.Id,
                    Description = l.Description ?? l.ChargeType,
                    ChargeType = l.ChargeType,
                    Quantity = l.Quantity,
                    Rate = l.Rate,
                    Amount = l.Amount,
                    TaxAmount = l.TaxAmount,
                    MeterReadingId = l.MeterReadingId,
                    SortOrder = l.SortOrder,
                }).ToList(),
            };
        }).ToList();
    }

    public async Task<List<SocietyChargeDto>> GetSocietyChargesAsync(Guid societyId, Guid? unitId)
    {
        var today = Today;

        var charges = await Db.SocietyCharges.ForCompany(Tenant)
            .Where(c => c.SocietyId == societyId)
            .WhereIf(unitId.HasValue, c => c.UnitId == unitId || c.UnitId == null)
            .OrderBy(c => c.Label)
            .ToListAsync();

        var unitIds = charges.Where(c => c.UnitId.HasValue).Select(c => c.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        return charges.Select(c => new SocietyChargeDto
        {
            Id = c.Id,
            SocietyId = c.SocietyId,
            UnitId = c.UnitId,
            UnitLabel = c.UnitId is null ? "All units" : units.GetValueOrDefault(c.UnitId.Value),
            ChargeType = c.ChargeType,
            Label = c.Label,
            Amount = c.Amount,
            Frequency = c.Frequency,
            IsRecurring = c.IsRecurring,
            EffectiveFrom = c.EffectiveFrom,
            EffectiveTo = c.EffectiveTo,
            IsTaxable = c.IsTaxable,
            IsActive = c.IsActive && c.EffectiveFrom <= today && (c.EffectiveTo is null || c.EffectiveTo >= today),
        }).ToList();
    }

    public async Task<SocietyChargeDto> SaveSocietyChargeAsync(SocietyChargeDto dto, Guid userId)
    {
        var charge = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.SocietyCharges.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
            : null;

        if (charge is null)
        {
            charge = new SocietyCharge { SocietyId = dto.SocietyId, UnitId = dto.UnitId }.StampNew(Tenant, userId);
            Db.SocietyCharges.Add(charge);
        }
        else charge.StampUpdated(userId);

        charge.ChargeType = dto.ChargeType;
        charge.Label = dto.Label;
        charge.Amount = dto.Amount;
        charge.Frequency = dto.Frequency;
        charge.IsRecurring = dto.IsRecurring;
        charge.EffectiveFrom = dto.EffectiveFrom == default ? Today : dto.EffectiveFrom;
        charge.EffectiveTo = dto.EffectiveTo;
        charge.IsTaxable = dto.IsTaxable;
        charge.IsActive = dto.IsActive;

        await Db.SaveChangesAsync();
        dto.Id = charge.Id;
        return dto;
    }

    // ═══ Penalties ═══════════════════════════════════════════════════════════

    /// <summary>
    /// A penalty under the society's bye-laws. It needs evidence and a notice before it becomes
    /// collectable, and it carries an appeal window — a fine somebody could not contest is one a
    /// committee loses in front of a registrar.
    /// </summary>
    public async Task<SocietyPenaltyDto> ImposePenaltyAsync(SocietyPenaltyDto dto, Guid userId)
    {
        _ = await RequireAsync<Society>(dto.SocietyId, "That society does not exist.");

        if (string.IsNullOrWhiteSpace(dto.EvidenceUrl))
            throw new InvalidOperationException("Attach the evidence before imposing a penalty. A fine nobody can see the basis for will not stand.");

        var penalty = new SocietyPenalty
        {
            Reference = await numbering.NextMasterCodeAsync(Db.SocietyPenalties, "PEN"),
            SocietyId = dto.SocietyId,
            UnitId = dto.UnitId,
            PartyId = dto.PartyId,
            ViolationType = dto.ViolationType,
            Description = dto.Description,
            ObservedOn = dto.ObservedOn == default ? Today : dto.ObservedOn,
            Amount = dto.Amount,
            ImposedByUserId = userId,
            EvidenceUrl = dto.EvidenceUrl,
            NoticeServed = dto.NoticeServed,
            NoticeServedOn = dto.NoticeServed ? dto.NoticeServedOn ?? Today : null,
            AppealWindowDays = dto.AppealWindowDays <= 0 ? 14 : dto.AppealWindowDays,
        }.StampNew(Tenant, userId);

        Db.SocietyPenalties.Add(penalty);
        await Db.SaveChangesAsync();

        await QueueNotificationAsync(
            "SocietyPenaltyImposed",
            $"Penalty notice {penalty.Reference}",
            $"{penalty.Amount:N0} for {penalty.ViolationType} observed on {penalty.ObservedOn:dd MMM yyyy}. " +
            $"You may appeal within {penalty.AppealWindowDays} days.",
            $"/realestate/penalties/{penalty.Id}",
            recipientPartyId: penalty.PartyId,
            entityType: "SocietyPenalty",
            entityId: penalty.Id,
            severity: AlertSeverity.Warning);

        await Db.SaveChangesAsync();
        return (await MapPenaltiesAsync([penalty]))[0];
    }

    public async Task<SocietyPenaltyDto> WaivePenaltyAsync(Guid id, string reason, Guid userId)
    {
        var penalty = await RequireAsync<SocietyPenalty>(id, "That penalty does not exist.");

        if (penalty.IsPaid)
            throw new InvalidOperationException("This penalty has been paid. Refund it rather than waiving it.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("A waiver has to say why. Members are entitled to see that fines are waived consistently.");

        var approval = await RaiseApprovalAsync(
            "SocietyPenaltyWaiver", id, penalty.Reference, penalty.Amount,
            $"Waive the {penalty.Amount:N0} penalty for {penalty.ViolationType}. {reason}", userId, note: reason);

        penalty.WaiverApprovalRequestId = approval?.Id;
        penalty.IsWaived = approval is null;
        penalty.StampUpdated(userId);

        await WriteAuditNoteAsync(
            "SocietyPenalty", id, "PenaltyWaived", Guid.Empty, userId,
            amountImpact: penalty.Amount, note: reason,
            approvalRequestId: approval?.Id,
            entityReference: penalty.Reference);

        await Db.SaveChangesAsync();
        return (await MapPenaltiesAsync([penalty]))[0];
    }

    public async Task<PaginatedResponse<SocietyPenaltyDto>> GetPenaltiesAsync(ListQueryDto query, Guid societyId)
    {
        var q = Db.SocietyPenalties.ForCompany(Tenant)
            .Where(p => p.SocietyId == societyId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), p => p.Reference.Contains(query.Search!))
            .WhereIf(query.FromDate.HasValue, p => p.ObservedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, p => p.ObservedOn <= query.ToDate)
            .OrderByDescending(p => p.ObservedOn);

        return await PageAsync(q, query, MapPenaltiesAsync);
    }

    private async Task<List<SocietyPenaltyDto>> MapPenaltiesAsync(List<SocietyPenalty> penalties)
    {
        if (penalties.Count == 0) return [];

        var names = await PartyNamesAsync(penalties.Select(p => p.PartyId));
        var users = await AgentUserNamesAsync(penalties.Select(p => p.ImposedByUserId));

        var unitIds = penalties.Where(p => p.UnitId.HasValue).Select(p => p.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        return penalties.Select(p => new SocietyPenaltyDto
        {
            Id = p.Id,
            Reference = p.Reference,
            SocietyId = p.SocietyId,
            UnitId = p.UnitId,
            UnitLabel = p.UnitId is null ? null : units.GetValueOrDefault(p.UnitId.Value),
            PartyId = p.PartyId,
            PartyName = names.GetValueOrDefault(p.PartyId, "—"),
            ViolationType = p.ViolationType,
            Description = p.Description ?? string.Empty,
            ObservedOn = p.ObservedOn,
            Amount = p.Amount,
            ImposedByName = p.ImposedByUserId is null ? null : users.GetValueOrDefault(p.ImposedByUserId.Value),
            EvidenceUrl = p.EvidenceUrl,
            NoticeServed = p.NoticeServed,
            NoticeServedOn = p.NoticeServedOn,
            AppealWindowDays = p.AppealWindowDays,
            IsAppealed = p.IsAppealed,
            AppealNote = p.AppealNote,
            IsWaived = p.IsWaived,
            IsPaid = p.IsPaid,
            IsRectified = p.IsRectified,
            RectifiedOn = p.RectifiedOn,
        }).ToList();
    }
}
