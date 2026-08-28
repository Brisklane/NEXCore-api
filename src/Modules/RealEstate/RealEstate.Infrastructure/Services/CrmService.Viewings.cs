using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Viewings, developer site visits, the diary and the keys register.
///
/// Feedback is structured rather than free text, because the loop that keeps a vendor with an
/// agency is "four viewings, all said the price" — and that is a report, not an anecdote.
/// </summary>
public partial class CrmService
{
    // ═══ Diary ═══════════════════════════════════════════════════════════════

    public async Task<DiaryDayDto> GetDiaryAsync(Guid? agentId, DateOnly date, Guid userId)
    {
        var agent = agentId is not null
            ? await Db.AgentProfiles.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == agentId)
            : await Db.AgentProfiles.ForCompany(Tenant).FirstOrDefaultAsync(a => a.UserId == userId);

        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = date.ToDateTime(TimeOnly.MaxValue);

        var day = new DiaryDayDto { Date = date, AgentId = agent?.Id, AgentName = agent?.DisplayName };

        var viewings = await Db.Viewings.ForCompany(Tenant)
            .WhereIf(agent is not null, v => v.AgentId == agent!.Id)
            .Where(v => v.ScheduledAt >= dayStart && v.ScheduledAt <= dayEnd)
            .Where(v => v.Status != ViewingStatus.Cancelled)
            .OrderBy(v => v.ScheduledAt)
            .ToListAsync();

        var viewingDtos = await MapViewingListAsync(viewings);

        foreach (var v in viewingDtos)
        {
            day.Slots.Add(new DiarySlotDto
            {
                Id = v.Id,
                Kind = "viewing",
                StartsAt = v.ScheduledAt,
                EndsAt = v.ScheduledAt.AddMinutes(v.DurationMinutes),
                Title = v.PropertySummary,
                Subtitle = v.ApplicantName,
                Address = v.FirstAddress,
                ContactName = v.ApplicantName,
                ContactPhone = v.ApplicantPhone,
                Tone = v.Status == ViewingStatus.Confirmed ? "success" : null,
                Route = $"/realestate/diary?viewing={v.Id}",
            });
        }

        var visits = await Db.SiteVisits.ForCompany(Tenant)
            .WhereIf(agent is not null, v => v.SalesExecutiveId == agent!.Id)
            .Where(v => v.ScheduledAt >= dayStart && v.ScheduledAt <= dayEnd)
            .Where(v => v.Status != ViewingStatus.Cancelled)
            .OrderBy(v => v.ScheduledAt)
            .ToListAsync();

        var visitDtos = await MapSiteVisitListAsync(visits);

        foreach (var v in visitDtos)
        {
            day.Slots.Add(new DiarySlotDto
            {
                Id = v.Id,
                Kind = "sitevisit",
                StartsAt = v.ScheduledAt,
                EndsAt = v.ScheduledAt.AddHours(2),
                Title = v.ProjectName,
                Subtitle = v.VisitorName,
                ContactName = v.VisitorName,
                ContactPhone = v.VisitorPhone,
                Tone = v.IsRevisit ? "violet" : null,
                Route = $"/realestate/site-visits?visit={v.Id}",
            });
        }

        day.Slots = day.Slots.OrderBy(s => s.StartsAt).ToList();
        day.ViewingCount = viewingDtos.Count;
        day.SiteVisitCount = visitDtos.Count;
        day.TotalTravelMinutes = viewingDtos.Sum(v => v.TravelMinutes);

        // Conflict detection: overlapping slots, or a travel gap that cannot physically be made.
        for (var i = 1; i < day.Slots.Count; i++)
        {
            var previous = day.Slots[i - 1];
            var current = day.Slots[i];

            if (current.StartsAt < previous.EndsAt)
            {
                current.HasConflict = true;
                current.ConflictReason = $"Overlaps “{previous.Title}”.";
                day.ConflictCount++;
            }
            else
            {
                var gap = (current.StartsAt - previous.EndsAt).TotalMinutes;
                var needed = viewingDtos.FirstOrDefault(v => v.Id == current.Id)?.TravelMinutes ?? 0;

                if (needed > 0 && gap < needed)
                {
                    current.HasConflict = true;
                    current.ConflictReason = $"Only {gap:N0} minutes to travel, {needed} needed.";
                    day.ConflictCount++;
                }
            }
        }

