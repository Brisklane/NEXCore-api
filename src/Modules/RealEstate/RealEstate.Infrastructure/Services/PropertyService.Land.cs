using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Valuation evidence, the price opinion, and the land bank.
///
/// The price opinion is the part that earns its keep: it never asserts a number, it shows the
/// comparables it used, the adjustment applied to each and why, and states its own confidence.
/// A valuer can disagree with it in the meeting rather than after the sale.
/// </summary>
public partial class PropertyService
{
    // ═══ Valuation ═══════════════════════════════════════════════════════════

    public async Task<List<PropertyValuationDto>> GetValuationsAsync(Guid propertyId)
    {
        var valuations = await Db.PropertyValuations.ForCompany(Tenant)
            .Where(v => v.PropertyId == propertyId)
            .Include(v => v.Comparables)
            .OrderByDescending(v => v.ValuedOn)
            .ToListAsync();

        var currency = await CurrencyAsync();
        var today = Today;

        return valuations.Select(v => new PropertyValuationDto
        {
            Id = v.Id,
            PropertyId = v.PropertyId,
            Purpose = v.Purpose,
            Method = v.Method,
            Value = v.Value,
            ValueLow = v.ValueLow,
            ValueHigh = v.ValueHigh,
            CurrencyCode = v.CurrencyCode ?? currency,
            ValuedOn = v.ValuedOn,
            ValuerName = v.ValuerName,
            ReportUrl = v.ReportUrl,
            Rationale = v.Rationale,
            AnnualRent = v.AnnualRent,
            CapRatePercent = v.CapRatePercent,
            GrossYieldPercent = v.GrossYieldPercent,
            NetYieldPercent = v.NetYieldPercent,
            IsCurrent = v.IsCurrent,
            Comparables = v.Comparables.OrderByDescending(c => c.Weight).Select(c => MapComparable(c, today)).ToList(),
        }).ToList();
    }

    private static PropertyComparableDto MapComparable(PropertyComparable c, DateOnly today) => new()
    {
        Id = c.Id,
        ComparablePropertyId = c.ComparablePropertyId,
        Address = c.Address,
        SubType = c.SubType,
        AreaSqFt = c.AreaSqFt,
        Bedrooms = c.Bedrooms,
        TransactionPrice = c.TransactionPrice,
        TransactionDate = c.TransactionDate,
        EvidenceType = c.EvidenceType,
        Source = c.Source,
        AdjustmentPercent = c.AdjustmentPercent,
        AdjustmentNote = c.AdjustmentNote,
        AdjustedPricePerSqFt = c.AdjustedPricePerSqFt,
        Weight = c.Weight,
        MonthsAgo = MonthsBetween(c.TransactionDate, today),
    };

    private static int MonthsBetween(DateOnly from, DateOnly to)
        => Math.Max(0, ((to.Year - from.Year) * 12) + to.Month - from.Month);

