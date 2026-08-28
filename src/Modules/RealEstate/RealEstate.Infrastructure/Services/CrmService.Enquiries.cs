using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Enquiries, KYC, matching, activities, tasks and the diary — the rest of <see cref="CrmService"/>.
/// </summary>
public partial class CrmService
{
    // ═══ KYC ═════════════════════════════════════════════════════════════════

    public async Task<KycCaseDto?> GetKycAsync(Guid partyId)
    {
        var kyc = await Db.KycCases.ForCompany(Tenant)
            .Include(k => k.Documents)
            .Where(k => k.PartyId == partyId)
            .OrderByDescending(k => k.OpenedOn)
            .FirstOrDefaultAsync();

        return kyc is null ? null : await MapKycAsync(kyc);
    }

    private async Task<KycCaseDto> MapKycAsync(KycCase k)
    {
        var names = await PartyNamesAsync([k.PartyId]);

        return new KycCaseDto
        {
            Id = k.Id,
            Reference = k.Reference,
            PartyId = k.PartyId,
            PartyName = names.GetValueOrDefault(k.PartyId, "—"),
            Status = k.Status,
            RiskRating = k.RiskRating,
            OpenedOn = k.OpenedOn,
            CompletedOn = k.CompletedOn,
            NextReviewDue = k.NextReviewDue,
            SanctionsScreeningRef = k.SanctionsScreeningRef,
            SanctionsScreenedOn = k.SanctionsScreenedOn,
            SanctionsHit = k.SanctionsHit,
            SourceOfFunds = k.SourceOfFunds,
            DeclaredNetWorth = k.DeclaredNetWorth,
            RejectionReason = k.RejectionReason,
            Notes = k.Notes,
            DaysOpen = (k.CompletedOn ?? Today).DayNumber - k.OpenedOn.DayNumber,
            Documents = k.Documents.Select(d => new ChecklistItemDto
            {
                Id = d.Id,
                Key = d.DocumentType,
                Label = d.DocumentType,
                IsSatisfied = d.State is DocumentState.Verified,
                IsMandatory = d.IsMandatory,
                State = d.State,
                Url = d.Url,
                Note = d.RejectionNote,
                SatisfiedOn = d.ExpiresOn,
            }).ToList(),
        };
    }

    public async Task<KycCaseDto> SaveKycAsync(KycCaseDto dto, Guid userId)
    {
        var kyc = dto.Id != Guid.Empty
            ? await Db.KycCases.ForCompany(Tenant).Include(k => k.Documents).FirstOrDefaultAsync(k => k.Id == dto.Id)
            : null;

        if (kyc is null)
        {
            kyc = new KycCase
            {
                Reference = await numbering.NextMasterCodeAsync(Db.KycCases, "KYC"),
                PartyId = dto.PartyId,
                OpenedOn = Today,
                Status = KycStatus.InProgress,
            }.StampNew(Tenant, userId);

            Db.KycCases.Add(kyc);
        }
        else kyc.StampUpdated(userId);

        kyc.RiskRating = dto.RiskRating;
        kyc.NextReviewDue = dto.NextReviewDue;
        kyc.SanctionsScreeningRef = dto.SanctionsScreeningRef;
        kyc.SanctionsScreenedOn = dto.SanctionsScreenedOn;
        kyc.SanctionsHit = dto.SanctionsHit;
        kyc.SourceOfFunds = dto.SourceOfFunds;
        kyc.DeclaredNetWorth = dto.DeclaredNetWorth;
        kyc.Notes = dto.Notes;

        // A sanctions hit never silently passes; it goes to enhanced review.
        if (dto.SanctionsHit && kyc.Status != KycStatus.Rejected) kyc.Status = KycStatus.EnhancedReview;

        foreach (var d in dto.Documents)
        {
            var document = d.Id == Guid.Empty ? null : kyc.Documents.FirstOrDefault(x => x.Id == d.Id);

            if (document is null)
            {
                document = new KycDocument { DocumentType = d.Key }.StampNew(Tenant, userId);
                kyc.Documents.Add(document);
            }
            else document.StampUpdated(userId);

            document.Url = d.Url;
            document.State = d.State ?? DocumentState.Required;
            document.IsMandatory = d.IsMandatory;
        }

        await Db.SaveChangesAsync();
        return await MapKycAsync(kyc);
    }

