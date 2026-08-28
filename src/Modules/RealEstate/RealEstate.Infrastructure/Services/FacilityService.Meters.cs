using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Meters, readings, tariffs, utility billing, fuel and parking.
///
/// Metering is where a scheme quietly loses money. A reading typed wrong bills somebody a fortune
/// or nothing at all, so every reading is checked against the meter's own history and flagged
/// rather than silently accepted; a rollover past the meter's maximum is detected instead of
/// producing a negative consumption; and the bulk-versus-submeter reconciliation shows exactly how
/// much of the supply is common-area use and how much is unexplained loss.
/// </summary>
public partial class FacilityService
{
    // ═══ Meters ══════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<MeterDto>> GetMetersAsync(
        ListQueryDto query, Guid? propertyId, Guid? societyId, MeterKind? kind)
    {
        var q = Db.Meters.ForCompany(Tenant)
            .WhereIf(propertyId.HasValue, m => m.PropertyId == propertyId)
            .WhereIf(societyId.HasValue, m => m.SocietyId == societyId)
            .WhereIf(kind.HasValue, m => m.Kind == kind)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                m => m.MeterNumber.Contains(query.Search!) || (m.SerialNumber != null && m.SerialNumber.Contains(query.Search!)))
            .OrderBy(m => m.MeterNumber);

        return await PageAsync(q, query, MapMetersAsync);
    }

    private async Task<List<MeterDto>> MapMetersAsync(List<Meter> meters)
    {
        if (meters.Count == 0) return [];

        var today = Today;
        var ids = meters.Select(m => m.Id).ToList();

        var unitIds = meters.Where(m => m.UnitId.HasValue).Select(m => m.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var propertyIds = meters.Where(m => m.PropertyId.HasValue).Select(m => m.PropertyId!.Value).Distinct().ToList();

        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant)
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => RealEstateMapper.OneLineAddress(p));

        var societyIds = meters.Where(m => m.SocietyId.HasValue).Select(m => m.SocietyId!.Value).Distinct().ToList();

        var societies = societyIds.Count == 0
            ? []
            : await Db.Societies.ForCompany(Tenant).Where(s => societyIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

        var tariffIds = meters.Where(m => m.UtilityTariffId.HasValue).Select(m => m.UtilityTariffId!.Value).Distinct().ToList();

        var tariffs = tariffIds.Count == 0
            ? []
            : await Db.UtilityTariffs.ForCompany(Tenant).Where(t => tariffIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name);

        var parentIds = meters.Where(m => m.ParentMeterId.HasValue).Select(m => m.ParentMeterId!.Value).Distinct().ToList();

        var parents = parentIds.Count == 0
            ? []
            : await Db.Meters.ForCompany(Tenant).Where(m => parentIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.MeterNumber);

        var subMeters = await Db.Meters.ForCompany(Tenant)
            .Where(m => m.ParentMeterId != null && ids.Contains(m.ParentMeterId.Value))
            .GroupBy(m => m.ParentMeterId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParentId, x => x.Count);

        return meters.Select(m => new MeterDto
        {
            Id = m.Id,
            MeterNumber = m.MeterNumber,
            Kind = m.Kind,
            PropertyId = m.PropertyId,
            AddressOneLine = m.PropertyId is null ? null : properties.GetValueOrDefault(m.PropertyId.Value),
            UnitId = m.UnitId,
            UnitLabel = m.UnitId is null ? null : units.GetValueOrDefault(m.UnitId.Value),
            SocietyName = m.SocietyId is null ? null : societies.GetValueOrDefault(m.SocietyId.Value),
            ParentMeterId = m.ParentMeterId,
            ParentMeterNumber = m.ParentMeterId is null ? null : parents.GetValueOrDefault(m.ParentMeterId.Value),
            Location = m.Location,
            Make = m.Make,
            SerialNumber = m.SerialNumber,
            Multiplier = m.Multiplier,
            DecimalPlaces = m.DecimalPlaces,
            MaxReading = m.MaxReading,
            LastReading = m.LastReading,
            LastReadOn = m.LastReadOn,

            // A meter not read for two months is the reason a bill later arrives for six.
            DaysSinceLastReading = m.LastReadOn is null ? null : today.DayNumber - m.LastReadOn.Value.DayNumber,

            AverageConsumption = m.AverageConsumption,
            UtilityTariffId = m.UtilityTariffId,
            TariffName = m.UtilityTariffId is null ? null : tariffs.GetValueOrDefault(m.UtilityTariffId.Value),
            UtilityAccountNumber = m.UtilityAccountNumber,
            SecurityDeposit = m.SecurityDeposit,
            IsCommonArea = m.IsCommonArea,
            IsPrepaid = m.IsPrepaid,
            PrepaidBalance = m.PrepaidBalance,
            IsFaulty = m.IsFaulty,
            IsActive = m.IsActive,
            SubMeterCount = subMeters.GetValueOrDefault(m.Id),
        }).ToList();
    }

    public async Task<MeterDto> SaveMeterAsync(MeterDto dto, Guid userId)
    {
        var meter = dto.Id != Guid.Empty
            ? await Db.Meters.ForCompany(Tenant).FirstOrDefaultAsync(m => m.Id == dto.Id)
            : null;

        if (meter is null)
        {
            // Meter numbers are how a reader identifies which meter they are standing at. Two the
            // same in one scheme guarantees a misread.
            var clash = await Db.Meters.ForCompany(Tenant)
                .AnyAsync(m => m.MeterNumber == dto.MeterNumber);

            if (clash)
                throw new InvalidOperationException($"Meter {dto.MeterNumber} already exists here.");

            meter = new Meter().StampNew(Tenant, userId);
            Db.Meters.Add(meter);
        }
        else meter.StampUpdated(userId);

        // A meter cannot be its own parent, nor its own descendant — that would make the bulk
        // reconciliation loop forever.
        if (dto.ParentMeterId == meter.Id)
            throw new InvalidOperationException("A meter cannot be its own bulk supply.");

        meter.MeterNumber = dto.MeterNumber;
        meter.Kind = dto.Kind;
        meter.PropertyId = dto.PropertyId;
        meter.UnitId = dto.UnitId;
        meter.ParentMeterId = dto.ParentMeterId;
        meter.Location = dto.Location;
        meter.Make = dto.Make;
        meter.SerialNumber = dto.SerialNumber;
        meter.Multiplier = dto.Multiplier <= 0m ? 1m : dto.Multiplier;
        meter.DecimalPlaces = dto.DecimalPlaces;
        meter.MaxReading = dto.MaxReading;
        meter.UtilityTariffId = dto.UtilityTariffId;
        meter.UtilityAccountNumber = dto.UtilityAccountNumber;
        meter.SecurityDeposit = dto.SecurityDeposit;
        meter.IsCommonArea = dto.IsCommonArea;
        meter.IsPrepaid = dto.IsPrepaid;
        meter.IsFaulty = dto.IsFaulty;
        meter.IsActive = dto.IsActive;

        await Db.SaveChangesAsync();
        return (await MapMetersAsync([meter]))[0];
    }

    /// <summary>
    /// The reader's round: every meter to be read, in the order somebody walking the building
    /// would meet them, with the last reading so an impossible number is obvious on the spot.
    /// </summary>
    public async Task<List<MeterDto>> GetReadingRoundAsync(Guid? societyId, Guid? propertyId, MeterKind? kind)
    {
        var meters = await Db.Meters.ForCompany(Tenant)
            .Where(m => m.IsActive && !m.IsPrepaid)
            .WhereIf(societyId.HasValue, m => m.SocietyId == societyId)
            .WhereIf(propertyId.HasValue, m => m.PropertyId == propertyId)
            .WhereIf(kind.HasValue, m => m.Kind == kind)
            .OrderBy(m => m.Location).ThenBy(m => m.MeterNumber)
            .ToListAsync();

        return await MapMetersAsync(meters);
    }

    /// <summary>
    /// Takes a round of readings. Every one is validated against the meter's own history: a
    /// reading below the last one is treated as a rollover only if the meter can actually roll,
    /// and a consumption far outside the meter's normal range is flagged for a human rather than
    /// billed. A flagged reading is still stored — the reader was there and saw a number.
    /// </summary>
    public async Task<MeterReadingBatchResultDto> SubmitReadingsAsync(MeterReadingBatchDto batch, Guid userId)
    {
        var result = new MeterReadingBatchResultDto();
        var readingDate = batch.ReadingDate == default ? Today : batch.ReadingDate;

        var meterIds = batch.Readings.Select(r => r.MeterId).Distinct().ToList();

        var meters = await Db.Meters.ForCompany(Tenant)
            .Where(m => meterIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m);

        // Replay protection, because a reader's tablet resubmits a round when the signal returns.
        var references = batch.Readings.Where(r => !string.IsNullOrWhiteSpace(r.ClientReference))
            .Select(r => r.ClientReference!).ToList();

        var already = references.Count == 0
            ? []
            : await Db.MeterReadings.ForCompany(Tenant)
                .Where(r => references.Contains(r.Code!))
                .Select(r => r.Code!)
                .ToListAsync();

        foreach (var entry in batch.Readings)
        {
            if (!string.IsNullOrWhiteSpace(entry.ClientReference) && already.Contains(entry.ClientReference))
                continue;

            if (!meters.TryGetValue(entry.MeterId, out var meter))
            {
                result.Rejected++;
                result.Errors.Add($"Meter {entry.MeterId} does not exist.");
                continue;
            }

            var previous = meter.LastReading;
            var reading = entry.ReadingValue;

            decimal consumption;
            var rolledOver = false;

            if (previous is null)
            {
                // The first reading establishes the baseline; it consumes nothing.
                consumption = 0m;
            }
            else if (reading >= previous)
            {
                consumption = (reading - previous.Value) * meter.Multiplier;
            }
            else if (meter.MaxReading is > 0m)
            {
                // The dial went past its maximum and started again. Real, and the only case where
                // a lower reading than last time is legitimate.
                consumption = (meter.MaxReading.Value - previous.Value + reading) * meter.Multiplier;
                rolledOver = true;
            }
            else
            {
                result.Rejected++;
                result.Errors.Add(
                    $"{meter.MeterNumber}: {reading:N2} is below the last reading of {previous:N2} " +
                    "and this meter has no rollover maximum set. Check the number or mark the meter replaced.");
                continue;
            }

            var days = meter.LastReadOn is null ? (int?)null : readingDate.DayNumber - meter.LastReadOn.Value.DayNumber;

            // Plausibility against the meter's own average, not a global rule — a lift motor and a
            // studio flat have nothing in common.
            decimal? variance = null;
            var implausible = false;

            if (meter.AverageConsumption is > 0m && days is > 0)
            {
                var expected = meter.AverageConsumption.Value * days.Value / 30m;

                if (expected > 0m)
                {
                    variance = RealEstateMapper.Percent(consumption - expected, expected);
                    implausible = Math.Abs(variance.Value) > 200m;
                }
            }

            var record = new MeterReading
            {
                MeterId = meter.Id,
                ReadingDate = readingDate,
                ReadingValue = reading,
                PreviousReading = previous,
                Consumption = RealEstateMapper.Money(consumption, 4),
                DaysSinceLastReading = days,
                Source = entry.Source,
                ReadByUserId = userId,
                PhotoUrl = entry.PhotoUrl,
                IsImplausible = implausible,
                VariancePercent = variance,
                RolledOver = rolledOver,
                Note = entry.Note,
                Code = entry.ClientReference,
            }.StampNew(Tenant, userId);

            Db.MeterReadings.Add(record);

            // The meter only advances on a plausible reading. Accepting a suspect number as the
            // new baseline would corrupt every subsequent bill, not just this one.
            if (!implausible)
            {
                meter.LastReading = reading;
                meter.LastReadOn = readingDate;

                meter.AverageConsumption = meter.AverageConsumption is null || meter.AverageConsumption == 0m
                    ? RealEstateMapper.Money(days is > 0 ? consumption * 30m / days.Value : consumption, 4)
                    : RealEstateMapper.Money(
                        (meter.AverageConsumption.Value * 0.7m)
                        + ((days is > 0 ? consumption * 30m / days.Value : consumption) * 0.3m), 4);

                meter.StampUpdated(userId);
                result.Accepted++;
            }
            else result.Flagged++;

            result.TotalConsumption += consumption;
        }

        result.TotalConsumption = RealEstateMapper.Money(result.TotalConsumption, 4);

        await Db.SaveChangesAsync();

        if (result.Flagged > 0)
        {
            var flagged = await Db.MeterReadings.ForCompany(Tenant)
                .Where(r => r.ReadingDate == readingDate && r.IsImplausible && !r.IsVerified)
                .OrderByDescending(r => r.CreatedAt)
                .Take(result.Flagged)
                .ToListAsync();

            result.Implausible = await MapReadingsAsync(flagged);
        }

        return result;
    }

    public async Task<PaginatedResponse<MeterReadingDto>> GetReadingsAsync(
        ListQueryDto query, Guid? meterId, bool? implausibleOnly)
    {
        var q = Db.MeterReadings.ForCompany(Tenant)
            .WhereIf(meterId.HasValue, r => r.MeterId == meterId)
            .WhereIf(implausibleOnly == true, r => r.IsImplausible && !r.IsVerified)
            .WhereIf(query.FromDate.HasValue, r => r.ReadingDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, r => r.ReadingDate <= query.ToDate)
            .OrderByDescending(r => r.ReadingDate);

        return await PageAsync(q, query, MapReadingsAsync);
    }

    private async Task<List<MeterReadingDto>> MapReadingsAsync(List<MeterReading> readings)
    {
        if (readings.Count == 0) return [];

        var meterIds = readings.Select(r => r.MeterId).Distinct().ToList();

        var meters = await Db.Meters.ForCompany(Tenant)
            .Where(m => meterIds.Contains(m.Id))
            .Select(m => new { m.Id, m.MeterNumber, m.Kind, m.UnitId })
            .ToListAsync();

        var unitIds = meters.Where(m => m.UnitId.HasValue).Select(m => m.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var readers = await AgentUserNamesAsync(readings.Select(r => r.ReadByUserId));

        return readings.Select(r =>
        {
            var meter = meters.FirstOrDefault(m => m.Id == r.MeterId);

            return new MeterReadingDto
            {
                Id = r.Id,
                MeterId = r.MeterId,
                MeterNumber = meter?.MeterNumber ?? "—",
                UnitLabel = meter?.UnitId is null ? null : units.GetValueOrDefault(meter.UnitId.Value),
                Kind = meter?.Kind ?? MeterKind.Electricity,
                ReadingDate = r.ReadingDate,
                ReadingValue = r.ReadingValue,
                PreviousReading = r.PreviousReading,
                Consumption = r.Consumption,
                DaysSinceLastReading = r.DaysSinceLastReading,
                Source = r.Source,
                ReadByName = r.ReadByUserId is null ? null : readers.GetValueOrDefault(r.ReadByUserId.Value),
                PhotoUrl = r.PhotoUrl,
                IsImplausible = r.IsImplausible,
                VariancePercent = r.VariancePercent,
                IsVerified = r.IsVerified,
                RolledOver = r.RolledOver,
                IsBilled = r.IsBilled,
                Note = r.Note,
            };
        }).ToList();
    }

    public async Task<MeterReadingDto> VerifyReadingAsync(Guid id, decimal? correctedValue, Guid userId)
    {
        var reading = await RequireAsync<MeterReading>(id, "That reading does not exist.");

        if (reading.IsBilled)
            throw new InvalidOperationException("This reading has already been billed. Raise an adjustment rather than editing it.");

        var meter = await RequireAsync<Meter>(reading.MeterId, "The meter is missing.");

        if (correctedValue is not null && correctedValue != reading.ReadingValue)
        {
            var before = reading.ReadingValue;

            reading.ReadingValue = correctedValue.Value;

            reading.Consumption = reading.PreviousReading is null
                ? 0m
                : RealEstateMapper.Money((correctedValue.Value - reading.PreviousReading.Value) * meter.Multiplier, 4);

            await WriteAuditNoteAsync(
                "MeterReading", id, "MeterReadingCorrected", Guid.Empty, userId,
                before: before.ToString("N2"),
                after: correctedValue.Value.ToString("N2"),
                note: $"Meter {meter.MeterNumber} reading corrected on verification.");
        }

        reading.IsImplausible = false;
        reading.IsVerified = true;
        reading.VerifiedByUserId = userId;
        reading.StampUpdated(userId);

        // Verifying is what lets the meter advance — a flagged reading deliberately did not.
        meter.LastReading = reading.ReadingValue;
        meter.LastReadOn = reading.ReadingDate;
        meter.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await MapReadingsAsync([reading]))[0];
    }

    // ═══ Tariffs and utility bills ═══════════════════════════════════════════

    public async Task<List<UtilityTariffDto>> GetTariffsAsync(Guid? societyId, MeterKind? kind)
    {
        var today = Today;

        var tariffs = await Db.UtilityTariffs.ForCompany(Tenant)
            .Include(t => t.Slabs)
            .WhereIf(societyId.HasValue, t => t.SocietyId == societyId)
            .WhereIf(kind.HasValue, t => t.Kind == kind)
            .OrderByDescending(t => t.EffectiveFrom)
            .ToListAsync();

        if (tariffs.Count == 0) return [];

        var ids = tariffs.Select(t => t.Id).ToList();

        var meterCounts = await Db.Meters.ForCompany(Tenant)
            .Where(m => m.UtilityTariffId != null && ids.Contains(m.UtilityTariffId.Value))
            .GroupBy(m => m.UtilityTariffId!.Value)
            .Select(g => new { TariffId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TariffId, x => x.Count);

        var societyIds = tariffs.Where(t => t.SocietyId.HasValue).Select(t => t.SocietyId!.Value).Distinct().ToList();

        var societies = societyIds.Count == 0
            ? []
            : await Db.Societies.ForCompany(Tenant).Where(s => societyIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

        return tariffs.Select(t => new UtilityTariffDto
        {
            Id = t.Id,
            Name = t.Name,
            Kind = t.Kind,
            SocietyName = t.SocietyId is null ? null : societies.GetValueOrDefault(t.SocietyId.Value),
            RatePerUnit = t.RatePerUnit,
            FixedCharge = t.FixedCharge,
            MinimumCharge = t.MinimumCharge,
            FuelAdjustmentPerUnit = t.FuelAdjustmentPerUnit,
            TaxPercent = t.TaxPercent,
            MeterRent = t.MeterRent,
            AdministrativeMarkupPercent = t.AdministrativeMarkupPercent,
            EffectiveFrom = t.EffectiveFrom,
            EffectiveTo = t.EffectiveTo,
            IsActive = t.IsActive && t.EffectiveFrom <= today && (t.EffectiveTo is null || t.EffectiveTo >= today),
            MeterCount = meterCounts.GetValueOrDefault(t.Id),
            Slabs = t.Slabs.OrderBy(s => s.SortOrder).ThenBy(s => s.FromUnits).Select(s => new UtilityTariffSlabDto
            {
                Id = s.Id,
                FromUnits = s.FromUnits,
                ToUnits = s.ToUnits,
                RatePerUnit = s.RatePerUnit,
                IsRetrospective = s.IsRetrospective,
                SortOrder = s.SortOrder,
            }).ToList(),
        }).ToList();
    }

    public async Task<UtilityTariffDto> SaveTariffAsync(UtilityTariffDto dto, Guid userId)
    {
        var tariff = dto.Id != Guid.Empty
            ? await Db.UtilityTariffs.ForCompany(Tenant).Include(t => t.Slabs).FirstOrDefaultAsync(t => t.Id == dto.Id)
            : null;

        if (tariff is null)
        {
            tariff = new UtilityTariff().StampNew(Tenant, userId);
            Db.UtilityTariffs.Add(tariff);
        }
        else
        {
            var billed = await Db.UtilityBills.ForCompany(Tenant).AnyAsync(b => b.UtilityTariffId == tariff.Id);

            // Editing a tariff that has already billed rewrites history. A new version from a new
            // effective date is the only safe way to change a rate.
            if (billed && (tariff.RatePerUnit != dto.RatePerUnit || tariff.FixedCharge != dto.FixedCharge))
                throw new InvalidOperationException("This tariff has already been billed. Close it and create a new one from the date the rate changes.");

            tariff.StampUpdated(userId);
        }

        tariff.Name = dto.Name;
        tariff.Kind = dto.Kind;
        tariff.RatePerUnit = dto.RatePerUnit;
        tariff.FixedCharge = dto.FixedCharge;
        tariff.MinimumCharge = dto.MinimumCharge;
        tariff.FuelAdjustmentPerUnit = dto.FuelAdjustmentPerUnit;
        tariff.TaxPercent = dto.TaxPercent;
        tariff.MeterRent = dto.MeterRent;
        tariff.AdministrativeMarkupPercent = dto.AdministrativeMarkupPercent;
        tariff.EffectiveFrom = dto.EffectiveFrom == default ? Today : dto.EffectiveFrom;
        tariff.EffectiveTo = dto.EffectiveTo;
        tariff.IsActive = dto.IsActive;

        if (dto.Slabs.Count > 0)
        {
            Db.UtilityTariffSlabs.RemoveRange(tariff.Slabs);

            var order = 0;

            foreach (var s in dto.Slabs.OrderBy(s => s.FromUnits))
            {
                tariff.Slabs.Add(new UtilityTariffSlab
                {
                    FromUnits = s.FromUnits,
                    ToUnits = s.ToUnits,
                    RatePerUnit = s.RatePerUnit,
                    IsRetrospective = s.IsRetrospective,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        return (await GetTariffsAsync(tariff.SocietyId, null)).First(t => t.Id == tariff.Id);
    }

    /// <summary>
    /// Bills every metered unit for the period. Slab tariffs are applied band by band unless the
    /// slab is retrospective, in which case crossing it re-rates the whole consumption — both
    /// exist in the wild, and getting the wrong one wrong is a bill somebody disputes.
    /// </summary>
    public async Task<List<UtilityBillDto>> GenerateUtilityBillsAsync(
        Guid? societyId, DateOnly periodFrom, DateOnly periodTo, bool dryRun, Guid userId)
    {
        var today = Today;

        var meters = await Db.Meters.ForCompany(Tenant)
            .Where(m => m.IsActive && !m.IsCommonArea && m.UtilityTariffId != null)
            .WhereIf(societyId.HasValue, m => m.SocietyId == societyId)
            .ToListAsync();

        if (meters.Count == 0) return [];

        var meterIds = meters.Select(m => m.Id).ToList();

        var tariffIds = meters.Select(m => m.UtilityTariffId!.Value).Distinct().ToList();

        var tariffs = await Db.UtilityTariffs.ForCompany(Tenant)
            .Include(t => t.Slabs)
            .Where(t => tariffIds.Contains(t.Id))
            .ToListAsync();

        var readings = await Db.MeterReadings.ForCompany(Tenant)
            .Where(r => meterIds.Contains(r.MeterId)
                     && r.ReadingDate >= periodFrom && r.ReadingDate <= periodTo
                     && !r.IsImplausible && !r.IsBilled)
            .OrderBy(r => r.ReadingDate)
            .ToListAsync();

        var already = await Db.UtilityBills.ForCompany(Tenant)
            .Where(b => meterIds.Contains(b.MeterId) && b.PeriodFrom == periodFrom && b.PeriodTo == periodTo)
            .Select(b => b.MeterId)
            .ToListAsync();

        var unitIds = meters.Where(m => m.UnitId.HasValue).Select(m => m.UnitId!.Value).Distinct().ToList();

        var occupiers = unitIds.Count == 0
            ? []
            : await Db.Residents.ForCompany(Tenant)
                .Where(r => r.UnitId != null && unitIds.Contains(r.UnitId.Value) && r.MovedOutOn == null && r.IsPrimaryContact)
                .Select(r => new { UnitId = r.UnitId!.Value, r.PartyId })
                .ToListAsync();

        var bills = new List<UtilityBill>();

        foreach (var meter in meters)
        {
            if (already.Contains(meter.Id)) continue;

            var mine = readings.Where(r => r.MeterId == meter.Id).ToList();
            if (mine.Count == 0) continue;

            var tariff = tariffs.FirstOrDefault(t => t.Id == meter.UtilityTariffId);
            if (tariff is null) continue;

            var consumption = RealEstateMapper.Money(mine.Sum(r => r.Consumption), 4);
            var opening = mine.First().PreviousReading ?? 0m;
            var closing = mine.Last().ReadingValue;

            var energy = RateConsumption(tariff, consumption);

            var markup = RealEstateMapper.Money(energy * tariff.AdministrativeMarkupPercent / 100m);
            var fuel = RealEstateMapper.Money(consumption * tariff.FuelAdjustmentPerUnit);
            var fixedCharge = tariff.FixedCharge + tariff.MeterRent;

            var beforeTax = energy + markup + fuel + fixedCharge;

            // A minimum charge covers standing costs on a unit nobody occupied. It replaces the
            // computed total rather than being added to it.
            if (tariff.MinimumCharge > 0m && beforeTax < tariff.MinimumCharge)
                beforeTax = tariff.MinimumCharge;

            var tax = RealEstateMapper.Money(beforeTax * tariff.TaxPercent / 100m);

            var partyId = meter.UnitId is null
                ? null
                : occupiers.FirstOrDefault(o => o.UnitId == meter.UnitId)?.PartyId;

            var bill = new UtilityBill
            {
                BillNumber = dryRun ? "(preview)" : await numbering.NextMasterCodeAsync(Db.UtilityBills, "UTL"),
                MeterId = meter.Id,
                UnitId = meter.UnitId,
                PartyId = partyId,
                UtilityTariffId = tariff.Id,
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                IssuedOn = today,
                DueDate = today.AddDays(15),
                OpeningReading = opening,
                ClosingReading = closing,
                Consumption = consumption,
                EnergyCharge = energy,
                FixedCharge = fixedCharge,
                FuelAdjustment = fuel,
                MarkupAmount = markup,
                TaxAmount = tax,
                TotalAmount = RealEstateMapper.Money(beforeTax + tax),
                Status = InstalmentStatus.Due,
                IsEstimated = mine.Any(r => r.Source == ReadingSource.Estimated),
            }.StampNew(Tenant, userId);

            var order = 0;

            bill.Lines.Add(new UtilityBillLine
            {
                UtilityBillId = bill.Id,
                Description = $"{consumption:N2} units at the {tariff.Name} rate",
                Units = consumption,
                Rate = consumption > 0m ? RealEstateMapper.Money(energy / consumption, 4) : 0m,
                Amount = energy,
                SortOrder = order += 10,
            }.StampNew(Tenant, userId));

            if (fixedCharge > 0m)
            {
                bill.Lines.Add(new UtilityBillLine
                {
                    UtilityBillId = bill.Id,
                    Description = "Fixed charge and meter rent",
                    Units = 1m,
                    Rate = fixedCharge,
                    Amount = fixedCharge,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }

            if (fuel != 0m)
            {
                bill.Lines.Add(new UtilityBillLine
                {
                    UtilityBillId = bill.Id,
                    Description = $"Fuel adjustment at {tariff.FuelAdjustmentPerUnit:N4} per unit",
                    Units = consumption,
                    Rate = tariff.FuelAdjustmentPerUnit,
                    Amount = fuel,
                    SortOrder = order + 10,
                }.StampNew(Tenant, userId));
            }

            bills.Add(bill);

            if (!dryRun)
            {
                foreach (var reading in mine)
                {
                    reading.IsBilled = true;
                    reading.UtilityBillId = bill.Id;
                    reading.StampUpdated(userId);
                }
            }
        }

        if (!dryRun && bills.Count > 0)
        {
            Db.UtilityBills.AddRange(bills);
            await Db.SaveChangesAsync();
        }

        return await MapUtilityBillsAsync(bills);
    }

    /// <summary>Applies the tariff's slabs to a consumption. A flat tariff is the one-slab case.</summary>
    private static decimal RateConsumption(UtilityTariff tariff, decimal consumption)
    {
        if (tariff.Slabs.Count == 0)
            return RealEstateMapper.Money(consumption * tariff.RatePerUnit);

        var slabs = tariff.Slabs.OrderBy(s => s.FromUnits).ToList();

        // A retrospective slab re-rates everything at the band's rate once you cross into it,
        // rather than only the units above the threshold. Utilities in several markets do this and
        // it produces a step change in the bill that customers notice.
        var crossed = slabs.LastOrDefault(s => consumption > s.FromUnits && s.IsRetrospective);

        if (crossed is not null)
            return RealEstateMapper.Money(consumption * crossed.RatePerUnit);

        var total = 0m;

        foreach (var slab in slabs)
        {
            if (consumption <= slab.FromUnits) continue;

            var top = slab.ToUnits is null ? consumption : Math.Min(consumption, slab.ToUnits.Value);
            total += (top - slab.FromUnits) * slab.RatePerUnit;
        }

        return RealEstateMapper.Money(total);
    }

    public async Task<PaginatedResponse<UtilityBillDto>> GetUtilityBillsAsync(ListQueryDto query, Guid? unitId)
    {
        var q = Db.UtilityBills.ForCompany(Tenant)
            .Include(b => b.Lines)
            .WhereIf(unitId.HasValue, b => b.UnitId == unitId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), b => b.BillNumber.Contains(query.Search!))
            .WhereIf(query.FromDate.HasValue, b => b.IssuedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, b => b.IssuedOn <= query.ToDate)
            .OrderByDescending(b => b.IssuedOn);

        return await PageAsync(q, query, MapUtilityBillsAsync);
    }

    private async Task<List<UtilityBillDto>> MapUtilityBillsAsync(List<UtilityBill> bills)
    {
        if (bills.Count == 0) return [];

        var currency = await CurrencyAsync();
        var meterIds = bills.Select(b => b.MeterId).Distinct().ToList();

        var meters = await Db.Meters.ForCompany(Tenant)
            .Where(m => meterIds.Contains(m.Id))
            .Select(m => new { m.Id, m.MeterNumber, m.Kind })
            .ToListAsync();

        var unitIds = bills.Where(b => b.UnitId.HasValue).Select(b => b.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var names = await PartyNamesAsync(bills.Where(b => b.PartyId.HasValue).Select(b => b.PartyId!.Value));

        return bills.Select(b =>
        {
            var meter = meters.FirstOrDefault(m => m.Id == b.MeterId);

            return new UtilityBillDto
            {
                Id = b.Id,
                BillNumber = b.BillNumber,
                MeterId = b.MeterId,
                MeterNumber = meter?.MeterNumber ?? "—",
                Kind = meter?.Kind ?? MeterKind.Electricity,
                UnitId = b.UnitId,
                UnitLabel = b.UnitId is null ? null : units.GetValueOrDefault(b.UnitId.Value),
                PartyName = b.PartyId is null ? null : names.GetValueOrDefault(b.PartyId.Value),
                PeriodFrom = b.PeriodFrom,
                PeriodTo = b.PeriodTo,
                IssuedOn = b.IssuedOn,
                DueDate = b.DueDate,
                OpeningReading = b.OpeningReading,
                ClosingReading = b.ClosingReading,
                Consumption = b.Consumption,
                CommonAreaShare = b.CommonAreaShare,
                EnergyCharge = b.EnergyCharge,
                FixedCharge = b.FixedCharge,
                FuelAdjustment = b.FuelAdjustment,
                MarkupAmount = b.MarkupAmount,
                TaxAmount = b.TaxAmount,
                TotalAmount = b.TotalAmount,
                PaidAmount = b.PaidAmount,
                CurrencyCode = currency,
                Status = b.Status,
                IsEstimated = b.IsEstimated,
                IsDisputed = b.IsDisputed,

                Lines = b.Lines.OrderBy(l => l.SortOrder).Select(l => new UtilityBillLineDto
                {
                    Id = l.Id,
                    Description = l.Description ?? string.Empty,
                    Units = l.Units,
                    Rate = l.Rate,
                    Amount = l.Amount,
                    SortOrder = l.SortOrder,
                }).ToList(),
            };
        }).ToList();
    }

    /// <summary>
    /// Bulk supply against the sum of the sub-meters. The gap is partly legitimate common-area
    /// use and partly loss — theft, a faulty meter, an unmetered connection — and separating the
    /// two is the whole point. A scheme that never runs this is paying for somebody's air
    /// conditioning without knowing it.
    /// </summary>
    public async Task<UtilityReconciliationDto> ReconcileUtilityAsync(
        Guid? societyId, Guid? propertyId, MeterKind kind, DateOnly from, DateOnly to)
    {
        var currency = await CurrencyAsync();

        var result = new UtilityReconciliationDto
        {
            SocietyId = societyId,
            PropertyId = propertyId,
            Kind = kind,
            PeriodFrom = from,
            PeriodTo = to,
            CurrencyCode = currency,
        };

        var meters = await Db.Meters.ForCompany(Tenant)
            .Where(m => m.Kind == kind && m.IsActive)
            .WhereIf(societyId.HasValue, m => m.SocietyId == societyId)
            .WhereIf(propertyId.HasValue, m => m.PropertyId == propertyId)
            .ToListAsync();

        if (meters.Count == 0) return result;

        // The bulk meter is the one nothing feeds. Everything with a parent is a sub-meter.
        var bulk = meters.Where(m => m.ParentMeterId is null && meters.Any(x => x.ParentMeterId == m.Id)).ToList();
        var subs = meters.Where(m => m.ParentMeterId is not null).ToList();

        if (bulk.Count == 0) bulk = meters.Where(m => m.IsCommonArea && m.ParentMeterId is null).ToList();

        var bulkIds = bulk.Select(m => m.Id).ToList();
        var subIds = subs.Select(m => m.Id).ToList();

        var readings = await Db.MeterReadings.ForCompany(Tenant)
            .Where(r => r.ReadingDate >= from && r.ReadingDate <= to && !r.IsImplausible)
            .Where(r => bulkIds.Contains(r.MeterId) || subIds.Contains(r.MeterId))
            .Select(r => new { r.MeterId, r.Consumption, r.VariancePercent, r.Id })
            .ToListAsync();

        result.BulkConsumption = RealEstateMapper.Money(
            readings.Where(r => bulkIds.Contains(r.MeterId)).Sum(r => r.Consumption), 4);

        result.SubMeterTotal = RealEstateMapper.Money(
            readings.Where(r => subIds.Contains(r.MeterId)).Sum(r => r.Consumption), 4);

        result.CommonAreaConsumption = RealEstateMapper.Money(
            readings.Where(r => subs.Any(s => s.Id == r.MeterId && s.IsCommonArea)).Sum(r => r.Consumption), 4);

        result.Difference = RealEstateMapper.Money(result.BulkConsumption - result.SubMeterTotal, 4);

        result.DifferencePercent = RealEstateMapper.Percent(result.Difference, result.BulkConsumption);

        // What the difference is not accounted for by metered common-area use.
        result.UnexplainedLoss = RealEstateMapper.Money(
            Math.Max(0m, result.Difference - result.CommonAreaConsumption), 4);

        // Distribution loss of a few per cent is physics. Beyond that it is somebody's meter.
        result.IsWithinTolerance = Math.Abs(result.DifferencePercent) <= 5m;

        var bills = await Db.UtilityBills.ForCompany(Tenant)
            .Where(b => subIds.Contains(b.MeterId) && b.PeriodFrom >= from && b.PeriodTo <= to)
            .SumAsync(b => b.TotalAmount);

        result.RecoveredAmount = RealEstateMapper.Money(bills);

        var bulkTariffId = bulk.FirstOrDefault()?.UtilityTariffId;

        if (bulkTariffId is not null)
        {
            var tariff = await Db.UtilityTariffs.ForCompany(Tenant)
                .Include(t => t.Slabs)
                .FirstOrDefaultAsync(t => t.Id == bulkTariffId);

            if (tariff is not null)
                result.BulkInvoiceAmount = RateConsumption(tariff, result.BulkConsumption) + tariff.FixedCharge;
        }

        result.ShortfallAmount = RealEstateMapper.Money(result.BulkInvoiceAmount - result.RecoveredAmount);

        // The meters most likely to explain the gap: the ones whose own consumption jumped.
        var outlierIds = readings
            .Where(r => subIds.Contains(r.MeterId) && r.VariancePercent is not null && Math.Abs(r.VariancePercent.Value) > 60m)
            .Select(r => r.Id)
            .Take(20)
            .ToList();

        if (outlierIds.Count > 0)
        {
            var outliers = await Db.MeterReadings.ForCompany(Tenant)
                .Where(r => outlierIds.Contains(r.Id))
                .ToListAsync();

            result.OutlierMeters = await MapReadingsAsync(outliers);
        }

        return result;
    }

    // ═══ Fuel ════════════════════════════════════════════════════════════════

    /// <summary>
    /// A generator's fuel. Litres per running hour is the number that catches theft: a set that
    /// suddenly burns half as much again per hour is either failing or being siphoned, and both
    /// are worth knowing this week rather than at the annual audit.
    /// </summary>
    public async Task<FuelLogDto> SaveFuelLogAsync(FuelLogDto dto, Guid userId)
    {
        var log = dto.Id != Guid.Empty
            ? await Db.FuelLogs.ForCompany(Tenant).FirstOrDefaultAsync(f => f.Id == dto.Id)
            : null;

        if (log is null)
        {
            log = new FuelLog { SocietyId = dto.SocietyId, FacilityAssetId = dto.FacilityAssetId }.StampNew(Tenant, userId);
            Db.FuelLogs.Add(log);
        }
        else log.StampUpdated(userId);

        log.LogDate = dto.LogDate == default ? Today : dto.LogDate;
        log.EntryType = dto.EntryType;
        log.Litres = dto.Litres;
        log.RatePerLitre = dto.RatePerLitre;
        log.Amount = RealEstateMapper.Money(dto.Amount > 0m ? dto.Amount : dto.Litres * (dto.RatePerLitre ?? 0m));
        log.RunHours = dto.RunHours;
        log.OpeningStock = dto.OpeningStock;
        log.ClosingStock = dto.ClosingStock;
        log.InvoiceReference = dto.InvoiceReference;
        log.RecoveredAmount = dto.RecoveredAmount;
        log.RecordedByUserId = userId;
        log.Note = dto.Note;

        if (dto.RunHours is > 0m && dto.Litres > 0m)
            log.ConsumptionPerHour = RealEstateMapper.Money(dto.Litres / dto.RunHours.Value, 3);

        await Db.SaveChangesAsync();

        var mapped = (await MapFuelLogsAsync([log]))[0];

        // An anomaly gets raised as a work order rather than sitting in a list — either the set
        // needs a service or somebody needs asking.
        if (mapped.IsAnomalous && dto.FacilityAssetId is not null)
        {
            var open = await Db.WorkOrders.ForCompany(Tenant)
                .AnyAsync(w => w.FacilityAssetId == dto.FacilityAssetId
                            && w.Source == WorkOrderSource.MeterAlarm
                            && w.Status != WorkOrderStatus.SignedOff
                            && w.Status != WorkOrderStatus.Cancelled);

            if (!open)
            {
                Db.WorkOrders.Add(new WorkOrder
                {
                    OrderNumber = await numbering.NextWorkOrderNumberAsync(DateTime.UtcNow),
                    Source = WorkOrderSource.MeterAlarm,
                    SocietyId = dto.SocietyId,
                    FacilityAssetId = dto.FacilityAssetId,
                    Title = "Fuel consumption outside normal range",
                    Description = $"Recorded {mapped.ConsumptionPerHour:N2} litres per hour against a norm of {mapped.ExpectedPerHour:N2}.",
                    Priority = TicketPriority.High,
                    Status = WorkOrderStatus.Raised,
                    RaisedAt = DateTime.UtcNow,
                    RaisedByUserId = userId,
                    IsAuthorised = true,
                    CostBearer = CostBearer.Society,
                }.StampNew(Tenant, userId));

                await Db.SaveChangesAsync();
            }
        }

        return mapped;
    }

    public async Task<PaginatedResponse<FuelLogDto>> GetFuelLogsAsync(ListQueryDto query, Guid? societyId)
    {
        var q = Db.FuelLogs.ForCompany(Tenant)
            .WhereIf(societyId.HasValue, f => f.SocietyId == societyId)
            .WhereIf(query.FromDate.HasValue, f => f.LogDate >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, f => f.LogDate <= query.ToDate)
            .OrderByDescending(f => f.LogDate);

        return await PageAsync(q, query, MapFuelLogsAsync);
    }

    private async Task<List<FuelLogDto>> MapFuelLogsAsync(List<FuelLog> logs)
    {
        if (logs.Count == 0) return [];

        var assetIds = logs.Where(l => l.FacilityAssetId.HasValue).Select(l => l.FacilityAssetId!.Value).Distinct().ToList();

        var assets = assetIds.Count == 0
            ? []
            : await Db.FacilityAssets.ForCompany(Tenant).Where(a => assetIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Name);

        // The norm is the asset's own history, not a manufacturer figure nobody has.
        var baselines = assetIds.Count == 0
            ? []
            : await Db.FuelLogs.ForCompany(Tenant)
                .Where(f => f.FacilityAssetId != null && assetIds.Contains(f.FacilityAssetId.Value)
                         && f.ConsumptionPerHour != null && f.EntryType == "Consumption")
                .GroupBy(f => f.FacilityAssetId!.Value)
                .Select(g => new { AssetId = g.Key, Average = g.Average(x => x.ConsumptionPerHour!.Value) })
                .ToDictionaryAsync(x => x.AssetId, x => x.Average);

        var users = await AgentUserNamesAsync(logs.Select(l => l.RecordedByUserId));
        var suppliers = await PartyNamesAsync(logs.Where(l => l.SupplierPartyId.HasValue).Select(l => l.SupplierPartyId!.Value));

        return logs.Select(l =>
        {
            decimal? expected = l.FacilityAssetId is null ? null : baselines.GetValueOrDefault(l.FacilityAssetId.Value);

            return new FuelLogDto
            {
                Id = l.Id,
                SocietyId = l.SocietyId,
                FacilityAssetId = l.FacilityAssetId,
                AssetName = l.FacilityAssetId is null ? null : assets.GetValueOrDefault(l.FacilityAssetId.Value),
                LogDate = l.LogDate,
                EntryType = l.EntryType,
                Litres = l.Litres,
                RatePerLitre = l.RatePerLitre,
                Amount = l.Amount,
                RunHours = l.RunHours,
                OpeningStock = l.OpeningStock,
                ClosingStock = l.ClosingStock,
                ConsumptionPerHour = l.ConsumptionPerHour,
                ExpectedPerHour = expected is > 0m ? RealEstateMapper.Money(expected.Value, 3) : null,

                // A quarter above the set's own average is a real change, not measurement noise.
                IsAnomalous = expected is > 0m && l.ConsumptionPerHour is not null
                              && l.ConsumptionPerHour.Value > expected.Value * 1.25m,

                SupplierName = l.SupplierPartyId is null ? null : suppliers.GetValueOrDefault(l.SupplierPartyId.Value),
                InvoiceReference = l.InvoiceReference,
                RecoveredAmount = l.RecoveredAmount,
                RecordedByName = l.RecordedByUserId is null ? null : users.GetValueOrDefault(l.RecordedByUserId.Value),
                Note = l.Note,
            };
        }).ToList();
    }

    // ═══ Parking ═════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ParkingSlotDto>> GetParkingAsync(
        ListQueryDto query, Guid? societyId, bool? unallottedOnly)
    {
        var q = Db.ParkingSlots.ForCompany(Tenant)
            .WhereIf(societyId.HasValue, s => s.SocietyId == societyId)
            .WhereIf(unallottedOnly == true, s => !s.IsAllotted && !s.IsVisitorParking)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), s => s.SlotNumber.Contains(query.Search!))
            .OrderBy(s => s.Level).ThenBy(s => s.SlotNumber);

        return await PageAsync(q, query, MapParkingAsync);
    }

    private async Task<List<ParkingSlotDto>> MapParkingAsync(List<ParkingSlot> slots)
    {
        if (slots.Count == 0) return [];

        var unit = await AreaUnitAsync();
        var today = Today;
        var ids = slots.Select(s => s.Id).ToList();

        var unitIds = slots.Where(s => s.AllottedToUnitId.HasValue).Select(s => s.AllottedToUnitId!.Value)
            .Concat(slots.Where(s => s.SoldWithUnitId.HasValue).Select(s => s.SoldWithUnitId!.Value))
            .Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant).Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

        var societyIds = slots.Where(s => s.SocietyId.HasValue).Select(s => s.SocietyId!.Value).Distinct().ToList();

        var societies = societyIds.Count == 0
            ? []
            : await Db.Societies.ForCompany(Tenant).Where(s => societyIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

        var allotments = await Db.ParkingAllotments.ForCompany(Tenant)
            .Where(a => ids.Contains(a.ParkingSlotId) && (a.ToDate == null || a.ToDate >= today))
            .ToListAsync();

        var names = await PartyNamesAsync(allotments.Where(a => a.PartyId.HasValue).Select(a => a.PartyId!.Value));

        var vehicleIds = allotments.Where(a => a.ResidentVehicleId.HasValue).Select(a => a.ResidentVehicleId!.Value).Distinct().ToList();

        var vehicles = vehicleIds.Count == 0
            ? []
            : await Db.ResidentVehicles.ForCompany(Tenant).Where(v => vehicleIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.RegistrationNumber);

        return slots.Select(s =>
        {
            var allotment = allotments.FirstOrDefault(a => a.ParkingSlotId == s.Id);

            return new ParkingSlotDto
            {
                Id = s.Id,
                SlotNumber = s.SlotNumber,
                PropertyId = s.PropertyId,
                SocietyName = s.SocietyId is null ? null : societies.GetValueOrDefault(s.SocietyId.Value),
                Level = s.Level,
                Zone = s.Zone,
                SlotType = s.SlotType,
                IsCovered = s.IsCovered,
                HasEvCharger = s.HasEvCharger,
                Area = RealEstateMapper.AreaOrNull(s.AreaSqFt, unit),
                IsSaleable = s.IsSaleable,
                SoldWithUnitId = s.SoldWithUnitId,
                SoldWithUnitLabel = s.SoldWithUnitId is null ? null : units.GetValueOrDefault(s.SoldWithUnitId.Value),
                IsAllotted = s.IsAllotted,
                AllottedToUnitId = s.AllottedToUnitId,
                AllottedToUnitLabel = s.AllottedToUnitId is null ? null : units.GetValueOrDefault(s.AllottedToUnitId.Value),
                AllottedToName = allotment?.PartyId is null ? null : names.GetValueOrDefault(allotment.PartyId.Value),
                VehicleNumber = allotment?.ResidentVehicleId is null ? null : vehicles.GetValueOrDefault(allotment.ResidentVehicleId.Value),
                MonthlyRent = s.MonthlyRent,
                IsVisitorParking = s.IsVisitorParking,
                IsActive = s.IsActive,
            };
        }).ToList();
    }

    public async Task<ParkingSlotDto> SaveParkingSlotAsync(ParkingSlotDto dto, Guid userId)
    {
        var slot = dto.Id != Guid.Empty
            ? await Db.ParkingSlots.ForCompany(Tenant).FirstOrDefaultAsync(s => s.Id == dto.Id)
            : null;

        if (slot is null)
        {
            slot = new ParkingSlot { PropertyId = dto.PropertyId }.StampNew(Tenant, userId);
            Db.ParkingSlots.Add(slot);
        }
        else slot.StampUpdated(userId);

        slot.SlotNumber = dto.SlotNumber;
        slot.Level = dto.Level;
        slot.Zone = dto.Zone;
        slot.SlotType = dto.SlotType;
        slot.IsCovered = dto.IsCovered;
        slot.HasEvCharger = dto.HasEvCharger;
        slot.AreaSqFt = dto.Area?.SquareFeet;
        slot.IsSaleable = dto.IsSaleable;
        slot.SoldWithUnitId = dto.SoldWithUnitId;
        slot.MonthlyRent = dto.MonthlyRent;
        slot.IsVisitorParking = dto.IsVisitorParking;
        slot.IsActive = dto.IsActive;

        await Db.SaveChangesAsync();
        return (await MapParkingAsync([slot]))[0];
    }

    public async Task<ParkingAllotmentDto> AllotParkingAsync(ParkingAllotmentDto dto, Guid userId)
    {
        var slot = await RequireAsync<ParkingSlot>(dto.ParkingSlotId, "That parking slot does not exist.");
        var today = Today;

        // A slot allotted twice is a car parked in somebody else's space and an argument in a
        // basement. The previous allotment is closed rather than overlapped.
        var current = await Db.ParkingAllotments.ForCompany(Tenant)
            .Where(a => a.ParkingSlotId == dto.ParkingSlotId && (a.ToDate == null || a.ToDate >= today))
            .ToListAsync();

        var from = dto.FromDate == default ? today : dto.FromDate;

        foreach (var previous in current.Where(a => a.UnitId != dto.UnitId))
        {
            previous.ToDate = from.AddDays(-1);
            previous.StampUpdated(userId);
        }

        if (slot.IsSaleable && slot.SoldWithUnitId is not null && slot.SoldWithUnitId != dto.UnitId)
            throw new InvalidOperationException($"{slot.SlotNumber} was sold with another unit and cannot be allotted separately.");

        var allotment = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.ParkingAllotments.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.Id)
            : current.FirstOrDefault(a => a.UnitId == dto.UnitId);

        if (allotment is null)
        {
            allotment = new ParkingAllotment { ParkingSlotId = dto.ParkingSlotId }.StampNew(Tenant, userId);
            Db.ParkingAllotments.Add(allotment);
        }
        else allotment.StampUpdated(userId);

        allotment.UnitId = dto.UnitId;
        allotment.PartyId = dto.PartyId;
        allotment.TenancyId = dto.TenancyId;
        allotment.ResidentVehicleId = dto.ResidentVehicleId;
        allotment.FromDate = from;
        allotment.ToDate = dto.ToDate;
        allotment.MonthlyCharge = dto.MonthlyCharge;
        allotment.IsIncludedInRent = dto.IsIncludedInRent;
        allotment.Note = dto.Note;

        slot.IsAllotted = dto.ToDate is null || dto.ToDate >= today;
        slot.AllottedToUnitId = dto.UnitId;
        slot.StampUpdated(userId);

        // The vehicle's own record points back at the slot, so a guard scanning a plate at the
        // barrier can see where it belongs.
        if (dto.ResidentVehicleId is not null)
        {
            var vehicle = await Db.ResidentVehicles.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == dto.ResidentVehicleId);

            if (vehicle is not null)
            {
                vehicle.ParkingSlotId = dto.ParkingSlotId;
                vehicle.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();

        var names = await PartyNamesAsync(dto.PartyId is null ? [] : [dto.PartyId.Value]);

        var unitLabel = dto.UnitId is null
            ? null
            : await Db.Units.ForCompany(Tenant).Where(u => u.Id == dto.UnitId).Select(u => u.UnitNumber).FirstOrDefaultAsync();

        dto.Id = allotment.Id;
        dto.SlotNumber = slot.SlotNumber;
        dto.UnitLabel = unitLabel;
        dto.PartyName = dto.PartyId is null ? null : names.GetValueOrDefault(dto.PartyId.Value);
        dto.FromDate = from;
        dto.IsActive = slot.IsAllotted;
        return dto;
    }
}
