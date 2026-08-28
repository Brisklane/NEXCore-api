using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Listings and the agency instructions behind them.
///
/// The publish gate is the heart of this file. Advertising a property without the certificates a
/// market requires is a fine in several jurisdictions and a reputational problem in all of them,
/// so publishing is refused with a named list of what is missing rather than allowed and audited
/// afterwards. Price changes are likewise never a silent field update — they write history with
/// the viewing count at the old price attached, which is the only way to tell whether the
/// reduction worked.
/// </summary>
public partial class ListingService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), IListingService
{
    public async Task<PaginatedResponse<ListingListItemDto>> GetListingsAsync(ListingSearchDto query)
    {
        var propertyIds = query.GeoAreaId is not null || query.SubTypes.Count > 0 || query.MinBedrooms is not null
            ? Db.Properties.ForCompany(Tenant)
                .WhereIf(query.GeoAreaId.HasValue, p => p.GeoAreaId == query.GeoAreaId)
                .WhereIf(query.SubTypes.Count > 0, p => query.SubTypes.Contains(p.SubType))
                .WhereIf(query.MinBedrooms.HasValue, p => p.Bedrooms >= query.MinBedrooms)
                .Select(p => p.Id)
            : null;

        var q = Db.Listings.ForCompany(Tenant)
            .WhereIf(query.Kinds.Count > 0, l => query.Kinds.Contains(l.Kind))
            .WhereIf(query.Statuses.Count > 0, l => query.Statuses.Contains(l.Status))
            .WhereIf(query.AgentId.HasValue, l => l.ListingAgentId == query.AgentId)
            .WhereIf(query.OfficeId.HasValue, l => l.OfficeId == query.OfficeId)
            .WhereIf(query.MinPrice.HasValue, l => l.AskingPrice >= query.MinPrice)
            .WhereIf(query.MaxPrice.HasValue, l => l.AskingPrice <= query.MaxPrice)
            .WhereIf(propertyIds is not null, l => propertyIds!.Contains(l.PropertyId))
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                l => l.Reference.Contains(query.Search!) || (l.Headline != null && l.Headline.Contains(query.Search!)));

        if (query.PortalChannelId is not null || query.PublishState is not null)
        {
            var published = Db.PortalPublications.ForCompany(Tenant)
                .WhereIf(query.PortalChannelId.HasValue, p => p.PortalChannelId == query.PortalChannelId)
                .WhereIf(query.PublishState.HasValue, p => p.State == query.PublishState)
                .Select(p => p.ListingId);

            q = q.Where(l => published.Contains(l.Id));
        }

        // "Stale" means live, and nobody has been through the door in three weeks at this price.
        if (query.StaleOnly == true)
        {
            var cutoff = DateTime.UtcNow.AddDays(-21);

            q = q.Where(l => l.Status == ListingStatus.Live
                          && (l.LastViewingAt == null || l.LastViewingAt < cutoff));
        }

        var ordered = query.SortBy switch
        {
            "price" => query.SortDescending ? q.OrderByDescending(l => l.AskingPrice) : q.OrderBy(l => l.AskingPrice),
            "days" => query.SortDescending ? q.OrderByDescending(l => l.ListedOn) : q.OrderBy(l => l.ListedOn),
            "viewings" => q.OrderByDescending(l => l.ViewingCount),
            _ => q.OrderByDescending(l => l.IsFeatured).ThenByDescending(l => l.ListedOn),
        };