    public async Task<KycCaseDto> DecideKycAsync(Guid kycCaseId, KycStatus status, string? note, Guid userId)
    {
        var kyc = await Db.KycCases.ForCompany(Tenant).Include(k => k.Documents).FirstOrDefaultAsync(k => k.Id == kycCaseId)
            ?? throw new InvalidOperationException("That KYC case does not exist.");

        if (status == KycStatus.Verified)
        {
            var missing = kyc.Documents
                .Where(d => d.IsMandatory && d.State != DocumentState.Verified && d.State != DocumentState.Waived)
                .Select(d => d.DocumentType)
                .ToList();

            if (missing.Count > 0)
                throw new InvalidOperationException($"These documents are still outstanding: {string.Join(", ", missing)}.");
        }

        kyc.Status = status;
        kyc.CompletedOn = status is KycStatus.Verified or KycStatus.Rejected ? Today : null;
        kyc.ApprovedByUserId = userId;
        kyc.RejectionReason = status == KycStatus.Rejected ? note : null;
        kyc.Notes = string.IsNullOrWhiteSpace(note) ? kyc.Notes : $"{kyc.Notes}\n{note}".Trim();
        kyc.StampUpdated(userId);

        var party = await Db.Parties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == kyc.PartyId);
        if (party is not null)
        {
            party.KycStatus = status;
            party.RiskRating = kyc.RiskRating;
            party.KycVerifiedOn = status == KycStatus.Verified ? Today : null;
            party.KycExpiresOn = status == KycStatus.Verified ? kyc.NextReviewDue : null;
            party.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        return await MapKycAsync(kyc);
    }

    public async Task<PaginatedResponse<KycCaseDto>> GetKycQueueAsync(ListQueryDto query, KycStatus? status)
    {
        var q = Db.KycCases.ForCompany(Tenant)
            .WhereIf(status.HasValue, k => k.Status == status)
            .WhereIf(!status.HasValue, k => k.Status != KycStatus.Verified && k.Status != KycStatus.Rejected)
            .OrderBy(k => k.OpenedOn);

        return await PageAsync(q, query, async rows =>
        {
            var result = new List<KycCaseDto>();
            foreach (var r in rows) result.Add(await MapKycAsync(r));
            return result;
        });
    }

    public async Task<CautionListEntryDto> AddCautionAsync(CautionListEntryDto dto, Guid userId)
    {
        var entry = new CautionListEntry
        {
            PartyId = dto.PartyId,
            Category = dto.Category,
            Severity = dto.Severity,
            Reason = dto.Reason,
            EvidenceUrl = dto.EvidenceUrl,
            RaisedByUserId = userId,
            RaisedOn = Today,
            ExpiresOn = dto.ExpiresOn,
            BlocksNewBusiness = dto.BlocksNewBusiness,
            IsActive = true,
        }.StampNew(Tenant, userId);

        Db.CautionListEntries.Add(entry);

        var party = await Db.Parties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.PartyId);
        if (party is not null)
        {
            party.IsCautioned = true;
            party.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
        dto.Id = entry.Id;
        dto.RaisedOn = entry.RaisedOn;
        dto.IsActive = true;
        return dto;
    }

    public async Task<CautionListEntryDto> ClearCautionAsync(Guid id, string? note, Guid userId)
    {
        var entry = await Db.CautionListEntries.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("That caution does not exist.");

        entry.IsActive = false;
        entry.ClearedOn = Today;
        entry.ClearedByUserId = userId;
        entry.Reason = string.IsNullOrWhiteSpace(note) ? entry.Reason : $"{entry.Reason}\nCleared: {note}";
        entry.StampUpdated(userId);

        var stillCautioned = await Db.CautionListEntries.ForCompany(Tenant)
            .AnyAsync(c => c.PartyId == entry.PartyId && c.IsActive && c.Id != id);

        var party = await Db.Parties.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == entry.PartyId);
        if (party is not null && !stillCautioned)
        {
            party.IsCautioned = false;
            party.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return new CautionListEntryDto
        {
            Id = entry.Id,
            Category = entry.Category,
            Severity = entry.Severity,
            Reason = entry.Reason,
            RaisedOn = entry.RaisedOn,
            BlocksNewBusiness = entry.BlocksNewBusiness,
            IsActive = false,
        };
    }

    public async Task<PaginatedResponse<CautionListEntryDto>> GetCautionListAsync(ListQueryDto query)
    {
        var q = Db.CautionListEntries.ForCompany(Tenant)
            .WhereIf(!query.IncludeInactive, c => c.IsActive)
            .OrderByDescending(c => c.RaisedOn);

        return await PageAsync(q, query, async rows =>
        {
            var names = await PartyNamesAsync(rows.Select(r => r.PartyId));

            return rows.Select(c => new CautionListEntryDto
            {
                Id = c.Id,
                Category = c.Category,
                Severity = c.Severity,
                Reason = c.Reason,
                EvidenceUrl = c.EvidenceUrl,
                RaisedByName = names.GetValueOrDefault(c.PartyId, "—"),
                RaisedOn = c.RaisedOn,
                ExpiresOn = c.ExpiresOn,
                BlocksNewBusiness = c.BlocksNewBusiness,
                IsActive = c.IsActive,
            }).ToList();
        });
    }

