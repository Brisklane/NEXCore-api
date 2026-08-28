using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Requirements and matching, activities and tasks, the diary, viewings, site visits and keys —
/// the last third of <see cref="CrmService"/>.
/// </summary>
public partial class CrmService
{
    // ═══ Requirements & matching ═════════════════════════════════════════════

    public async Task<RequirementProfileDto> SaveRequirementAsync(RequirementProfileUpsertDto dto, Guid userId)
    {
        var profile = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.RequirementProfiles.ForCompany(Tenant).Include(r => r.Areas).FirstOrDefaultAsync(r => r.Id == dto.Id)
            : null;

        if (profile is null)
        {
            profile = new RequirementProfile { PartyId = dto.PartyId ?? Guid.Empty }.StampNew(Tenant, userId);
            Db.RequirementProfiles.Add(profile);
        }
        else
        {
            Db.RequirementAreas.RemoveRange(profile.Areas);
            profile.StampUpdated(userId);
        }

        profile.EnquiryId = dto.EnquiryId;
        profile.Name = dto.Name;
        profile.Interest = dto.Interest;
        profile.Category = dto.Category;
        profile.SubTypes = dto.SubTypes.Count == 0 ? null : string.Join(",", dto.SubTypes.Select(s => (int)s));
        profile.Purpose = dto.Purpose;
        profile.Funding = dto.Funding;
        profile.BudgetMin = dto.BudgetMin;
        profile.BudgetMax = dto.BudgetMax;
        profile.MinAreaSqFt = dto.MinArea is null ? null : RealEstateMapper.ToSquareFeet(dto.MinArea.Value, dto.InputAreaUnit);
        profile.MaxAreaSqFt = dto.MaxArea is null ? null : RealEstateMapper.ToSquareFeet(dto.MaxArea.Value, dto.InputAreaUnit);
        profile.MinBedrooms = dto.MinBedrooms;
        profile.MaxBedrooms = dto.MaxBedrooms;
        profile.MinBathrooms = dto.MinBathrooms;
        profile.MinFloor = dto.MinFloor;
        profile.MaxFloor = dto.MaxFloor;
        profile.PreferredFacing = dto.PreferredFacing;
        profile.Furnishing = dto.Furnishing;
        profile.MustHaveFeatures = dto.MustHaveFeatures.Count == 0 ? null : string.Join(",", dto.MustHaveFeatures);
        profile.NiceToHaveFeatures = dto.NiceToHaveFeatures.Count == 0 ? null : string.Join(",", dto.NiceToHaveFeatures);
        profile.PreferredAreasGeoJson = dto.PreferredAreasGeoJson;
        profile.AvailableFrom = dto.AvailableFrom;
        profile.Exclusions = dto.Exclusions;
        profile.AlertsEnabled = dto.AlertsEnabled;

        foreach (var areaId in dto.PreferredAreaIds.Distinct())
            profile.Areas.Add(new RequirementArea { GeoAreaId = areaId, Priority = 1 }.StampNew(Tenant, userId));

        await Db.SaveChangesAsync();
        return (await GetRequirementsAsync(profile.PartyId)).First(r => r.Id == profile.Id);
    }

