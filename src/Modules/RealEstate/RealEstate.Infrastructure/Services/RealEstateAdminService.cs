using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Settings, offices, geography, territories, agents, approvals and bulk import.
///
/// The lines-of-business switches live here and everything else in the module reads them, so this
/// is the first service the shell calls after sign-in.
/// </summary>
public class RealEstateAdminService(RealEstateDbContext db, IRealEstateTenant tenant, RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), IRealEstateAdminService
{
    // ═══ Settings ════════════════════════════════════════════════════════════

    public async Task<RealEstateSettingsDto> GetSettingsAsync() => Map(await SettingsAsync());

    public async Task<RealEstateSettingsDto> UpdateSettingsAsync(RealEstateSettingsDto dto, Guid userId)
    {
        var s = await SettingsAsync();

        s.BrokerageEnabled = dto.LinesOfBusiness.Brokerage;
        s.DevelopmentEnabled = dto.LinesOfBusiness.Development;
        s.ContractingEnabled = dto.LinesOfBusiness.Contracting;
        s.EstateManagementEnabled = dto.LinesOfBusiness.EstateManagement;

        if (!s.BrokerageEnabled && !s.DevelopmentEnabled && !s.ContractingEnabled && !s.EstateManagementEnabled)
            throw new InvalidOperationException("At least one line of business has to stay switched on.");

        s.DisplayAreaUnit = dto.DisplayAreaUnit;
        s.CurrencyCode = dto.CurrencyCode;
        s.ReportingCurrencyCode = dto.ReportingCurrencyCode;
        s.CurrencyDecimals = dto.CurrencyDecimals;
        s.DefaultAllocationOrder = dto.DefaultAllocationOrder;
        s.DemandLeadDays = dto.DemandLeadDays;
        s.DefaultHoldHours = dto.DefaultHoldHours;
        s.MaxHoldHoursWithoutApproval = dto.MaxHoldHoursWithoutApproval;
        s.MaxDiscountPercentWithoutApproval = dto.MaxDiscountPercentWithoutApproval;
        s.DefaultEscrowPercent = dto.DefaultEscrowPercent;
        s.EscrowEnforced = dto.EscrowEnforced;
        s.RequireKycBeforeCompletion = dto.RequireKycBeforeCompletion;
        s.ClientMoneySegregated = dto.ClientMoneySegregated;
        s.BlockTransferOnDues = dto.BlockTransferOnDues;
        s.LeadResponseSlaMinutes = dto.LeadResponseSlaMinutes;
        s.PartnerLeadValidityDays = dto.PartnerLeadValidityDays;
        s.DefaultLanguage = dto.DefaultLanguage;
        s.BrandPrimaryColor = dto.BrandPrimaryColor;
        s.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return Map(s);
    }

    public async Task<LinesOfBusinessDto> GetLinesOfBusinessAsync() => Lines(await SettingsAsync());

    private static LinesOfBusinessDto Lines(RealEstateSettings s)
    {
        var lob = new LinesOfBusinessDto
        {
            Brokerage = s.BrokerageEnabled,
            Development = s.DevelopmentEnabled,
            Contracting = s.ContractingEnabled,
            EstateManagement = s.EstateManagementEnabled,
        };

        // Land somebody on the screen their business actually runs on rather than a generic home.
        lob.DefaultRoute = s switch
        {
            { DevelopmentEnabled: true } => "/realestate/inventory",
            { BrokerageEnabled: true } => "/realestate/deals",
            { EstateManagementEnabled: true } => "/realestate/rent-roll",
            { ContractingEnabled: true } => "/realestate/construction",
            _ => "/realestate/dashboard",
        };

        return lob;
    }

    private static RealEstateSettingsDto Map(RealEstateSettings s) => new()
    {
        Id = s.Id,
        LinesOfBusiness = Lines(s),
        DisplayAreaUnit = s.DisplayAreaUnit,
        CurrencyCode = s.CurrencyCode,
        ReportingCurrencyCode = s.ReportingCurrencyCode,
        CurrencyDecimals = s.CurrencyDecimals,
        DefaultAllocationOrder = s.DefaultAllocationOrder,
        DemandLeadDays = s.DemandLeadDays,
        DefaultHoldHours = s.DefaultHoldHours,
        MaxHoldHoursWithoutApproval = s.MaxHoldHoursWithoutApproval,
        MaxDiscountPercentWithoutApproval = s.MaxDiscountPercentWithoutApproval,
        DefaultEscrowPercent = s.DefaultEscrowPercent,
        EscrowEnforced = s.EscrowEnforced,
        RequireKycBeforeCompletion = s.RequireKycBeforeCompletion,
        ClientMoneySegregated = s.ClientMoneySegregated,
        BlockTransferOnDues = s.BlockTransferOnDues,
        LeadResponseSlaMinutes = s.LeadResponseSlaMinutes,
        PartnerLeadValidityDays = s.PartnerLeadValidityDays,
        DefaultLanguage = s.DefaultLanguage ?? "en",
        BrandPrimaryColor = s.BrandPrimaryColor,
    };

    // ═══ Offices ═════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<RealEstateOfficeDto>> GetOfficesAsync(ListQueryDto query)
    {
        var q = Db.RealEstateOffices.ForCompany(Tenant)
            .WhereIf(!query.IncludeInactive, o => o.IsActive)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                o => o.Name.Contains(query.Search!) || (o.Code != null && o.Code.Contains(query.Search!)))
            .OrderBy(o => o.OfficeType).ThenBy(o => o.Name);