    public async Task<PropertyValuationDto> SaveValuationAsync(PropertyValuationDto dto, Guid userId)
    {
        var property = await RequireAsync<Property>(dto.PropertyId, "That property does not exist.");

        var valuation = dto.Id != Guid.Empty
            ? await Db.PropertyValuations.ForCompany(Tenant)
                .Include(v => v.Comparables)
                .FirstOrDefaultAsync(v => v.Id == dto.Id)
            : null;

        if (valuation is null)
        {
            valuation = new PropertyValuation { PropertyId = dto.PropertyId }.StampNew(Tenant, userId);
            Db.PropertyValuations.Add(valuation);
        }
        else valuation.StampUpdated(userId);

        valuation.Purpose = dto.Purpose;
        valuation.Method = dto.Method;
        valuation.Value = dto.Value;
        valuation.ValueLow = dto.ValueLow;
        valuation.ValueHigh = dto.ValueHigh;
        valuation.CurrencyCode = dto.CurrencyCode;
        valuation.ValuedOn = dto.ValuedOn == default ? Today : dto.ValuedOn;
        valuation.ValuerName = dto.ValuerName;
        valuation.ReportUrl = dto.ReportUrl;
        valuation.Rationale = dto.Rationale;
        valuation.AnnualRent = dto.AnnualRent;
        valuation.CapRatePercent = dto.CapRatePercent;

        // Yields are derived, never typed. Two people typing them is two people disagreeing.
        if (dto.AnnualRent is > 0m && dto.Value > 0m)
        {
            valuation.GrossYieldPercent = RealEstateMapper.Percent(dto.AnnualRent.Value, dto.Value);

            var net = dto.AnnualRent.Value
                - (property.AnnualPropertyTax ?? 0m)
                - ((property.ServiceChargeRatePerSqFt ?? 0m) * (property.SaleableAreaSqFt ?? 0m));

            valuation.NetYieldPercent = RealEstateMapper.Percent(Math.Max(0m, net), dto.Value);
        }

        if (dto.Comparables.Count > 0)
        {
            Db.PropertyComparables.RemoveRange(valuation.Comparables);

            foreach (var c in dto.Comparables)
            {
                var adjusted = c.AreaSqFt is > 0m
                    ? RealEstateMapper.Money(c.TransactionPrice * (1m + (c.AdjustmentPercent / 100m)) / c.AreaSqFt.Value)
                    : 0m;

                valuation.Comparables.Add(new PropertyComparable
                {
                    ComparablePropertyId = c.ComparablePropertyId,
                    Address = c.Address,
                    SubType = c.SubType,
                    AreaSqFt = c.AreaSqFt,
                    Bedrooms = c.Bedrooms,
                    TransactionPrice = c.TransactionPrice,
                    TransactionDate = c.TransactionDate,
                    EvidenceType = c.EvidenceType,
                    Source = c.Source,
                    AdjustmentPercent = c.AdjustmentPercent,
                    AdjustmentNote = c.AdjustmentNote,
                    AdjustedPricePerSqFt = adjusted,
                    Weight = c.Weight,
                }.StampNew(Tenant, userId));
            }
        }

        if (dto.IsCurrent)
        {
            var others = await Db.PropertyValuations.ForCompany(Tenant)
                .Where(v => v.PropertyId == dto.PropertyId && v.Id != valuation.Id && v.IsCurrent)
                .ToListAsync();

            foreach (var other in others) { other.IsCurrent = false; other.StampUpdated(userId); }

            valuation.IsCurrent = true;
            property.CurrentValuation = dto.Value;
            property.ValuedOn = valuation.ValuedOn;
            property.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        return (await GetValuationsAsync(dto.PropertyId)).First(v => v.Id == valuation.Id);
    }

    /// <summary>
    /// A price opinion built from our own transaction evidence. Each comparable is adjusted for
    /// area, floor, condition, age and how stale the evidence is, and every adjustment is written
    /// out in words. Confidence falls with thin or old evidence rather than being asserted.
    /// </summary>
    public async Task<PriceOpinionDto> GetPriceOpinionAsync(Guid propertyId, ListingKind kind)
    {
        var property = await RequireAsync<Property>(propertyId, "That property does not exist.");
        var currency = property.CurrencyCode ?? await CurrencyAsync();

        var opinion = new PriceOpinionDto
        {
            PropertyId = propertyId,
            CurrencyCode = currency,
            Basis = kind == ListingKind.ForRent ? "Comparable lettings" : "Comparable sales",
        };

        var subject = property.SaleableAreaSqFt ?? property.CoveredAreaSqFt ?? property.PlotAreaSqFt ?? 0m;

        if (subject <= 0m)
        {
            opinion.Confidence = "None";
            opinion.Workings.Add("No area is recorded on this property, so a rate cannot be applied. Enter the saleable area first.");
            return opinion;
        }

        var comparables = await FindComparablesAsync(propertyId, 12);

        opinion.Comparables = comparables;
        opinion.ComparableCount = comparables.Count;

        if (comparables.Count == 0)
        {
            opinion.Confidence = "None";
            opinion.Workings.Add("No transaction in the last 24 months matched this type, area band and locality.");
            return opinion;
        }

        // Weighted average of adjusted rates. Weight already carries recency and similarity.
        var totalWeight = comparables.Sum(c => c.Weight);
        if (totalWeight <= 0m) totalWeight = comparables.Count;

        var rate = comparables.Sum(c => c.AdjustedPricePerSqFt * c.Weight) / totalWeight;

        opinion.SuggestedRatePerSqFt = RealEstateMapper.Money(rate);
        opinion.SuggestedPrice = RealEstateMapper.Money(rate * subject);

        // The band widens as evidence thins, because that is honestly what we know.
        var spread = comparables.Count >= 8 ? 0.05m : comparables.Count >= 4 ? 0.08m : 0.14m;

        opinion.LowPrice = RealEstateMapper.Money(opinion.SuggestedPrice * (1m - spread));
        opinion.HighPrice = RealEstateMapper.Money(opinion.SuggestedPrice * (1m + spread));

        var medianAge = comparables.OrderBy(c => c.MonthsAgo).ElementAt(comparables.Count / 2).MonthsAgo;

        opinion.Confidence = comparables.Count >= 8 && medianAge <= 6 ? "High"
            : comparables.Count >= 4 && medianAge <= 12 ? "Medium"
            : "Low";

        var unit = await AreaUnitAsync();
        var displayArea = RealEstateMapper.Area(subject, unit);

        opinion.Workings.Add($"{comparables.Count} comparable transactions found, median age {medianAge} months.");
        opinion.Workings.Add($"Weighted adjusted rate {opinion.SuggestedRatePerSqFt:N0} per sq ft.");
        opinion.Workings.Add($"Subject area {displayArea.DisplayValue:N2} {RealEstateMapper.AreaUnitLabel(displayArea.DisplayUnit)} ({subject:N0} sq ft).");
        opinion.Workings.Add($"Indicative range ±{spread * 100m:N0}% reflecting {comparables.Count} pieces of evidence.");

        foreach (var c in comparables.Take(5).Where(c => !string.IsNullOrWhiteSpace(c.AdjustmentNote)))
            opinion.Workings.Add($"{c.Address}: {c.AdjustmentNote}");

        opinion.Workings.Add("This is a price opinion generated from our own records. It is not a certified valuation.");

        return opinion;
    }

    /// <summary>
    /// Finds evidence for the subject property. Everything is scored, nothing is eliminated for
    /// being a slightly different size — an agent would rather see a weak comparable labelled
    /// weak than not see it at all.
    /// </summary>
    public async Task<List<PropertyComparableDto>> FindComparablesAsync(Guid propertyId, int take)
    {
        var property = await RequireAsync<Property>(propertyId, "That property does not exist.");
        var today = Today;
        var since = today.AddMonths(-24);

        var subjectArea = property.SaleableAreaSqFt ?? property.CoveredAreaSqFt ?? property.PlotAreaSqFt ?? 0m;
        if (subjectArea <= 0m) return [];

        // Same sub-type, same locality where we know it, sold in the last two years.
        var candidates = await Db.Properties.ForCompany(Tenant)
            .Where(p => p.Id != propertyId
                     && p.SubType == property.SubType
                     && p.LastSoldPrice != null && p.LastSoldPrice > 0m
                     && p.LastSoldOn != null && p.LastSoldOn >= since)
            .WhereIf(property.GeoAreaId.HasValue, p => p.GeoAreaId == property.GeoAreaId)
            .Select(p => new
            {
                p.Id,
                p.AddressLine1,
                p.Street,
                p.UnitNumber,
                p.SubType,
                p.SaleableAreaSqFt,
                p.CoveredAreaSqFt,
                p.PlotAreaSqFt,
                p.Bedrooms,
                p.FloorNumber,
                p.Condition,
                p.YearBuilt,
                p.IsCorner,
                p.LastSoldPrice,
                p.LastSoldOn,
                p.ProjectId,
            })
            .Take(200)
            .ToListAsync();

        var results = new List<PropertyComparableDto>();

        foreach (var c in candidates)
        {
            var area = c.SaleableAreaSqFt ?? c.CoveredAreaSqFt ?? c.PlotAreaSqFt ?? 0m;
            if (area <= 0m) continue;

            var sizeRatio = area / subjectArea;

            // More than double or less than half is a different product, not a comparable.
            if (sizeRatio is < 0.5m or > 2m) continue;

            var months = MonthsBetween(c.LastSoldOn!.Value, today);
            var notes = new List<string>();
            var adjustment = 0m;

            // Size. Smaller units carry a higher rate per foot, so the subject is adjusted toward it.
            if (sizeRatio < 0.85m) { adjustment -= 4m; notes.Add("smaller, −4% for size premium"); }
            else if (sizeRatio > 1.15m) { adjustment += 4m; notes.Add("larger, +4% for size discount"); }

            // Floor. Roughly half a percent a level, capped, which is what the market actually pays.
            if (property.FloorNumber is not null && c.FloorNumber is not null)
            {
                var floors = property.FloorNumber.Value - c.FloorNumber.Value;
                var floorAdjustment = Math.Clamp(floors * 0.5m, -8m, 8m);

                if (floorAdjustment != 0m)
                {
                    adjustment += floorAdjustment;
                    notes.Add($"{Math.Abs(floors)} floors {(floors > 0 ? "below" : "above")} subject, {floorAdjustment:+0.0;-0.0}%");
                }
            }

            // Bedrooms, where the count differs despite similar area.
            if (property.Bedrooms is not null && c.Bedrooms is not null && property.Bedrooms != c.Bedrooms)
            {
                var beds = (property.Bedrooms.Value - c.Bedrooms.Value) * 3m;
                adjustment += beds;
                notes.Add($"{Math.Abs(property.Bedrooms.Value - c.Bedrooms.Value)} bedroom difference, {beds:+0;-0}%");
            }

            // Condition.
            if (property.Condition is not null && c.Condition is not null && property.Condition != c.Condition)
            {
                var condition = ((int)property.Condition.Value - (int)c.Condition.Value) * 2.5m;
                adjustment += condition;
                notes.Add($"condition {c.Condition} vs {property.Condition}, {condition:+0.0;-0.0}%");
            }

            // Age of the building.
            if (property.YearBuilt is not null && c.YearBuilt is not null)
            {
                var years = property.YearBuilt.Value - c.YearBuilt.Value;
                var age = Math.Clamp(years * 0.4m, -10m, 10m);

                if (Math.Abs(age) >= 1m)
                {
                    adjustment += age;
                    notes.Add($"{Math.Abs(years)} years {(years > 0 ? "newer" : "older")} than comparable, {age:+0.0;-0.0}%");
                }
            }

            if (property.IsCorner != c.IsCorner)
            {
                var corner = property.IsCorner ? 5m : -5m;
                adjustment += corner;
                notes.Add($"corner plot difference, {corner:+0;-0}%");
            }

            adjustment = Math.Clamp(adjustment, -30m, 30m);

            var adjustedRate = RealEstateMapper.Money(c.LastSoldPrice!.Value * (1m + (adjustment / 100m)) / area);

            // Weight: recency first, then how close the size is, plus a bump for the same project.
            var recency = Math.Max(0.2m, 1m - (months / 30m));
            var similarity = 1m - Math.Min(0.5m, Math.Abs(1m - sizeRatio));
            var sameProject = property.ProjectId is not null && c.ProjectId == property.ProjectId ? 1.3m : 1m;

            var weight = RealEstateMapper.Money(recency * similarity * sameProject * 100m);

            results.Add(new PropertyComparableDto
            {
                ComparablePropertyId = c.Id,
                Address = string.Join(", ", new[] { c.UnitNumber, c.AddressLine1, c.Street }.Where(s => !string.IsNullOrWhiteSpace(s))),
                SubType = c.SubType,
                AreaSqFt = area,
                Bedrooms = c.Bedrooms,
                TransactionPrice = c.LastSoldPrice.Value,
                TransactionDate = c.LastSoldOn.Value,
                EvidenceType = "Sold",
                Source = "Own records",
                AdjustmentPercent = RealEstateMapper.Money(adjustment),
                AdjustmentNote = notes.Count == 0 ? "like for like, no adjustment" : string.Join("; ", notes),
                AdjustedPricePerSqFt = adjustedRate,
                Weight = weight,
                MonthsAgo = months,
            });
        }

        return results.OrderByDescending(r => r.Weight).Take(take).ToList();
    }

    // ═══ Land bank ═══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<LandParcelListItemDto>> GetParcelsAsync(ListQueryDto query)
    {
        var q = Db.LandParcels.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                p => p.Reference.Contains(query.Search!)
                     || (p.Name != null && p.Name.Contains(query.Search!))
                     || (p.SurveyNumber != null && p.SurveyNumber.Contains(query.Search!))
                     || (p.KhasraNumber != null && p.KhasraNumber.Contains(query.Search!))
                     || (p.Mouza != null && p.Mouza.Contains(query.Search!)))
            .WhereIf(query.ProjectId.HasValue, p => p.ProjectId == query.ProjectId)
            .OrderBy(p => p.Reference);

        return await PageAsync(q, query, MapParcelListAsync);
    }

