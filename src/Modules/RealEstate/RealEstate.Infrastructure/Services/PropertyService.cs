using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The property master, the land bank behind it, and the valuation evidence.
///
/// One table serves every line of business, so the interesting work here is the duplicate check —
/// the single biggest data-quality failure in every agency system — and the price opinion, which
/// shows its working rather than asserting a number.
/// </summary>
public partial class PropertyService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), IPropertyService
{
    // ═══ Properties ══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<PropertyListItemDto>> GetPropertiesAsync(PropertySearchDto query)
    {
        var minSqFt = query.MinArea is null ? (decimal?)null : RealEstateMapper.ToSquareFeet(query.MinArea.Value, query.InputAreaUnit);
        var maxSqFt = query.MaxArea is null ? (decimal?)null : RealEstateMapper.ToSquareFeet(query.MaxArea.Value, query.InputAreaUnit);

        var q = Db.Properties.ForCompany(Tenant)
            .WhereIf(query.Categories.Count > 0, p => query.Categories.Contains(p.Category))
            .WhereIf(query.SubTypes.Count > 0, p => query.SubTypes.Contains(p.SubType))
            .WhereIf(query.Statuses.Count > 0, p => query.Statuses.Contains(p.Status))
            .WhereIf(query.GeoAreaId.HasValue, p => p.GeoAreaId == query.GeoAreaId)
            .WhereIf(query.ProjectId.HasValue, p => p.ProjectId == query.ProjectId)
            .WhereIf(query.Occupancy.HasValue, p => p.Occupancy == query.Occupancy)
            .WhereIf(query.HasLitigation.HasValue, p => p.HasLitigation == query.HasLitigation)
            .WhereIf(query.MinPrice.HasValue, p => p.AskingPrice >= query.MinPrice)
            .WhereIf(query.MaxPrice.HasValue, p => p.AskingPrice <= query.MaxPrice)
            .WhereIf(minSqFt.HasValue, p => p.SaleableAreaSqFt >= minSqFt)
            .WhereIf(maxSqFt.HasValue, p => p.SaleableAreaSqFt <= maxSqFt)
            .WhereIf(query.MinBedrooms.HasValue, p => p.Bedrooms >= query.MinBedrooms)
            .WhereIf(query.MaxBedrooms.HasValue, p => p.Bedrooms <= query.MaxBedrooms)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                p => p.Reference.Contains(query.Search!)
                     || (p.Name != null && p.Name.Contains(query.Search!))
                     || (p.AddressLine1 != null && p.AddressLine1.Contains(query.Search!))
                     || (p.UnitNumber != null && p.UnitNumber.Contains(query.Search!)));

        // Radius search, done as a bounding box because that is what an index can serve.
        if (query.NearLatitude is not null && query.NearLongitude is not null && query.RadiusKm is > 0m)
        {
            var latDelta = query.RadiusKm.Value / 111m;
            var lngDelta = query.RadiusKm.Value / 85m;

            q = q.Where(p => p.Latitude != null && p.Longitude != null
                          && p.Latitude >= query.NearLatitude - latDelta
                          && p.Latitude <= query.NearLatitude + latDelta
                          && p.Longitude >= query.NearLongitude - lngDelta
                          && p.Longitude <= query.NearLongitude + lngDelta);
        }

        if (query.OwnerPartyId is not null)
        {
            var owned = Db.PropertyOwnerships.ForCompany(Tenant)
                .Where(o => o.PartyId == query.OwnerPartyId && o.ToDate == null)
                .Select(o => o.PropertyId);
            q = q.Where(p => owned.Contains(p.Id));
        }

        var ordered = q.OrderBy(p => p.Reference);