    // ═══ Enquiries ═══════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<EnquiryListItemDto>> GetEnquiriesAsync(EnquirySearchDto query)
        => await PageAsync(BuildEnquiryQuery(query), query, MapEnquiryListAsync);

    private IOrderedQueryable<Enquiry> BuildEnquiryQuery(EnquirySearchDto query)
    {
        var now = DateTime.UtcNow;

        return Db.Enquiries.ForCompany(Tenant)
            .WhereIf(query.Stages.Count > 0, e => query.Stages.Contains(e.Stage))
            .WhereIf(query.Channels.Count > 0, e => query.Channels.Contains(e.Channel))
            .WhereIf(query.AssignedAgentId.HasValue, e => e.AssignedAgentId == query.AssignedAgentId)
            .WhereIf(query.ChannelPartnerId.HasValue, e => e.ChannelPartnerId == query.ChannelPartnerId)
            .WhereIf(query.CampaignId.HasValue, e => e.CampaignId == query.CampaignId)
            .WhereIf(query.ProjectId.HasValue, e => e.ProjectId == query.ProjectId)
            .WhereIf(query.Interest.HasValue, e => e.Interest == query.Interest)
            .WhereIf(query.BreachingSlaOnly == true, e => e.SlaBreached && e.FirstContactedAt == null)
            .WhereIf(query.UnassignedOnly == true, e => e.AssignedAgentId == null)
            .WhereIf(query.OverdueFollowUpOnly == true, e => e.NextFollowUpAt != null && e.NextFollowUpAt < now)
            .WhereIf(query.MinScore.HasValue, e => e.Score >= query.MinScore)
            .WhereIf(query.FromDate.HasValue, e => e.ReceivedAt >= query.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(query.ToDate.HasValue, e => e.ReceivedAt <= query.ToDate!.Value.ToDateTime(TimeOnly.MaxValue))
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                e => e.Reference.Contains(query.Search!)
                     || (e.ContactName != null && e.ContactName.Contains(query.Search!))
                     || (e.ContactPhone != null && e.ContactPhone.Contains(query.Search!)))
            .OrderByDescending(e => e.ReceivedAt);
    }

    private async Task<List<EnquiryListItemDto>> MapEnquiryListAsync(List<Enquiry> enquiries)
    {
        if (enquiries.Count == 0) return [];

        var partyIds = enquiries.Where(e => e.PartyId.HasValue).Select(e => e.PartyId!.Value).ToList();
        var names = await PartyNamesAsync(partyIds);
        var projects = await ProjectNamesAsync(enquiries.Select(e => e.ProjectId));
        var agents = await Db.AgentProfiles.ForCompany(Tenant).ToDictionaryAsync(a => a.Id, a => a.DisplayName);
        var partners = await Db.ChannelPartners.ForCompany(Tenant).ToDictionaryAsync(p => p.Id, p => p.Name);
        var portals = await Db.PortalChannels.ForCompany(Tenant).ToDictionaryAsync(p => p.Id, p => p.Name);

        var propertyIds = enquiries.Where(e => e.PropertyId.HasValue).Select(e => e.PropertyId!.Value).ToList();
        var properties = propertyIds.Count == 0
            ? []
            : await Db.Properties.ForCompany(Tenant).Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Reference);

        var now = DateTime.UtcNow;
        var currency = await CurrencyAsync();