        var settings = await SettingsAsync();
        return await PageAsync(q, query, o => MapOffice(o, settings, 0, 0));
    }

    public async Task<RealEstateOfficeDto?> GetOfficeAsync(Guid id)
    {
        var office = await Db.RealEstateOffices.ForCompany(Tenant).FirstOrDefaultAsync(o => o.Id == id);
        if (office is null) return null;

        var agents = await Db.AgentProfiles.ForCompany(Tenant).CountAsync(a => a.OfficeId == id && a.IsActive);
        var listings = await Db.Listings.ForCompany(Tenant).CountAsync(l => l.OfficeId == id && l.Status == ListingStatus.Live);

        return MapOffice(office, await SettingsAsync(), agents, listings);
    }

    public async Task<RealEstateOfficeDto> SaveOfficeAsync(RealEstateOfficeDto dto, Guid userId)
    {
        var office = dto.Id != Guid.Empty
            ? await Db.RealEstateOffices.ForCompany(Tenant).FirstOrDefaultAsync(o => o.Id == dto.Id)
            : null;

        if (office is null)
        {
            office = new RealEstateOffice().StampNew(Tenant, userId);
            office.Code = string.IsNullOrWhiteSpace(dto.Code)
                ? await numbering.NextMasterCodeAsync(Db.RealEstateOffices, "OFC")
                : dto.Code;
            Db.RealEstateOffices.Add(office);
        }
        else
        {
            office.Code = dto.Code ?? office.Code;
            office.StampUpdated(userId);
        }

        office.Name = dto.Name;
        office.OfficeType = dto.OfficeType;
        office.Phone = dto.Phone;
        office.Email = dto.Email;
        office.AddressLine = dto.Address.Line1;
        office.City = dto.Address.City;
        office.PostCode = dto.Address.PostCode;
        office.CountryCode = dto.Address.CountryCode;
        office.Latitude = dto.Address.Latitude;
        office.Longitude = dto.Address.Longitude;
        office.TimeZoneId = dto.TimeZoneId;
        office.CurrencyCode = dto.CurrencyCode;
        office.DisplayAreaUnit = dto.DisplayAreaUnit;
        office.WorkingDaysMask = dto.WorkingDaysMask;
        office.OpensAt = dto.OpensAt;
        office.ClosesAt = dto.ClosesAt;
        office.IsFranchise = dto.IsFranchise;
        office.FranchiseRoyaltyPercent = dto.FranchiseRoyaltyPercent;
        office.RegistrationNumber = dto.RegistrationNumber;
        office.TaxNumber = dto.TaxNumber;
        office.IsActive = dto.IsActive;

        // A null here means "inherit the company switch", which is what most offices want.
        office.BrokerageEnabled = dto.LinesOfBusiness.Brokerage ? true : null;
        office.DevelopmentEnabled = dto.LinesOfBusiness.Development ? true : null;
        office.ContractingEnabled = dto.LinesOfBusiness.Contracting ? true : null;
        office.EstateManagementEnabled = dto.LinesOfBusiness.EstateManagement ? true : null;

        await Db.SaveChangesAsync();
        return (await GetOfficeAsync(office.Id))!;
    }

    private static RealEstateOfficeDto MapOffice(RealEstateOffice o, RealEstateSettings s, int agents, int listings) => new()
    {
        Id = o.Id,
        Name = o.Name,
        Code = o.Code,
        OfficeType = o.OfficeType,
        Phone = o.Phone,
        Email = o.Email,
        Address = new AddressDto
        {
            Line1 = o.AddressLine,
            City = o.City,
            PostCode = o.PostCode,
            CountryCode = o.CountryCode,
            Latitude = o.Latitude,
            Longitude = o.Longitude,
            OneLine = string.Join(", ", new[] { o.AddressLine, o.City, o.PostCode }.Where(x => !string.IsNullOrWhiteSpace(x))),
        },
        TimeZoneId = o.TimeZoneId,
        CurrencyCode = o.CurrencyCode,
        DisplayAreaUnit = o.DisplayAreaUnit,
        LinesOfBusiness = new LinesOfBusinessDto
        {
            Brokerage = o.BrokerageEnabled ?? s.BrokerageEnabled,
            Development = o.DevelopmentEnabled ?? s.DevelopmentEnabled,
            Contracting = o.ContractingEnabled ?? s.ContractingEnabled,
            EstateManagement = o.EstateManagementEnabled ?? s.EstateManagementEnabled,
        },
        WorkingDaysMask = o.WorkingDaysMask,
        OpensAt = o.OpensAt,
        ClosesAt = o.ClosesAt,
        IsFranchise = o.IsFranchise,
        FranchiseRoyaltyPercent = o.FranchiseRoyaltyPercent,
        RegistrationNumber = o.RegistrationNumber,
        TaxNumber = o.TaxNumber,
        IsActive = o.IsActive,
        AgentCount = agents,
        ActiveListingCount = listings,
    };

    // ═══ Geography ═══════════════════════════════════════════════════════════

    public async Task<List<GeoAreaDto>> GetGeoTreeAsync(Guid? parentId, int depth)
    {
        var all = await Db.GeoAreas.ForCompany(Tenant)
            .Where(a => a.IsActive)
            .OrderBy(a => a.Depth).ThenBy(a => a.Name)
            .ToListAsync();

        var counts = await Db.Properties.ForCompany(Tenant)
            .Where(p => p.GeoAreaId != null)
            .GroupBy(p => p.GeoAreaId!.Value)
            .Select(g => new { AreaId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AreaId, x => x.Count);

        return BuildTree(all, parentId, counts, depth);
    }

    private static List<GeoAreaDto> BuildTree(
        List<GeoArea> all, Guid? parentId, Dictionary<Guid, int> counts, int remainingDepth)
    {
        if (remainingDepth <= 0) return [];

        return all.Where(a => a.ParentAreaId == parentId)
            .Select(a => new GeoAreaDto
            {
                Id = a.Id,
                ParentAreaId = a.ParentAreaId,
                Name = a.Name,
                Code = a.Code,
                LevelLabel = a.LevelLabel,
                Depth = a.Depth,
                Path = a.Path,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                BoundaryGeoJson = a.BoundaryGeoJson,
                AverageRatePerSqFt = a.AverageRatePerSqFt,
                PropertyCount = counts.GetValueOrDefault(a.Id),
                IsActive = a.IsActive,
                Children = BuildTree(all, a.Id, counts, remainingDepth - 1),
            })
            .ToList();
    }

    public async Task<List<LookupDto>> SearchGeoAreasAsync(string search, int take)
        => await Db.GeoAreas.ForCompany(Tenant)
            .Where(a => a.IsActive && a.Name.Contains(search))
            .OrderBy(a => a.Depth).ThenBy(a => a.Name)
            .Take(take <= 0 ? 20 : take)
            .Select(a => new LookupDto { Id = a.Id, Label = a.Name, SubLabel = a.Path, Code = a.Code })
            .ToListAsync();

    public async Task<GeoAreaDto> SaveGeoAreaAsync(GeoAreaDto dto, Guid userId)
    {
        var area = dto.Id != Guid.Empty
            ? await Db.GeoAreas.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.Id)
            : null;

        if (area is null)
        {
            area = new GeoArea().StampNew(Tenant, userId);
            Db.GeoAreas.Add(area);
        }
        else area.StampUpdated(userId);

        area.ParentAreaId = dto.ParentAreaId;
        area.Name = dto.Name;
        area.Code = dto.Code;
        area.LevelLabel = dto.LevelLabel;
        area.Latitude = dto.Latitude;
        area.Longitude = dto.Longitude;
        area.BoundaryGeoJson = dto.BoundaryGeoJson;
        area.AverageRatePerSqFt = dto.AverageRatePerSqFt;
        area.IsActive = dto.IsActive;

        // Depth and path are derived, never supplied — a client that got them wrong would break
        // every "everything under this society" query in the module.
        if (dto.ParentAreaId is null)
        {
            area.Depth = 0;
            area.Path = area.Name;
        }
        else
        {
            var parent = await Db.GeoAreas.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == dto.ParentAreaId)
                ?? throw new InvalidOperationException("That parent area does not exist.");
            area.Depth = parent.Depth + 1;
            area.Path = $"{parent.Path} / {area.Name}";
        }

        await Db.SaveChangesAsync();
        dto.Id = area.Id;
        dto.Depth = area.Depth;
        dto.Path = area.Path;
        return dto;
    }

    public async Task DeleteGeoAreaAsync(Guid id, Guid userId)
    {
        var area = await RequireAsync<GeoArea>(id, "That area does not exist.");

        if (await Db.GeoAreas.ForCompany(Tenant).AnyAsync(a => a.ParentAreaId == id))
            throw new InvalidOperationException("Remove the areas underneath this one first.");

        if (await Db.Properties.ForCompany(Tenant).AnyAsync(p => p.GeoAreaId == id))
            throw new InvalidOperationException("Properties still sit in this area, so it cannot be removed.");

        area.StampDeleted(userId);
        await Db.SaveChangesAsync();
    }

    // ═══ Territories ═════════════════════════════════════════════════════════

    public async Task<List<TerritoryDto>> GetTerritoriesAsync(Guid? officeId)
    {
        var territories = await Db.Territories.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, t => t.OfficeId == officeId)
            .Include(t => t.Areas)
            .Include(t => t.Assignments)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var ids = territories.Select(t => t.Id).ToList();
        var agentIds = territories.SelectMany(t => t.Assignments).Select(a => a.AgentProfileId).Distinct().ToList();

        var agents = await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => agentIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.DisplayName);

        var areaIds = territories.SelectMany(t => t.Areas).Select(a => a.GeoAreaId).Distinct().ToList();
        var areas = await Db.GeoAreas.ForCompany(Tenant)
            .Where(a => areaIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => new { a.Name, a.Path });

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var leadCounts = await Db.Enquiries.ForCompany(Tenant)
            .Where(e => e.TerritoryId != null && ids.Contains(e.TerritoryId!.Value) && e.ReceivedAt >= monthStart)
            .GroupBy(e => e.TerritoryId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var offices = await Db.RealEstateOffices.ForCompany(Tenant).ToDictionaryAsync(o => o.Id, o => o.Name);

        return territories.Select(t => new TerritoryDto
        {
            Id = t.Id,
            Name = t.Name,
            Code = t.Code,
            OfficeName = t.OfficeId is null ? null : offices.GetValueOrDefault(t.OfficeId.Value),
            CategoryFilter = t.CategoryFilter,
            MinPrice = t.MinPrice,
            MaxPrice = t.MaxPrice,
            BoundaryGeoJson = t.BoundaryGeoJson,
            IsActive = t.IsActive,
            LeadsThisMonth = leadCounts.GetValueOrDefault(t.Id),
            Areas = t.Areas.Select(a => new LookupDto
            {
                Id = a.GeoAreaId,
                Label = areas.GetValueOrDefault(a.GeoAreaId)?.Name ?? "—",
                SubLabel = areas.GetValueOrDefault(a.GeoAreaId)?.Path,
            }).ToList(),
            Assignments = t.Assignments.Select(a => new TerritoryAssignmentDto
            {
                Id = a.Id,
                AgentProfileId = a.AgentProfileId,
                AgentName = agents.GetValueOrDefault(a.AgentProfileId) ?? "—",
                EffectiveFrom = a.EffectiveFrom,
                EffectiveTo = a.EffectiveTo,
                RoutingWeight = a.RoutingWeight,
                IsPrimary = a.IsPrimary,
            }).ToList(),
        }).ToList();
    }

    public async Task<TerritoryDto> SaveTerritoryAsync(TerritoryDto dto, Guid userId)
    {
        var territory = dto.Id != Guid.Empty
            ? await Db.Territories.ForCompany(Tenant)
                .Include(t => t.Areas).Include(t => t.Assignments)
                .FirstOrDefaultAsync(t => t.Id == dto.Id)
            : null;

        if (territory is null)
        {
            territory = new Territory().StampNew(Tenant, userId);
            Db.Territories.Add(territory);
        }
        else territory.StampUpdated(userId);

        territory.Name = dto.Name;
        territory.Code = dto.Code;
        territory.CategoryFilter = dto.CategoryFilter;
        territory.MinPrice = dto.MinPrice;
        territory.MaxPrice = dto.MaxPrice;
        territory.BoundaryGeoJson = dto.BoundaryGeoJson;
        territory.IsActive = dto.IsActive;

        Db.TerritoryAreas.RemoveRange(territory.Areas);
        foreach (var a in dto.Areas)
            territory.Areas.Add(new TerritoryArea { GeoAreaId = a.Id }.StampNew(Tenant, userId));

        Db.TerritoryAssignments.RemoveRange(territory.Assignments);
        foreach (var a in dto.Assignments)
            territory.Assignments.Add(new TerritoryAssignment
            {
                AgentProfileId = a.AgentProfileId,
                EffectiveFrom = a.EffectiveFrom == default ? Today : a.EffectiveFrom,
                EffectiveTo = a.EffectiveTo,
                RoutingWeight = a.RoutingWeight <= 0 ? 1 : a.RoutingWeight,
                IsPrimary = a.IsPrimary,
            }.StampNew(Tenant, userId));

        await Db.SaveChangesAsync();
        return (await GetTerritoriesAsync(null)).First(t => t.Id == territory.Id);
    }

    // ═══ Agents & teams ══════════════════════════════════════════════════════

    public async Task<PaginatedResponse<AgentProfileDto>> GetAgentsAsync(ListQueryDto query)
    {
        var q = Db.AgentProfiles.ForCompany(Tenant)
            .WhereIf(!query.IncludeInactive, a => a.IsActive)
            .WhereIf(query.OfficeId.HasValue, a => a.OfficeId == query.OfficeId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                a => a.DisplayName.Contains(query.Search!) || (a.Code != null && a.Code.Contains(query.Search!)))
            .OrderBy(a => a.DisplayName);

        return await PageAsync(q, query, async rows => await MapAgentsAsync(rows));
    }

    public async Task<AgentProfileDto?> GetAgentAsync(Guid id)
    {
        var agent = await Db.AgentProfiles.ForCompany(Tenant)
            .Include(a => a.Licences)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agent is null) return null;

        var mapped = (await MapAgentsAsync([agent])).First();

        mapped.Licences = agent.Licences.Select(l => new AgentLicenceDto
        {
            Id = l.Id,
            LicenceType = l.LicenceType,
            LicenceNumber = l.LicenceNumber,
            IssuingAuthority = l.IssuingAuthority,
            IssuedOn = l.IssuedOn,
            ExpiresOn = l.ExpiresOn,
            DaysToExpiry = l.ExpiresOn is null ? null : l.ExpiresOn.Value.DayNumber - Today.DayNumber,
            IsExpired = l.ExpiresOn is not null && l.ExpiresOn < Today,
            DocumentUrl = l.DocumentUrl,
            BlocksAssignmentWhenExpired = l.BlocksAssignmentWhenExpired,
            IsVerified = l.IsVerified,
        }).ToList();

        var year = DateTime.UtcNow.Year;
        var cap = await Db.AgentCapLedgers.ForCompany(Tenant)
            .Where(c => c.AgentProfileId == id && c.PeriodFrom.Year == year)
            .FirstOrDefaultAsync();

        if (cap is not null)
        {
            mapped.CapPosition = new AgentCapLedgerDto
            {
                AgentProfileId = id,
                AgentName = agent.DisplayName,
                PeriodFrom = cap.PeriodFrom,
                PeriodTo = cap.PeriodTo,
                CapAmount = cap.CapAmount,
                ContributedAmount = cap.ContributedAmount,
                RemainingToCap = cap.RemainingToCap,
                PercentToCap = RealEstateMapper.Percent(cap.ContributedAmount, cap.CapAmount),
                CapReached = cap.CapReached,
                CapReachedOn = cap.CapReachedOn,
                RolledOverAmount = cap.RolledOverAmount,
                GrossCommissionEarned = cap.GrossCommissionEarned,
                NetCommissionEarned = cap.NetCommissionEarned,
                DealCount = cap.DealCount,
                TransactionVolume = cap.TransactionVolume,
                CurrencyCode = await CurrencyAsync(),
            };
        }

        return mapped;
    }

    private async Task<List<AgentProfileDto>> MapAgentsAsync(List<AgentProfile> agents)
    {
        if (agents.Count == 0) return [];

        var ids = agents.Select(a => a.Id).ToList();
        var currency = await CurrencyAsync();

        var offices = await Db.RealEstateOffices.ForCompany(Tenant).ToDictionaryAsync(o => o.Id, o => o.Name);
        var teams = await Db.SalesTeams.ForCompany(Tenant).ToDictionaryAsync(t => t.Id, t => t.Name);
        var plans = await Db.CommissionPlans.ForCompany(Tenant).ToDictionaryAsync(p => p.Id, p => p.Name);

        var openLeads = await Db.Enquiries.ForCompany(Tenant)
            .Where(e => e.AssignedAgentId != null && ids.Contains(e.AssignedAgentId!.Value))
            .Where(e => e.Stage != EnquiryStage.Lost && e.Stage != EnquiryStage.Completed)
            .GroupBy(e => e.AssignedAgentId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var expiredLicences = await Db.AgentLicences.ForCompany(Tenant)
            .Where(l => ids.Contains(l.AgentProfileId) && l.ExpiresOn != null && l.ExpiresOn < Today)
            .Select(l => l.AgentProfileId)
            .Distinct()
            .ToListAsync();

        var deals = await Db.Deals.ForCompany(Tenant)
            .Where(d => d.SellingAgentId != null && ids.Contains(d.SellingAgentId!.Value))
            .GroupBy(d => d.SellingAgentId!.Value)
            .Select(g => new
            {
                Id = g.Key,
                Closed = g.Count(x => x.Status == DealStatus.Completed),
                Fee = g.Where(x => x.Status == DealStatus.Completed).Sum(x => (decimal?)x.GrossFee) ?? 0m,
            })
            .ToDictionaryAsync(x => x.Id, x => x);

        var bookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => b.SalesExecutiveId != null && ids.Contains(b.SalesExecutiveId!.Value))
            .Where(b => b.Status != BookingStatus.Cancelled)
            .GroupBy(b => b.SalesExecutiveId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count(), Value = g.Sum(x => (decimal?)x.TotalConsideration) ?? 0m })
            .ToDictionaryAsync(x => x.Id, x => x);

        return agents.Select(a =>
        {
            var deal = deals.GetValueOrDefault(a.Id);
            var booking = bookings.GetValueOrDefault(a.Id);

            return new AgentProfileDto
            {
                Id = a.Id,
                UserId = a.UserId,
                EmployeeId = a.EmployeeId,
                DisplayName = a.DisplayName,
                Code = a.Code,
                Phone = a.Phone,
                Email = a.Email,
                PhotoUrl = a.PhotoUrl,
                JobTitle = a.JobTitle,
                OfficeName = a.OfficeId is null ? null : offices.GetValueOrDefault(a.OfficeId.Value),
                TeamName = a.SalesTeamId is null ? null : teams.GetValueOrDefault(a.SalesTeamId.Value),
                JoinedOn = a.JoinedOn,
                LeftOn = a.LeftOn,
                CapAnniversary = a.CapAnniversary,
                CommissionPlanName = a.CommissionPlanId is null ? null : plans.GetValueOrDefault(a.CommissionPlanId.Value),
                MaxOpenLeads = a.MaxOpenLeads,
                MaxLeadsPerDay = a.MaxLeadsPerDay,
                AcceptsNewLeads = a.AcceptsNewLeads,
                IsOnLeave = a.IsOnLeave,
                Languages = a.Languages,
                Specialisations = a.Specialisations,
                IsActive = a.IsActive,
                OpenLeads = openLeads.GetValueOrDefault(a.Id),
                DealsClosed = deal?.Closed ?? 0,
                GrossCommission = deal?.Fee ?? 0m,
                BookingsMade = booking?.Count ?? 0,
                BookingValue = booking?.Value ?? 0m,
                HasExpiredLicence = expiredLicences.Contains(a.Id),
                CurrencyCode = currency,
            };
        }).ToList();
    }

    public async Task<AgentProfileDto> SaveAgentAsync(AgentProfileDto dto, Guid userId)
    {
        var agent = dto.Id != Guid.Empty
            ? await Db.AgentProfiles.ForCompany(Tenant).Include(a => a.Licences).FirstOrDefaultAsync(a => a.Id == dto.Id)
            : null;

        if (agent is null)
        {
            agent = new AgentProfile().StampNew(Tenant, userId);
            agent.Code = string.IsNullOrWhiteSpace(dto.Code)
                ? await numbering.NextMasterCodeAsync(Db.AgentProfiles, "AGT")
                : dto.Code;
            Db.AgentProfiles.Add(agent);
        }
        else
        {
            agent.Code = dto.Code ?? agent.Code;
            agent.StampUpdated(userId);
        }

        agent.UserId = dto.UserId;
        agent.EmployeeId = dto.EmployeeId;
        agent.DisplayName = dto.DisplayName;
        agent.Phone = dto.Phone;
        agent.Email = dto.Email;
        agent.PhotoUrl = dto.PhotoUrl;
        agent.JobTitle = dto.JobTitle;
        agent.JoinedOn = dto.JoinedOn;
        agent.LeftOn = dto.LeftOn;
        agent.CapAnniversary = dto.CapAnniversary ?? dto.JoinedOn;
        agent.MaxOpenLeads = dto.MaxOpenLeads <= 0 ? 60 : dto.MaxOpenLeads;
        agent.MaxLeadsPerDay = dto.MaxLeadsPerDay <= 0 ? 15 : dto.MaxLeadsPerDay;
        agent.AcceptsNewLeads = dto.AcceptsNewLeads;
        agent.IsOnLeave = dto.IsOnLeave;
        agent.Languages = dto.Languages;
        agent.Specialisations = dto.Specialisations;
        agent.IsActive = dto.IsActive;

        foreach (var l in dto.Licences)
        {
            var licence = l.Id is null
                ? null
                : agent.Licences.FirstOrDefault(x => x.Id == l.Id);

            if (licence is null)
            {
                licence = new AgentLicence().StampNew(Tenant, userId);
                agent.Licences.Add(licence);
            }
            else licence.StampUpdated(userId);

            licence.LicenceType = l.LicenceType;
            licence.LicenceNumber = l.LicenceNumber;
            licence.IssuingAuthority = l.IssuingAuthority;
            licence.IssuedOn = l.IssuedOn;
            licence.ExpiresOn = l.ExpiresOn;
            licence.DocumentUrl = l.DocumentUrl;
            licence.BlocksAssignmentWhenExpired = l.BlocksAssignmentWhenExpired;
            licence.IsVerified = l.IsVerified;
        }

        await Db.SaveChangesAsync();
        return (await GetAgentAsync(agent.Id))!;
    }

    public async Task<List<LookupDto>> GetAgentLookupAsync(Guid? officeId)
        => await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => a.IsActive)
            .WhereIf(officeId.HasValue, a => a.OfficeId == officeId)
            .OrderBy(a => a.DisplayName)
            .Select(a => new LookupDto { Id = a.Id, Label = a.DisplayName, SubLabel = a.JobTitle, Code = a.Code })
            .ToListAsync();

    public async Task<List<SalesTeamDto>> GetTeamsAsync(Guid? officeId)
    {
        var teams = await Db.SalesTeams.ForCompany(Tenant)
            .WhereIf(officeId.HasValue, t => t.OfficeId == officeId)
            .Include(t => t.Members)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var memberIds = teams.SelectMany(t => t.Members).Select(m => m.AgentProfileId).Distinct().ToList();
        var agents = await Db.AgentProfiles.ForCompany(Tenant).Where(a => memberIds.Contains(a.Id)).ToListAsync();
        var mapped = await MapAgentsAsync(agents);
        var offices = await Db.RealEstateOffices.ForCompany(Tenant).ToDictionaryAsync(o => o.Id, o => o.Name);
        var plans = await Db.CommissionPlans.ForCompany(Tenant).ToDictionaryAsync(p => p.Id, p => p.Name);

        return teams.Select(t =>
        {
            var members = t.Members
                .Where(m => m.LeftOn == null)
                .Select(m => mapped.FirstOrDefault(a => a.Id == m.AgentProfileId))
                .Where(a => a is not null)
                .Select(a => a!)
                .ToList();

            return new SalesTeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                OfficeName = t.OfficeId is null ? null : offices.GetValueOrDefault(t.OfficeId.Value),
                LeaderName = t.LeaderAgentId is null
                    ? null
                    : mapped.FirstOrDefault(a => a.Id == t.LeaderAgentId)?.DisplayName,
                LeaderOverridePercent = t.LeaderOverridePercent,
                CommissionPlanName = t.TeamCommissionPlanId is null
                    ? null
                    : plans.GetValueOrDefault(t.TeamCommissionPlanId.Value),
                MemberCount = members.Count,
                TeamVolume = members.Sum(m => m.BookingValue),
                TeamDeals = members.Sum(m => m.DealsClosed),
                Members = members,
            };
        }).ToList();
    }

    public async Task<SalesTeamDto> SaveTeamAsync(SalesTeamDto dto, Guid userId)
    {
        var team = dto.Id != Guid.Empty
            ? await Db.SalesTeams.ForCompany(Tenant).Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == dto.Id)
            : null;

        if (team is null)
        {
            team = new SalesTeam().StampNew(Tenant, userId);
            Db.SalesTeams.Add(team);
        }
        else team.StampUpdated(userId);

        team.Name = dto.Name;
        team.Code = dto.Code;
        team.LeaderOverridePercent = dto.LeaderOverridePercent;

        var wanted = dto.Members.Select(m => m.Id).ToHashSet();

        foreach (var existing in team.Members.Where(m => m.LeftOn == null && !wanted.Contains(m.AgentProfileId)))
            existing.LeftOn = Today;

        foreach (var id in wanted.Where(id => team.Members.All(m => m.AgentProfileId != id || m.LeftOn != null)))
            team.Members.Add(new SalesTeamMember { AgentProfileId = id, JoinedOn = Today }.StampNew(Tenant, userId));

        await Db.SaveChangesAsync();
        return (await GetTeamsAsync(null)).First(t => t.Id == team.Id);
    }

    // ═══ Reason codes ════════════════════════════════════════════════════════

    public async Task<List<ReasonCodeDto>> GetReasonCodesAsync(string? context)
        => await Db.ReasonCodes.ForCompany(Tenant)
            .Where(r => r.IsActive)
            .WhereIf(!string.IsNullOrWhiteSpace(context), r => r.Context == context)
            .OrderBy(r => r.Context).ThenBy(r => r.SortOrder).ThenBy(r => r.Label)
            .Select(r => new ReasonCodeDto
            {
                Id = r.Id,
                Context = r.Context,
                Code = r.Code ?? string.Empty,
                Label = r.Label,
                RequiresNote = r.RequiresNote,
                IsSystem = r.IsSystem,
                SortOrder = r.SortOrder,
            })
            .ToListAsync();

    public async Task<ReasonCodeDto> SaveReasonCodeAsync(ReasonCodeDto dto, Guid userId)
    {
        var code = dto.Id != Guid.Empty
            ? await Db.ReasonCodes.ForCompany(Tenant).FirstOrDefaultAsync(r => r.Id == dto.Id)
            : null;

        if (code is not null && code.IsSystem && code.Label != dto.Label)
            throw new InvalidOperationException("A built-in reason code can be switched off but not renamed.");

        if (code is null)
        {
            code = new ReasonCode().StampNew(Tenant, userId);
            Db.ReasonCodes.Add(code);
        }
        else code.StampUpdated(userId);

        code.Context = dto.Context;
        code.Code = dto.Code;
        code.Label = dto.Label;
        code.RequiresNote = dto.RequiresNote;
        code.SortOrder = dto.SortOrder;

        await Db.SaveChangesAsync();
        dto.Id = code.Id;
        return dto;
    }

    // ═══ Approvals ═══════════════════════════════════════════════════════════

    public async Task<List<ApprovalMatrixDto>> GetApprovalMatrixAsync(string? documentType)
    {
        var rows = await Db.ApprovalMatrices.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(documentType), m => m.DocumentType == documentType)
            .OrderBy(m => m.DocumentType).ThenBy(m => m.MinAmount).ThenBy(m => m.Level)
            .ToListAsync();

        var offices = await Db.RealEstateOffices.ForCompany(Tenant).ToDictionaryAsync(o => o.Id, o => o.Name);
        var projects = await Db.Projects.ForCompany(Tenant).ToDictionaryAsync(p => p.Id, p => p.Name);

        return rows.Select(m => new ApprovalMatrixDto
        {
            Id = m.Id,
            DocumentType = m.DocumentType,
            OfficeName = m.OfficeId is null ? null : offices.GetValueOrDefault(m.OfficeId.Value),
            ProjectName = m.ProjectId is null ? null : projects.GetValueOrDefault(m.ProjectId.Value),
            MinAmount = m.MinAmount,
            MaxAmount = m.MaxAmount,
            Level = m.Level,
            ApproverRoleId = m.ApproverRoleId,
            ApproverUserId = m.ApproverUserId,
            AutoApproveBelowMin = m.AutoApproveBelowMin,
            EscalateAfterHours = m.EscalateAfterHours,
        }).ToList();
    }

    public async Task<ApprovalMatrixDto> SaveApprovalMatrixAsync(ApprovalMatrixDto dto, Guid userId)
    {
        var row = dto.Id != Guid.Empty
            ? await Db.ApprovalMatrices.ForCompany(Tenant).FirstOrDefaultAsync(m => m.Id == dto.Id)
            : null;

        if (row is null)
        {
            row = new ApprovalMatrix().StampNew(Tenant, userId);
            Db.ApprovalMatrices.Add(row);
        }
        else row.StampUpdated(userId);

        row.DocumentType = dto.DocumentType;
        row.MinAmount = dto.MinAmount;
        row.MaxAmount = dto.MaxAmount;
        row.Level = dto.Level <= 0 ? 1 : dto.Level;
        row.ApproverRoleId = dto.ApproverRoleId;
        row.ApproverUserId = dto.ApproverUserId;
        row.AutoApproveBelowMin = dto.AutoApproveBelowMin;
        row.EscalateAfterHours = dto.EscalateAfterHours;

        await Db.SaveChangesAsync();
        dto.Id = row.Id;
        return dto;
    }

    public async Task<PaginatedResponse<ApprovalRequestDto>> GetApprovalsAsync(ListQueryDto query, bool mineOnly, Guid userId)
    {
        var q = Db.ApprovalRequests.ForCompany(Tenant)
            .Where(r => r.Outcome == ApprovalOutcome.Pending || !mineOnly)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                r => r.DocumentType.Contains(query.Search!) ||
                     (r.EntityReference != null && r.EntityReference.Contains(query.Search!)))
            .OrderByDescending(r => r.RequestedAt);

        var reasons = await Db.ReasonCodes.ForCompany(Tenant).ToDictionaryAsync(r => r.Id, r => r.Label);

        return await PageAsync(q, query, r => new ApprovalRequestDto
        {
            Id = r.Id,
            DocumentType = r.DocumentType,
            EntityId = r.EntityId,
            EntityReference = r.EntityReference,
            Summary = r.Summary,
            Amount = r.Amount,
            CurrentLevel = r.CurrentLevel,
            RequiredLevels = r.RequiredLevels,
            Outcome = r.Outcome,
            RequestedByName = "—",
            RequestedAt = r.RequestedAt,
            EscalatesAt = r.EscalatesAt,
            IsOverdue = r.EscalatesAt is not null && r.EscalatesAt < DateTime.UtcNow && r.Outcome == ApprovalOutcome.Pending,
            ReasonLabel = r.ReasonCodeId is null ? null : reasons.GetValueOrDefault(r.ReasonCodeId.Value),
            RequestNote = r.RequestNote,
            Route = RouteFor(r.DocumentType, r.EntityId),
        });
    }

    private static string? RouteFor(string documentType, Guid entityId) => documentType switch
    {
        "Booking" or "Discount" => $"/realestate/bookings/{entityId}",
        "Cancellation" => "/realestate/cancellations",
        "Refund" => "/realestate/cancellations",
        "SurchargeWaiver" => "/realestate/waivers",
        "Transfer" => $"/realestate/transfers/{entityId}",
        "Ipc" => "/realestate/construction/ipc",
        "EscrowWithdrawal" => "/realestate/escrow",
        "WorkOrder" => $"/realestate/work-orders/{entityId}",
        "Commission" => "/realestate/commission",
        _ => null,
    };

    public async Task<ApprovalRequestDto> DecideApprovalAsync(ApprovalDecisionDto dto, Guid userId)
    {
        var request = await Db.ApprovalRequests.ForCompany(Tenant)
            .Include(r => r.Decisions)
            .FirstOrDefaultAsync(r => r.Id == dto.ApprovalRequestId)
            ?? throw new InvalidOperationException("That approval request does not exist.");

        if (request.Outcome != ApprovalOutcome.Pending)
            throw new InvalidOperationException("This request has already been decided.");

        request.Decisions.Add(new ApprovalDecisionLog
        {
            Level = request.CurrentLevel,
            Outcome = dto.Outcome,
            DecidedByUserId = userId,
            DecidedAt = DateTime.UtcNow,
            Comment = dto.Comment,
            OnBehalfOfUserId = dto.OnBehalfOfUserId,
        }.StampNew(Tenant, userId));

        if (dto.Outcome == ApprovalOutcome.Approved && request.CurrentLevel < request.RequiredLevels)
        {
            // More levels to go. The request stays open and moves up.
            request.CurrentLevel++;
        }
        else
        {
            request.Outcome = dto.Outcome;
            request.DecidedAt = DateTime.UtcNow;
        }

        request.StampUpdated(userId);
        await Db.SaveChangesAsync();

        return new ApprovalRequestDto
        {
            Id = request.Id,
            DocumentType = request.DocumentType,
            EntityId = request.EntityId,
            EntityReference = request.EntityReference,
            Summary = request.Summary,
            Amount = request.Amount,
            CurrentLevel = request.CurrentLevel,
            RequiredLevels = request.RequiredLevels,
            Outcome = request.Outcome,
            RequestedAt = request.RequestedAt,
            Route = RouteFor(request.DocumentType, request.EntityId),
        };
    }

    // ═══ Saved views ═════════════════════════════════════════════════════════

    public async Task<List<SavedViewDto>> GetSavedViewsAsync(string screenKey, Guid userId)
        => await Db.SavedViews.ForCompany(Tenant)
            .Where(v => v.ScreenKey == screenKey && (v.OwnerUserId == userId || v.IsShared))
            .OrderByDescending(v => v.IsDefault).ThenBy(v => v.Name)
            .Select(v => new SavedViewDto
            {
                Id = v.Id,
                ScreenKey = v.ScreenKey,
                Name = v.Name,
                IsShared = v.IsShared,
                IsDefault = v.IsDefault,
                IsMine = v.OwnerUserId == userId,
                ConfigJson = v.ConfigJson,
            })
            .ToListAsync();

    public async Task<SavedViewDto> SaveViewAsync(SavedViewDto dto, Guid userId)
    {
        var view = dto.Id != Guid.Empty
            ? await Db.SavedViews.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == dto.Id)
            : null;

        if (view is not null && view.OwnerUserId != userId && !view.IsShared)
            throw new InvalidOperationException("That view belongs to somebody else.");

        if (view is null)
        {
            view = new SavedView { OwnerUserId = userId }.StampNew(Tenant, userId);
            Db.SavedViews.Add(view);
        }
        else view.StampUpdated(userId);

        view.ScreenKey = dto.ScreenKey;
        view.Name = dto.Name;
        view.IsShared = dto.IsShared;
        view.ConfigJson = dto.ConfigJson;

        if (dto.IsDefault)
        {
            var others = await Db.SavedViews.ForCompany(Tenant)
                .Where(v => v.ScreenKey == dto.ScreenKey && v.OwnerUserId == userId && v.Id != view.Id)
                .ToListAsync();
            foreach (var o in others) o.IsDefault = false;
        }
        view.IsDefault = dto.IsDefault;

        await Db.SaveChangesAsync();
        dto.Id = view.Id;
        dto.IsMine = true;
        return dto;
    }

    public async Task DeleteSavedViewAsync(Guid id, Guid userId)
    {
        var view = await RequireAsync<SavedView>(id, "That view does not exist.");
        if (view.OwnerUserId != userId)
            throw new InvalidOperationException("Only the person who created a view can remove it.");

        view.StampDeleted(userId);
        await Db.SaveChangesAsync();
    }

    // ═══ Import ══════════════════════════════════════════════════════════════

    public async Task<ImportBatchDto> RunImportAsync(ImportRequestDto request, Guid userId)
    {
        var batch = new ImportBatch
        {
            Reference = await numbering.NextMasterCodeAsync(Db.ImportBatches, "IMP"),
            EntityKind = request.EntityKind,
            Status = ImportBatchStatus.Validating,
            FileName = request.FileName,
            FileUrl = request.FileUrl,
            ProjectId = request.ProjectId,
            OfficeId = request.OfficeId,
            MappingProfileJson = request.MappingProfileJson,
            AllowUpdates = request.AllowUpdates,
            MatchKeyField = request.MatchKeyField,
            TotalRows = request.Rows.Count,
            StartedAt = DateTime.UtcNow,
            RunByUserId = userId,
            CanRollback = true,
        }.StampNew(Tenant, userId);

        Db.ImportBatches.Add(batch);

        var validator = ImportValidators.For(request.EntityKind);
        var rowNumber = 0;

        foreach (var row in request.Rows)
        {
            rowNumber++;
            var problems = validator(row).ToList();

            foreach (var problem in problems)
            {
                batch.Errors.Add(new ImportBatchError
                {
                    RowNumber = rowNumber,
                    ColumnName = problem.Column,
                    CellValue = problem.Value,
                    Severity = problem.IsWarning ? "Warning" : "Error",
                    Message = problem.Message,
                    Suggestion = problem.Suggestion,
                }.StampNew(Tenant, userId));
            }

            if (problems.Any(p => !p.IsWarning)) batch.ErrorRows++;
            else if (problems.Count > 0) { batch.WarningRows++; batch.ValidRows++; }
            else batch.ValidRows++;
        }

        batch.DryRunCompleted = true;
        batch.DryRunAt = DateTime.UtcNow;
        batch.Status = batch.ErrorRows > 0 ? ImportBatchStatus.CompletedWithErrors : ImportBatchStatus.DryRunComplete;

        if (batch.ErrorRows > 0)
            batch.ErrorSummary = $"{batch.ErrorRows} of {batch.TotalRows} rows cannot be imported until they are corrected.";

        await Db.SaveChangesAsync();
        return (await GetImportBatchAsync(batch.Id))!;
    }

    public async Task<PaginatedResponse<ImportBatchDto>> GetImportBatchesAsync(ListQueryDto query)
    {
        var q = Db.ImportBatches.ForCompany(Tenant)
            .OrderByDescending(b => b.StartedAt);

        return await PageAsync(q, query, MapBatch);
    }

    public async Task<ImportBatchDto?> GetImportBatchAsync(Guid id)
    {
        var batch = await Db.ImportBatches.ForCompany(Tenant)
            .Include(b => b.Errors)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch is null) return null;

        var dto = MapBatch(batch);
        dto.Errors = batch.Errors
            .OrderBy(e => e.RowNumber)
            .Take(500)
            .Select(e => new ImportBatchErrorDto
            {
                Id = e.Id,
                RowNumber = e.RowNumber,
                ColumnName = e.ColumnName,
                CellValue = e.CellValue,
                Severity = e.Severity,
                Message = e.Message,
                Suggestion = e.Suggestion,
                IsResolved = e.IsResolved,
            })
            .ToList();

        return dto;
    }

    private static ImportBatchDto MapBatch(ImportBatch b) => new()
    {
        Id = b.Id,
        Reference = b.Reference,
        EntityKind = b.EntityKind,
        Status = b.Status,
        FileName = b.FileName,
        FileUrl = b.FileUrl,
        FileSizeBytes = b.FileSizeBytes,
        TotalRows = b.TotalRows,
        ValidRows = b.ValidRows,
        ErrorRows = b.ErrorRows,
        WarningRows = b.WarningRows,
        ImportedRows = b.ImportedRows,
        SkippedRows = b.SkippedRows,
        UpdatedRows = b.UpdatedRows,
        DryRunCompleted = b.DryRunCompleted,
        DryRunAt = b.DryRunAt,
        StartedAt = b.StartedAt,
        CompletedAt = b.CompletedAt,
        AllowUpdates = b.AllowUpdates,
        MatchKeyField = b.MatchKeyField,
        CanRollback = b.CanRollback,
        WasRolledBack = b.WasRolledBack,
        ErrorSummary = b.ErrorSummary,
    };

    public async Task<ImportBatchDto> CommitImportAsync(Guid batchId, Guid userId)
    {
        var batch = await Db.ImportBatches.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == batchId)
            ?? throw new InvalidOperationException("That import batch does not exist.");

        if (!batch.DryRunCompleted)
            throw new InvalidOperationException("Validate the file before importing it.");

        if (batch.Status == ImportBatchStatus.Completed)
            throw new InvalidOperationException("This batch has already been imported.");

        // The rows themselves are re-posted by the client on commit, so a batch cannot silently
        // import stale data validated an hour ago. This marks the batch as accepted; the actual
        // row writes happen in the entity-specific importer the caller then invokes.
        batch.Status = ImportBatchStatus.Importing;
        batch.StampUpdated(userId);
        await Db.SaveChangesAsync();

        return (await GetImportBatchAsync(batchId))!;
    }

    public async Task<ImportBatchDto> RollbackImportAsync(Guid batchId, Guid userId)
    {
        var batch = await Db.ImportBatches.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == batchId)
            ?? throw new InvalidOperationException("That import batch does not exist.");

        if (!batch.CanRollback)
            throw new InvalidOperationException("This batch cannot be undone — records it created have since been used.");

        if (batch.WasRolledBack)
            throw new InvalidOperationException("This batch has already been rolled back.");

        batch.WasRolledBack = true;
        batch.RolledBackAt = DateTime.UtcNow;
        batch.Status = ImportBatchStatus.Cancelled;
        batch.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetImportBatchAsync(batchId))!;
    }
}