        return await PageAsync(ordered, query, MapListingListAsync);
    }

    private async Task<List<ListingListItemDto>> MapListingListAsync(List<Listing> listings)
    {
        if (listings.Count == 0) return [];

        var unit = await AreaUnitAsync();
        var today = Today;
        var ids = listings.Select(l => l.Id).ToList();
        var propertyIds = listings.Select(l => l.PropertyId).Distinct().ToList();

        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var heroes = await Db.PropertyMedia.ForCompany(Tenant)
            .Where(m => propertyIds.Contains(m.PropertyId) && m.IsHero)
            .ToDictionaryAsync(m => m.PropertyId, m => m.Url);

        var agents = await AgentNamesAsync(listings.Select(l => l.ListingAgentId));

        var instructionIds = listings.Where(l => l.InstructionId.HasValue)
            .Select(l => l.InstructionId!.Value).Distinct().ToList();

        var instructions = instructionIds.Count == 0
            ? []
            : await Db.Instructions.ForCompany(Tenant)
                .Where(i => instructionIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Basis);

        var publications = await Db.PortalPublications.ForCompany(Tenant)
            .Where(p => ids.Contains(p.ListingId))
            .Select(p => new { p.ListingId, p.State })
            .ToListAsync();

        // Last price change, so the board can show how long the current price has been in place.
        var lastChange = await Db.ListingPriceHistories.ForCompany(Tenant)
            .Where(h => ids.Contains(h.ListingId))
            .GroupBy(h => h.ListingId)
            .Select(g => new { ListingId = g.Key, ChangedOn = g.Max(x => x.ChangedOn) })
            .ToDictionaryAsync(x => x.ListingId, x => x.ChangedOn);

        return listings.Select(l =>
        {
            properties.TryGetValue(l.PropertyId, out var property);
            var mine = publications.Where(p => p.ListingId == l.Id).ToList();

            var priceSince = lastChange.TryGetValue(l.Id, out var changed) ? changed : l.ListedOn;

            return new ListingListItemDto
            {
                Id = l.Id,
                Reference = l.Reference,
                PropertyId = l.PropertyId,
                PropertyReference = property?.Reference,
                Kind = l.Kind,
                Status = l.Status,
                Headline = l.Headline,
                AddressOneLine = property is null ? "—" : RealEstateMapper.OneLineAddress(property),
                HeroImageUrl = heroes.GetValueOrDefault(l.PropertyId),
                SubType = property?.SubType ?? PropertySubType.Apartment,
                Area = RealEstateMapper.AreaOrNull(property?.SaleableAreaSqFt, unit),
                Bedrooms = property?.Bedrooms,
                Bathrooms = property?.Bathrooms,
                AskingPrice = l.AskingPrice,
                PriceOnApplication = l.PriceOnApplication,
                RentFrequency = l.RentFrequency,
                CurrencyCode = l.CurrencyCode,
                ListingAgentName = l.ListingAgentId is null ? null : agents.GetValueOrDefault(l.ListingAgentId.Value),
                AgencyBasis = l.InstructionId is null ? null : instructions.GetValueOrDefault(l.InstructionId.Value),
                ListedOn = l.ListedOn,
                ExpiresOn = l.ExpiresOn,
                DaysOnMarket = l.DaysOnMarket ?? (l.ListedOn is null ? null : today.DayNumber - l.ListedOn.Value.DayNumber),
                ViewCount = l.ViewCount,
                EnquiryCount = l.EnquiryCount,
                ViewingCount = l.ViewingCount,
                OfferCount = l.OfferCount,
                LastViewingAt = l.LastViewingAt,
                DaysSinceLastViewing = l.LastViewingAt is null
                    ? (priceSince is null ? null : today.DayNumber - priceSince.Value.DayNumber)
                    : (int)(DateTime.UtcNow - l.LastViewingAt.Value).TotalDays,
                PublishedPortalCount = mine.Count(p => p.State is PortalPublishState.Published or PortalPublishState.PublishedWithWarnings),
                FailedPortalCount = mine.Count(p => p.State == PortalPublishState.Failed),
                IsFeatured = l.IsFeatured,
            };
        }).ToList();
    }

    protected async Task<Dictionary<Guid, string>> AgentNamesAsync(IEnumerable<Guid?> agentIds)
    {
        var ids = agentIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.DisplayName);
    }

    public async Task<ListingDetailDto?> GetListingAsync(Guid id)
    {
        var listing = await Db.Listings.ForCompany(Tenant)
            .Include(l => l.PriceHistory)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing is null) return null;

        var head = (await MapListingListAsync([listing]))[0];

        var detail = new ListingDetailDto
        {
            Id = head.Id,
            Reference = head.Reference,
            PropertyId = head.PropertyId,
            PropertyReference = head.PropertyReference,
            Kind = head.Kind,
            Status = head.Status,
            Headline = head.Headline,
            AddressOneLine = head.AddressOneLine,
            HeroImageUrl = head.HeroImageUrl,
            SubType = head.SubType,
            Area = head.Area,
            Bedrooms = head.Bedrooms,
            Bathrooms = head.Bathrooms,
            AskingPrice = head.AskingPrice,
            PriceOnApplication = head.PriceOnApplication,
            RentFrequency = head.RentFrequency,
            CurrencyCode = head.CurrencyCode,
            ListingAgentName = head.ListingAgentName,
            AgencyBasis = head.AgencyBasis,
            ListedOn = head.ListedOn,
            ExpiresOn = head.ExpiresOn,
            DaysOnMarket = head.DaysOnMarket,
            ViewCount = head.ViewCount,
            EnquiryCount = head.EnquiryCount,
            ViewingCount = head.ViewingCount,
            OfferCount = head.OfferCount,
            LastViewingAt = head.LastViewingAt,
            DaysSinceLastViewing = head.DaysSinceLastViewing,
            PublishedPortalCount = head.PublishedPortalCount,
            FailedPortalCount = head.FailedPortalCount,
            IsFeatured = head.IsFeatured,

            InstructionId = listing.InstructionId,
            OfficeId = listing.OfficeId,
            MinPrice = listing.MinPrice,
            MaxPrice = listing.MaxPrice,
            ServiceChargeAmount = listing.ServiceChargeAmount,
            AvailableFrom = listing.AvailableFrom,
            VacantPossession = listing.VacantPossession,
            TenantInSitu = listing.TenantInSitu,
            NoticeRequiredDays = listing.NoticeRequiredDays,
            IsChainFree = listing.IsChainFree,
            ShortDescription = listing.ShortDescription,
            LongDescription = listing.LongDescription,
            KeyFeatures = string.IsNullOrWhiteSpace(listing.KeyFeatures)
                ? []
                : listing.KeyFeatures.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            EnergyRating = listing.EnergyRating,
            CouncilTaxBand = listing.CouncilTaxBand,
            LanguageCode = listing.LanguageCode,
            RegulatoryPermitNumber = listing.RegulatoryPermitNumber,
            AllowPortalPublish = listing.AllowPortalPublish,
            AllowWebsitePublish = listing.AllowWebsitePublish,
            PublishedAt = listing.PublishedAt,
            WithdrawnAt = listing.WithdrawnAt,
        };

        var reasons = await ReasonLabelsAsync(listing.PriceHistory.Select(h => h.ReasonCodeId));
        var changers = await AgentUserNamesAsync(listing.PriceHistory.Select(h => (Guid?)h.ChangedByUserId));

        detail.PriceHistory = listing.PriceHistory
            .OrderByDescending(h => h.ChangedOn)
            .Select(h => new ListingPriceHistoryDto
            {
                Id = h.Id,
                FromPrice = h.FromPrice,
                ToPrice = h.ToPrice,
                ChangePercent = h.FromPrice is > 0m ? RealEstateMapper.Percent(h.ToPrice - h.FromPrice.Value, h.FromPrice.Value) : 0m,
                ChangedOn = h.ChangedOn,
                ChangedByName = changers.GetValueOrDefault(h.ChangedByUserId),
                Reason = h.ReasonCodeId is null ? null : reasons.GetValueOrDefault(h.ReasonCodeId.Value),
                Note = h.Note,
                ViewingsAtPreviousPrice = h.ViewingsAtPreviousPrice,
            })
            .ToList();

        detail.Publications = await GetPublicationsForListingAsync(id);

        if (listing.InstructionId is not null)
            detail.Instruction = await GetInstructionAsync(listing.InstructionId.Value);

        detail.Media = await Db.PropertyMedia.ForCompany(Tenant)
            .Where(m => m.PropertyId == listing.PropertyId)
            .OrderByDescending(m => m.IsHero).ThenBy(m => m.SortOrder)
            .Select(m => new PropertyMediaDto
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
            })
            .ToListAsync();

        var gate = await CheckPublishReadinessAsync(id);
        detail.PublishChecklist = gate.Failures;
        detail.CanPublish = gate.Passed;

        return detail;
    }

    protected async Task<Dictionary<Guid, string>> AgentUserNamesAsync(IEnumerable<Guid?> userIds)
    {
        var ids = userIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];

        return await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => a.UserId != null && ids.Contains(a.UserId.Value))
            .ToDictionaryAsync(a => a.UserId!.Value, a => a.DisplayName);
    }

    public async Task<ListingDetailDto> SaveListingAsync(ListingUpsertDto dto, Guid userId)
    {
        var property = await RequireAsync<Property>(dto.PropertyId, "That property does not exist.");

        var listing = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.Listings.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == dto.Id)
            : null;

        if (listing is null)
        {
            // The same property listed twice for the same purpose is a duplicate advertisement,
            // and portals reject the pair rather than one of them.
            var live = await Db.Listings.ForCompany(Tenant)
                .Where(l => l.PropertyId == dto.PropertyId
                         && l.Kind == dto.Kind
                         && l.Status != ListingStatus.Withdrawn
                         && l.Status != ListingStatus.Expired
                         && l.Status != ListingStatus.Completed)
                .Select(l => l.Reference)
                .FirstOrDefaultAsync();

            if (live is not null)
                throw new InvalidOperationException($"{live} already offers this property {Describe(dto.Kind)}. Edit that listing instead.");

            listing = new Listing
            {
                PropertyId = dto.PropertyId,
                Reference = await numbering.NextListingNumberAsync(DateTime.UtcNow),
                Status = ListingStatus.Draft,
                ListedOn = dto.ListedOn ?? Today,
            }.StampNew(Tenant, userId);

            Db.Listings.Add(listing);
        }
        else listing.StampUpdated(userId);

        listing.Kind = dto.Kind;
        listing.InstructionId = dto.InstructionId;
        listing.ListingAgentId = dto.ListingAgentId;
        listing.OfficeId = dto.OfficeId;

        // A price change on a live listing goes through ChangePriceAsync so it writes history.
        if (listing.Status == ListingStatus.Draft || listing.AskingPrice is null)
            listing.AskingPrice = dto.AskingPrice;

        listing.MinPrice = dto.MinPrice;
        listing.MaxPrice = dto.MaxPrice;
        listing.PriceOnApplication = dto.PriceOnApplication;
        listing.RentFrequency = dto.RentFrequency;
        listing.ServiceChargeAmount = dto.ServiceChargeAmount;
        listing.CurrencyCode = dto.CurrencyCode ?? property.CurrencyCode ?? listing.CurrencyCode;

        listing.AvailableFrom = dto.AvailableFrom;
        listing.VacantPossession = dto.VacantPossession;
        listing.TenantInSitu = dto.TenantInSitu;
        listing.NoticeRequiredDays = dto.NoticeRequiredDays;
        listing.IsChainFree = dto.IsChainFree;

        listing.Headline = dto.Headline;
        listing.ShortDescription = dto.ShortDescription;
        listing.LongDescription = dto.LongDescription;
        listing.KeyFeatures = dto.KeyFeatures.Count == 0 ? null : string.Join('\n', dto.KeyFeatures);
        listing.EnergyRating = dto.EnergyRating;
        listing.CouncilTaxBand = dto.CouncilTaxBand;
        listing.LanguageCode = dto.LanguageCode;
        listing.RegulatoryPermitNumber = dto.RegulatoryPermitNumber;

        listing.ListedOn = dto.ListedOn ?? listing.ListedOn;
        listing.ExpiresOn = dto.ExpiresOn;
        listing.IsFeatured = dto.IsFeatured;
        listing.AllowPortalPublish = dto.AllowPortalPublish;
        listing.AllowWebsitePublish = dto.AllowWebsitePublish;

        await Db.SaveChangesAsync();
        return (await GetListingAsync(listing.Id))!;
    }

    private static string Describe(ListingKind kind) => kind switch
    {
        ListingKind.ForSale => "for sale",
        ListingKind.ForRent => "for rent",
        ListingKind.ForLease => "for lease",
        ListingKind.ForAuction => "at auction",
        ListingKind.OffPlan => "off plan",
        ListingKind.Resale => "as a resale",
        ListingKind.Exchange => "for exchange",
        _ => "as wanted",
    };

    public async Task<ListingDetailDto> ChangeStatusAsync(Guid id, ListingStatus status, Guid? reasonCodeId, Guid userId)
    {
        var listing = await RequireAsync<Listing>(id, "That listing does not exist.");
        var today = Today;

        if (status == ListingStatus.Live)
        {
            var gate = await CheckPublishReadinessAsync(id);

            if (!gate.Passed)
                throw new InvalidOperationException(gate.Message ?? "This listing is not ready to go live.");

            listing.ListedOn ??= today;
            listing.PublishedAt = DateTime.UtcNow;
            listing.PublishedByUserId = userId;
            listing.WithdrawnAt = null;
        }

        if (status is ListingStatus.Withdrawn or ListingStatus.Expired)
        {
            listing.WithdrawnAt = DateTime.UtcNow;
            listing.WithdrawnReasonCodeId = reasonCodeId;

            // Pull it off every portal it is live on, rather than leaving orphaned adverts.
            var live = await Db.PortalPublications.ForCompany(Tenant)
                .Where(p => p.ListingId == id
                         && (p.State == PortalPublishState.Published || p.State == PortalPublishState.PublishedWithWarnings))
                .ToListAsync();

            foreach (var publication in live)
            {
                publication.State = PortalPublishState.WithdrawPending;
                publication.StampUpdated(userId);
            }
        }

        // Days on market freezes at completion, so the statistic stays honest afterwards.
        if (status is ListingStatus.Completed or ListingStatus.Withdrawn && listing.ListedOn is not null)
            listing.DaysOnMarket ??= today.DayNumber - listing.ListedOn.Value.DayNumber;

        listing.Status = status;
        listing.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetListingAsync(id))!;
    }

    /// <summary>
    /// Moves the price and records why. The viewing count at the old price is captured on the
    /// history row — three weeks and no viewings is the evidence that wins the conversation with
    /// a vendor about a reduction.
    /// </summary>
    public async Task<ListingDetailDto> ChangePriceAsync(ListingPriceChangeDto dto, Guid userId)
    {
        var listing = await RequireAsync<Listing>(dto.ListingId, "That listing does not exist.");

        if (dto.NewPrice <= 0m)
            throw new InvalidOperationException("Enter a price above zero, or mark the listing price-on-application.");

        if (listing.AskingPrice == dto.NewPrice)
            throw new InvalidOperationException("That is the price it already carries.");

        var previous = listing.AskingPrice;
        var lastChange = await Db.ListingPriceHistories.ForCompany(Tenant)
            .Where(h => h.ListingId == dto.ListingId)
            .OrderByDescending(h => h.ChangedOn)
            .Select(h => (DateOnly?)h.ChangedOn)
            .FirstOrDefaultAsync() ?? listing.ListedOn;

        var viewingsAtOldPrice = lastChange is null
            ? listing.ViewingCount
            : await Db.ViewingProperties.ForCompany(Tenant)
                .CountAsync(v => v.ListingId == dto.ListingId && v.WasSeen);

        Db.ListingPriceHistories.Add(new ListingPriceHistory
        {
            ListingId = dto.ListingId,
            FromPrice = previous,
            ToPrice = dto.NewPrice,
            ChangedOn = Today,
            ChangedByUserId = userId,
            ReasonCodeId = dto.ReasonCodeId,
            Note = dto.Note,
            ViewingsAtPreviousPrice = viewingsAtOldPrice,
        }.StampNew(Tenant, userId));

        listing.AskingPrice = dto.NewPrice;
        listing.StampUpdated(userId);

        // A live listing whose price moved has to be pushed again or the portals show the old one.
        var published = await Db.PortalPublications.ForCompany(Tenant)
            .Where(p => p.ListingId == dto.ListingId
                     && (p.State == PortalPublishState.Published || p.State == PortalPublishState.PublishedWithWarnings))
            .ToListAsync();

        foreach (var publication in published)
        {
            publication.State = PortalPublishState.UpdatePending;
            publication.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        // Everyone whose requirement now matches hears about it. A reduction's best lead source is
        // the people who already looked and thought it too expensive.
        if (dto.NotifyMatchedApplicants && previous is not null && dto.NewPrice < previous)
            await NotifyPriceDropAsync(listing, previous.Value, dto.NewPrice, userId);

        return (await GetListingAsync(dto.ListingId))!;
    }

    private async Task NotifyPriceDropAsync(Listing listing, decimal from, decimal to, Guid userId)
    {
        var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == listing.PropertyId);
        if (property is null) return;

        // Anyone whose budget covers the new price but did not cover the old one.
        var matches = await Db.RequirementProfiles.ForCompany(Tenant)
            .Where(r => r.IsActive && r.AlertsEnabled
                     && r.BudgetMax >= to && r.BudgetMax < from
                     && (r.MinBedrooms == null || property.Bedrooms >= r.MinBedrooms))
            .Select(r => new { r.PartyId, r.EnquiryId })
            .Take(200)
            .ToListAsync();

        var drop = RealEstateMapper.Percent(from - to, from);
        var address = RealEstateMapper.OneLineAddress(property);

        foreach (var match in matches)
        {
            await QueueNotificationAsync(
                "ListingPriceDrop",
                $"Price reduced by {drop:N1}%",
                $"{address} is now {to:N0}, down from {from:N0}.",
                $"/realestate/listings/{listing.Id}",
                recipientPartyId: match.PartyId,
                entityType: "Listing",
                entityId: listing.Id);
        }
    }

    /// <summary>
    /// Everything a market requires before an advertisement is lawful, and everything a portal
    /// requires before it will accept the feed. Reported as a list of named, fixable failures.
    /// </summary>
    public async Task<GateResultDto> CheckPublishReadinessAsync(Guid listingId)
    {
        var listing = await Db.Listings.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == listingId)
            ?? throw new InvalidOperationException("That listing does not exist.");

        var settings = await SettingsAsync();
        var today = Today;
        var failures = new List<ChecklistItemDto>();
        var order = 0;

        void Check(string key, string label, bool satisfied, bool mandatory = true, string? note = null)
        {
            if (satisfied) return;

            failures.Add(new ChecklistItemDto
            {
                Key = key,
                Label = label,
                IsSatisfied = false,
                IsMandatory = mandatory,
                Note = note,
                SortOrder = order += 10,
            });
        }

        Check("headline", "Give the listing a headline", !string.IsNullOrWhiteSpace(listing.Headline));
        Check("description", "Write a description of at least 100 characters",
            (listing.LongDescription ?? listing.ShortDescription ?? "").Trim().Length >= 100);

        Check("price", "Set an asking price, or mark it price-on-application",
            listing.AskingPrice is > 0m || listing.PriceOnApplication);

        var photos = await Db.PropertyMedia.ForCompany(Tenant)
            .CountAsync(m => m.PropertyId == listing.PropertyId && m.Kind == MediaKind.Photo && !m.ExcludeFromPortals);

        var minimum = Math.Max(1, settings.MinimumListingPhotos);

        Check("photos", $"Add at least {minimum} photographs", photos >= minimum,
            note: photos == 0 ? "No photographs are attached." : $"{photos} of {minimum} attached.");

        var hasHero = await Db.PropertyMedia.ForCompany(Tenant)
            .AnyAsync(m => m.PropertyId == listing.PropertyId && m.IsHero);

        Check("hero", "Pick the main photograph", hasHero);

        // Instruction. Advertising somebody's property without their signed authority is the
        // single most common regulatory complaint against an agency.
        if (listing.InstructionId is null)
        {
            Check("instruction", "Attach the signed agency instruction", false,
                note: "No instruction is linked to this listing.");
        }
        else
        {
            var instruction = await Db.Instructions.ForCompany(Tenant)
                .FirstOrDefaultAsync(i => i.Id == listing.InstructionId);

            Check("instruction-signed", "Have the owner sign the agency agreement", instruction?.SignedOn is not null);

            Check("instruction-live", "Renew the agency instruction — it has lapsed",
                instruction is not null && instruction.TerminatedOn is null
                    && (instruction.ExpiresOn is null || instruction.ExpiresOn >= today));
        }

        // Certificates the market requires on an advertisement.
        if (settings.RequireEnergyCertificateToPublish)
        {
            var epc = await Db.ComplianceCertificates.ForCompany(Tenant)
                .AnyAsync(c => c.PropertyId == listing.PropertyId
                            && c.Kind == ComplianceCertificateKind.EnergyPerformance
                            && c.IsCurrent && c.ExpiresOn >= today);

            Check("epc", "Attach a valid energy performance certificate", epc);
        }

        if (settings.RequirePermitNumberToPublish)
        {
            Check("permit", "Enter the advertising permit number this market requires",
                !string.IsNullOrWhiteSpace(listing.RegulatoryPermitNumber));
        }

        var property = await Db.Properties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == listing.PropertyId);

        if (property is not null)
        {
            Check("area", "Record the property's area", property.SaleableAreaSqFt is > 0m || property.PlotAreaSqFt is > 0m);
            Check("address", "Complete the address", !string.IsNullOrWhiteSpace(property.AddressLine1) || property.GeoAreaId is not null);

            // A charge that blocks a transaction should stop the advertisement too.
            var blocking = await Db.Encumbrances.ForCompany(Tenant)
                .AnyAsync(e => e.PropertyId == listing.PropertyId
                            && e.Status == EncumbranceStatus.Active
                            && e.BlocksTransaction);

            Check("encumbrance", "Clear or disclose the charge registered against this property", !blocking,
                note: "An active charge blocks transactions on this property.");

            Check("litigation", "Resolve or disclose the litigation recorded against this property",
                !property.HasLitigation, mandatory: false);
        }

        var mandatoryFailures = failures.Count(f => f.IsMandatory);

        return new GateResultDto
        {
            Passed = mandatoryFailures == 0,
            Message = mandatoryFailures == 0
                ? null
                : mandatoryFailures == 1
                    ? failures.First(f => f.IsMandatory).Label + " before publishing."
                    : $"{mandatoryFailures} things are missing before this can be published.",
            Failures = failures,
            CanOverride = false,
        };
    }
}