    private async Task<List<LandParcelListItemDto>> MapParcelListAsync(List<LandParcel> parcels)
    {
        if (parcels.Count == 0) return [];

        var unit = await AreaUnitAsync();
        var ids = parcels.Select(p => p.Id).ToList();
        var projects = await ProjectNamesAsync(parcels.Select(p => p.ProjectId));

        var openChecks = await Db.TitleVerificationItems.ForCompany(Tenant)
            .Where(v => ids.Contains(v.LandParcelId) && v.Verdict == VerificationVerdict.Pending)
            .GroupBy(v => v.LandParcelId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var agreed = await Db.LandAcquisitions.ForCompany(Tenant)
            .Where(a => ids.Contains(a.LandParcelId) && !a.IsAborted)
            .Select(a => new { a.LandParcelId, a.AgreedPrice })
            .ToListAsync();

        return parcels.Select(p => new LandParcelListItemDto
        {
            Id = p.Id,
            Reference = p.Reference,
            Name = p.Name,
            SurveyNumber = p.SurveyNumber,
            Mouza = p.Mouza,
            District = p.District,
            RecordArea = RealEstateMapper.Area(p.RecordAreaSqFt, unit),
            SurveyedArea = RealEstateMapper.AreaOrNull(p.SurveyedAreaSqFt, unit),
            AreaVarianceSqFt = p.AreaVarianceSqFt,
            Stage = p.Stage,
            IsAcquired = p.IsAcquired,
            ProjectName = p.ProjectId is null ? null : projects.GetValueOrDefault(p.ProjectId.Value),
            AgreedPrice = agreed.FirstOrDefault(a => a.LandParcelId == p.Id)?.AgreedPrice ?? p.AgreedPrice,
            TotalAcquisitionCost = p.TotalAcquisitionCost,
            MonthlyHoldingCost = p.MonthlyHoldingCost,
            HasEncumbrance = p.HasEncumbrance,
            HasLitigation = p.HasLitigation,
            OpenVerificationCount = openChecks.GetValueOrDefault(p.Id),
        }).ToList();
    }

    public async Task<LandParcelDetailDto?> GetParcelAsync(Guid id)
    {
        var parcel = await Db.LandParcels.ForCompany(Tenant)
            .Include(p => p.TitleChain)
            .Include(p => p.Encumbrances)
            .Include(p => p.VerificationItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (parcel is null) return null;

        var list = await MapParcelListAsync([parcel]);
        var head = list[0];
        var today = Today;

        var detail = new LandParcelDetailDto
        {
            Id = head.Id,
            Reference = head.Reference,
            Name = head.Name,
            SurveyNumber = head.SurveyNumber,
            Mouza = head.Mouza,
            District = head.District,
            RecordArea = head.RecordArea,
            SurveyedArea = head.SurveyedArea,
            AreaVarianceSqFt = head.AreaVarianceSqFt,
            Stage = head.Stage,
            IsAcquired = head.IsAcquired,
            ProjectName = head.ProjectName,
            AgreedPrice = head.AgreedPrice,
            TotalAcquisitionCost = head.TotalAcquisitionCost,
            MonthlyHoldingCost = head.MonthlyHoldingCost,
            HasEncumbrance = head.HasEncumbrance,
            HasLitigation = head.HasLitigation,
            OpenVerificationCount = head.OpenVerificationCount,

            KhasraNumber = parcel.KhasraNumber,
            KhewatNumber = parcel.KhewatNumber,
            KhatuniNumber = parcel.KhatuniNumber,
            Village = parcel.Village,
            Tehsil = parcel.Tehsil,
            RegistrarOffice = parcel.RegistrarOffice,
            Latitude = parcel.Latitude,
            Longitude = parcel.Longitude,
            BoundaryGeoJson = parcel.BoundaryGeoJson,
            CurrentLandUse = parcel.CurrentLandUse,
            IntendedLandUse = parcel.IntendedLandUse,
            MaxFloorAreaRatio = parcel.MaxFloorAreaRatio,
            MaxCoveragePercent = parcel.MaxCoveragePercent,
            MaxHeightFt = parcel.MaxHeightFt,

            TitleChain = parcel.TitleChain.OrderBy(t => t.SequenceNumber).Select(MapTitleEntry).ToList(),
            Encumbrances = parcel.Encumbrances.Select(MapEncumbrance).ToList(),
        };

        var assignedIds = parcel.VerificationItems
            .Where(v => v.AssignedToUserId.HasValue)
            .Select(v => v.AssignedToUserId!.Value)
            .Distinct()
            .ToList();

        var assignees = assignedIds.Count == 0
            ? []
            : await Db.AgentProfiles.ForCompany(Tenant)
                .Where(a => a.UserId != null && assignedIds.Contains(a.UserId.Value))
                .ToDictionaryAsync(a => a.UserId!.Value, a => a.DisplayName);

        detail.VerificationItems = parcel.VerificationItems
            .OrderBy(v => v.SortOrder)
            .Select(v => new TitleVerificationItemDto
            {
                Id = v.Id,
                CheckKey = v.CheckKey,
                Label = v.Label,
                Verdict = v.Verdict,
                AssignedToName = v.AssignedToUserId is null ? null : assignees.GetValueOrDefault(v.AssignedToUserId.Value),
                DueDate = v.DueDate,
                CompletedOn = v.CompletedOn,
                EvidenceUrl = v.EvidenceUrl,
                Findings = v.Findings,
                Condition = v.Condition,
                IsMandatory = v.IsMandatory,
                IsOverdue = v.Verdict == VerificationVerdict.Pending && v.DueDate != null && v.DueDate < today,
                SortOrder = v.SortOrder,
            })
            .ToList();

        var acquisition = await Db.LandAcquisitions.ForCompany(Tenant)
            .Include(a => a.Stages)
            .Include(a => a.CostLines)
            .Where(a => a.LandParcelId == id && !a.IsAborted)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (acquisition is not null)
        {
            detail.Acquisition = await MapAcquisitionAsync(acquisition, today);
            detail.CostLines = acquisition.CostLines.Select(MapCostLine).ToList();
        }

        return detail;
    }

    private static TitleChainEntryDto MapTitleEntry(TitleChainEntry t) => new()
    {
        Id = t.Id,
        SequenceNumber = t.SequenceNumber,
        Instrument = t.Instrument,
        InstrumentDate = t.InstrumentDate,
        InstrumentNumber = t.InstrumentNumber,
        RegistrarOffice = t.RegistrarOffice,
        TransferorName = t.TransferorName,
        TransfereeName = t.TransfereeName,
        Consideration = t.Consideration,
        DocumentUrl = t.DocumentUrl,
        Verdict = t.Verdict,
        VerificationNote = t.VerificationNote,
    };

    private static EncumbranceDto MapEncumbrance(Encumbrance e) => new()
    {
        Id = e.Id,
        Kind = e.Kind,
        Status = e.Status,
        HolderName = e.HolderName,
        Amount = e.Amount,
        CreatedOn = e.CreatedOn,
        ExpectedClearanceDate = e.ExpectedClearanceDate,
        ClearedOn = e.ClearedOn,
        ReferenceNumber = e.ReferenceNumber,
        DocumentUrl = e.DocumentUrl,
        BlocksTransaction = e.BlocksTransaction,
        Note = e.Note,
    };

    private static AcquisitionCostLineDto MapCostLine(AcquisitionCostLine c) => new()
    {
        Id = c.Id,
        CostType = c.CostType,
        Description = c.Description,
        BudgetAmount = c.BudgetAmount,
        ActualAmount = c.ActualAmount,
        Variance = c.ActualAmount - c.BudgetAmount,
        IncurredOn = c.IncurredOn,
        Reference = c.Reference,
        IsCapitalised = c.IsCapitalised,
    };

    public async Task<LandParcelDetailDto> SaveParcelAsync(LandParcelDetailDto dto, Guid userId)
    {
        var parcel = dto.Id != Guid.Empty
            ? await Db.LandParcels.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (parcel is null)
        {
            parcel = new LandParcel
            {
                Reference = string.IsNullOrWhiteSpace(dto.Reference)
                    ? await numbering.NextParcelReferenceAsync()
                    : dto.Reference,
            }.StampNew(Tenant, userId);

            Db.LandParcels.Add(parcel);

            // A new parcel starts with the standard due-diligence checklist rather than an empty
            // one, because the checks nobody remembers to add are the ones that sink a deal.
            var order = 0;

            foreach (var (key, label) in StandardTitleChecks)
            {
                parcel.VerificationItems.Add(new TitleVerificationItem
                {
                    CheckKey = key,
                    Label = label,
                    Verdict = VerificationVerdict.Pending,
                    IsMandatory = true,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }
        else parcel.StampUpdated(userId);

        parcel.Name = dto.Name;
        parcel.SurveyNumber = dto.SurveyNumber;
        parcel.KhasraNumber = dto.KhasraNumber;
        parcel.KhewatNumber = dto.KhewatNumber;
        parcel.KhatuniNumber = dto.KhatuniNumber;
        parcel.Mouza = dto.Mouza;
        parcel.Village = dto.Village;
        parcel.Tehsil = dto.Tehsil;
        parcel.District = dto.District;
        parcel.RegistrarOffice = dto.RegistrarOffice;

        parcel.RecordAreaSqFt = RealEstateMapper.ToSquareFeet(dto.RecordArea.DisplayValue, dto.RecordArea.DisplayUnit);

        parcel.SurveyedAreaSqFt = dto.SurveyedArea is null
            ? null
            : RealEstateMapper.ToSquareFeet(dto.SurveyedArea.DisplayValue, dto.SurveyedArea.DisplayUnit);

        // The variance is derived so the risk cannot be typed away.
        parcel.AreaVarianceSqFt = parcel.SurveyedAreaSqFt is null
            ? null
            : parcel.SurveyedAreaSqFt - parcel.RecordAreaSqFt;

        parcel.Latitude = dto.Latitude;
        parcel.Longitude = dto.Longitude;
        parcel.BoundaryGeoJson = dto.BoundaryGeoJson;
        parcel.Stage = dto.Stage;
        parcel.IsAcquired = dto.IsAcquired;
        parcel.CurrentLandUse = dto.CurrentLandUse;
        parcel.IntendedLandUse = dto.IntendedLandUse;
        parcel.MaxFloorAreaRatio = dto.MaxFloorAreaRatio;
        parcel.MaxCoveragePercent = dto.MaxCoveragePercent;
        parcel.MaxHeightFt = dto.MaxHeightFt;
        parcel.AgreedPrice = dto.AgreedPrice;
        parcel.MonthlyHoldingCost = dto.MonthlyHoldingCost;
        parcel.HasLitigation = dto.HasLitigation;

        await Db.SaveChangesAsync();
        return (await GetParcelAsync(parcel.Id))!;
    }

    private static readonly (string Key, string Label)[] StandardTitleChecks =
    [
        ("TitleSearch", "Title search over the last 30 years"),
        ("RevenueExtract", "Current revenue record extract"),
        ("NonEncumbrance", "Non-encumbrance certificate"),
        ("MutationStatus", "Mutation recorded in the seller's name"),
        ("LandUseClassification", "Land-use classification confirmed"),
        ("MasterPlanCompliance", "Master-plan and zoning compliance"),
        ("LitigationSearch", "Litigation search in local courts"),
        ("PhysicalPossession", "Physical possession and boundary verification"),
        ("TaxClearance", "Land revenue and tax dues cleared"),
        ("SellerIdentity", "Seller identity and authority to sell"),
    ];

    public async Task<TitleChainEntryDto> SaveTitleEntryAsync(Guid parcelId, TitleChainEntryDto dto, Guid userId)
    {
        _ = await RequireAsync<LandParcel>(parcelId, "That parcel does not exist.");

        var entry = dto.Id != Guid.Empty
            ? await Db.TitleChainEntries.ForCompany(Tenant).FirstOrDefaultAsync(t => t.Id == dto.Id)
            : null;

        if (entry is null)
        {
            var next = await Db.TitleChainEntries.ForCompany(Tenant)
                .Where(t => t.LandParcelId == parcelId)
                .MaxAsync(t => (int?)t.SequenceNumber) ?? 0;

            entry = new TitleChainEntry
            {
                LandParcelId = parcelId,
                SequenceNumber = dto.SequenceNumber > 0 ? dto.SequenceNumber : next + 1,
            }.StampNew(Tenant, userId);

            Db.TitleChainEntries.Add(entry);
        }
        else entry.StampUpdated(userId);

        entry.Instrument = dto.Instrument;
        entry.InstrumentDate = dto.InstrumentDate;
        entry.InstrumentNumber = dto.InstrumentNumber;
        entry.RegistrarOffice = dto.RegistrarOffice;
        entry.TransferorName = dto.TransferorName;
        entry.TransfereeName = dto.TransfereeName;
        entry.Consideration = dto.Consideration;
        entry.DocumentUrl = dto.DocumentUrl;
        entry.Verdict = dto.Verdict;
        entry.VerificationNote = dto.VerificationNote;

        await Db.SaveChangesAsync();
        return MapTitleEntry(entry);
    }

    public async Task<EncumbranceDto> SaveEncumbranceAsync(Guid? parcelId, Guid? propertyId, EncumbranceDto dto, Guid userId)
    {
        if (parcelId is null && propertyId is null)
            throw new InvalidOperationException("A charge has to attach to either a parcel or a property.");

        var encumbrance = dto.Id != Guid.Empty
            ? await Db.Encumbrances.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == dto.Id)
            : null;

        if (encumbrance is null)
        {
            encumbrance = new Encumbrance
            {
                LandParcelId = parcelId,
                PropertyId = propertyId,
            }.StampNew(Tenant, userId);

            Db.Encumbrances.Add(encumbrance);
        }
        else encumbrance.StampUpdated(userId);

        encumbrance.Kind = dto.Kind;
        encumbrance.Status = dto.Status;
        encumbrance.HolderName = dto.HolderName;
        encumbrance.Amount = dto.Amount;
        encumbrance.CreatedOn = dto.CreatedOn;
        encumbrance.ExpectedClearanceDate = dto.ExpectedClearanceDate;
        encumbrance.ClearedOn = dto.ClearedOn;
        encumbrance.ReferenceNumber = dto.ReferenceNumber;
        encumbrance.DocumentUrl = dto.DocumentUrl;
        encumbrance.BlocksTransaction = dto.BlocksTransaction;
        encumbrance.Note = dto.Note;

        if (dto.Status == EncumbranceStatus.Cleared && encumbrance.ClearedOn is null)
            encumbrance.ClearedOn = Today;

        await Db.SaveChangesAsync();

        // The flag on the parent is what every list, board and gate reads. Keep it honest.
        await RefreshEncumbranceFlagsAsync(parcelId, propertyId, userId);

        return MapEncumbrance(encumbrance);
    }

    private async Task RefreshEncumbranceFlagsAsync(Guid? parcelId, Guid? propertyId, Guid userId)
    {
        if (parcelId is not null)
        {
            var parcel = await Db.LandParcels.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == parcelId);

            if (parcel is not null)
            {
                parcel.HasEncumbrance = await Db.Encumbrances.ForCompany(Tenant)
                    .AnyAsync(e => e.LandParcelId == parcelId && e.Status == EncumbranceStatus.Active);

                parcel.StampUpdated(userId);
            }
        }

        if (propertyId is not null)
        {
            var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == propertyId);

            if (property is not null)
            {
                property.IsMortgaged = await Db.Encumbrances.ForCompany(Tenant)
                    .AnyAsync(e => e.PropertyId == propertyId
                               && e.Status == EncumbranceStatus.Active
                               && e.Kind == EncumbranceKind.Mortgage);

                property.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();
    }

    public async Task<TitleVerificationItemDto> SaveVerificationAsync(Guid parcelId, TitleVerificationItemDto dto, Guid userId)
    {
        _ = await RequireAsync<LandParcel>(parcelId, "That parcel does not exist.");

        // A verdict without evidence is an opinion, and this is exactly where opinions cost money.
        if (dto.Verdict == VerificationVerdict.Passed && string.IsNullOrWhiteSpace(dto.EvidenceUrl))
            throw new InvalidOperationException($"Attach the evidence document before passing \"{dto.Label}\".");

        if (dto.Verdict == VerificationVerdict.Conditional && string.IsNullOrWhiteSpace(dto.Condition))
            throw new InvalidOperationException("A conditional pass has to say what the condition is.");

        var item = dto.Id != Guid.Empty
            ? await Db.TitleVerificationItems.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == dto.Id)
            : null;

        if (item is null)
        {
            item = new TitleVerificationItem { LandParcelId = parcelId }.StampNew(Tenant, userId);
            Db.TitleVerificationItems.Add(item);
        }
        else item.StampUpdated(userId);

        item.CheckKey = dto.CheckKey;
        item.Label = dto.Label;
        item.Verdict = dto.Verdict;
        item.DueDate = dto.DueDate;
        item.EvidenceUrl = dto.EvidenceUrl;
        item.Findings = dto.Findings;
        item.Condition = dto.Condition;
        item.IsMandatory = dto.IsMandatory;
        item.SortOrder = dto.SortOrder;

        if (dto.Verdict != VerificationVerdict.Pending && item.CompletedOn is null)
            item.CompletedOn = Today;

        await Db.SaveChangesAsync();

        dto.Id = item.Id;
        dto.CompletedOn = item.CompletedOn;
        dto.IsOverdue = item.Verdict == VerificationVerdict.Pending && item.DueDate != null && item.DueDate < Today;
        return dto;
    }

    public async Task<PaginatedResponse<LandAcquisitionDto>> GetAcquisitionsAsync(ListQueryDto query)
    {
        var q = Db.LandAcquisitions.ForCompany(Tenant)
            .Include(a => a.Stages)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                a => a.Reference.Contains(query.Search!) || (a.SellerName != null && a.SellerName.Contains(query.Search!)))
            .OrderByDescending(a => a.StartedOn);

        var today = Today;

        return await PageAsync(q, query, async list =>
        {
            var result = new List<LandAcquisitionDto>();
            foreach (var a in list) result.Add(await MapAcquisitionAsync(a, today));
            return result;
        });
    }

    private async Task<LandAcquisitionDto> MapAcquisitionAsync(LandAcquisition a, DateOnly today)
    {
        var parcel = await Db.LandParcels.ForCompany(Tenant)
            .Where(p => p.Id == a.LandParcelId)
            .Select(p => p.Reference)
            .FirstOrDefaultAsync();

        var owner = a.OwnerUserId is null
            ? null
            : await Db.AgentProfiles.ForCompany(Tenant)
                .Where(x => x.UserId == a.OwnerUserId)
                .Select(x => x.DisplayName)
                .FirstOrDefaultAsync();

        var currentStage = a.Stages.Where(s => s.Kind == a.Stage).OrderBy(s => s.SortOrder).FirstOrDefault();

        var stageStart = currentStage?.ActualDate
            ?? a.Stages.Where(s => s.ActualDate != null).OrderByDescending(s => s.ActualDate).FirstOrDefault()?.ActualDate
            ?? a.StartedOn;

        return new LandAcquisitionDto
        {
            Id = a.Id,
            Reference = a.Reference,
            LandParcelId = a.LandParcelId,
            ParcelReference = parcel,
            SellerName = a.SellerName,
            Stage = a.Stage,
            StartedOn = a.StartedOn,
            TargetCompletionDate = a.TargetCompletionDate,
            CompletedOn = a.CompletedOn,
            AskingPrice = a.AskingPrice,
            OfferedPrice = a.OfferedPrice,
            AgreedPrice = a.AgreedPrice,
            AdvancePaid = a.AdvancePaid,
            TotalPaid = a.TotalPaid,
            OwnerName = owner,
            BlockingIssue = a.BlockingIssue,
            IsAborted = a.IsAborted,
            DaysInStage = stageStart is null ? 0 : today.DayNumber - stageStart.Value.DayNumber,
            Stages = a.Stages.OrderBy(s => s.SortOrder).Select(s => new AcquisitionStageDto
            {
                Id = s.Id,
                Kind = s.Kind,
                TargetDate = s.TargetDate,
                ActualDate = s.ActualDate,
                EstimatedCost = s.EstimatedCost,
                ActualCost = s.ActualCost,
                Note = s.Note,
                IsOverdue = s.ActualDate is null && s.TargetDate != null && s.TargetDate < today,
                SortOrder = s.SortOrder,
            }).ToList(),
        };
    }

    public async Task<LandAcquisitionDto> SaveAcquisitionAsync(LandAcquisitionDto dto, Guid userId)
    {
        var parcel = await RequireAsync<LandParcel>(dto.LandParcelId, "That parcel does not exist.");

        var acquisition = dto.Id != Guid.Empty
            ? await Db.LandAcquisitions.ForCompany(Tenant)
                .Include(a => a.Stages)
                .FirstOrDefaultAsync(a => a.Id == dto.Id)
            : null;

        if (acquisition is null)
        {
            acquisition = new LandAcquisition
            {
                LandParcelId = dto.LandParcelId,
                Reference = string.IsNullOrWhiteSpace(dto.Reference)
                    ? await numbering.NextMasterCodeAsync(Db.LandAcquisitions, "ACQ")
                    : dto.Reference,
                StartedOn = dto.StartedOn ?? Today,
            }.StampNew(Tenant, userId);

            Db.LandAcquisitions.Add(acquisition);

            var order = 0;

            foreach (var kind in StandardAcquisitionStages)
            {
                acquisition.Stages.Add(new AcquisitionStage
                {
                    Kind = kind,
                    SortOrder = order += 10,
                }.StampNew(Tenant, userId));
            }
        }
        else acquisition.StampUpdated(userId);

        var previousStage = acquisition.Stage;

        acquisition.SellerName = dto.SellerName;
        acquisition.Stage = dto.Stage;
        acquisition.TargetCompletionDate = dto.TargetCompletionDate;
        acquisition.AskingPrice = dto.AskingPrice;
        acquisition.OfferedPrice = dto.OfferedPrice;
        acquisition.AgreedPrice = dto.AgreedPrice;
        acquisition.AdvancePaid = dto.AdvancePaid;
        acquisition.BlockingIssue = dto.BlockingIssue;
        acquisition.IsAborted = dto.IsAborted;

        foreach (var s in dto.Stages)
        {
            var stage = acquisition.Stages.FirstOrDefault(x => x.Id == s.Id)
                     ?? acquisition.Stages.FirstOrDefault(x => x.Kind == s.Kind);

            if (stage is null) continue;

            stage.TargetDate = s.TargetDate;
            stage.ActualDate = s.ActualDate;
            stage.EstimatedCost = s.EstimatedCost;
            stage.ActualCost = s.ActualCost;
            stage.Note = s.Note;
            stage.StampUpdated(userId);
        }

        // Moving into a stage stamps it, so days-in-stage means something without extra typing.
        if (previousStage != dto.Stage)
        {
            var entered = acquisition.Stages.FirstOrDefault(s => s.Kind == dto.Stage);
            if (entered is not null && entered.ActualDate is null) entered.ActualDate = Today;
        }

        if (dto.Stage == AcquisitionStageKind.PossessionTaken)
        {
            acquisition.CompletedOn = dto.CompletedOn ?? Today;
            parcel.IsAcquired = true;
            parcel.AcquiredOn = acquisition.CompletedOn;
        }

        parcel.Stage = dto.Stage;
        parcel.AgreedPrice = dto.AgreedPrice;
        parcel.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return await MapAcquisitionAsync(acquisition, Today);
    }

    private static readonly AcquisitionStageKind[] StandardAcquisitionStages =
    [
        AcquisitionStageKind.Identified,
        AcquisitionStageKind.UnderNegotiation,
        AcquisitionStageKind.TermSheet,
        AcquisitionStageKind.DueDiligence,
        AcquisitionStageKind.AgreementToSell,
        AcquisitionStageKind.AdvancePaid,
        AcquisitionStageKind.Registration,
        AcquisitionStageKind.Mutation,
        AcquisitionStageKind.PossessionTaken,
    ];

    public async Task<AcquisitionCostLineDto> SaveAcquisitionCostAsync(Guid acquisitionId, AcquisitionCostLineDto dto, Guid userId)
    {
        var acquisition = await RequireAsync<LandAcquisition>(acquisitionId, "That acquisition does not exist.");

        var line = dto.Id != Guid.Empty
            ? await Db.AcquisitionCostLines.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
            : null;

        if (line is null)
        {
            line = new AcquisitionCostLine { LandAcquisitionId = acquisitionId }.StampNew(Tenant, userId);
            Db.AcquisitionCostLines.Add(line);
        }
        else line.StampUpdated(userId);

        line.CostType = dto.CostType;
        line.Description = dto.Description;
        line.BudgetAmount = dto.BudgetAmount;
        line.ActualAmount = dto.ActualAmount;
        line.IncurredOn = dto.IncurredOn;
        line.Reference = dto.Reference;
        line.IsCapitalised = dto.IsCapitalised;

        await Db.SaveChangesAsync();

        // The parcel carries the true landed cost, which is what unit costing has to divide.
        var total = await Db.AcquisitionCostLines.ForCompany(Tenant)
            .Where(c => c.LandAcquisitionId == acquisitionId && c.IsCapitalised)
            .SumAsync(c => c.ActualAmount);

        var paid = await Db.AcquisitionCostLines.ForCompany(Tenant)
            .Where(c => c.LandAcquisitionId == acquisitionId)
            .SumAsync(c => c.ActualAmount);

        acquisition.TotalPaid = paid;
        acquisition.StampUpdated(userId);

        var parcel = await Db.LandParcels.ForCompany(Tenant)
            .FirstOrDefaultAsync(p => p.Id == acquisition.LandParcelId);

        if (parcel is not null)
        {
            parcel.TotalAcquisitionCost = total;
            parcel.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        return MapCostLine(line);
    }
}