        return day;
    }

    // ═══ Viewings ════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<ViewingListItemDto>> GetViewingsAsync(
        ListQueryDto query, Guid? agentId, ViewingStatus? status)
    {
        var q = Db.Viewings.ForCompany(Tenant)
            .WhereIf(agentId.HasValue, v => v.AgentId == agentId)
            .WhereIf(status.HasValue, v => v.Status == status)
            .WhereIf(query.FromDate.HasValue, v => v.ScheduledAt >= query.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(query.ToDate.HasValue, v => v.ScheduledAt <= query.ToDate!.Value.ToDateTime(TimeOnly.MaxValue))
            .OrderByDescending(v => v.ScheduledAt);

        return await PageAsync(q, query, MapViewingListAsync);
    }

    private async Task<List<ViewingListItemDto>> MapViewingListAsync(List<Viewing> viewings)
    {
        if (viewings.Count == 0) return [];

        var ids = viewings.Select(v => v.Id).ToList();

        var stops = await Db.ViewingProperties.ForCompany(Tenant)
            .Where(p => ids.Contains(p.ViewingId))
            .OrderBy(p => p.SequenceNumber)
            .ToListAsync();

        var propertyIds = stops.Select(s => s.PropertyId).Distinct().ToList();
        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant).Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

        var agents = await Db.AgentProfiles.ForCompany(Tenant).ToDictionaryAsync(a => a.Id, a => a.DisplayName);

        var partyIds = viewings.Where(v => v.PartyId.HasValue).Select(v => v.PartyId!.Value).ToList();
        var names = await PartyNamesAsync(partyIds);
        var phones = partyIds.Count == 0
            ? []
            : await Db.Parties.ForCompany(Tenant).Where(p => partyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.PrimaryPhone);

        var feedback = await Db.ViewingFeedbacks.ForCompany(Tenant)
            .Where(f => ids.Contains(f.ViewingId))
            .ToListAsync();

        return viewings.Select(v =>
        {
            var mine = stops.Where(s => s.ViewingId == v.Id).ToList();
            var first = mine.FirstOrDefault();
            var firstProperty = first is null ? null : properties.GetValueOrDefault(first.PropertyId);

            var summary = mine.Count switch
            {
                0 => "No properties",
                1 => firstProperty is null ? "1 property" : RealEstateMapper.OneLineAddress(firstProperty),
                _ => $"{mine.Count} properties",
            };

            return new ViewingListItemDto
            {
                Id = v.Id,
                Reference = v.Reference,
                ScheduledAt = v.ScheduledAt,
                DurationMinutes = v.DurationMinutes,
                TravelMinutes = v.TravelMinutes,
                Status = v.Status,
                AgentName = agents.GetValueOrDefault(v.AgentId, "—"),
                ApplicantName = v.PartyId is null ? null : names.GetValueOrDefault(v.PartyId.Value),
                ApplicantPhone = v.PartyId is null ? null : phones.GetValueOrDefault(v.PartyId.Value),
                EnquiryId = v.EnquiryId,
                PartyId = v.PartyId,
                PropertyCount = mine.Count,
                PropertySummary = summary,
                FirstAddress = firstProperty is null ? null : RealEstateMapper.OneLineAddress(firstProperty),
                FirstLatitude = firstProperty?.Latitude,
                FirstLongitude = firstProperty?.Longitude,
                Access = v.Access,
                ReminderSent = v.ReminderSent,
                VendorNotified = v.VendorNotified,
                FeedbackReceived = v.FeedbackReceived,
                Interest = feedback.FirstOrDefault(f => f.ViewingId == v.Id)?.Interest,
            };
        }).ToList();
    }

    public async Task<ViewingDetailDto?> GetViewingAsync(Guid id)
    {
        var viewing = await Db.Viewings.ForCompany(Tenant)
            .Include(v => v.Properties)
            .Include(v => v.Attendees)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (viewing is null) return null;

        var summary = (await MapViewingListAsync([viewing]))[0];

        var propertyIds = viewing.Properties.Select(p => p.PropertyId).ToList();
        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var heroes = await Db.PropertyMedia.ForCompany(Tenant)
            .Where(m => propertyIds.Contains(m.PropertyId) && m.IsHero)
            .ToDictionaryAsync(m => m.PropertyId, m => m.Url);

        var feedback = await Db.ViewingFeedbacks.ForCompany(Tenant)
            .Where(f => f.ViewingId == id)
            .ToListAsync();

        var attendeeNames = await PartyNamesAsync(
            viewing.Attendees.Where(a => a.PartyId.HasValue).Select(a => a.PartyId!.Value));

        var reasons = await ReasonLabelsAsync(feedback.Select(f => f.ObjectionReasonCodeId));

        var keySet = viewing.KeySetId is null
            ? null
            : await Db.KeySets.ForCompany(Tenant).FirstOrDefaultAsync(k => k.Id == viewing.KeySetId);

        return new ViewingDetailDto
        {
            Id = summary.Id,
            Reference = summary.Reference,
            ScheduledAt = summary.ScheduledAt,
            DurationMinutes = summary.DurationMinutes,
            TravelMinutes = summary.TravelMinutes,
            Status = summary.Status,
            AgentName = summary.AgentName,
            ApplicantName = summary.ApplicantName,
            ApplicantPhone = summary.ApplicantPhone,
            EnquiryId = summary.EnquiryId,
            PartyId = summary.PartyId,
            PropertyCount = summary.PropertyCount,
            PropertySummary = summary.PropertySummary,
            FirstAddress = summary.FirstAddress,
            Access = summary.Access,
            ReminderSent = summary.ReminderSent,
            VendorNotified = summary.VendorNotified,
            FeedbackReceived = summary.FeedbackReceived,
            Interest = summary.Interest,
            AccessNote = viewing.AccessNote,
            KeySetId = viewing.KeySetId,
            KeyLabel = keySet?.Label,
            ConfirmedAt = viewing.ConfirmedAt,
            StartedAt = viewing.StartedAt,
            EndedAt = viewing.EndedAt,
            CancelNote = viewing.CancelNote,
            RouteGeoJson = viewing.RouteGeoJson,

            Properties = viewing.Properties.OrderBy(p => p.SequenceNumber).Select(p =>
            {
                var property = properties.GetValueOrDefault(p.PropertyId);

                return new ViewingPropertyDto
                {
                    Id = p.Id,
                    PropertyId = p.PropertyId,
                    ListingId = p.ListingId,
                    UnitId = p.UnitId,
                    Reference = property?.Reference ?? "—",
                    AddressOneLine = property is null ? "—" : RealEstateMapper.OneLineAddress(property),
                    HeroImageUrl = heroes.GetValueOrDefault(p.PropertyId),
                    AskingPrice = property?.AskingPrice,
                    Latitude = property?.Latitude,
                    Longitude = property?.Longitude,
                    SequenceNumber = p.SequenceNumber,
                    ArrivedAt = p.ArrivedAt,
                    LeftAt = p.LeftAt,
                    WasSeen = p.WasSeen,
                    NotSeenReason = p.NotSeenReason,
                    Feedback = MapFeedback(feedback.FirstOrDefault(f => f.ViewingPropertyId == p.Id), reasons, property),
                };
            }).ToList(),

            Attendees = viewing.Attendees.Select(a => new ViewingAttendeeDto
            {
                Id = a.Id,
                PartyId = a.PartyId,
                Name = (a.PartyId is null ? a.Name : attendeeNames.GetValueOrDefault(a.PartyId.Value) ?? a.Name) ?? string.Empty,
                Phone = a.Phone,
                Role = a.Role,
                Attended = a.Attended,
                IsDecisionMaker = a.IsDecisionMaker,
            }).ToList(),

            Feedback = feedback.Select(f => MapFeedback(f, reasons, null)!).ToList(),
        };
    }

    private static ViewingFeedbackDto? MapFeedback(
        ViewingFeedback? f, Dictionary<Guid, string> reasons, Property? property)
        => f is null ? null : new ViewingFeedbackDto
        {
            Id = f.Id,
            ViewingId = f.ViewingId,
            ViewingPropertyId = f.ViewingPropertyId,
            PropertyId = f.PropertyId,
            PropertyReference = property?.Reference,
            Interest = f.Interest,
            PriceOpinion = f.PriceOpinion,
            WouldOfferAmount = f.WouldOfferAmount,
            Liked = f.Liked,
            Disliked = f.Disliked,
            ObjectionReasonCodeId = f.ObjectionReasonCodeId,
            ObjectionLabel = f.ObjectionReasonCodeId is null ? null : reasons.GetValueOrDefault(f.ObjectionReasonCodeId.Value),
            NextStep = f.NextStep,
            CapturedAt = f.CapturedAt,
            SharedWithVendor = f.SharedWithVendor,
            SharedAt = f.SharedAt,
        };

    public async Task<ViewingDetailDto> SaveViewingAsync(ViewingUpsertDto dto, Guid userId)
    {
        var viewing = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.Viewings.ForCompany(Tenant)
                .Include(v => v.Properties).Include(v => v.Attendees)
                .FirstOrDefaultAsync(v => v.Id == dto.Id)
            : null;

        if (viewing is null)
        {
            viewing = new Viewing
            {
                Reference = await numbering.NextMasterCodeAsync(Db.Viewings, "VWG"),
                Status = ViewingStatus.Scheduled,
            }.StampNew(Tenant, userId);

            Db.Viewings.Add(viewing);
        }
        else
        {
            Db.ViewingProperties.RemoveRange(viewing.Properties);
            Db.ViewingAttendees.RemoveRange(viewing.Attendees);
            viewing.StampUpdated(userId);
        }

        viewing.EnquiryId = dto.EnquiryId;
        viewing.PartyId = dto.PartyId;
        viewing.AgentId = dto.AgentId;
        viewing.OfficeId = dto.OfficeId;
        viewing.ScheduledAt = dto.ScheduledAt;
        viewing.DurationMinutes = dto.DurationMinutes <= 0 ? 30 : dto.DurationMinutes;
        viewing.TravelMinutes = dto.TravelMinutes;
        viewing.Access = dto.Access;
        viewing.AccessNote = dto.AccessNote;
        viewing.KeySetId = dto.KeySetId;

        var sequence = 1;
        foreach (var propertyId in dto.PropertyIds)
        {
            viewing.Properties.Add(new ViewingProperty
            {
                PropertyId = propertyId,
                SequenceNumber = sequence++,
            }.StampNew(Tenant, userId));
        }

        foreach (var attendee in dto.Attendees)
        {
            viewing.Attendees.Add(new ViewingAttendee
            {
                PartyId = attendee.PartyId,
                Name = attendee.Name,
                Phone = attendee.Phone,
                Role = attendee.Role,
                IsDecisionMaker = attendee.IsDecisionMaker,
            }.StampNew(Tenant, userId));
        }

        if (dto.SendConfirmation)
        {
            viewing.ReminderSent = true;

            if (viewing.PartyId is not null)
            {
                await QueueNotificationAsync(
                    "viewing_confirmed",
                    "Your viewing is booked",
                    $"{viewing.ScheduledAt:dddd d MMMM 'at' HH:mm}",
                    $"/portal/customer",
                    recipientPartyId: viewing.PartyId,
                    entityType: "Viewing", entityId: viewing.Id);
            }
        }

        // Statutory notice on a sitting tenant, computed rather than guessed.
        if (dto.ServeAccessNotice && dto.PropertyIds.Count > 0)
        {
            foreach (var propertyId in dto.PropertyIds)
            {
                var tenancy = await Db.Tenancies.ForCompany(Tenant)
                    .FirstOrDefaultAsync(t => t.PropertyId == propertyId && t.Status == TenancyStatus.Active);

                if (tenancy is null) continue;

                Db.AccessNotices.Add(new AccessNotice
                {
                    PropertyId = propertyId,
                    TenancyId = tenancy.Id,
                    ViewingId = viewing.Id,
                    Purpose = "Viewing",
                    ServedAt = DateTime.UtcNow,
                    ServedVia = NotificationChannel.Email,
                    AccessAt = viewing.ScheduledAt,
                    NoticePeriodHours = 24,
                }.StampNew(Tenant, userId));
            }
        }

        if (viewing.EnquiryId is not null)
        {
            var enquiry = await Db.Enquiries.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == viewing.EnquiryId);
            if (enquiry is not null)
            {
                enquiry.ViewingCount++;
                if (enquiry.Stage is EnquiryStage.New or EnquiryStage.Contacted or EnquiryStage.Qualified)
                    enquiry.Stage = EnquiryStage.ViewingBooked;
                enquiry.LastActivityAt = DateTime.UtcNow;
                enquiry.StampUpdated(userId);
            }
        }

        // Keys go out with the agent, and the register says so.
        if (dto.KeySetId is not null && dto.Access == AccessArrangement.AgentHasKeys)
        {
            var keySet = await Db.KeySets.ForCompany(Tenant).FirstOrDefaultAsync(k => k.Id == dto.KeySetId);
            if (keySet is not null && !keySet.IsOut)
            {
                keySet.IsOut = true;
                keySet.CurrentHolderUserId = userId;
                keySet.OutSince = DateTime.UtcNow;
                keySet.DueBackAt = viewing.ScheduledAt.AddHours(4);

                keySet.Movements.Add(new KeyMovement
                {
                    Movement = "Out",
                    OccurredAt = DateTime.UtcNow,
                    HolderUserId = userId,
                    ViewingId = viewing.Id,
                    DueBackAt = keySet.DueBackAt,
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        return (await GetViewingAsync(viewing.Id))!;
    }

    public async Task<ViewingDetailDto> ChangeViewingStatusAsync(
        Guid id, ViewingStatus status, Guid? reasonCodeId, string? note, Guid userId)
    {
        var viewing = await Db.Viewings.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new InvalidOperationException("That viewing does not exist.");

        viewing.Status = status;

        switch (status)
        {
            case ViewingStatus.Confirmed:
                viewing.ConfirmedAt = DateTime.UtcNow;
                break;
            case ViewingStatus.Completed:
                viewing.EndedAt = DateTime.UtcNow;
                viewing.StartedAt ??= viewing.ScheduledAt;
                break;
            case ViewingStatus.Cancelled:
            case ViewingStatus.NoShow:
                viewing.CancelReasonCodeId = reasonCodeId;
                viewing.CancelNote = note;
                break;
        }

        viewing.StampUpdated(userId);

        if (status == ViewingStatus.Completed && viewing.EnquiryId is not null)
        {
            var enquiry = await Db.Enquiries.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == viewing.EnquiryId);
            if (enquiry is not null && enquiry.Stage == EnquiryStage.ViewingBooked)
            {
                enquiry.Stage = EnquiryStage.Viewed;
                enquiry.LastActivityAt = DateTime.UtcNow;
            }
        }

        await Db.SaveChangesAsync();
        return (await GetViewingAsync(id))!;
    }

    public async Task<ViewingFeedbackDto> SaveFeedbackAsync(ViewingFeedbackDto dto, Guid userId)
    {
        var feedback = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.ViewingFeedbacks.ForCompany(Tenant).FirstOrDefaultAsync(f => f.Id == dto.Id)
            : null;

        if (feedback is null)
        {
            feedback = new ViewingFeedback { ViewingId = dto.ViewingId }.StampNew(Tenant, userId);
            Db.ViewingFeedbacks.Add(feedback);
        }
        else feedback.StampUpdated(userId);

        feedback.ViewingPropertyId = dto.ViewingPropertyId;
        feedback.PropertyId = dto.PropertyId;
        feedback.Interest = dto.Interest;
        feedback.PriceOpinion = dto.PriceOpinion;
        feedback.WouldOfferAmount = dto.WouldOfferAmount;
        feedback.Liked = dto.Liked;
        feedback.Disliked = dto.Disliked;
        feedback.ObjectionReasonCodeId = dto.ObjectionReasonCodeId;
        feedback.NextStep = dto.NextStep;
        feedback.CapturedAt = DateTime.UtcNow;
        feedback.CapturedByUserId = userId;

        var viewing = await Db.Viewings.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == dto.ViewingId);
        if (viewing is not null)
        {
            viewing.FeedbackReceived = true;
            viewing.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        var reasons = await ReasonLabelsAsync([feedback.ObjectionReasonCodeId]);
        return MapFeedback(feedback, reasons, null)!;
    }

    /// <summary>
    /// Sends the feedback to the vendor. Vendors leave agencies because they never hear back after
    /// a viewing; this is the loop that closes.
    /// </summary>
    public async Task<ViewingFeedbackDto> ShareFeedbackWithVendorAsync(Guid feedbackId, Guid userId)
    {
        var feedback = await Db.ViewingFeedbacks.ForCompany(Tenant).FirstOrDefaultAsync(f => f.Id == feedbackId)
            ?? throw new InvalidOperationException("That feedback does not exist.");

        feedback.SharedWithVendor = true;
        feedback.SharedAt = DateTime.UtcNow;
        feedback.StampUpdated(userId);

        if (feedback.PropertyId is not null)
        {
            var owner = await Db.PropertyOwnerships.ForCompany(Tenant)
                .Where(o => o.PropertyId == feedback.PropertyId && o.ToDate == null && o.IsPrimaryOwner)
                .Select(o => o.PartyId)
                .FirstOrDefaultAsync();

            if (owner != Guid.Empty)
            {
                await QueueNotificationAsync(
                    "viewing_feedback",
                    "Feedback from a viewing",
                    $"Interest: {feedback.Interest}. {feedback.Liked}".Trim(),
                    "/portal/owner",
                    recipientPartyId: owner,
                    entityType: "ViewingFeedback", entityId: feedback.Id);
            }
        }

        await Db.SaveChangesAsync();

        var reasons = await ReasonLabelsAsync([feedback.ObjectionReasonCodeId]);
        return MapFeedback(feedback, reasons, null)!;
    }

    // ═══ Site visits ═════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<SiteVisitListItemDto>> GetSiteVisitsAsync(
        ListQueryDto query, Guid? projectId, ViewingStatus? status)
    {
        var q = Db.SiteVisits.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, v => v.ProjectId == projectId)
            .WhereIf(status.HasValue, v => v.Status == status)
            .WhereIf(query.FromDate.HasValue, v => v.ScheduledAt >= query.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(query.ToDate.HasValue, v => v.ScheduledAt <= query.ToDate!.Value.ToDateTime(TimeOnly.MaxValue))
            .OrderByDescending(v => v.ScheduledAt);

        return await PageAsync(q, query, MapSiteVisitListAsync);
    }

    private async Task<List<SiteVisitListItemDto>> MapSiteVisitListAsync(List<SiteVisit> visits)
    {
        if (visits.Count == 0) return [];

        var projects = await ProjectNamesAsync(visits.Select(v => (Guid?)v.ProjectId));
        var agents = await Db.AgentProfiles.ForCompany(Tenant).ToDictionaryAsync(a => a.Id, a => a.DisplayName);
        var partners = await Db.ChannelPartners.ForCompany(Tenant).ToDictionaryAsync(p => p.Id, p => p.Name);

        var partyIds = visits.Where(v => v.PartyId.HasValue).Select(v => v.PartyId!.Value).ToList();
        var names = await PartyNamesAsync(partyIds);
        var phones = partyIds.Count == 0
            ? []
            : await Db.Parties.ForCompany(Tenant).Where(p => partyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.PrimaryPhone);

        var ids = visits.Select(v => v.Id).ToList();

        var transports = await Db.SiteVisitTransports.ForCompany(Tenant)
            .Where(t => ids.Contains(t.SiteVisitId))
            .ToDictionaryAsync(t => t.SiteVisitId, t => t);

        var feedback = await Db.SiteVisitFeedbacks.ForCompany(Tenant)
            .Where(f => ids.Contains(f.SiteVisitId))
            .ToDictionaryAsync(f => f.SiteVisitId, f => f);

        return visits.Select(v =>
        {
            var transport = transports.GetValueOrDefault(v.Id);

            return new SiteVisitListItemDto
            {
                Id = v.Id,
                Reference = v.Reference,
                ProjectId = v.ProjectId,
                ProjectName = projects.GetValueOrDefault(v.ProjectId, "—"),
                ScheduledAt = v.ScheduledAt,
                Status = v.Status,
                IsRevisit = v.IsRevisit,
                VisitNumber = v.VisitNumber,
                GuestCount = v.GuestCount,
                VisitorName = v.PartyId is null ? null : names.GetValueOrDefault(v.PartyId.Value),
                VisitorPhone = v.PartyId is null ? null : phones.GetValueOrDefault(v.PartyId.Value),
                EnquiryId = v.EnquiryId,
                PartyId = v.PartyId,
                PartnerName = v.ChannelPartnerId is null ? null : partners.GetValueOrDefault(v.ChannelPartnerId.Value),
                SalesExecutiveName = v.SalesExecutiveId is null ? null : agents.GetValueOrDefault(v.SalesExecutiveId.Value),
                Transport = transport?.Arrangement,
                PickupAddress = transport?.PickupAddress,
                PickupAt = transport?.PickupAt,
                DriverName = transport?.DriverName,
                VehicleNumber = transport?.VehicleNumber,
                ArrivedAt = v.ArrivedAt,
                CostSheetIssued = v.CostSheetIssued,
                ResultingBookingId = v.ResultingBookingId,
                Interest = feedback.GetValueOrDefault(v.Id)?.Interest,
                ReminderSent = v.ReminderSent,
            };
        }).ToList();
    }

    public async Task<SiteVisitDetailDto?> GetSiteVisitAsync(Guid id)
    {
        var visit = await Db.SiteVisits.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == id);
        if (visit is null) return null;

        var summary = (await MapSiteVisitListAsync([visit]))[0];

        var feedback = await Db.SiteVisitFeedbacks.ForCompany(Tenant).FirstOrDefaultAsync(f => f.SiteVisitId == id);
        var reasons = await ReasonLabelsAsync([feedback?.ObjectionReasonCodeId]);

        var unitNumber = visit.ShownUnitId is null
            ? null
            : await Db.Units.ForCompany(Tenant).Where(u => u.Id == visit.ShownUnitId)
                .Select(u => u.UnitNumber).FirstOrDefaultAsync();

        return new SiteVisitDetailDto
        {
            Id = summary.Id,
            Reference = summary.Reference,
            ProjectId = summary.ProjectId,
            ProjectName = summary.ProjectName,
            ScheduledAt = summary.ScheduledAt,
            Status = summary.Status,
            IsRevisit = summary.IsRevisit,
            VisitNumber = summary.VisitNumber,
            GuestCount = summary.GuestCount,
            VisitorName = summary.VisitorName,
            VisitorPhone = summary.VisitorPhone,
            EnquiryId = summary.EnquiryId,
            PartyId = summary.PartyId,
            PartnerName = summary.PartnerName,
            SalesExecutiveName = summary.SalesExecutiveName,
            Transport = summary.Transport,
            PickupAddress = summary.PickupAddress,
            PickupAt = summary.PickupAt,
            DriverName = summary.DriverName,
            VehicleNumber = summary.VehicleNumber,
            ArrivedAt = summary.ArrivedAt,
            CostSheetIssued = summary.CostSheetIssued,
            ResultingBookingId = summary.ResultingBookingId,
            Interest = summary.Interest,
            ReminderSent = summary.ReminderSent,
            LeftAt = visit.LeftAt,
            UnitsShown = visit.UnitsShown,
            ShownUnitId = visit.ShownUnitId,
            ShownUnitNumber = unitNumber,
            BrochureIssued = visit.BrochureIssued,
            Feedback = feedback is null ? null : new SiteVisitFeedbackDto
            {
                Id = feedback.Id,
                SiteVisitId = feedback.SiteVisitId,
                Interest = feedback.Interest,
                PriceOpinion = feedback.PriceOpinion,
                PreferredUnitType = feedback.PreferredUnitType,
                BudgetIndicated = feedback.BudgetIndicated,
                Liked = feedback.Liked,
                ObjectionReasonCodeId = feedback.ObjectionReasonCodeId,
                Objection = feedback.ObjectionReasonCodeId is null
                    ? feedback.Objection
                    : reasons.GetValueOrDefault(feedback.ObjectionReasonCodeId.Value, feedback.Objection ?? ""),
                NextStep = feedback.NextStep,
                NextStepDate = feedback.NextStepDate,
                SatisfactionRating = feedback.SatisfactionRating,
                CapturedAt = feedback.CapturedAt,
            },
        };
    }

    public async Task<SiteVisitDetailDto> SaveSiteVisitAsync(SiteVisitUpsertDto dto, Guid userId)
    {
        var visit = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.SiteVisits.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == dto.Id)
            : null;

        if (visit is null)
        {
            // Revisits convert far better, so the visit number is counted rather than guessed.
            var previous = dto.PartyId is null
                ? 0
                : await Db.SiteVisits.ForCompany(Tenant)
                    .CountAsync(v => v.PartyId == dto.PartyId && v.ProjectId == dto.ProjectId);

            visit = new SiteVisit
            {
                Reference = await numbering.NextMasterCodeAsync(Db.SiteVisits, "SVT"),
                ProjectId = dto.ProjectId,
                Status = ViewingStatus.Scheduled,
                VisitNumber = previous + 1,
                IsRevisit = previous > 0,
            }.StampNew(Tenant, userId);

            Db.SiteVisits.Add(visit);
        }
        else visit.StampUpdated(userId);

        visit.EnquiryId = dto.EnquiryId;
        visit.PartyId = dto.PartyId;
        visit.ChannelPartnerId = dto.ChannelPartnerId;
        visit.SalesExecutiveId = dto.SalesExecutiveId;
        visit.ScheduledAt = dto.ScheduledAt;
        visit.GuestCount = dto.GuestCount <= 0 ? 1 : dto.GuestCount;
        if (dto.IsRevisit) visit.IsRevisit = true;

        await Db.SaveChangesAsync();

        var transport = await Db.SiteVisitTransports.ForCompany(Tenant)
            .FirstOrDefaultAsync(t => t.SiteVisitId == visit.Id);

        if (transport is null)
        {
            transport = new SiteVisitTransport { SiteVisitId = visit.Id }.StampNew(Tenant, userId);
            Db.SiteVisitTransports.Add(transport);
        }
        else transport.StampUpdated(userId);

        transport.Arrangement = dto.Transport;
        transport.PickupAddress = dto.PickupAddress;
        transport.PickupAt = dto.PickupAt;
        transport.PickupLatitude = dto.PickupLatitude;
        transport.PickupLongitude = dto.PickupLongitude;
        transport.DriverUserId = dto.DriverUserId;
        transport.DriverName = dto.DriverName;
        transport.DriverPhone = dto.DriverPhone;
        transport.VehicleNumber = dto.VehicleNumber;
        transport.Cost = dto.TransportCost;
        transport.WasProvided = dto.Transport != TransportArrangement.OwnTransport;

        visit.TransportId = transport.Id;

        if (dto.SendConfirmation)
        {
            visit.ReminderSent = true;

            if (visit.PartyId is not null)
            {
                await QueueNotificationAsync(
                    "site_visit_confirmed",
                    "Your site visit is booked",
                    $"{visit.ScheduledAt:dddd d MMMM 'at' HH:mm}"
                    + (transport.PickupAt is null ? "" : $". Pickup at {transport.PickupAt:HH:mm}."),
                    "/portal/customer",
                    recipientPartyId: visit.PartyId,
                    entityType: "SiteVisit", entityId: visit.Id);
            }
        }

        if (visit.EnquiryId is not null)
        {
            var enquiry = await Db.Enquiries.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == visit.EnquiryId);
            if (enquiry is not null)
            {
                enquiry.SiteVisitCount++;
                if (enquiry.Stage is EnquiryStage.New or EnquiryStage.Contacted or EnquiryStage.Qualified)
                    enquiry.Stage = EnquiryStage.ViewingBooked;
                enquiry.LastActivityAt = DateTime.UtcNow;
                enquiry.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();
        return (await GetSiteVisitAsync(visit.Id))!;
    }

    public async Task<SiteVisitDetailDto> ChangeVisitStatusAsync(
        Guid id, ViewingStatus status, Guid? reasonCodeId, Guid userId)
    {
        var visit = await Db.SiteVisits.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new InvalidOperationException("That site visit does not exist.");

        visit.Status = status;

        switch (status)
        {
            case ViewingStatus.Completed:
                visit.ArrivedAt ??= visit.ScheduledAt;
                visit.LeftAt = DateTime.UtcNow;
                break;
            case ViewingStatus.NoShow:
                visit.NoShowReasonCodeId = reasonCodeId;
                break;
        }

        visit.StampUpdated(userId);

        if (status == ViewingStatus.Completed && visit.EnquiryId is not null)
        {
            var enquiry = await Db.Enquiries.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == visit.EnquiryId);
            if (enquiry is not null && enquiry.Stage == EnquiryStage.ViewingBooked)
            {
                enquiry.Stage = EnquiryStage.Viewed;
                enquiry.LastActivityAt = DateTime.UtcNow;
            }
        }

        await Db.SaveChangesAsync();
        return (await GetSiteVisitAsync(id))!;
    }

    public async Task<SiteVisitFeedbackDto> SaveVisitFeedbackAsync(SiteVisitFeedbackDto dto, Guid userId)
    {
        var feedback = await Db.SiteVisitFeedbacks.ForCompany(Tenant)
            .FirstOrDefaultAsync(f => f.SiteVisitId == dto.SiteVisitId);

        if (feedback is null)
        {
            feedback = new SiteVisitFeedback { SiteVisitId = dto.SiteVisitId }.StampNew(Tenant, userId);
            Db.SiteVisitFeedbacks.Add(feedback);
        }
        else feedback.StampUpdated(userId);

        feedback.Interest = dto.Interest;
        feedback.PriceOpinion = dto.PriceOpinion;
        feedback.PreferredUnitType = dto.PreferredUnitType;
        feedback.BudgetIndicated = dto.BudgetIndicated;
        feedback.Liked = dto.Liked;
        feedback.ObjectionReasonCodeId = dto.ObjectionReasonCodeId;
        feedback.Objection = dto.Objection;
        feedback.NextStep = dto.NextStep;
        feedback.NextStepDate = dto.NextStepDate;
        feedback.SatisfactionRating = dto.SatisfactionRating;
        feedback.CapturedAt = DateTime.UtcNow;
        feedback.CapturedByUserId = userId;

        var visit = await Db.SiteVisits.ForCompany(Tenant).FirstOrDefaultAsync(v => v.Id == dto.SiteVisitId);
        if (visit is not null)
        {
            visit.FeedbackId = feedback.Id;
            visit.StampUpdated(userId);

            // A "next step" with a date is a follow-up, so create it rather than trusting memory.
            if (dto.NextStepDate is not null && visit.SalesExecutiveId is not null)
            {
                var agent = await Db.AgentProfiles.ForCompany(Tenant)
                    .FirstOrDefaultAsync(a => a.Id == visit.SalesExecutiveId);

                Db.FollowUpTasks.Add(new FollowUpTask
                {
                    Title = $"{dto.NextStep ?? "Follow up"} — site visit {visit.Reference}",
                    SuggestedAction = ActivityKind.Call,
                    DueAt = dto.NextStepDate.Value.ToDateTime(new TimeOnly(10, 0)),
                    Priority = dto.Interest >= InterestLevel.VeryInterested ? TicketPriority.High : TicketPriority.Normal,
                    State = TaskState.Open,
                    AssignedToUserId = agent?.UserId ?? userId,
                    PartyId = visit.PartyId,
                    EnquiryId = visit.EnquiryId,
                    IsAutoGenerated = true,
                    SourceRuleKey = "site_visit_next_step",
                }.StampNew(Tenant, userId));
            }
        }

        await Db.SaveChangesAsync();
        dto.Id = feedback.Id;
        dto.CapturedAt = feedback.CapturedAt;
        return dto;
    }

    // ═══ Keys ════════════════════════════════════════════════════════════════

    public async Task<List<KeySetDto>> GetKeysAsync(Guid? propertyId, bool outOnly)
    {
        var sets = await Db.KeySets.ForCompany(Tenant)
            .WhereIf(propertyId.HasValue, k => k.PropertyId == propertyId)
            .WhereIf(outOnly, k => k.IsOut)
            .OrderBy(k => k.Label)
            .ToListAsync();

        if (sets.Count == 0) return [];

        var propertyIds = sets.Select(k => k.PropertyId).Distinct().ToList();
        var properties = await Db.Properties.ForCompany(Tenant)
            .Where(p => propertyIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p);

        var holderNames = await PartyNamesAsync(
            sets.Where(k => k.CurrentHolderPartyId.HasValue).Select(k => k.CurrentHolderPartyId!.Value));

        var now = DateTime.UtcNow;

        return sets.Select(k =>
        {
            var property = properties.GetValueOrDefault(k.PropertyId);

            return new KeySetDto
            {
                Id = k.Id,
                PropertyId = k.PropertyId,
                PropertyReference = property?.Reference ?? "—",
                AddressOneLine = property is null ? "—" : RealEstateMapper.OneLineAddress(property),
                Label = k.Label,
                TagNumber = k.TagNumber,
                KeyCount = k.KeyCount,
                KeySafeLocation = k.KeySafeLocation,
                IsOut = k.IsOut,
                CurrentHolderName = k.CurrentHolderPartyId is null
                    ? null
                    : holderNames.GetValueOrDefault(k.CurrentHolderPartyId.Value),
                OutSince = k.OutSince,
                DueBackAt = k.DueBackAt,
                IsOverdue = k.IsOut && k.DueBackAt is not null && k.DueBackAt < now,
                IsLost = k.IsLost,
                Description = k.Description,
            };
        }).ToList();
    }

    public async Task<KeySetDto> SaveKeySetAsync(KeySetDto dto, Guid userId)
    {
        var set = dto.Id != Guid.Empty
            ? await Db.KeySets.ForCompany(Tenant).FirstOrDefaultAsync(k => k.Id == dto.Id)
            : null;

        if (set is null)
        {
            set = new KeySet { PropertyId = dto.PropertyId }.StampNew(Tenant, userId);
            Db.KeySets.Add(set);
        }
        else set.StampUpdated(userId);

        set.Label = dto.Label;
        set.TagNumber = dto.TagNumber;
        set.KeyCount = dto.KeyCount <= 0 ? 1 : dto.KeyCount;
        set.KeySafeLocation = dto.KeySafeLocation;
        set.Description = dto.Description;
        set.IsLost = dto.IsLost;

        await Db.SaveChangesAsync();
        return (await GetKeysAsync(set.PropertyId, false)).First(k => k.Id == set.Id);
    }

    public async Task<KeySetDto> MoveKeysAsync(
        Guid keySetId, string movement, Guid? holderUserId, Guid? holderPartyId,
        DateTime? dueBack, string? note, Guid userId)
    {
        var set = await Db.KeySets.ForCompany(Tenant).Include(k => k.Movements).FirstOrDefaultAsync(k => k.Id == keySetId)
            ?? throw new InvalidOperationException("That key set does not exist.");

        switch (movement)
        {
            case "Out":
                if (set.IsOut) throw new InvalidOperationException("These keys are already out.");
                set.IsOut = true;
                set.CurrentHolderUserId = holderUserId;
                set.CurrentHolderPartyId = holderPartyId;
                set.OutSince = DateTime.UtcNow;
                set.DueBackAt = dueBack;
                break;

            case "In":
                set.IsOut = false;
                set.CurrentHolderUserId = null;
                set.CurrentHolderPartyId = null;
                set.OutSince = null;
                set.DueBackAt = null;
                break;

            case "Lost":
                set.IsLost = true;
                set.IsOut = false;
                break;

            case "ReleasedToOwner":
                set.IsOut = false;
                set.CurrentHolderPartyId = holderPartyId;
                break;
        }

        set.Movements.Add(new KeyMovement
        {
            Movement = movement,
            OccurredAt = DateTime.UtcNow,
            HolderUserId = holderUserId,
            HolderPartyId = holderPartyId,
            DueBackAt = dueBack,
            Note = note,
        }.StampNew(Tenant, userId));

        set.StampUpdated(userId);
        await Db.SaveChangesAsync();

        return (await GetKeysAsync(set.PropertyId, false)).First(k => k.Id == set.Id);
    }
}