    public async Task<List<RequirementProfileDto>> GetRequirementsAsync(Guid partyId)
    {
        var profiles = await Db.RequirementProfiles.ForCompany(Tenant)
            .Where(r => r.PartyId == partyId)
            .Include(r => r.Areas)
            .OrderByDescending(r => r.IsActive).ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        if (profiles.Count == 0) return [];

        var areaUnit = await AreaUnitAsync();
        var currency = await CurrencyAsync();
        var ids = profiles.Select(p => p.Id).ToList();

        var matchCounts = await Db.MatchResults.ForCompany(Tenant)
            .Where(m => ids.Contains(m.RequirementProfileId) && !m.IsDismissed)
            .GroupBy(m => m.RequirementProfileId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var areaIds = profiles.SelectMany(p => p.Areas).Select(a => a.GeoAreaId).Distinct().ToList();
        var areas = areaIds.Count == 0
            ? []
            : await Db.GeoAreas.ForCompany(Tenant).Where(a => areaIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => new { a.Name, a.Path });

        return profiles.Select(p => new RequirementProfileDto
        {
            Id = p.Id,
            PartyId = p.PartyId,
            Name = p.Name,
            Interest = p.Interest,
            Category = p.Category,
            SubTypes = ParseSubTypes(p.SubTypes),
            Purpose = p.Purpose,
            Funding = p.Funding,
            BudgetMin = p.BudgetMin,
            BudgetMax = p.BudgetMax,
            CurrencyCode = currency,
            MinArea = RealEstateMapper.AreaOrNull(p.MinAreaSqFt, areaUnit),
            MaxArea = RealEstateMapper.AreaOrNull(p.MaxAreaSqFt, areaUnit),
            MinBedrooms = p.MinBedrooms,
            MaxBedrooms = p.MaxBedrooms,
            MinBathrooms = p.MinBathrooms,
            MinFloor = p.MinFloor,
            MaxFloor = p.MaxFloor,
            PreferredFacing = p.PreferredFacing,
            Furnishing = p.Furnishing,
            MustHaveFeatures = Split(p.MustHaveFeatures),
            NiceToHaveFeatures = Split(p.NiceToHaveFeatures),
            PreferredAreas = p.Areas.Select(a => new LookupDto
            {
                Id = a.GeoAreaId,
                Label = areas.GetValueOrDefault(a.GeoAreaId)?.Name ?? "—",
                SubLabel = areas.GetValueOrDefault(a.GeoAreaId)?.Path,
            }).ToList(),
            PreferredAreasGeoJson = p.PreferredAreasGeoJson,
            AvailableFrom = p.AvailableFrom,
            Exclusions = p.Exclusions,
            IsActive = p.IsActive,
            AlertsEnabled = p.AlertsEnabled,
            LastMatchedAt = p.LastMatchedAt,
            MatchCount = matchCounts.GetValueOrDefault(p.Id),
        }).ToList();
    }

    private static List<PropertySubType> ParseSubTypes(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Where(s => int.TryParse(s, out _))
                 .Select(s => (PropertySubType)int.Parse(s))
                 .ToList();

    private static List<string> Split(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>
    /// Matches a requirement against live stock, scoring each candidate and keeping the working.
    ///
    /// A must-have that is missing eliminates the candidate outright; everything else contributes
    /// points, and the factors are returned so the screen can show why a property scored 72.
    /// </summary>
    public async Task<List<MatchResultDto>> RunMatchAsync(Guid requirementProfileId, int take)
    {
        var profile = await Db.RequirementProfiles.ForCompany(Tenant)
            .Include(r => r.Areas)
            .FirstOrDefaultAsync(r => r.Id == requirementProfileId)
            ?? throw new InvalidOperationException("That requirement does not exist.");

        var areaUnit = await AreaUnitAsync();
        var subTypes = ParseSubTypes(profile.SubTypes);
        var mustHave = Split(profile.MustHaveFeatures);
        var niceToHave = Split(profile.NiceToHaveFeatures);
        var areaIds = profile.Areas.Select(a => a.GeoAreaId).ToList();

        var candidates = await (
            from l in Db.Listings.ForCompany(Tenant)
            join p in Db.Properties.ForCompany(Tenant) on l.PropertyId equals p.Id
            where l.Status == ListingStatus.Live && l.Kind == profile.Interest
            select new { Listing = l, Property = p })
            .WhereIf(profile.Category.HasValue, x => x.Property.Category == profile.Category)
            .WhereIf(subTypes.Count > 0, x => subTypes.Contains(x.Property.SubType))
            .WhereIf(areaIds.Count > 0, x => x.Property.GeoAreaId != null && areaIds.Contains(x.Property.GeoAreaId!.Value))
            .WhereIf(profile.MinBedrooms.HasValue, x => x.Property.Bedrooms >= profile.MinBedrooms)
            .WhereIf(profile.MaxBedrooms.HasValue, x => x.Property.Bedrooms <= profile.MaxBedrooms)
            .WhereIf(profile.MinBathrooms.HasValue, x => x.Property.Bathrooms >= profile.MinBathrooms)
            .WhereIf(profile.MinAreaSqFt.HasValue, x => x.Property.SaleableAreaSqFt >= profile.MinAreaSqFt)
            .WhereIf(profile.MaxAreaSqFt.HasValue, x => x.Property.SaleableAreaSqFt <= profile.MaxAreaSqFt)
            .Take(500)
            .ToListAsync();

        var propertyIds = candidates.Select(c => c.Property.Id).ToList();

        var features = propertyIds.Count == 0
            ? []
            : await Db.PropertyFeatures.ForCompany(Tenant)
                .Where(f => propertyIds.Contains(f.PropertyId))
                .Select(f => new { f.PropertyId, f.FeatureKey })
                .ToListAsync();

        var heroes = propertyIds.Count == 0
            ? []
            : await Db.PropertyMedia.ForCompany(Tenant)
                .Where(m => propertyIds.Contains(m.PropertyId) && m.IsHero)
                .ToDictionaryAsync(m => m.PropertyId, m => m.Url);

        var results = new List<MatchResultDto>();

        foreach (var candidate in candidates)
        {
            var property = candidate.Property;
            var listing = candidate.Listing;
            var factors = new List<MatchFactorDto>();
            var score = 0;
            var eliminated = false;

            // Must-haves gate. A missing one takes the property out entirely.
            var propertyFeatures = features.Where(f => f.PropertyId == property.Id).Select(f => f.FeatureKey).ToHashSet();

            foreach (var required in mustHave)
            {
                var has = propertyFeatures.Contains(required);
                factors.Add(new MatchFactorDto
                {
                    Criterion = required,
                    Outcome = has ? "present" : "missing",
                    Points = has ? 0 : 0,
                    IsMustHave = true,
                });

                if (!has) eliminated = true;
            }

            if (eliminated) continue;

            // Price. The heaviest single factor, and the one buyers actually filter on.
            var price = listing.AskingPrice ?? property.AskingPrice;
            if (price is > 0m && (profile.BudgetMin is > 0m || profile.BudgetMax is > 0m))
            {
                var min = profile.BudgetMin ?? 0m;
                var max = profile.BudgetMax ?? decimal.MaxValue;

                if (price >= min && price <= max)
                {
                    score += 35;
                    factors.Add(new MatchFactorDto { Criterion = "Price", Outcome = "within budget", Points = 35 });
                }
                else if (profile.BudgetMax is > 0m && price <= profile.BudgetMax * 1.1m)
                {
                    score += 18;
                    factors.Add(new MatchFactorDto { Criterion = "Price", Outcome = "just above budget", Points = 18 });
                }
                else
                {
                    factors.Add(new MatchFactorDto { Criterion = "Price", Outcome = "outside budget", Points = 0 });
                }
            }

            if (profile.MinBedrooms.HasValue && property.Bedrooms.HasValue)
            {
                score += 20;
                factors.Add(new MatchFactorDto { Criterion = "Bedrooms", Outcome = $"{property.Bedrooms}", Points = 20 });
            }

            if (areaIds.Count > 0 && property.GeoAreaId is not null && areaIds.Contains(property.GeoAreaId.Value))
            {
                score += 20;
                factors.Add(new MatchFactorDto { Criterion = "Area", Outcome = "preferred area", Points = 20 });
            }

            if (profile.PreferredFacing.HasValue && property.Facing == profile.PreferredFacing)
            {
                score += 8;
                factors.Add(new MatchFactorDto { Criterion = "Facing", Outcome = property.Facing.ToString()!, Points = 8 });
            }

            if (profile.Furnishing.HasValue && property.Furnishing == profile.Furnishing)
            {
                score += 7;
                factors.Add(new MatchFactorDto { Criterion = "Furnishing", Outcome = "as requested", Points = 7 });
            }

            var niceHits = niceToHave.Count(n => propertyFeatures.Contains(n));
            if (niceHits > 0)
            {
                var points = Math.Min(10, niceHits * 3);
                score += points;
                factors.Add(new MatchFactorDto
                {
                    Criterion = "Preferred features",
                    Outcome = $"{niceHits} of {niceToHave.Count}",
                    Points = points,
                });
            }

            results.Add(new MatchResultDto
            {
                ListingId = listing.Id,
                PropertyId = property.Id,
                Title = listing.Headline ?? property.Name ?? property.Reference,
                AddressOneLine = RealEstateMapper.OneLineAddress(property),
                HeroImageUrl = heroes.GetValueOrDefault(property.Id),
                Price = price,
                CurrencyCode = listing.CurrencyCode,
                Area = RealEstateMapper.AreaOrNull(property.SaleableAreaSqFt, areaUnit),
                Bedrooms = property.Bedrooms,
                SubType = property.SubType,
                Score = Math.Clamp(score, 0, 100),
                Factors = factors,
                MatchedAt = DateTime.UtcNow,
            });
        }

        var top = results.OrderByDescending(r => r.Score).Take(take <= 0 ? 20 : take).ToList();

        // Stored so "we sent you these six on Tuesday" is provable and open tracking has a home.
        var existing = await Db.MatchResults.ForCompany(Tenant)
            .Where(m => m.RequirementProfileId == requirementProfileId)
            .ToListAsync();

        foreach (var match in top)
        {
            var stored = existing.FirstOrDefault(m => m.ListingId == match.ListingId);

            if (stored is null)
            {
                stored = new MatchResult
                {
                    RequirementProfileId = requirementProfileId,
                    ListingId = match.ListingId,
                    PropertyId = match.PropertyId,
                    MatchedAt = DateTime.UtcNow,
                }.StampNew(Tenant);

                Db.MatchResults.Add(stored);
            }

            stored.Score = match.Score;
            stored.ScoreBreakdown = string.Join("\n", match.Factors.Select(f => $"{f.Criterion}: {f.Outcome} (+{f.Points})"));
            match.Id = stored.Id;
        }

        profile.LastMatchedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();

        return top;
    }

    public async Task<List<MatchResultDto>> MatchListingToRequirementsAsync(Guid listingId, int take)
    {
        var listing = await Db.Listings.ForCompany(Tenant).FirstOrDefaultAsync(l => l.Id == listingId)
            ?? throw new InvalidOperationException("That listing does not exist.");

        var profiles = await Db.RequirementProfiles.ForCompany(Tenant)
            .Where(r => r.IsActive && r.Interest == listing.Kind)
            .Take(500)
            .ToListAsync();

        var results = new List<MatchResultDto>();

        foreach (var profile in profiles)
        {
            var matches = await RunMatchAsync(profile.Id, 50);
            var mine = matches.FirstOrDefault(m => m.ListingId == listingId);
            if (mine is not null) results.Add(mine);
        }

        return results.OrderByDescending(r => r.Score).Take(take <= 0 ? 20 : take).ToList();
    }

    private async Task<List<MatchResultDto>> GetStoredMatchesAsync(Guid requirementProfileId, int take)
    {
        var stored = await Db.MatchResults.ForCompany(Tenant)
            .Where(m => m.RequirementProfileId == requirementProfileId && !m.IsDismissed)
            .OrderByDescending(m => m.Score)
            .Take(take)
            .ToListAsync();

        if (stored.Count == 0) return [];

        var listingIds = stored.Where(m => m.ListingId.HasValue).Select(m => m.ListingId!.Value).ToList();
        var areaUnit = await AreaUnitAsync();

        var listings = await (
            from l in Db.Listings.ForCompany(Tenant)
            join p in Db.Properties.ForCompany(Tenant) on l.PropertyId equals p.Id
            where listingIds.Contains(l.Id)
            select new { l.Id, l.Headline, l.AskingPrice, l.CurrencyCode, Property = p })
            .ToDictionaryAsync(x => x.Id, x => x);

        return stored.Select(m =>
        {
            var listing = m.ListingId is null ? null : listings.GetValueOrDefault(m.ListingId.Value);

            return new MatchResultDto
            {
                Id = m.Id,
                ListingId = m.ListingId,
                PropertyId = m.PropertyId,
                Title = listing?.Headline ?? listing?.Property.Reference ?? "—",
                AddressOneLine = listing is null ? null : RealEstateMapper.OneLineAddress(listing.Property),
                Price = listing?.AskingPrice,
                CurrencyCode = listing?.CurrencyCode,
                Area = listing is null ? null : RealEstateMapper.AreaOrNull(listing.Property.SaleableAreaSqFt, areaUnit),
                Bedrooms = listing?.Property.Bedrooms,
                SubType = listing?.Property.SubType,
                Score = m.Score,
                MatchedAt = m.MatchedAt,
                WasSent = m.WasSent,
                SentAt = m.SentAt,
                OpenedAt = m.OpenedAt,
                ClickedAt = m.ClickedAt,
                IsDismissed = m.IsDismissed,
                LedToViewing = m.LedToViewing,
                Factors = (m.ScoreBreakdown ?? string.Empty)
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => new MatchFactorDto { Criterion = line, Outcome = string.Empty })
                    .ToList(),
            };
        }).ToList();
    }

    public async Task<int> SendMatchesAsync(SendMatchesDto dto, Guid userId)
    {
        var matches = await Db.MatchResults.ForCompany(Tenant)
            .Where(m => dto.MatchResultIds.Contains(m.Id))
            .ToListAsync();

        if (matches.Count == 0) return 0;

        var profile = await Db.RequirementProfiles.ForCompany(Tenant)
            .FirstOrDefaultAsync(r => r.Id == dto.RequirementProfileId);

        foreach (var match in matches)
        {
            match.WasSent = true;
            match.SentAt = DateTime.UtcNow;
            match.SentVia = dto.Channel;
            match.StampUpdated(userId);
        }

        if (profile is not null)
        {
            Db.Activities.Add(new Activity
            {
                Kind = dto.Channel == NotificationChannel.WhatsApp ? ActivityKind.WhatsApp : ActivityKind.Email,
                Direction = ActivityDirection.Outbound,
                PartyId = profile.PartyId,
                EnquiryId = profile.EnquiryId,
                OccurredAt = DateTime.UtcNow,
                Subject = $"{matches.Count} matching properties sent",
                Body = dto.Message,
                UserId = userId,
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();
        return matches.Count;
    }

    public async Task DismissMatchAsync(Guid matchId, Guid? reasonCodeId, Guid userId)
    {
        var match = await Db.MatchResults.ForCompany(Tenant).FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new InvalidOperationException("That match does not exist.");

        match.IsDismissed = true;
        match.DismissReasonCodeId = reasonCodeId;
        match.StampUpdated(userId);

        await Db.SaveChangesAsync();
    }

    // ═══ Activities & tasks ══════════════════════════════════════════════════

    public async Task<ActivityDto> LogActivityAsync(ActivityCreateDto dto, Guid userId)
    {
        var activity = new Activity
        {
            Kind = dto.Kind,
            Direction = dto.Direction,
            PartyId = dto.PartyId,
            EnquiryId = dto.EnquiryId,
            BookingId = dto.BookingId,
            TenancyId = dto.TenancyId,
            DealId = dto.DealId,
            PropertyId = dto.PropertyId,
            ListingId = dto.ListingId,
            ChannelPartnerId = dto.ChannelPartnerId,
            OccurredAt = dto.OccurredAt ?? DateTime.UtcNow,
            UserId = userId,
            Subject = dto.Subject,
            Body = dto.Body,
            DurationSeconds = dto.DurationSeconds,
            Outcome = dto.Outcome,
            OutcomeReasonCodeId = dto.OutcomeReasonCodeId,
            AttachmentUrl = dto.AttachmentUrl,
        }.StampNew(Tenant, userId);

        Db.Activities.Add(activity);

        // Logging a real contact stops the speed-to-lead clock.
        if (dto.EnquiryId is not null)
        {
            var enquiry = await Db.Enquiries.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == dto.EnquiryId);
            if (enquiry is not null)
            {
                enquiry.LastActivityAt = activity.OccurredAt;

                if (enquiry.FirstContactedAt is null && dto.Direction == ActivityDirection.Outbound
                    && dto.Kind is ActivityKind.Call or ActivityKind.WhatsApp or ActivityKind.Email
                        or ActivityKind.Sms or ActivityKind.Meeting)
                {
                    enquiry.FirstContactedAt = activity.OccurredAt;
                }

                if (enquiry.Stage == EnquiryStage.New) enquiry.Stage = EnquiryStage.Contacted;
                enquiry.StampUpdated(userId);
            }
        }

        if (dto.NextFollowUp is not null)
        {
            dto.NextFollowUp.PartyId ??= dto.PartyId;
            dto.NextFollowUp.EnquiryId ??= dto.EnquiryId;
            dto.NextFollowUp.BookingId ??= dto.BookingId;
            await SaveTaskAsync(dto.NextFollowUp, userId);

            if (dto.EnquiryId is not null)
            {
                var enquiry = await Db.Enquiries.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == dto.EnquiryId);
                if (enquiry is not null) enquiry.NextFollowUpAt = dto.NextFollowUp.DueAt;
            }
        }

        await Db.SaveChangesAsync();
        return MapActivity(activity);
    }

    private static ActivityDto MapActivity(Activity a) => new()
    {
        Id = a.Id,
        Kind = a.Kind,
        Direction = a.Direction,
        OccurredAt = a.OccurredAt,
        Subject = a.Subject,
        Body = a.Body,
        DurationSeconds = a.DurationSeconds,
        Outcome = a.Outcome,
        RecordingUrl = a.RecordingUrl,
        AttachmentUrl = a.AttachmentUrl,
        DeliveredAt = a.DeliveredAt,
        ReadAt = a.ReadAt,
        DeliveryFailed = a.DeliveryFailed,
        FailureReason = a.FailureReason,
        IsSystemGenerated = a.IsSystemGenerated,
        IsPinned = a.IsPinned,
    };

    public async Task<PaginatedResponse<ActivityDto>> GetActivitiesAsync(Guid? partyId, Guid? enquiryId, ListQueryDto query)
    {
        var q = Db.Activities.ForCompany(Tenant)
            .WhereIf(partyId.HasValue, a => a.PartyId == partyId)
            .WhereIf(enquiryId.HasValue, a => a.EnquiryId == enquiryId)
            .OrderByDescending(a => a.OccurredAt);

        return await PageAsync(q, query, MapActivity);
    }

    public async Task<FollowUpTaskDto> SaveTaskAsync(FollowUpTaskCreateDto dto, Guid userId)
    {
        var task = new FollowUpTask
        {
            Title = dto.Title,
            Note = dto.Note,
            SuggestedAction = dto.SuggestedAction,
            DueAt = dto.DueAt,
            Priority = dto.Priority,
            State = TaskState.Open,
            AssignedToUserId = dto.AssignedToUserId ?? userId,
            AssignedByUserId = userId,
            PartyId = dto.PartyId,
            EnquiryId = dto.EnquiryId,
            BookingId = dto.BookingId,
            TenancyId = dto.TenancyId,
            DealId = dto.DealId,
            WorkOrderId = dto.WorkOrderId,
            DunningCaseId = dto.DunningCaseId,
        }.StampNew(Tenant, userId);

        Db.FollowUpTasks.Add(task);
        await Db.SaveChangesAsync();

        return (await MapTasksAsync([task]))[0];
    }

    private async Task<List<FollowUpTaskDto>> MapTasksAsync(List<FollowUpTask> tasks)
    {
        if (tasks.Count == 0) return [];

        var partyIds = tasks.Where(t => t.PartyId.HasValue).Select(t => t.PartyId!.Value).ToList();
        var names = await PartyNamesAsync(partyIds);

        var phones = partyIds.Count == 0
            ? []
            : await Db.Parties.ForCompany(Tenant).Where(p => partyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.PrimaryPhone);

        var bookingIds = tasks.Where(t => t.BookingId.HasValue).Select(t => t.BookingId!.Value).Distinct().ToList();
        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant).Where(b => bookingIds.Contains(b.Id))
                .Select(b => new { b.Id, b.Reference, b.OverdueAmount })
                .ToDictionaryAsync(b => b.Id, b => b);

        var now = DateTime.UtcNow;

        return tasks.Select(t =>
        {
            var booking = t.BookingId is null ? null : bookings.GetValueOrDefault(t.BookingId.Value);

            return new FollowUpTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Note = t.Note,
                SuggestedAction = t.SuggestedAction,
                DueAt = t.DueAt,
                Priority = t.Priority,
                State = t.State,
                IsOverdue = t.State == TaskState.Open && t.DueAt < now,
                IsAutoGenerated = t.IsAutoGenerated,
                SourceRuleKey = t.SourceRuleKey,
                SnoozeCount = t.SnoozeCount,
                PartyId = t.PartyId,
                PartyName = t.PartyId is null ? null : names.GetValueOrDefault(t.PartyId.Value),
                PartyPhone = t.PartyId is null ? null : phones.GetValueOrDefault(t.PartyId.Value),
                EnquiryId = t.EnquiryId,
                BookingId = t.BookingId,
                TenancyId = t.TenancyId,
                DealId = t.DealId,
                WorkOrderId = t.WorkOrderId,
                DunningCaseId = t.DunningCaseId,
                ContextLabel = booking?.Reference,
                Amount = booking?.OverdueAmount,
                Route = RouteForTask(t),
            };
        }).ToList();
    }

    private static string? RouteForTask(FollowUpTask t)
    {
        if (t.EnquiryId is not null) return $"/realestate/enquiries/{t.EnquiryId}";
        if (t.BookingId is not null) return $"/realestate/bookings/{t.BookingId}";
        if (t.TenancyId is not null) return $"/realestate/tenancies/{t.TenancyId}";
        if (t.DealId is not null) return $"/realestate/deals/{t.DealId}";
        if (t.WorkOrderId is not null) return $"/realestate/work-orders/{t.WorkOrderId}";
        if (t.DunningCaseId is not null) return "/realestate/collections";
        if (t.PartyId is not null) return $"/realestate/contacts/{t.PartyId}";
        return null;
    }

    public async Task<FollowUpTaskDto> CompleteTaskAsync(TaskCompletionDto dto, Guid userId)
    {
        var task = await Db.FollowUpTasks.ForCompany(Tenant).FirstOrDefaultAsync(t => t.Id == dto.TaskId)
            ?? throw new InvalidOperationException("That task does not exist.");

        task.State = TaskState.Done;
        task.CompletedAt = DateTime.UtcNow;
        task.CompletedByUserId = userId;
        task.CompletionNote = dto.CompletionNote;
        task.StampUpdated(userId);

        // Completing a task without setting the next one is how leads go cold, so the caller can
        // create the follow-up in the same call.
        if (dto.NextTask is not null)
        {
            dto.NextTask.PartyId ??= task.PartyId;
            dto.NextTask.EnquiryId ??= task.EnquiryId;
            dto.NextTask.BookingId ??= task.BookingId;
            await SaveTaskAsync(dto.NextTask, userId);
        }

        await Db.SaveChangesAsync();
        return (await MapTasksAsync([task]))[0];
    }

    public async Task<FollowUpTaskDto> SnoozeTaskAsync(Guid taskId, DateTime until, Guid userId)
    {
        var task = await Db.FollowUpTasks.ForCompany(Tenant).FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new InvalidOperationException("That task does not exist.");

        task.DueAt = until;
        task.SnoozeCount++;
        task.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await MapTasksAsync([task]))[0];
    }