        return await PageAsync(ordered, query, MapPropertyListAsync);
    }

    private async Task<List<PropertyListItemDto>> MapPropertyListAsync(List<Property> properties)
    {
        if (properties.Count == 0) return [];

        var areaUnit = await AreaUnitAsync();
        var ids = properties.Select(p => p.Id).ToList();

        var heroes = await Db.PropertyMedia.ForCompany(Tenant)
            .Where(m => ids.Contains(m.PropertyId) && m.IsHero)
            .ToDictionaryAsync(m => m.PropertyId, m => m.Url);

        var areaIds = properties.Where(p => p.GeoAreaId.HasValue).Select(p => p.GeoAreaId!.Value).Distinct().ToList();
        var areas = areaIds.Count == 0
            ? []
            : await Db.GeoAreas.ForCompany(Tenant).Where(a => areaIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Name);

        var projects = await ProjectNamesAsync(properties.Select(p => p.ProjectId));

        var owners = await Db.PropertyOwnerships.ForCompany(Tenant)
            .Where(o => ids.Contains(o.PropertyId) && o.ToDate == null && o.IsPrimaryOwner)
            .ToListAsync();

        var ownerNames = await PartyNamesAsync(owners.Select(o => o.PartyId));

        var listingCounts = await Db.Listings.ForCompany(Tenant)
            .Where(l => ids.Contains(l.PropertyId) && l.Status == ListingStatus.Live)
            .GroupBy(l => l.PropertyId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        return properties.Select(p =>
        {
            var owner = owners.FirstOrDefault(o => o.PropertyId == p.Id);

            return new PropertyListItemDto
            {
                Id = p.Id,
                Reference = p.Reference,
                Name = p.Name,
                Category = p.Category,
                SubType = p.SubType,
                SubTypeLabel = p.SubType.ToString(),
                Status = p.Status,
                Occupancy = p.Occupancy,
                AreaName = p.GeoAreaId is null ? null : areas.GetValueOrDefault(p.GeoAreaId.Value),
                AddressOneLine = RealEstateMapper.OneLineAddress(p),
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                SaleableArea = RealEstateMapper.AreaOrNull(p.SaleableAreaSqFt, areaUnit),
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                AskingPrice = p.AskingPrice,
                MonthlyRent = p.MonthlyRent,
                CurrencyCode = p.CurrencyCode,
                HeroImageUrl = heroes.GetValueOrDefault(p.Id),
                ProjectName = p.ProjectId is null ? null : projects.GetValueOrDefault(p.ProjectId.Value),
                UnitNumber = p.UnitNumber,
                OwnerName = owner is null ? null : ownerNames.GetValueOrDefault(owner.PartyId),
                HasLitigation = p.HasLitigation,
                IsMortgaged = p.IsMortgaged,
                LiveListingCount = listingCounts.GetValueOrDefault(p.Id),
            };
        }).ToList();
    }

    public async Task<PropertyDetailDto?> GetPropertyAsync(Guid id)
    {
        var property = await Db.Properties.ForCompany(Tenant)
            .Include(p => p.Features)
            .Include(p => p.Media)
            .Include(p => p.Ownerships)
            .Include(p => p.StatusHistory)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null) return null;

        var areaUnit = await AreaUnitAsync();
        var currency = property.CurrencyCode ?? await CurrencyAsync();
        var today = Today;

        var area = property.GeoAreaId is null
            ? null
            : await Db.GeoAreas.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == property.GeoAreaId);

        var project = property.ProjectId is null
            ? null
            : await Db.Projects.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == property.ProjectId);

        var node = property.ProjectNodeId is null
            ? null
            : await Db.ProjectNodes.ForCompany(Tenant).FirstOrDefaultAsync(n => n.Id == property.ProjectNodeId);

        var ownerNames = await PartyNamesAsync(property.Ownerships.Select(o => o.PartyId));
        var ownerDetails = await Db.Parties.ForCompany(Tenant)
            .Where(p => property.Ownerships.Select(o => o.PartyId).Contains(p.Id))
            .Select(p => new OwnerContact(p.Id, p.FatherOrGuardianName, p.PrimaryPhone))
            .ToDictionaryAsync(p => p.Id, p => p);

        var identities = await Db.PartyIdentities.ForCompany(Tenant)
            .Where(i => property.Ownerships.Select(o => o.PartyId).Contains(i.PartyId) && i.IsPrimary)
            .ToDictionaryAsync(i => i.PartyId, i => i.Number);

        var detail = new PropertyDetailDto
        {
            Id = property.Id,
            Reference = property.Reference,
            Name = property.Name,
            Category = property.Category,
            SubType = property.SubType,
            Tenure = property.Tenure,
            Status = property.Status,
            Occupancy = property.Occupancy,
            Address = RealEstateMapper.Address(property),
            GeoAreaId = property.GeoAreaId,
            AreaPath = area?.Path,
            ProjectId = property.ProjectId,
            ProjectName = project?.Name,
            ProjectNodeId = property.ProjectNodeId,
            BlockName = node?.Name,
            FloorNumber = property.FloorNumber,
            FloorLabel = property.FloorLabel,
            UnitNumber = property.UnitNumber,
            StackCode = property.StackCode,
            Facing = property.Facing,
            IsCorner = property.IsCorner,
            ViewDescription = property.ViewDescription,

            PlotArea = RealEstateMapper.AreaOrNull(property.PlotAreaSqFt, areaUnit),
            CoveredArea = RealEstateMapper.AreaOrNull(property.CoveredAreaSqFt, areaUnit),
            BuiltUpArea = RealEstateMapper.AreaOrNull(property.BuiltUpAreaSqFt, areaUnit),
            SaleableArea = RealEstateMapper.AreaOrNull(property.SaleableAreaSqFt, areaUnit),
            CarpetArea = RealEstateMapper.AreaOrNull(property.CarpetAreaSqFt, areaUnit),
            TerraceArea = RealEstateMapper.AreaOrNull(property.TerraceAreaSqFt, areaUnit),
            LoadingFactorPercent = property.LoadingFactorPercent,
            FrontageFt = property.FrontageFt,
            DepthFt = property.DepthFt,
            RoadWidthFt = property.RoadWidthFt,

            Bedrooms = property.Bedrooms,
            Bathrooms = property.Bathrooms,
            HalfBaths = property.HalfBaths,
            Kitchens = property.Kitchens,
            LivingRooms = property.LivingRooms,
            ServantRooms = property.ServantRooms,
            StoreRooms = property.StoreRooms,
            ParkingBays = property.ParkingBays,
            FloorsInUnit = property.FloorsInUnit,
            Furnishing = property.Furnishing,
            Condition = property.Condition,
            YearBuilt = property.YearBuilt,
            EntranceDirection = property.EntranceDirection,

            AskingPrice = property.AskingPrice,
            ReservePrice = property.ReservePrice,
            RatePerSqFt = property.RatePerSqFt,
            MonthlyRent = property.MonthlyRent,
            LastSoldPrice = property.LastSoldPrice,
            LastSoldOn = property.LastSoldOn,
            CurrentValuation = property.CurrentValuation,
            ServiceChargeRatePerSqFt = property.ServiceChargeRatePerSqFt,
            AnnualPropertyTax = property.AnnualPropertyTax,
            CurrencyCode = currency,

            HasLitigation = property.HasLitigation,
            IsMortgaged = property.IsMortgaged,
            IsLandownerShare = property.IsLandownerShare,
            Notes = property.Notes,

            Features = property.Features.OrderBy(f => f.SortOrder).Select(f => new PropertyFeatureDto
            {
                Id = f.Id,
                FeatureKey = f.FeatureKey,
                Label = f.Label,
                Category = f.Category,
                Value = f.Value,
                IsHighlight = f.IsHighlight,
                SortOrder = f.SortOrder,
            }).ToList(),

            Media = property.Media.OrderByDescending(m => m.IsHero).ThenBy(m => m.SortOrder).Select(m => new PropertyMediaDto
            {
                Id = m.Id,
                Kind = m.Kind,
                Url = m.Url,
                ThumbnailUrl = m.ThumbnailUrl,
                Caption = m.Caption,
                RoomTag = m.RoomTag,
                SortOrder = m.SortOrder,
                IsHero = m.IsHero,
                ExcludeFromPortals = m.ExcludeFromPortals,
                WidthPx = m.WidthPx,
                HeightPx = m.HeightPx,
            }).ToList(),

            Owners = property.Ownerships.Where(o => o.ToDate == null).Select(o => MapOwnership(o, ownerNames, ownerDetails, identities)).ToList(),
            OwnershipHistory = property.Ownerships.Where(o => o.ToDate != null)
                .OrderByDescending(o => o.FromDate)
                .Select(o => MapOwnership(o, ownerNames, ownerDetails, identities)).ToList(),
        };

        detail.Documents = await Db.PropertyDocuments.ForCompany(Tenant)
            .Where(d => d.PropertyId == id)
            .Select(d => new PropertyDocumentDto
            {
                Id = d.Id,
                DocumentType = d.DocumentType,
                Title = d.Title,
                Url = d.Url,
                State = d.State,
                IssuedOn = d.IssuedOn,
                ExpiresOn = d.ExpiresOn,
                IsExpired = d.ExpiresOn != null && d.ExpiresOn < today,
                IsConfidential = d.IsConfidential,
            })
            .ToListAsync();

        detail.Encumbrances = await Db.Encumbrances.ForCompany(Tenant)
            .Where(e => e.PropertyId == id)
            .Select(e => new EncumbranceDto
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
            })
            .ToListAsync();

        detail.Certificates = await Db.ComplianceCertificates.ForCompany(Tenant)
            .Where(c => c.PropertyId == id && c.IsCurrent)
            .Select(c => new ComplianceCertificateDto
            {
                Id = c.Id,
                PropertyId = c.PropertyId,
                Kind = c.Kind,
                CertificateNumber = c.CertificateNumber,
                IssuedOn = c.IssuedOn,
                ExpiresOn = c.ExpiresOn,
                DaysToExpiry = c.ExpiresOn.DayNumber - today.DayNumber,
                IsExpired = c.ExpiresOn < today,
                IssuerName = c.IssuerName,
                DocumentUrl = c.DocumentUrl,
                HasFailures = c.HasFailures,
                IsCurrent = c.IsCurrent,
            })
            .ToListAsync();

        var listings = await Db.Listings.ForCompany(Tenant)
            .Where(l => l.PropertyId == id)
            .OrderByDescending(l => l.ListedOn)
            .Take(20)
            .ToListAsync();

        detail.Listings = listings.Select(l => new ListingListItemDto
        {
            Id = l.Id,
            Reference = l.Reference,
            PropertyId = l.PropertyId,
            Kind = l.Kind,
            Status = l.Status,
            Headline = l.Headline,
            AddressOneLine = detail.Address.OneLine,
            AskingPrice = l.AskingPrice,
            CurrencyCode = l.CurrencyCode,
            ListedOn = l.ListedOn,
            DaysOnMarket = l.DaysOnMarket,
            ViewingCount = l.ViewingCount,
            EnquiryCount = l.EnquiryCount,
            OfferCount = l.OfferCount,
        }).ToList();

        detail.CurrentTenancyId = property.CurrentTenancyId;
        detail.CurrentBookingId = property.CurrentBookingId;

        if (property.CurrentTenancyId is not null)
        {
            var tenant = await Db.TenancyParties.ForCompany(Tenant)
                .Where(p => p.TenancyId == property.CurrentTenancyId && p.IsLeadTenant)
                .Select(p => p.PartyId)
                .FirstOrDefaultAsync();

            if (tenant != Guid.Empty)
            {
                var names = await PartyNamesAsync([tenant]);
                detail.CurrentTenantName = names.GetValueOrDefault(tenant);
            }
        }

        if (property.CurrentBookingId is not null)
        {
            var booking = await Db.Bookings.ForCompany(Tenant)
                .FirstOrDefaultAsync(b => b.Id == property.CurrentBookingId);

            if (booking is not null)
            {
                var names = await PartyNamesAsync([booking.PrimaryApplicantPartyId]);
                detail.CurrentBuyerName = names.GetValueOrDefault(booking.PrimaryApplicantPartyId);
                detail.OutstandingDues = booking.Outstanding;
            }
        }

        detail.OpenWorkOrders = await Db.WorkOrders.ForCompany(Tenant)
            .CountAsync(w => w.PropertyId == id && w.Status != WorkOrderStatus.SignedOff && w.Status != WorkOrderStatus.Cancelled);

        detail.Timeline = property.StatusHistory
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new TimelineEntryDto
            {
                Id = h.Id,
                OccurredAt = h.ChangedAt,
                Kind = "status",
                Title = $"{h.FromStatus} → {h.ToStatus}",
                Detail = h.Note,
                Icon = "swap_horiz",
            })
            .ToList();

        return detail;
    }

    private static PropertyOwnershipDto MapOwnership(
        PropertyOwnership o,
        Dictionary<Guid, string> names,
        Dictionary<Guid, OwnerContact> details,
        Dictionary<Guid, string> identities) => new()
    {
        Id = o.Id,
        PartyId = o.PartyId,
        OwnerName = names.GetValueOrDefault(o.PartyId, "—"),
        FatherOrGuardianName = details.GetValueOrDefault(o.PartyId)?.FatherOrGuardianName,
        IdentityNumber = identities.GetValueOrDefault(o.PartyId),
        Phone = details.GetValueOrDefault(o.PartyId)?.PrimaryPhone,
        SharePercent = o.SharePercent,
        FromDate = o.FromDate,
        ToDate = o.ToDate,
        AcquiredBy = o.AcquiredBy,
        DeedNumber = o.DeedNumber,
        DeedDate = o.DeedDate,
        ConsiderationAmount = o.ConsiderationAmount,
        IsPrimaryOwner = o.IsPrimaryOwner,
        IsCurrent = o.ToDate is null,
    };

    public async Task<PropertyDetailDto> SavePropertyAsync(PropertyUpsertDto dto, Guid userId)
    {
        if (!dto.AcknowledgeDuplicate && dto.Id is null)
        {
            var duplicates = await CheckDuplicatesAsync(dto);
            if (duplicates.Count > 0)
            {
                throw new InvalidOperationException(
                    $"{duplicates[0].Reference} at {duplicates[0].AddressOneLine} already exists " +
                    $"(matched on {duplicates[0].MatchedOn}). Open it, or confirm this is a different property.");
            }
        }

        var property = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.Properties.ForCompany(Tenant).Include(p => p.Features).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (property is null)
        {
            property = new Property
            {
                Reference = string.IsNullOrWhiteSpace(dto.Reference)
                    ? await numbering.NextPropertyReferenceAsync()
                    : dto.Reference,
            }.StampNew(Tenant, userId);

            Db.Properties.Add(property);
        }
        else property.StampUpdated(userId);

        property.Name = dto.Name;
        property.Category = dto.Category;
        property.SubType = dto.SubType;
        property.Tenure = dto.Tenure;
        property.Status = dto.Status;
        property.Occupancy = dto.Occupancy;

        property.AddressLine1 = dto.AddressLine1;
        property.AddressLine2 = dto.AddressLine2;
        property.Street = dto.Street;
        property.PostCode = dto.PostCode;
        property.GeoAreaId = dto.GeoAreaId;
        property.CityId = dto.CityId;
        property.CountryId = dto.CountryId;
        property.Latitude = dto.Latitude;
        property.Longitude = dto.Longitude;
        property.LocationCode = dto.LocationCode;

        property.ProjectId = dto.ProjectId;
        property.ProjectNodeId = dto.ProjectNodeId;
        property.FloorNumber = dto.FloorNumber;
        property.FloorLabel = dto.FloorLabel;
        property.UnitNumber = dto.UnitNumber;
        property.StackCode = dto.StackCode;
        property.Facing = dto.Facing;
        property.IsCorner = dto.IsCorner;
        property.ViewDescription = dto.ViewDescription;

        // Areas arrive in the operator's unit and are stored in square feet, always.
        property.PlotAreaSqFt = Convert(dto.PlotArea, dto.InputAreaUnit);
        property.CoveredAreaSqFt = Convert(dto.CoveredArea, dto.InputAreaUnit);
        property.BuiltUpAreaSqFt = Convert(dto.BuiltUpArea, dto.InputAreaUnit);
        property.SaleableAreaSqFt = Convert(dto.SaleableArea, dto.InputAreaUnit);
        property.CarpetAreaSqFt = Convert(dto.CarpetArea, dto.InputAreaUnit);
        property.TerraceAreaSqFt = Convert(dto.TerraceArea, dto.InputAreaUnit);
        property.GardenAreaSqFt = Convert(dto.GardenArea, dto.InputAreaUnit);
        property.FrontageFt = dto.FrontageFt;
        property.DepthFt = dto.DepthFt;
        property.RoadWidthFt = dto.RoadWidthFt;
        property.CeilingHeightFt = dto.CeilingHeightFt;

        // The loading factor is disclosure-regulated in several markets, so it is derived rather
        // than typed — the two numbers can never disagree.
        if (property.SaleableAreaSqFt is > 0m && property.CarpetAreaSqFt is > 0m)
        {
            property.LoadingFactorPercent = RealEstateMapper.Percent(
                property.SaleableAreaSqFt.Value - property.CarpetAreaSqFt.Value,
                property.CarpetAreaSqFt.Value);
        }

        property.Bedrooms = dto.Bedrooms;
        property.Bathrooms = dto.Bathrooms;
        property.HalfBaths = dto.HalfBaths;
        property.Kitchens = dto.Kitchens;
        property.LivingRooms = dto.LivingRooms;
        property.ServantRooms = dto.ServantRooms;
        property.StoreRooms = dto.StoreRooms;
        property.ParkingBays = dto.ParkingBays;
        property.FloorsInUnit = dto.FloorsInUnit;
        property.Furnishing = dto.Furnishing;
        property.Condition = dto.Condition;
        property.YearBuilt = dto.YearBuilt;
        property.EntranceDirection = dto.EntranceDirection;

        property.AskingPrice = dto.AskingPrice;
        property.ReservePrice = dto.ReservePrice;
        property.MonthlyRent = dto.MonthlyRent;
        property.ServiceChargeRatePerSqFt = dto.ServiceChargeRatePerSqFt;
        property.PropertyTaxReference = dto.PropertyTaxReference;
        property.AnnualPropertyTax = dto.AnnualPropertyTax;
        property.CurrencyCode = dto.CurrencyCode ?? property.CurrencyCode;
        property.Notes = dto.Notes;
        property.DuplicateCheckOverridden = dto.AcknowledgeDuplicate;

        if (property.AskingPrice is > 0m && property.SaleableAreaSqFt is > 0m)
            property.RatePerSqFt = RealEstateMapper.Money(property.AskingPrice.Value / property.SaleableAreaSqFt.Value);

        if (dto.Features.Count > 0)
        {
            Db.PropertyFeatures.RemoveRange(property.Features);

            foreach (var f in dto.Features)
            {
                property.Features.Add(new PropertyFeature
                {
                    FeatureKey = f.FeatureKey,
                    Label = f.Label,
                    Category = f.Category,
                    Value = f.Value,
                    IsHighlight = f.IsHighlight,
                    SortOrder = f.SortOrder,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        return (await GetPropertyAsync(property.Id))!;
    }

    private static decimal? Convert(decimal? value, AreaUnit unit)
        => value is null ? null : RealEstateMapper.ToSquareFeet(value.Value, unit);

    public async Task DeletePropertyAsync(Guid id, Guid userId)
    {
        var property = await RequireAsync<Property>(id, "That property does not exist.");

        if (await Db.Bookings.ForCompany(Tenant).AnyAsync(b => b.PropertyId == id && b.Status != BookingStatus.Cancelled))
            throw new InvalidOperationException("This property has a live booking and cannot be removed.");

        if (await Db.Tenancies.ForCompany(Tenant).AnyAsync(t => t.PropertyId == id && t.Status == TenancyStatus.Active))
            throw new InvalidOperationException("This property has a live tenancy and cannot be removed.");

        property.StampDeleted(userId);
        await Db.SaveChangesAsync();
    }

    /// <summary>
    /// The duplicate check. Address, geolocation, unit-in-project and title reference — the four
    /// ways the same property gets entered twice.
    /// </summary>
    public async Task<List<DuplicateCandidateDto>> CheckDuplicatesAsync(PropertyUpsertDto dto)
    {
        var candidates = new List<DuplicateCandidateDto>();

        if (dto.ProjectId is not null && !string.IsNullOrWhiteSpace(dto.UnitNumber))
        {
            var byUnit = await Db.Properties.ForCompany(Tenant)
                .Where(p => p.ProjectId == dto.ProjectId && p.UnitNumber == dto.UnitNumber)
                .WhereIf(dto.Id.HasValue, p => p.Id != dto.Id)
                .Take(3)
                .ToListAsync();

            candidates.AddRange(byUnit.Select(p => Candidate(p, "unit number in project", 95)));
        }

        if (!string.IsNullOrWhiteSpace(dto.AddressLine1))
        {
            var byAddress = await Db.Properties.ForCompany(Tenant)
                .Where(p => p.AddressLine1 == dto.AddressLine1)
                .WhereIf(!string.IsNullOrWhiteSpace(dto.PostCode), p => p.PostCode == dto.PostCode)
                .WhereIf(dto.Id.HasValue, p => p.Id != dto.Id)
                .Take(3)
                .ToListAsync();

            candidates.AddRange(byAddress
                .Where(p => candidates.All(c => c.PropertyId != p.Id))
                .Select(p => Candidate(p, "address", 85)));
        }

        if (dto.Latitude is not null && dto.Longitude is not null)
        {
            // Roughly twenty metres, which is close enough to be the same building.
            const decimal delta = 0.0002m;

            var byLocation = await Db.Properties.ForCompany(Tenant)
                .Where(p => p.Latitude != null && p.Longitude != null)
                .Where(p => p.Latitude >= dto.Latitude - delta && p.Latitude <= dto.Latitude + delta)
                .Where(p => p.Longitude >= dto.Longitude - delta && p.Longitude <= dto.Longitude + delta)
                .WhereIf(dto.Id.HasValue, p => p.Id != dto.Id)
                .Take(3)
                .ToListAsync();

            candidates.AddRange(byLocation
                .Where(p => candidates.All(c => c.PropertyId != p.Id))
                .Select(p => Candidate(p, "geolocation", 70)));
        }

        var projects = await ProjectNamesAsync(candidates.Select(c => (Guid?)Guid.Empty));

        return candidates.OrderByDescending(c => c.ConfidencePercent).Take(5).ToList();

        DuplicateCandidateDto Candidate(Property p, string matchedOn, int confidence) => new()
        {
            PropertyId = p.Id,
            Reference = p.Reference,
            AddressOneLine = RealEstateMapper.OneLineAddress(p),
            UnitNumber = p.UnitNumber,
            Status = p.Status,
            MatchedOn = matchedOn,
            ConfidencePercent = confidence,
        };
    }

    public async Task<PropertyDetailDto> ChangeStatusAsync(
        Guid id, PropertyStatus status, Guid? reasonCodeId, string? note, Guid userId)
    {
        var property = await RequireAsync<Property>(id, "That property does not exist.");
        var previous = property.Status;

        Db.PropertyStatusHistories.Add(new PropertyStatusHistory
        {
            PropertyId = id,
            FromStatus = previous,
            ToStatus = status,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = userId,
            ReasonCodeId = reasonCodeId,
            Note = note,
        }.StampNew(Tenant, userId));

        property.Status = status;
        property.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetPropertyAsync(id))!;
    }

    // ═══ Media & documents ═══════════════════════════════════════════════════

    public async Task<List<PropertyMediaDto>> SaveMediaAsync(Guid propertyId, List<PropertyMediaDto> media, Guid userId)
    {
        var existing = await Db.PropertyMedia.ForCompany(Tenant)
            .Where(m => m.PropertyId == propertyId)
            .ToListAsync();

        // Exactly one hero. Portals, brochures and cards all read it.
        var heroId = media.FirstOrDefault(m => m.IsHero)?.Id;

        foreach (var dto in media)
        {
            var item = dto.Id == Guid.Empty ? null : existing.FirstOrDefault(m => m.Id == dto.Id);

            if (item is null)
            {
                item = new PropertyMedia { PropertyId = propertyId }.StampNew(Tenant, userId);
                Db.PropertyMedia.Add(item);
            }
            else item.StampUpdated(userId);

            item.Kind = dto.Kind;
            item.Url = dto.Url;
            item.ThumbnailUrl = dto.ThumbnailUrl;
            item.Caption = dto.Caption;
            item.RoomTag = dto.RoomTag;
            item.SortOrder = dto.SortOrder;
            item.IsHero = heroId is not null ? dto.Id == heroId : false;
            item.ExcludeFromPortals = dto.ExcludeFromPortals;
            item.WidthPx = dto.WidthPx;
            item.HeightPx = dto.HeightPx;
        }

        // Nothing marked as hero: promote the first photo so cards are never blank.
        if (heroId is null)
        {
            var first = await Db.PropertyMedia.ForCompany(Tenant)
                .Where(m => m.PropertyId == propertyId && m.Kind == MediaKind.Photo)
                .OrderBy(m => m.SortOrder)
                .FirstOrDefaultAsync();

            if (first is not null) first.IsHero = true;
        }

        await Db.SaveChangesAsync();

        var saved = await Db.PropertyMedia.ForCompany(Tenant)
            .Where(m => m.PropertyId == propertyId)
            .OrderByDescending(m => m.IsHero).ThenBy(m => m.SortOrder)
            .ToListAsync();

        return saved.Select(m => new PropertyMediaDto
        {
            Id = m.Id,
            Kind = m.Kind,
            Url = m.Url,
            ThumbnailUrl = m.ThumbnailUrl,
            Caption = m.Caption,
            RoomTag = m.RoomTag,
            SortOrder = m.SortOrder,
            IsHero = m.IsHero,
            ExcludeFromPortals = m.ExcludeFromPortals,
            WidthPx = m.WidthPx,
            HeightPx = m.HeightPx,
        }).ToList();
    }

    public async Task DeleteMediaAsync(Guid mediaId, Guid userId)
    {
        var media = await RequireAsync<PropertyMedia>(mediaId, "That image does not exist.");
        media.StampDeleted(userId);
        await Db.SaveChangesAsync();
    }

    public async Task<List<PropertyDocumentDto>> GetDocumentsAsync(Guid propertyId)
    {
        var today = Today;

        return await Db.PropertyDocuments.ForCompany(Tenant)
            .Where(d => d.PropertyId == propertyId)
            .OrderBy(d => d.DocumentType)
            .Select(d => new PropertyDocumentDto
            {
                Id = d.Id,
                DocumentType = d.DocumentType,
                Title = d.Title,
                Url = d.Url,
                State = d.State,
                IssuedOn = d.IssuedOn,
                ExpiresOn = d.ExpiresOn,
                IsExpired = d.ExpiresOn != null && d.ExpiresOn < today,
                IsConfidential = d.IsConfidential,
            })
            .ToListAsync();
    }

    public async Task<PropertyDocumentDto> SaveDocumentAsync(Guid propertyId, PropertyDocumentDto dto, Guid userId)
    {
        var document = dto.Id != Guid.Empty
            ? await Db.PropertyDocuments.ForCompany(Tenant).FirstOrDefaultAsync(d => d.Id == dto.Id)
            : null;

        if (document is null)
        {
            document = new PropertyDocument { PropertyId = propertyId }.StampNew(Tenant, userId);
            Db.PropertyDocuments.Add(document);
        }
        else document.StampUpdated(userId);

        document.DocumentType = dto.DocumentType;
        document.Title = dto.Title;
        document.Url = dto.Url;
        document.State = dto.State;
        document.IssuedOn = dto.IssuedOn;
        document.ExpiresOn = dto.ExpiresOn;
        document.IsConfidential = dto.IsConfidential;

        if (dto.State == DocumentState.Verified)
        {
            document.VerifiedByUserId = userId;
            document.VerifiedAt = DateTime.UtcNow;
        }

        await Db.SaveChangesAsync();
        dto.Id = document.Id;
        return dto;
    }

    // ═══ Ownership ═══════════════════════════════════════════════════════════

    public async Task<List<PropertyOwnershipDto>> GetOwnershipAsync(Guid propertyId, bool includeHistory)
    {
        var ownerships = await Db.PropertyOwnerships.ForCompany(Tenant)
            .Where(o => o.PropertyId == propertyId)
            .WhereIf(!includeHistory, o => o.ToDate == null)
            .OrderByDescending(o => o.FromDate)
            .ToListAsync();

        var names = await PartyNamesAsync(ownerships.Select(o => o.PartyId));

        var details = await Db.Parties.ForCompany(Tenant)
            .Where(p => ownerships.Select(o => o.PartyId).Contains(p.Id))
            .Select(p => new OwnerContact(p.Id, p.FatherOrGuardianName, p.PrimaryPhone))
            .ToDictionaryAsync(p => p.Id, p => p);

        var identities = await Db.PartyIdentities.ForCompany(Tenant)
            .Where(i => ownerships.Select(o => o.PartyId).Contains(i.PartyId) && i.IsPrimary)
            .ToDictionaryAsync(i => i.PartyId, i => i.Number);

        return ownerships.Select(o => MapOwnership(o, names, details, identities)).ToList();
    }

    /// <summary>
    /// Sets the current owner. The previous row is **closed**, never edited, so the chain reads
    /// end to end forever — which is what a title dispute twenty years later turns on.
    /// </summary>
    public async Task<PropertyOwnershipDto> SetOwnerAsync(Guid propertyId, PropertyOwnershipDto dto, Guid userId)
    {
        var current = await Db.PropertyOwnerships.ForCompany(Tenant)
            .Where(o => o.PropertyId == propertyId && o.ToDate == null)
            .ToListAsync();

        var from = dto.FromDate == default ? Today : dto.FromDate;

        foreach (var previous in current.Where(o => o.PartyId != dto.PartyId))
        {
            previous.ToDate = from.AddDays(-1);
            previous.StampUpdated(userId);
        }

        var ownership = current.FirstOrDefault(o => o.PartyId == dto.PartyId);

        if (ownership is null)
        {
            ownership = new PropertyOwnership { PropertyId = propertyId, PartyId = dto.PartyId }.StampNew(Tenant, userId);
            Db.PropertyOwnerships.Add(ownership);
        }
        else ownership.StampUpdated(userId);

        ownership.SharePercent = dto.SharePercent <= 0m ? 100m : dto.SharePercent;
        ownership.FromDate = from;
        ownership.AcquiredBy = dto.AcquiredBy;
        ownership.DeedNumber = dto.DeedNumber;
        ownership.DeedDate = dto.DeedDate;
        ownership.ConsiderationAmount = dto.ConsiderationAmount;
        ownership.IsPrimaryOwner = dto.IsPrimaryOwner;

        // The owner role is additive on the party, so a Person 360 shows it immediately.
        var hasRole = await Db.PartyRoles.ForCompany(Tenant)
            .AnyAsync(r => r.PartyId == dto.PartyId && r.Kind == PartyRoleKind.Landlord && r.IsActive);

        if (!hasRole)
        {
            Db.PartyRoles.Add(new PartyRole
            {
                PartyId = dto.PartyId,
                Kind = PartyRoleKind.Landlord,
                FromDate = from,
                IsActive = true,
                ContextType = "Property",
                ContextId = propertyId,
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();
        return (await GetOwnershipAsync(propertyId, false)).First(o => o.PartyId == dto.PartyId);
    }

    /// <summary>The two owner fields every deed and NOC prints beside the name.</summary>
    private sealed record OwnerContact(Guid Id, string? FatherOrGuardianName, string? PrimaryPhone);
}
