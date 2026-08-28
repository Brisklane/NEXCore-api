using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Instructions, portal syndication, and marketing.
///
/// Syndication is deliberately a queue rather than a live call. A portal being slow or down must
/// never make an agent's save fail, and a firm pushing 400 listings must not be blocked for the
/// twenty minutes that takes. So <see cref="PublishAsync"/> validates, checks the plan quota and
/// records intent; a background worker does the talking and writes back what happened. Every
/// failure keeps the portal's own error message, because "publish failed" helps nobody.
/// </summary>
public partial class ListingService
{
    // ═══ Instructions ════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<InstructionDto>> GetInstructionsAsync(ListQueryDto query)
    {
        var q = Db.Instructions.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), i => i.Reference.Contains(query.Search!))
            .WhereIf(query.OfficeId.HasValue, i => i.OfficeId == query.OfficeId)
            .WhereIf(!query.IncludeInactive, i => i.TerminatedOn == null)
            .OrderByDescending(i => i.InstructedOn);

        return await PageAsync(q, query, MapInstructionsAsync);
    }

    private async Task<List<InstructionDto>> MapInstructionsAsync(List<Instruction> instructions)
    {
        if (instructions.Count == 0) return [];

        var today = Today;
        var ids = instructions.Select(i => i.Id).ToList();
        var propertyIds = instructions.Select(i => i.PropertyId).Distinct().ToList();

        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var owners = await PartyNamesAsync(instructions.Where(i => i.OwnerPartyId.HasValue).Select(i => i.OwnerPartyId!.Value));
        var agents = await AgentNamesAsync(instructions.Select(i => i.AgentId));
        var reasons = await ReasonLabelsAsync(instructions.Select(i => i.TerminationReasonCodeId));

        var listingCounts = await Db.Listings.ForCompany(Tenant)
            .Where(l => l.InstructionId != null && ids.Contains(l.InstructionId.Value))
            .GroupBy(l => l.InstructionId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count(), Price = g.Max(x => x.AskingPrice) })
            .ToDictionaryAsync(x => x.Id, x => x);

        return instructions.Select(i =>
        {
            properties.TryGetValue(i.PropertyId, out var property);
            var listings = listingCounts.GetValueOrDefault(i.Id);

            return new InstructionDto
            {
                Id = i.Id,
                Reference = i.Reference,
                PropertyId = i.PropertyId,
                PropertyReference = property?.Reference,
                AddressOneLine = property is null ? "—" : RealEstateMapper.OneLineAddress(property),
                LandlordId = i.LandlordId,
                OwnerPartyId = i.OwnerPartyId,
                OwnerName = i.OwnerPartyId is null ? null : owners.GetValueOrDefault(i.OwnerPartyId.Value),
                AgentName = i.AgentId is null ? null : agents.GetValueOrDefault(i.AgentId.Value),

                Basis = i.Basis,
                Kind = i.Kind,
                InstructedOn = i.InstructedOn,
                ExpiresOn = i.ExpiresOn,
                NoticePeriodDays = i.NoticePeriodDays,
                TailPeriodDays = i.TailPeriodDays,

                FeeBasis = i.FeeBasis,
                FeePercent = i.FeePercent,
                FeeFixedAmount = i.FeeFixedAmount,
                MinimumFee = i.MinimumFee,
                FeePeriodsOfRent = i.FeePeriodsOfRent,
                FeeIncludesTax = i.FeeIncludesTax,
                TaxPercent = i.TaxPercent,
                WithdrawalFee = i.WithdrawalFee,
                MarketingBudget = i.MarketingBudget,
                ManagementService = i.ManagementService,

                DocumentUrl = i.DocumentUrl,
                SignedOn = i.SignedOn,
                IsActive = i.TerminatedOn is null && (i.ExpiresOn is null || i.ExpiresOn >= today),
                TerminatedOn = i.TerminatedOn,
                TerminationReason = i.TerminationReasonCodeId is null ? null : reasons.GetValueOrDefault(i.TerminationReasonCodeId.Value),
                DaysToExpiry = i.ExpiresOn is null ? null : i.ExpiresOn.Value.DayNumber - today.DayNumber,
                ListingCount = listings?.Count ?? 0,
                EstimatedFee = EstimateFee(i, listings?.Price ?? property?.AskingPrice, property?.MonthlyRent),
            };
        }).ToList();
    }

    /// <summary>
    /// What this instruction is worth if it completes at today's asking price. Shown on the list
    /// because an agency's pipeline is its instruction book, not its listing count.
    /// </summary>
    private static decimal? EstimateFee(Instruction i, decimal? price, decimal? monthlyRent)
    {
        decimal? gross = i.FeeBasis switch
        {
            FeeBasis.PercentOfPrice => price is null ? null : price * i.FeePercent / 100m,
            FeeBasis.FixedAmount => i.FeeFixedAmount,
            FeeBasis.PeriodsOfRent => monthlyRent is null || i.FeePeriodsOfRent is null
                ? null
                // Periods are weeks in most letting markets; a month is 52/12 weeks of rent.
                : monthlyRent * i.FeePeriodsOfRent * 12m / 52m,
            _ => i.FeeFixedAmount > 0m ? i.FeeFixedAmount : (price is null ? null : price * i.FeePercent / 100m),
        };

        if (gross is null) return null;

        var fee = Math.Max(gross.Value, i.MinimumFee);
        if (!i.FeeIncludesTax && i.TaxPercent > 0m) fee += fee * i.TaxPercent / 100m;

        return RealEstateMapper.Money(fee);
    }

    public async Task<InstructionDto?> GetInstructionAsync(Guid id)
    {
        var instruction = await Db.Instructions.ForCompany(Tenant).FirstOrDefaultAsync(i => i.Id == id);
        if (instruction is null) return null;

        return (await MapInstructionsAsync([instruction]))[0];
    }

    public async Task<InstructionDto> SaveInstructionAsync(InstructionUpsertDto dto, Guid userId)
    {
        _ = await RequireAsync<Property>(dto.PropertyId, "That property does not exist.");

        var instruction = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.Instructions.ForCompany(Tenant).FirstOrDefaultAsync(i => i.Id == dto.Id)
            : null;

        if (instruction is null)
        {
            // Sole agency means sole. Two live sole instructions on one property is a claim from
            // whichever agent loses the fee, and it is worth refusing at the point of entry.
            if (dto.Basis is AgencyBasis.SoleAgency or AgencyBasis.SoleSellingRights)
            {
                var clash = await Db.Instructions.ForCompany(Tenant)
                    .Where(i => i.PropertyId == dto.PropertyId
                             && i.Kind == dto.Kind
                             && i.TerminatedOn == null
                             && (i.ExpiresOn == null || i.ExpiresOn >= Today))
                    .Select(i => i.Reference)
                    .FirstOrDefaultAsync();

                if (clash is not null)
                    throw new InvalidOperationException($"{clash} is still live on this property. End it before taking a sole instruction.");
            }

            instruction = new Instruction
            {
                PropertyId = dto.PropertyId,
                Reference = await numbering.NextInstructionNumberAsync(DateTime.UtcNow),
            }.StampNew(Tenant, userId);

            Db.Instructions.Add(instruction);
        }
        else instruction.StampUpdated(userId);

        instruction.LandlordId = dto.LandlordId;
        instruction.OwnerPartyId = dto.OwnerPartyId;
        instruction.OfficeId = dto.OfficeId;
        instruction.AgentId = dto.AgentId;
        instruction.Basis = dto.Basis;
        instruction.Kind = dto.Kind;
        instruction.InstructedOn = dto.InstructedOn == default ? Today : dto.InstructedOn;
        instruction.ExpiresOn = dto.ExpiresOn;
        instruction.NoticePeriodDays = dto.NoticePeriodDays;
        instruction.TailPeriodDays = dto.TailPeriodDays;

        instruction.FeeBasis = dto.FeeBasis;
        instruction.FeePercent = dto.FeePercent;
        instruction.FeeFixedAmount = dto.FeeFixedAmount;
        instruction.MinimumFee = dto.MinimumFee;
        instruction.FeePeriodsOfRent = dto.FeePeriodsOfRent;
        instruction.FeeIncludesTax = dto.FeeIncludesTax;
        instruction.TaxPercent = dto.TaxPercent;
        instruction.WithdrawalFee = dto.WithdrawalFee;
        instruction.MarketingBudget = dto.MarketingBudget;
        instruction.ManagementService = dto.ManagementService;
        instruction.DocumentUrl = dto.DocumentUrl;
        instruction.SignedOn = dto.SignedOn;

        if (dto.FeeBasis == FeeBasis.PercentOfPrice && dto.FeePercent <= 0m)
            throw new InvalidOperationException("A percentage fee needs a percentage above zero.");

        await Db.SaveChangesAsync();

        // The owner picks up the landlord role so a Person 360 shows the instruction immediately.
        if (dto.OwnerPartyId is not null)
        {
            var hasRole = await Db.PartyRoles.ForCompany(Tenant)
                .AnyAsync(r => r.PartyId == dto.OwnerPartyId && r.Kind == PartyRoleKind.Seller && r.IsActive);

            if (!hasRole)
            {
                Db.PartyRoles.Add(new PartyRole
                {
                    PartyId = dto.OwnerPartyId.Value,
                    Kind = dto.Kind == ListingKind.ForSale ? PartyRoleKind.Seller : PartyRoleKind.Landlord,
                    FromDate = instruction.InstructedOn,
                    IsActive = true,
                    ContextType = "Instruction",
                    ContextId = instruction.Id,
                }.StampNew(Tenant, userId));

                await Db.SaveChangesAsync();
            }
        }

        return (await GetInstructionAsync(instruction.Id))!;
    }

    /// <summary>
    /// Ends an instruction. Live listings under it are withdrawn in the same breath, and the tail
    /// period is spelled out — a buyer we introduced still earns the fee for that long, and it is
    /// the clause agencies forget and lose money on.
    /// </summary>
    public async Task<InstructionDto> TerminateInstructionAsync(Guid id, Guid reasonCodeId, string? note, Guid userId)
    {
        var instruction = await RequireAsync<Instruction>(id, "That instruction does not exist.");

        if (instruction.TerminatedOn is not null)
            throw new InvalidOperationException($"This instruction already ended on {instruction.TerminatedOn:dd MMM yyyy}.");

        var today = Today;

        instruction.TerminatedOn = today;
        instruction.TerminationReasonCodeId = reasonCodeId;
        instruction.StampUpdated(userId);

        var listings = await Db.Listings.ForCompany(Tenant)
            .Where(l => l.InstructionId == id
                     && l.Status != ListingStatus.Completed
                     && l.Status != ListingStatus.Withdrawn)
            .ToListAsync();

        foreach (var listing in listings)
        {
            listing.Status = ListingStatus.Withdrawn;
            listing.WithdrawnAt = DateTime.UtcNow;
            listing.WithdrawnReasonCodeId = reasonCodeId;
            listing.StampUpdated(userId);

            var publications = await Db.PortalPublications.ForCompany(Tenant)
                .Where(p => p.ListingId == listing.Id
                         && (p.State == PortalPublishState.Published || p.State == PortalPublishState.PublishedWithWarnings))
                .ToListAsync();

            foreach (var publication in publications)
            {
                publication.State = PortalPublishState.WithdrawPending;
                publication.StampUpdated(userId);
            }
        }

        await WriteAuditNoteAsync(
            "Instruction", id, "InstructionTerminated", reasonCodeId, userId,
            note: instruction.TailPeriodDays > 0
                ? $"{note} Introductions we made still earn the fee until {today.AddDays(instruction.TailPeriodDays):dd MMM yyyy}.".Trim()
                : note,
            entityReference: instruction.Reference);

        await Db.SaveChangesAsync();
        return (await GetInstructionAsync(id))!;
    }

    // ═══ Portals ═════════════════════════════════════════════════════════════

    public async Task<List<PortalChannelDto>> GetPortalsAsync()
    {
        var portals = await Db.PortalChannels.ForCompany(Tenant)
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (portals.Count == 0) return [];

        var ids = portals.Select(p => p.Id).ToList();

        var stats = await Db.PortalPublications.ForCompany(Tenant)
            .Where(p => ids.Contains(p.PortalChannelId))
            .GroupBy(p => p.PortalChannelId)
            .Select(g => new
            {
                PortalId = g.Key,
                Live = g.Count(x => x.State == PortalPublishState.Published || x.State == PortalPublishState.PublishedWithWarnings),
                Failed = g.Count(x => x.State == PortalPublishState.Failed),
                Impressions = g.Sum(x => x.Impressions),
                Clicks = g.Sum(x => x.Clicks),
                Leads = g.Sum(x => x.Leads),
            })
            .ToDictionaryAsync(x => x.PortalId, x => x);

        // Bookings traced back to each portal, so a renewal decision has a number behind it.
        var bookings = await Db.CampaignAttributions.ForCompany(Tenant)
            .Where(a => a.PortalChannelId != null && ids.Contains(a.PortalChannelId.Value) && a.BookingId != null)
            .GroupBy(a => a.PortalChannelId!.Value)
            .Select(g => new { PortalId = g.Key, Count = g.Select(x => x.BookingId).Distinct().Count() })
            .ToDictionaryAsync(x => x.PortalId, x => x.Count);

        return portals.Select(p =>
        {
            var stat = stats.GetValueOrDefault(p.Id);
            var live = stat?.Live ?? p.CurrentListingCount;
            var leads = stat?.Leads ?? 0;

            return new PortalChannelDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code ?? string.Empty,
                FeedFormat = p.FeedFormat,
                FeedUrl = p.FeedUrl,
                CountryCode = p.CountryCode,
                IsActive = p.IsActive,
                ListingQuota = p.ListingQuota,
                FeaturedQuota = p.FeaturedQuota,
                CurrentListingCount = live,
                RemainingSlots = p.ListingQuota <= 0 ? int.MaxValue : Math.Max(0, p.ListingQuota - live),
                QuotaExceeded = p.ListingQuota > 0 && live >= p.ListingQuota,
                MonthlyCost = p.MonthlyCost,
                ContractExpiresOn = p.ContractExpiresOn,
                MinPhotoCount = p.MinPhotoCount,
                MaxPhotoCount = p.MaxPhotoCount,
                MinPhotoWidthPx = p.MinPhotoWidthPx,
                SupportsFloorPlan = p.SupportsFloorPlan,
                SupportsVirtualTour = p.SupportsVirtualTour,
                RefreshIntervalMinutes = p.RefreshIntervalMinutes,
                Impressions = stat?.Impressions ?? 0,
                Clicks = stat?.Clicks ?? 0,
                Leads = leads,
                Bookings = bookings.GetValueOrDefault(p.Id),
                CostPerLead = leads > 0 ? RealEstateMapper.Money(p.MonthlyCost / leads) : null,
                FailedCount = stat?.Failed ?? 0,
            };
        }).ToList();
    }

    public async Task<PortalChannelDto> SavePortalAsync(PortalChannelDto dto, Guid userId)
    {
        var portal = dto.Id != Guid.Empty
            ? await Db.PortalChannels.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (portal is null)
        {
            portal = new PortalChannel().StampNew(Tenant, userId);
            Db.PortalChannels.Add(portal);
        }
        else portal.StampUpdated(userId);

        portal.Name = dto.Name;
        portal.Code = dto.Code;
        portal.FeedFormat = dto.FeedFormat;
        portal.FeedUrl = dto.FeedUrl;
        portal.CountryCode = dto.CountryCode;
        portal.IsActive = dto.IsActive;
        portal.ListingQuota = dto.ListingQuota;
        portal.FeaturedQuota = dto.FeaturedQuota;
        portal.MonthlyCost = dto.MonthlyCost;
        portal.ContractExpiresOn = dto.ContractExpiresOn;
        portal.MinPhotoCount = dto.MinPhotoCount;
        portal.MaxPhotoCount = dto.MaxPhotoCount;
        portal.MinPhotoWidthPx = dto.MinPhotoWidthPx;
        portal.SupportsFloorPlan = dto.SupportsFloorPlan;
        portal.SupportsVirtualTour = dto.SupportsVirtualTour;
        portal.RefreshIntervalMinutes = dto.RefreshIntervalMinutes;

        await Db.SaveChangesAsync();
        return (await GetPortalsAsync()).First(p => p.Id == portal.Id);
    }

    public async Task<List<PortalMappingDto>> GetPortalMappingsAsync(Guid portalId)
        => await Db.PortalMappings.ForCompany(Tenant)
            .Where(m => m.PortalChannelId == portalId)
            .OrderBy(m => m.MappingKind).ThenBy(m => m.LocalValue)
            .Select(m => new PortalMappingDto
            {
                Id = m.Id,
                PortalChannelId = m.PortalChannelId,
                MappingKind = m.MappingKind,
                LocalValue = m.LocalValue,
                PortalValue = m.PortalValue,
                PortalLocationId = m.PortalLocationId,
            })
            .ToListAsync();

    public async Task<PortalMappingDto> SavePortalMappingAsync(PortalMappingDto dto, Guid userId)
    {
        var mapping = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.PortalMappings.ForCompany(Tenant).FirstOrDefaultAsync(m => m.Id == dto.Id)
            : await Db.PortalMappings.ForCompany(Tenant).FirstOrDefaultAsync(
                m => m.PortalChannelId == dto.PortalChannelId
                  && m.MappingKind == dto.MappingKind
                  && m.LocalValue == dto.LocalValue);

        if (mapping is null)
        {
            mapping = new PortalMapping { PortalChannelId = dto.PortalChannelId }.StampNew(Tenant, userId);
            Db.PortalMappings.Add(mapping);
        }
        else mapping.StampUpdated(userId);

        mapping.MappingKind = dto.MappingKind;
        mapping.LocalValue = dto.LocalValue;
        mapping.PortalValue = dto.PortalValue;
        mapping.PortalLocationId = dto.PortalLocationId;

        await Db.SaveChangesAsync();
        dto.Id = mapping.Id;
        return dto;
    }

    /// <summary>
    /// Queues a syndication operation for every listing/portal pair. Nothing talks to a portal on
    /// this thread: the operation is validated, checked against the plan's quota, and recorded.
    /// Each pair reports its own outcome, so a run of 400 that hits one quota tells the operator
    /// precisely which portal and which listings were left out.
    /// </summary>
    public async Task<PortalPublishResultDto> PublishAsync(PortalPublishRequestDto dto, Guid userId)
    {
        if (dto.ListingIds.Count == 0 || dto.PortalChannelIds.Count == 0)
            throw new InvalidOperationException("Pick at least one listing and one portal.");

        var result = new PortalPublishResultDto();

        var listings = await Db.Listings.ForCompany(Tenant)
            .Where(l => dto.ListingIds.Contains(l.Id))
            .ToListAsync();

        var portals = await Db.PortalChannels.ForCompany(Tenant)
            .Where(p => dto.PortalChannelIds.Contains(p.Id))
            .ToListAsync();

        var existing = await Db.PortalPublications.ForCompany(Tenant)
            .Where(p => dto.ListingIds.Contains(p.ListingId) && dto.PortalChannelIds.Contains(p.PortalChannelId))
            .ToListAsync();

        var isWithdraw = dto.Operation.Equals("withdraw", StringComparison.OrdinalIgnoreCase);

        // Quota is counted per portal across the whole run, so a batch cannot slip past it one
        // listing at a time.
        var liveCounts = new Dictionary<Guid, int>();

        foreach (var portal in portals)
        {
            liveCounts[portal.Id] = await Db.PortalPublications.ForCompany(Tenant)
                .CountAsync(p => p.PortalChannelId == portal.Id
                              && (p.State == PortalPublishState.Published || p.State == PortalPublishState.PublishedWithWarnings));
        }

        foreach (var listing in listings)
        {
            // Readiness is checked once per listing, not once per portal.
            var gate = isWithdraw ? new GateResultDto { Passed = true } : await CheckPublishReadinessAsync(listing.Id);

            foreach (var portal in portals)
            {
                result.Attempted++;

                var log = new PortalPublishLogDto
                {
                    ListingId = listing.Id,
                    ListingReference = listing.Reference,
                    PortalName = portal.Name,
                    Operation = dto.Operation,
                    AttemptedAt = DateTime.UtcNow,
                };

                if (!isWithdraw && !listing.AllowPortalPublish)
                {
                    log.Message = "This listing is marked as not for portal publication.";
                    result.Skipped++;
                    result.Log.Add(log);
                    continue;
                }

                if (!gate.Passed)
                {
                    log.Message = gate.Message;
                    result.Failed++;
                    result.Log.Add(log);
                    continue;
                }

                var publication = existing.FirstOrDefault(p => p.ListingId == listing.Id && p.PortalChannelId == portal.Id);

                var wouldGoLive = !isWithdraw
                    && (publication is null
                        || publication.State is not (PortalPublishState.Published or PortalPublishState.PublishedWithWarnings));

                if (wouldGoLive && portal.ListingQuota > 0 && liveCounts[portal.Id] >= portal.ListingQuota)
                {
                    log.Message = $"{portal.Name} is at its plan limit of {portal.ListingQuota} listings.";
                    result.Skipped++;
                    result.Log.Add(log);
                    continue;
                }

                // Photo minimums differ by portal; failing here is cheaper than failing at the feed.
                if (!isWithdraw && portal.MinPhotoCount > 0)
                {
                    var photos = await Db.PropertyMedia.ForCompany(Tenant)
                        .CountAsync(m => m.PropertyId == listing.PropertyId
                                      && m.Kind == MediaKind.Photo
                                      && !m.ExcludeFromPortals
                                      && (portal.MinPhotoWidthPx <= 0 || m.WidthPx >= portal.MinPhotoWidthPx));

                    if (photos < portal.MinPhotoCount)
                    {
                        log.Message = $"{portal.Name} needs {portal.MinPhotoCount} photographs of at least {portal.MinPhotoWidthPx}px; this listing has {photos}.";
                        result.Failed++;
                        result.Log.Add(log);
                        continue;
                    }
                }

                if (publication is null)
                {
                    publication = new PortalPublication
                    {
                        ListingId = listing.Id,
                        PortalChannelId = portal.Id,
                    }.StampNew(Tenant, userId);

                    Db.PortalPublications.Add(publication);
                    existing.Add(publication);
                }
                else publication.StampUpdated(userId);

                publication.State = isWithdraw
                    ? PortalPublishState.WithdrawPending
                    : publication.State is PortalPublishState.Published or PortalPublishState.PublishedWithWarnings
                        ? PortalPublishState.UpdatePending
                        : PortalPublishState.Queued;

                publication.IsFeatured = dto.AsFeatured;
                publication.LastError = null;

                Db.PortalPublishLogs.Add(new PortalPublishLog
                {
                    PortalPublicationId = publication.Id,
                    Operation = dto.Operation,
                    AttemptedAt = DateTime.UtcNow,
                    Succeeded = true,
                    Message = "Queued.",
                }.StampNew(Tenant, userId));

                if (wouldGoLive) liveCounts[portal.Id]++;

                log.Succeeded = true;
                log.Message = "Queued.";
                result.Succeeded++;
                result.Log.Add(log);
            }
        }

        await Db.SaveChangesAsync();
        return result;
    }

    public async Task<PaginatedResponse<PortalPublicationDto>> GetPublicationsAsync(
        ListQueryDto query, Guid? portalId, PortalPublishState? state)
    {
        var q = Db.PortalPublications.ForCompany(Tenant)
            .WhereIf(portalId.HasValue, p => p.PortalChannelId == portalId)
            .WhereIf(state.HasValue, p => p.State == state)
            .OrderByDescending(p => p.State == PortalPublishState.Failed)
            .ThenByDescending(p => p.LastPushedAt);

        return await PageAsync(q, query, MapPublicationsAsync);
    }

    private async Task<List<PortalPublicationDto>> GetPublicationsForListingAsync(Guid listingId)
    {
        var publications = await Db.PortalPublications.ForCompany(Tenant)
            .Where(p => p.ListingId == listingId)
            .ToListAsync();

        return await MapPublicationsAsync(publications);
    }

    private async Task<List<PortalPublicationDto>> MapPublicationsAsync(List<PortalPublication> publications)
    {
        if (publications.Count == 0) return [];

        var portalIds = publications.Select(p => p.PortalChannelId).Distinct().ToList();

        var portals = await Db.PortalChannels.ForCompany(Tenant)
            .Where(p => portalIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        return publications.Select(p => new PortalPublicationDto
        {
            Id = p.Id,
            ListingId = p.ListingId,
            PortalChannelId = p.PortalChannelId,
            PortalName = portals.GetValueOrDefault(p.PortalChannelId, "—"),
            State = p.State,
            PortalListingId = p.PortalListingId,
            PortalUrl = p.PortalUrl,
            PublishedAt = p.PublishedAt,
            LastPushedAt = p.LastPushedAt,
            IsFeatured = p.IsFeatured,
            LastError = p.LastError,
            FailureCount = p.FailureCount,
            Impressions = p.Impressions,
            Clicks = p.Clicks,
            Leads = p.Leads,
        }).ToList();
    }

    // ═══ Campaigns, events and content ═══════════════════════════════════════

    public async Task<PaginatedResponse<CampaignDto>> GetCampaignsAsync(ListQueryDto query)
    {
        var q = Db.Campaigns.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                c => c.Name.Contains(query.Search!) || c.Channel.Contains(query.Search!))
            .WhereIf(query.ProjectId.HasValue, c => c.ProjectId == query.ProjectId)
            .WhereIf(query.FromDate.HasValue, c => c.StartsOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, c => c.StartsOn <= query.ToDate)
            .OrderByDescending(c => c.StartsOn);

        return await PageAsync(q, query, async campaigns =>
        {
            var projects = await ProjectNamesAsync(campaigns.Select(c => c.ProjectId));
            var today = Today;

            return campaigns.Select(c =>
            {
                var spend = c.ActualSpend > 0m ? c.ActualSpend : c.Budget;

                return new CampaignDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code ?? string.Empty,
                    Channel = c.Channel,
                    ProjectId = c.ProjectId,
                    ProjectName = c.ProjectId is null ? null : projects.GetValueOrDefault(c.ProjectId.Value),
                    StartsOn = c.StartsOn,
                    EndsOn = c.EndsOn,
                    Budget = c.Budget,
                    ActualSpend = c.ActualSpend,
                    LandingPageUrl = c.LandingPageUrl,
                    TrackingCode = c.TrackingCode,
                    IsActive = c.IsActive && c.StartsOn <= today && (c.EndsOn is null || c.EndsOn >= today),
                    LeadCount = c.LeadCount,
                    VisitCount = c.VisitCount,
                    BookingCount = c.BookingCount,
                    BookingValue = c.BookingValue,

                    // Zero spend means these are meaningless, so they stay null rather than infinite.
                    CostPerLead = c.LeadCount > 0 && spend > 0m ? RealEstateMapper.Money(spend / c.LeadCount) : null,
                    CostPerVisit = c.VisitCount > 0 && spend > 0m ? RealEstateMapper.Money(spend / c.VisitCount) : null,
                    CostPerBooking = c.BookingCount > 0 && spend > 0m ? RealEstateMapper.Money(spend / c.BookingCount) : null,
                    ReturnOnAdSpend = spend > 0m ? RealEstateMapper.Money(c.BookingValue / spend, 2) : null,
                };
            }).ToList();
        });
    }

    public async Task<CampaignDto> SaveCampaignAsync(CampaignDto dto, Guid userId)
    {
        var campaign = dto.Id != Guid.Empty
            ? await Db.Campaigns.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
            : null;

        if (campaign is null)
        {
            campaign = new Campaign
            {
                Code = string.IsNullOrWhiteSpace(dto.Code)
                    ? await numbering.NextMasterCodeAsync(Db.Campaigns, "CMP")
                    : dto.Code,
            }.StampNew(Tenant, userId);

            Db.Campaigns.Add(campaign);
        }
        else campaign.StampUpdated(userId);

        campaign.Name = dto.Name;
        campaign.Channel = dto.Channel;
        campaign.ProjectId = dto.ProjectId;
        campaign.StartsOn = dto.StartsOn == default ? Today : dto.StartsOn;
        campaign.EndsOn = dto.EndsOn;
        campaign.Budget = dto.Budget;
        campaign.ActualSpend = dto.ActualSpend;
        campaign.LandingPageUrl = dto.LandingPageUrl;

        // The tracking code is what ties a lead back to the spend. Without one, attribution is
        // guesswork, so it is generated rather than left blank.
        campaign.TrackingCode = string.IsNullOrWhiteSpace(dto.TrackingCode)
            ? $"{campaign.Code}".ToLowerInvariant()
            : dto.TrackingCode.Trim();

        await Db.SaveChangesAsync();

        var page = await GetCampaignsAsync(new ListQueryDto { PageSize = 1, Search = campaign.Name });
        return page.Data.FirstOrDefault(c => c.Id == campaign.Id) ?? dto;
    }

    public async Task<PaginatedResponse<MarketingEventDto>> GetEventsAsync(ListQueryDto query)
    {
        var q = Db.MarketingEvents.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), e => e.Name.Contains(query.Search!))
            .WhereIf(query.ProjectId.HasValue, e => e.ProjectId == query.ProjectId)
            .OrderByDescending(e => e.StartsAt);

        return await PageAsync(q, query, async events =>
        {
            var projects = await ProjectNamesAsync(events.Select(e => e.ProjectId));

            return events.Select(e => new MarketingEventDto
            {
                Id = e.Id,
                Name = e.Name,
                EventType = e.EventType,
                ProjectId = e.ProjectId,
                ProjectName = e.ProjectId is null ? null : projects.GetValueOrDefault(e.ProjectId.Value),
                StartsAt = e.StartsAt,
                EndsAt = e.EndsAt,
                Venue = e.Venue,
                Budget = e.Budget,
                ActualSpend = e.ActualSpend,
                RegisteredCount = e.RegisteredCount,
                AttendedCount = e.AttendedCount,
                WalkInCount = e.WalkInCount,
                LeadCount = e.LeadCount,
                BookingCount = e.BookingCount,
                AttendanceRate = e.RegisteredCount > 0
                    ? RealEstateMapper.Percent(e.AttendedCount, e.RegisteredCount)
                    : null,
            }).ToList();
        });
    }

    public async Task<MarketingEventDto> SaveEventAsync(MarketingEventDto dto, Guid userId)
    {
        var marketingEvent = dto.Id != Guid.Empty
            ? await Db.MarketingEvents.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == dto.Id)
            : null;

        if (marketingEvent is null)
        {
            marketingEvent = new MarketingEvent().StampNew(Tenant, userId);
            Db.MarketingEvents.Add(marketingEvent);
        }
        else marketingEvent.StampUpdated(userId);

        if (dto.EndsAt <= dto.StartsAt)
            throw new InvalidOperationException("The event has to end after it starts.");

        marketingEvent.Name = dto.Name;
        marketingEvent.EventType = dto.EventType;
        marketingEvent.ProjectId = dto.ProjectId;
        marketingEvent.StartsAt = dto.StartsAt;
        marketingEvent.EndsAt = dto.EndsAt;
        marketingEvent.Venue = dto.Venue;
        marketingEvent.Budget = dto.Budget;
        marketingEvent.ActualSpend = dto.ActualSpend;

        await Db.SaveChangesAsync();
        dto.Id = marketingEvent.Id;
        return dto;
    }

    public async Task<PaginatedResponse<ContentAssetDto>> GetContentAsync(ListQueryDto query)
    {
        var q = Db.ContentAssets.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                c => c.Name.Contains(query.Search!) || c.AssetType.Contains(query.Search!))
            .WhereIf(query.ProjectId.HasValue, c => c.ProjectId == query.ProjectId)
            .WhereIf(!query.IncludeInactive, c => c.IsCurrent)
            .OrderBy(c => c.AssetType).ThenByDescending(c => c.Version);

        var today = Today;

        return await PageAsync(q, query, async assets =>
        {
            var projects = await ProjectNamesAsync(assets.Select(a => a.ProjectId));

            return assets.Select(a => new ContentAssetDto
            {
                Id = a.Id,
                Name = a.Name,
                AssetType = a.AssetType,
                ProjectId = a.ProjectId,
                ProjectName = a.ProjectId is null ? null : projects.GetValueOrDefault(a.ProjectId.Value),
                Url = a.Url,
                ThumbnailUrl = a.ThumbnailUrl,
                Version = a.Version,
                ValidFrom = a.ValidFrom,
                ExpiresOn = a.ExpiresOn,
                IsExpired = a.ExpiresOn is not null && a.ExpiresOn < today,
                IsExternallyShareable = a.IsExternallyShareable,
                LanguageCode = a.LanguageCode,
                DownloadCount = a.DownloadCount,
                IsCurrent = a.IsCurrent,
            }).ToList();
        });
    }

    public async Task<ContentAssetDto> SaveContentAsync(ContentAssetDto dto, Guid userId)
    {
        var asset = dto.Id != Guid.Empty
            ? await Db.ContentAssets.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == dto.Id)
            : null;

        if (asset is null)
        {
            // A new version supersedes the old one rather than sitting beside it, so nobody
            // forwards last year's price list to a customer.
            var previous = await Db.ContentAssets.ForCompany(Tenant)
                .Where(c => c.AssetType == dto.AssetType
                         && c.ProjectId == dto.ProjectId
                         && c.LanguageCode == dto.LanguageCode
                         && c.IsCurrent)
                .ToListAsync();

            foreach (var old in previous)
            {
                old.IsCurrent = false;
                old.StampUpdated(userId);
            }

            asset = new ContentAsset
            {
                Version = previous.Count == 0 ? 1 : previous.Max(p => p.Version) + 1,
                IsCurrent = true,
            }.StampNew(Tenant, userId);

            Db.ContentAssets.Add(asset);
        }
        else asset.StampUpdated(userId);

        asset.Name = dto.Name;
        asset.AssetType = dto.AssetType;
        asset.ProjectId = dto.ProjectId;
        asset.ListingId = dto.Id == Guid.Empty ? null : asset.ListingId;
        asset.Url = dto.Url;
        asset.ThumbnailUrl = dto.ThumbnailUrl;
        asset.ValidFrom = dto.ValidFrom;
        asset.ExpiresOn = dto.ExpiresOn;
        asset.IsExternallyShareable = dto.IsExternallyShareable;
        asset.LanguageCode = dto.LanguageCode;

        await Db.SaveChangesAsync();

        dto.Id = asset.Id;
        dto.Version = asset.Version;
        dto.IsCurrent = asset.IsCurrent;
        return dto;
    }
}