    /// <summary>The agent's day: what is overdue, what is due, what is booked, what needs them.</summary>
    public async Task<MyDayDto> GetMyDayAsync(Guid userId, DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = date.ToDateTime(TimeOnly.MaxValue);
        var now = DateTime.UtcNow;

        var agent = await Db.AgentProfiles.ForCompany(Tenant).FirstOrDefaultAsync(a => a.UserId == userId);

        var tasks = await Db.FollowUpTasks.ForCompany(Tenant)
            .Where(t => t.AssignedToUserId == userId && t.State == TaskState.Open)
            .OrderBy(t => t.DueAt)
            .Take(200)
            .ToListAsync();

        var mapped = await MapTasksAsync(tasks);

        var day = new MyDayDto
        {
            Date = date,
            AgentName = agent?.DisplayName ?? "—",
            Overdue = mapped.Where(t => t.DueAt < dayStart).ToList(),
            DueToday = mapped.Where(t => t.DueAt >= dayStart && t.DueAt <= dayEnd).ToList(),
            Upcoming = mapped.Where(t => t.DueAt > dayEnd).Take(20).ToList(),
        };

        if (agent is not null)
        {
            var viewings = await Db.Viewings.ForCompany(Tenant)
                .Where(v => v.AgentId == agent.Id && v.ScheduledAt >= dayStart && v.ScheduledAt <= dayEnd)
                .OrderBy(v => v.ScheduledAt)
                .ToListAsync();

            day.Viewings = await MapViewingListAsync(viewings);

            var visits = await Db.SiteVisits.ForCompany(Tenant)
                .Where(v => v.SalesExecutiveId == agent.Id && v.ScheduledAt >= dayStart && v.ScheduledAt <= dayEnd)
                .OrderBy(v => v.ScheduledAt)
                .ToListAsync();

            day.SiteVisits = await MapSiteVisitListAsync(visits);

            var unanswered = await Db.Enquiries.ForCompany(Tenant)
                .Where(e => e.AssignedAgentId == agent.Id && e.FirstContactedAt == null)
                .Where(e => e.Stage != EnquiryStage.Lost && e.Stage != EnquiryStage.Completed)
                .OrderBy(e => e.ResponseDueAt)
                .Take(20)
                .ToListAsync();

            day.UnansweredLeads = await MapEnquiryListAsync(unanswered);
        }

        day.CompletedToday = await Db.FollowUpTasks.ForCompany(Tenant)
            .CountAsync(t => t.CompletedByUserId == userId && t.CompletedAt >= dayStart && t.CompletedAt <= dayEnd);

        day.CallsMadeToday = await Db.Activities.ForCompany(Tenant)
            .CountAsync(a => a.UserId == userId && a.Kind == ActivityKind.Call
                          && a.OccurredAt >= dayStart && a.OccurredAt <= dayEnd);

        day.CollectedToday = await Db.Receipts.ForCompany(Tenant)
            .Where(r => r.ReceivedByUserId == userId && r.ReceivedOn == date && r.Status != ReceiptStatus.Reversed)
            .SumAsync(r => (decimal?)r.Amount);

        var approvals = await Db.ApprovalRequests.ForCompany(Tenant)
            .Where(r => r.Outcome == ApprovalOutcome.Pending)
            .OrderByDescending(r => r.Amount)
            .Take(10)
            .ToListAsync();

        day.AwaitingMyApproval = approvals.Select(r => new ApprovalRequestDto
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
            RequestedAt = r.RequestedAt,
            IsOverdue = r.EscalatesAt is not null && r.EscalatesAt < now,
        }).ToList();

        return day;
    }
}