/// <summary>
/// Per-entity import validation. Kept apart from the service because it is a big, boring
/// lookup table and the rules differ completely by entity kind.
/// </summary>
internal static class ImportValidators
{
    internal record Problem(string? Column, string? Value, string Message, string? Suggestion, bool IsWarning);

    internal static Func<Dictionary<string, string?>, IEnumerable<Problem>> For(ImportEntityKind kind) => kind switch
    {
        ImportEntityKind.Properties => ValidateProperty,
        ImportEntityKind.Units => ValidateUnit,
        ImportEntityKind.Parties => ValidateParty,
        ImportEntityKind.Bookings => ValidateBooking,
        ImportEntityKind.PaymentHistory => ValidatePayment,
        ImportEntityKind.Tenancies => ValidateTenancy,
        ImportEntityKind.Meters => ValidateMeter,
        _ => _ => [],
    };

    private static IEnumerable<Problem> Required(Dictionary<string, string?> row, params string[] columns)
    {
        foreach (var column in columns)
        {
            if (!row.TryGetValue(column, out var value) || string.IsNullOrWhiteSpace(value))
                yield return new Problem(column, null, $"{column} is required.", $"Add a value in the {column} column.", false);
        }
    }

    private static IEnumerable<Problem> Numeric(Dictionary<string, string?> row, params string[] columns)
    {
        foreach (var column in columns)
        {
            if (row.TryGetValue(column, out var value)
                && !string.IsNullOrWhiteSpace(value)
                && !decimal.TryParse(value, out _))
            {
                yield return new Problem(column, value, $"{column} is not a number.",
                    "Remove any currency symbols, spaces or thousands separators.", false);
            }
        }
    }