        return enquiries.Select(e =>
        {
            var contactName = e.PartyId is not null
                ? names.GetValueOrDefault(e.PartyId.Value, e.ContactName ?? "—")
                : e.ContactName ?? "—";

            return new EnquiryListItemDto
            {
                Id = e.Id,
                Reference = e.Reference,
                PartyId = e.PartyId,
                ContactName = contactName,
                ContactPhone = e.ContactPhone,
                ContactEmail = e.ContactEmail,
                Stage = e.Stage,
                Channel = e.Channel,
                SourceLabel = e.PortalChannelId is not null
                    ? portals.GetValueOrDefault(e.PortalChannelId.Value)
                    : e.SubSource,
                Interest = e.Interest,
                ProjectName = e.ProjectId is null ? null : projects.GetValueOrDefault(e.ProjectId.Value),
                PropertyReference = e.PropertyId is null ? null : properties.GetValueOrDefault(e.PropertyId.Value),
                BudgetMin = e.BudgetMin,
                BudgetMax = e.BudgetMax,
                CurrencyCode = currency,
                AssignedAgentName = e.AssignedAgentId is null ? null : agents.GetValueOrDefault(e.AssignedAgentId.Value),
                ReceivedAt = e.ReceivedAt,
                FirstContactedAt = e.FirstContactedAt,
                ResponseDueAt = e.ResponseDueAt,
                SlaBreached = e.SlaBreached,
                SpeedToLeadMinutes = e.FirstContactedAt is null
                    ? null
                    : (int)(e.FirstContactedAt.Value - e.ReceivedAt).TotalMinutes,
                MinutesToSlaDeadline = e.FirstContactedAt is not null || e.ResponseDueAt is null
                    ? null
                    : (int)(e.ResponseDueAt.Value - now).TotalMinutes,
                LastActivityAt = e.LastActivityAt,
                NextFollowUpAt = e.NextFollowUpAt,
                FollowUpOverdue = e.NextFollowUpAt is not null && e.NextFollowUpAt < now,
                Score = e.Score,
                ViewingCount = e.ViewingCount,
                SiteVisitCount = e.SiteVisitCount,
                PartnerName = e.ChannelPartnerId is null ? null : partners.GetValueOrDefault(e.ChannelPartnerId.Value),
                DaysInStage = (int)(now - (e.LastActivityAt ?? e.ReceivedAt)).TotalDays,
            };
        }).ToList();
    }

    public async Task<EnquiryBoardDto> GetBoardAsync(EnquirySearchDto query)
    {
        var stages = query.Stages.Count > 0
            ? query.Stages
            : [EnquiryStage.New, EnquiryStage.Contacted, EnquiryStage.Qualified, EnquiryStage.ViewingBooked,
               EnquiryStage.Viewed, EnquiryStage.Negotiation, EnquiryStage.Tokened, EnquiryStage.Booked];

        var perColumn = query.ItemsPerColumn <= 0 ? 25 : query.ItemsPerColumn;
        var board = new EnquiryBoardDto();

        var baseQuery = BuildEnquiryQuery(new EnquirySearchDto
        {
            Channels = query.Channels,
            AssignedAgentId = query.AssignedAgentId,
            ChannelPartnerId = query.ChannelPartnerId,
            CampaignId = query.CampaignId,
            ProjectId = query.ProjectId,
            Interest = query.Interest,
            BreachingSlaOnly = query.BreachingSlaOnly,
            UnassignedOnly = query.UnassignedOnly,
            OverdueFollowUpOnly = query.OverdueFollowUpOnly,
            MinScore = query.MinScore,
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            Search = query.Search,
        });

        foreach (var stage in stages)
        {
            var stageQuery = baseQuery.Where(e => e.Stage == stage);
            var count = await stageQuery.CountAsync();
            var rows = await stageQuery.Take(perColumn).ToListAsync();
            var items = await MapEnquiryListAsync(rows);

            board.Columns.Add(new EnquiryBoardColumnDto
            {
                Stage = stage,
                Label = StageLabel(stage),
                Count = count,
                Value = rows.Sum(r => r.BudgetMax ?? r.BudgetMin ?? 0m),
                Items = items,
                HasMore = count > perColumn,
            });
        }

        board.TotalCount = board.Columns.Sum(c => c.Count);
        board.TotalPipelineValue = board.Columns.Sum(c => c.Value);
        board.BreachingSlaCount = await baseQuery.CountAsync(e => e.SlaBreached && e.FirstContactedAt == null);
        board.UnassignedCount = await baseQuery.CountAsync(e => e.AssignedAgentId == null);

        return board;
    }

    private static string StageLabel(EnquiryStage stage) => stage switch
    {
        EnquiryStage.New => "New",
        EnquiryStage.Contacted => "Contacted",
        EnquiryStage.Qualified => "Qualified",
        EnquiryStage.ViewingBooked => "Viewing booked",
        EnquiryStage.Viewed => "Viewed",
        EnquiryStage.Revisit => "Revisit",
        EnquiryStage.Negotiation => "Negotiating",
        EnquiryStage.OfferMade => "Offer made",
        EnquiryStage.Tokened => "Token taken",
        EnquiryStage.Agreed => "Agreed",
        EnquiryStage.Booked => "Booked",
        EnquiryStage.Completed => "Completed",
        EnquiryStage.Lost => "Lost",
        EnquiryStage.Dormant => "Nurture",
        _ => stage.ToString(),
    };

    public async Task<EnquiryDetailDto?> GetEnquiryAsync(Guid id)
    {
        var enquiry = await Db.Enquiries.ForCompany(Tenant)
            .Include(e => e.StageHistory)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enquiry is null) return null;

        var summary = (await MapEnquiryListAsync([enquiry]))[0];

        var detail = new EnquiryDetailDto
        {
            Id = summary.Id,
            Reference = summary.Reference,
            PartyId = summary.PartyId,
            ContactName = summary.ContactName,
            ContactPhone = summary.ContactPhone,
            ContactEmail = summary.ContactEmail,
            Stage = summary.Stage,
            Channel = summary.Channel,
            SourceLabel = summary.SourceLabel,
            Interest = summary.Interest,
            ProjectName = summary.ProjectName,
            PropertyReference = summary.PropertyReference,
            BudgetMin = summary.BudgetMin,
            BudgetMax = summary.BudgetMax,
            CurrencyCode = summary.CurrencyCode,
            AssignedAgentName = summary.AssignedAgentName,
            ReceivedAt = summary.ReceivedAt,
            FirstContactedAt = summary.FirstContactedAt,
            ResponseDueAt = summary.ResponseDueAt,
            SlaBreached = summary.SlaBreached,
            SpeedToLeadMinutes = summary.SpeedToLeadMinutes,
            MinutesToSlaDeadline = summary.MinutesToSlaDeadline,
            LastActivityAt = summary.LastActivityAt,
            NextFollowUpAt = summary.NextFollowUpAt,
            FollowUpOverdue = summary.FollowUpOverdue,
            Score = summary.Score,
            ViewingCount = summary.ViewingCount,
            SiteVisitCount = summary.SiteVisitCount,
            PartnerName = summary.PartnerName,
            DaysInStage = summary.DaysInStage,

            SubSource = enquiry.SubSource,
            PortalChannelId = enquiry.PortalChannelId,
            CampaignId = enquiry.CampaignId,
            ChannelPartnerId = enquiry.ChannelPartnerId,
            ReferredByPartyId = enquiry.ReferredByPartyId,
            ListingId = enquiry.ListingId,
            PropertyId = enquiry.PropertyId,
            ProjectId = enquiry.ProjectId,
            Purpose = enquiry.Purpose,
            Funding = enquiry.Funding,
            Timeline = enquiry.Timeline,
            Message = enquiry.Message,
            IsQualified = enquiry.IsQualified,
            ScoreBreakdown = enquiry.ScoreBreakdown,
            ConvertedBookingId = enquiry.ConvertedBookingId,
            ConvertedDealId = enquiry.ConvertedDealId,
            ConvertedTenancyId = enquiry.ConvertedTenancyId,
            ClosedAt = enquiry.ClosedAt,
            LossNote = enquiry.LossNote,
            LostToCompetitor = enquiry.LostToCompetitor,

            StageHistory = enquiry.StageHistory.OrderByDescending(h => h.ChangedAt).Select(h => new EnquiryStageHistoryDto
            {
                FromStage = h.FromStage,
                ToStage = h.ToStage,
                ChangedAt = h.ChangedAt,
                DaysInPreviousStage = h.DaysInPreviousStage,
                Note = h.Note,
            }).ToList(),
        };

        if (enquiry.LossReasonCodeId is not null)
        {
            var reasons = await ReasonLabelsAsync([enquiry.LossReasonCodeId]);
            detail.LossReason = reasons.GetValueOrDefault(enquiry.LossReasonCodeId.Value);
        }

        var activities = await Db.Activities.ForCompany(Tenant)
            .Where(a => a.EnquiryId == id)
            .OrderByDescending(a => a.OccurredAt)
            .Take(100)
            .ToListAsync();

        detail.Activities = activities.Select(MapActivity).ToList();

        var tasks = await Db.FollowUpTasks.ForCompany(Tenant)
            .Where(t => t.EnquiryId == id && t.State == TaskState.Open)
            .OrderBy(t => t.DueAt)
            .ToListAsync();

        detail.Tasks = await MapTasksAsync(tasks);

        if (enquiry.RequirementProfileId is not null)
        {
            var requirements = await GetRequirementsAsync(enquiry.PartyId ?? Guid.Empty);
            detail.Requirement = requirements.FirstOrDefault(r => r.Id == enquiry.RequirementProfileId);

            if (detail.Requirement is not null)
                detail.Matches = await GetStoredMatchesAsync(detail.Requirement.Id, 20);
        }

        return detail;
    }

    /// <summary>
    /// Takes a lead in.
    ///
    /// Deduplicates on phone and email, starts the speed-to-lead clock, routes it to an agent by
    /// territory and round-robin, and scores it — all before the caller gets a reply, because an
    /// enquiry that sits unassigned for an hour is an enquiry that has gone somewhere else.
    /// </summary>
    public async Task<EnquiryDetailDto> SaveEnquiryAsync(EnquiryUpsertDto dto, Guid userId)
    {
        var settings = await SettingsAsync();

        var enquiry = dto.Id is not null && dto.Id != Guid.Empty
            ? await Db.Enquiries.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == dto.Id)
            : null;

        var isNew = enquiry is null;

        if (enquiry is null)
        {
            // Same phone or email inside the SLA window is the same enquiry, not a second one.
            if (!dto.AcknowledgeDuplicate && !string.IsNullOrWhiteSpace(dto.ContactPhone))
            {
                var recent = await Db.Enquiries.ForCompany(Tenant)
                    .Where(e => e.ContactPhone == dto.ContactPhone)
                    .Where(e => e.Stage != EnquiryStage.Lost && e.Stage != EnquiryStage.Completed)
                    .OrderByDescending(e => e.ReceivedAt)
                    .FirstOrDefaultAsync();

                if (recent is not null && (DateTime.UtcNow - recent.ReceivedAt).TotalDays < 30)
                {
                    // Log the touch against the existing enquiry rather than creating a duplicate.
                    Db.Activities.Add(new Activity
                    {
                        Kind = ActivityKind.Note,
                        Direction = ActivityDirection.Inbound,
                        PartyId = recent.PartyId,
                        EnquiryId = recent.Id,
                        OccurredAt = DateTime.UtcNow,
                        Subject = "Repeat enquiry",
                        Body = $"Another enquiry arrived from {dto.Channel}. {dto.Message}".Trim(),
                        IsSystemGenerated = true,
                    }.StampNew(Tenant, userId));

                    recent.LastActivityAt = DateTime.UtcNow;
                    await Db.SaveChangesAsync();

                    return (await GetEnquiryAsync(recent.Id))!;
                }
            }

            enquiry = new Enquiry
            {
                Reference = await numbering.NextEnquiryNumberAsync(DateTime.UtcNow),
                ReceivedAt = DateTime.UtcNow,
                Stage = EnquiryStage.New,
            }.StampNew(Tenant, userId);

            enquiry.ResponseDueAt = enquiry.ReceivedAt.AddMinutes(settings.LeadResponseSlaMinutes);
            Db.Enquiries.Add(enquiry);
        }
        else enquiry.StampUpdated(userId);

        enquiry.PartyId = dto.PartyId ?? enquiry.PartyId;
        enquiry.ContactName = dto.ContactName ?? enquiry.ContactName;
        enquiry.ContactPhone = dto.ContactPhone ?? enquiry.ContactPhone;
        enquiry.ContactEmail = dto.ContactEmail ?? enquiry.ContactEmail;
        enquiry.Channel = dto.Channel;
        enquiry.SubSource = dto.SubSource;
        enquiry.PortalChannelId = dto.PortalChannelId;
        enquiry.CampaignId = dto.CampaignId;
        enquiry.ChannelPartnerId = dto.ChannelPartnerId;
        enquiry.ReferredByPartyId = dto.ReferredByPartyId;
        enquiry.MarketingEventId = dto.MarketingEventId;
        enquiry.Interest = dto.Interest;
        enquiry.ListingId = dto.ListingId;
        enquiry.PropertyId = dto.PropertyId;
        enquiry.ProjectId = dto.ProjectId;
        enquiry.Purpose = dto.Purpose;
        enquiry.Funding = dto.Funding;
        enquiry.BudgetMin = dto.BudgetMin;
        enquiry.BudgetMax = dto.BudgetMax;
        enquiry.Timeline = dto.Timeline;
        enquiry.Message = dto.Message;
        enquiry.OfficeId = dto.OfficeId;

        if (dto.AssignedAgentId is not null)
        {
            enquiry.AssignedAgentId = dto.AssignedAgentId;
            enquiry.AssignedAt = DateTime.UtcNow;
        }
        else if (isNew && !dto.SkipAutoAssign)
        {
            var assigned = await RouteAsync(enquiry);
            if (assigned is not null)
            {
                enquiry.AssignedAgentId = assigned.Value.AgentId;
                enquiry.TerritoryId = assigned.Value.TerritoryId;
                enquiry.AssignedAt = DateTime.UtcNow;
            }
        }

        await Db.SaveChangesAsync();

        if (dto.Requirement is not null)
        {
            dto.Requirement.PartyId ??= enquiry.PartyId;
            dto.Requirement.EnquiryId = enquiry.Id;
            var requirement = await SaveRequirementAsync(dto.Requirement, userId);
            enquiry.RequirementProfileId = requirement.Id;
        }

        ScoreEnquiry(enquiry);
        await Db.SaveChangesAsync();

        if (isNew && enquiry.AssignedAgentId is not null)
        {
            await QueueNotificationAsync(
                "lead_assigned",
                $"New enquiry — {enquiry.ContactName}",
                $"{enquiry.Channel}. Respond by {enquiry.ResponseDueAt:HH:mm}.",
                $"/realestate/enquiries/{enquiry.Id}",
                entityType: "Enquiry", entityId: enquiry.Id,
                severity: AlertSeverity.Warning);

            await Db.SaveChangesAsync();
        }

        return (await GetEnquiryAsync(enquiry.Id))!;
    }

    /// <summary>
    /// Picks the agent. Territory first, then weighted round-robin among agents who are on, not on
    /// leave, and not already at their open-lead ceiling.
    /// </summary>
    private async Task<(Guid AgentId, Guid? TerritoryId)?> RouteAsync(Enquiry enquiry)
    {
        var candidates = await Db.TerritoryAssignments.ForCompany(Tenant)
            .Where(a => a.EffectiveTo == null || a.EffectiveTo >= Today)
            .ToListAsync();

        if (candidates.Count == 0) return null;

        var territories = await Db.Territories.ForCompany(Tenant)
            .Where(t => t.IsActive)
            .Include(t => t.Areas)
            .ToListAsync();

        // Narrow by price band and category where the territory declares them.
        var budget = enquiry.BudgetMax ?? enquiry.BudgetMin;

        var matching = territories.Where(t =>
            (t.MinPrice is null || budget is null || budget >= t.MinPrice) &&
            (t.MaxPrice is null || budget is null || budget <= t.MaxPrice)).ToList();

        var territoryIds = matching.Select(t => t.Id).ToHashSet();
        var pool = candidates.Where(c => territoryIds.Contains(c.TerritoryId)).ToList();
        if (pool.Count == 0) pool = candidates;

        var agentIds = pool.Select(p => p.AgentProfileId).Distinct().ToList();

        var agents = await Db.AgentProfiles.ForCompany(Tenant)
            .Where(a => agentIds.Contains(a.Id) && a.IsActive && a.AcceptsNewLeads && !a.IsOnLeave)
            .ToListAsync();

        if (agents.Count == 0) return null;

        var openCounts = await Db.Enquiries.ForCompany(Tenant)
            .Where(e => e.AssignedAgentId != null && agentIds.Contains(e.AssignedAgentId!.Value))
            .Where(e => e.Stage != EnquiryStage.Lost && e.Stage != EnquiryStage.Completed)
            .GroupBy(e => e.AssignedAgentId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var available = agents
            .Where(a => openCounts.GetValueOrDefault(a.Id) < a.MaxOpenLeads)
            .OrderBy(a => a.RoundRobinPosition)
            .ThenBy(a => openCounts.GetValueOrDefault(a.Id))
            .ToList();

        if (available.Count == 0) return null;

        var chosen = available[0];
        chosen.RoundRobinPosition++;

        var assignment = pool.FirstOrDefault(p => p.AgentProfileId == chosen.Id);
        return (chosen.Id, assignment?.TerritoryId);
    }

    /// <summary>
    /// Scores the lead and records why. Explainable on purpose — an agent who cannot see why a
    /// lead scored 30 will ignore the score entirely.
    /// </summary>
    private static void ScoreEnquiry(Enquiry enquiry)
    {
        var parts = new List<string>();
        var score = 0;

        var sourceScore = enquiry.Channel switch
        {
            EnquiryChannel.Referral => 25,
            EnquiryChannel.WalkIn => 22,
            EnquiryChannel.ChannelPartner => 18,
            EnquiryChannel.Exhibition => 15,
            EnquiryChannel.Portal => 12,
            EnquiryChannel.Website => 12,
            EnquiryChannel.Campaign => 10,
            EnquiryChannel.ColdCall => 4,
            _ => 8,
        };
        score += sourceScore;
        parts.Add($"Source ({enquiry.Channel}): +{sourceScore}");

        if (enquiry.BudgetMax is > 0m || enquiry.BudgetMin is > 0m)
        {
            score += 15;
            parts.Add("Budget stated: +15");
        }

        if (enquiry.Funding is FundingKind.Cash or FundingKind.CompanyFunds)
        {
            score += 15;
            parts.Add("Cash buyer: +15");
        }
        else if (enquiry.Funding is FundingKind.Mortgage or FundingKind.Instalments)
        {
            score += 8;
            parts.Add("Financed: +8");
        }

        if (enquiry.SiteVisitCount > 0)
        {
            var visitScore = Math.Min(20, enquiry.SiteVisitCount * 10);
            score += visitScore;
            parts.Add($"{enquiry.SiteVisitCount} site visit(s): +{visitScore}");
        }

        if (enquiry.ViewingCount > 0)
        {
            var viewScore = Math.Min(15, enquiry.ViewingCount * 5);
            score += viewScore;
            parts.Add($"{enquiry.ViewingCount} viewing(s): +{viewScore}");
        }

        var ageDays = (DateTime.UtcNow - (enquiry.LastActivityAt ?? enquiry.ReceivedAt)).TotalDays;
        if (ageDays > 30)
        {
            var decay = Math.Min(25, (int)((ageDays - 30) / 7) * 5);
            score -= decay;
            parts.Add($"No contact for {(int)ageDays} days: −{decay}");
        }

        if (enquiry.SlaBreached && enquiry.FirstContactedAt is null)
        {
            score -= 10;
            parts.Add("First response missed: −10");
        }

        enquiry.Score = Math.Clamp(score, 0, 100);
        enquiry.ScoreBreakdown = string.Join("\n", parts);
        enquiry.IsQualified = enquiry.Score >= 50 && enquiry.BudgetMax is > 0m;
    }

    public async Task<EnquiryDetailDto> ChangeStageAsync(EnquiryStageChangeDto dto, Guid userId)
    {
        var enquiry = await Db.Enquiries.ForCompany(Tenant)
            .Include(e => e.StageHistory)
            .FirstOrDefaultAsync(e => e.Id == dto.EnquiryId)
            ?? throw new InvalidOperationException("That enquiry does not exist.");

        if (dto.ToStage == EnquiryStage.Lost && dto.LossReasonCodeId is null)
            throw new InvalidOperationException(
                "A lost lead needs a reason — otherwise the loss teaches nobody anything.");

        var previous = enquiry.Stage;
        var lastChange = enquiry.StageHistory.OrderByDescending(h => h.ChangedAt).FirstOrDefault();
        var daysInPrevious = (int)(DateTime.UtcNow - (lastChange?.ChangedAt ?? enquiry.ReceivedAt)).TotalDays;

        enquiry.StageHistory.Add(new EnquiryStageHistory
        {
            FromStage = previous,
            ToStage = dto.ToStage,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = userId,
            DaysInPreviousStage = daysInPrevious,
            Note = dto.Note,
        }.StampNew(Tenant, userId));

        enquiry.Stage = dto.ToStage;
        enquiry.LastActivityAt = DateTime.UtcNow;

        if (dto.ToStage is EnquiryStage.Lost or EnquiryStage.Completed)
        {
            enquiry.ClosedAt = DateTime.UtcNow;
            enquiry.LossReasonCodeId = dto.LossReasonCodeId;
            enquiry.LossNote = dto.LossNote;
            enquiry.LostToCompetitor = dto.LostToCompetitor;
        }

        // Moving off New counts as first contact if nothing else has.
        if (previous == EnquiryStage.New && enquiry.FirstContactedAt is null)
            enquiry.FirstContactedAt = DateTime.UtcNow;

        ScoreEnquiry(enquiry);
        enquiry.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetEnquiryAsync(enquiry.Id))!;
    }

    public async Task<EnquiryDetailDto> AssignAsync(Guid enquiryId, Guid agentId, Guid userId)
    {
        var enquiry = await Db.Enquiries.ForCompany(Tenant).FirstOrDefaultAsync(e => e.Id == enquiryId)
            ?? throw new InvalidOperationException("That enquiry does not exist.");

        var agent = await Db.AgentProfiles.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == agentId)
            ?? throw new InvalidOperationException("That agent does not exist.");

        // An expired mandatory licence stops new instructions in several markets.
        var blocked = await Db.AgentLicences.ForCompany(Tenant)
            .AnyAsync(l => l.AgentProfileId == agentId && l.BlocksAssignmentWhenExpired
                        && l.ExpiresOn != null && l.ExpiresOn < Today);

        if (blocked)
            throw new InvalidOperationException($"{agent.DisplayName}'s licence has expired, so they cannot take new leads.");

        enquiry.AssignedAgentId = agentId;
        enquiry.AssignedAt = DateTime.UtcNow;
        enquiry.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetEnquiryAsync(enquiryId))!;
    }

    public async Task<int> RouteUnassignedAsync()
    {
        var unassigned = await Db.Enquiries.ForCompany(Tenant)
            .Where(e => e.AssignedAgentId == null && e.Stage != EnquiryStage.Lost && e.Stage != EnquiryStage.Completed)
            .ToListAsync();

        var routed = 0;

        foreach (var enquiry in unassigned)
        {
            var assigned = await RouteAsync(enquiry);
            if (assigned is null) continue;

            enquiry.AssignedAgentId = assigned.Value.AgentId;
            enquiry.TerritoryId = assigned.Value.TerritoryId;
            enquiry.AssignedAt = DateTime.UtcNow;
            routed++;
        }

        await Db.SaveChangesAsync();
        return routed;
    }

    /// <summary>
    /// Marks breached SLAs and escalates them. Conversion collapses when first contact is slow, so
    /// this runs often and puts the breach in front of a manager rather than in a report.
    /// </summary>
    public async Task<int> FlagSlaBreachesAsync()
    {
        var now = DateTime.UtcNow;

        var breaching = await Db.Enquiries.ForCompany(Tenant)
            .Where(e => !e.SlaBreached && e.FirstContactedAt == null && e.ResponseDueAt != null && e.ResponseDueAt < now)
            .ToListAsync();

        foreach (var enquiry in breaching)
        {
            enquiry.SlaBreached = true;
            ScoreEnquiry(enquiry);

            await QueueNotificationAsync(
                "lead_sla_breach",
                $"Enquiry {enquiry.Reference} has gone unanswered",
                $"{enquiry.ContactName} arrived {(int)(now - enquiry.ReceivedAt).TotalMinutes} minutes ago.",
                $"/realestate/enquiries/{enquiry.Id}",
                entityType: "Enquiry", entityId: enquiry.Id,
                severity: AlertSeverity.Critical);
        }

        await Db.SaveChangesAsync();
        return breaching.Count;
    }
}