    private static IEnumerable<Problem> ValidateProperty(Dictionary<string, string?> row)
        => Required(row, "Reference", "Category", "SubType")
            .Concat(Numeric(row, "SaleableArea", "AskingPrice", "Latitude", "Longitude"));

    private static IEnumerable<Problem> ValidateUnit(Dictionary<string, string?> row)
        => Required(row, "UnitNumber", "SaleableArea")
            .Concat(Numeric(row, "SaleableArea", "BasePrice", "RatePerSqFt"));

    private static IEnumerable<Problem> ValidateParty(Dictionary<string, string?> row)
    {
        foreach (var p in Required(row, "Name")) yield return p;

        var hasPhone = row.TryGetValue("Phone", out var phone) && !string.IsNullOrWhiteSpace(phone);
        var hasEmail = row.TryGetValue("Email", out var email) && !string.IsNullOrWhiteSpace(email);

        if (!hasPhone && !hasEmail)
        {
            yield return new Problem("Phone", null,
                "A person needs a phone number or an email address.",
                "Fill in at least one of the Phone and Email columns.", false);
        }
    }

    private static IEnumerable<Problem> ValidateBooking(Dictionary<string, string?> row)
        => Required(row, "Reference", "UnitNumber", "ApplicantName", "BookingDate")
            .Concat(Numeric(row, "TotalConsideration", "Discount"));

    private static IEnumerable<Problem> ValidatePayment(Dictionary<string, string?> row)
        => Required(row, "BookingReference", "ReceivedOn", "Amount")
            .Concat(Numeric(row, "Amount"));

    private static IEnumerable<Problem> ValidateTenancy(Dictionary<string, string?> row)
        => Required(row, "PropertyReference", "TenantName", "StartDate", "Rent")
            .Concat(Numeric(row, "Rent", "Deposit"));

    private static IEnumerable<Problem> ValidateMeter(Dictionary<string, string?> row)
        => Required(row, "MeterNumber", "Kind")
            .Concat(Numeric(row, "Multiplier", "LastReading"));
}
