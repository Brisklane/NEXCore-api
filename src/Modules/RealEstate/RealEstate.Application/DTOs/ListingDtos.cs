using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Listings, instructions, portal syndication, viewings and campaigns.
// =====================================================================================

public class ListingListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string? PropertyReference { get; set; }

    public ListingKind Kind { get; set; }
    public ListingStatus Status { get; set; }
    public string? Headline { get; set; }
    public string AddressOneLine { get; set; } = string.Empty;
    public string? HeroImageUrl { get; set; }

    public PropertySubType SubType { get; set; }
    public AreaDto? Area { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }

    public decimal? AskingPrice { get; set; }
    public bool PriceOnApplication { get; set; }
    public RentFrequency? RentFrequency { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public string? ListingAgentName { get; set; }
    public AgencyBasis? AgencyBasis { get; set; }
    public DateOnly? ListedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public int? DaysOnMarket { get; set; }

    public int ViewCount { get; set; }
    public int EnquiryCount { get; set; }
    public int ViewingCount { get; set; }
    public int OfferCount { get; set; }
    public DateTime? LastViewingAt { get; set; }

    /// <summary>Days since the last viewing at the current price. The stale-listing signal.</summary>
    public int? DaysSinceLastViewing { get; set; }

    public int PublishedPortalCount { get; set; }
    public int FailedPortalCount { get; set; }
    public bool IsFeatured { get; set; }
}

public class ListingDetailDto : ListingListItemDto
{
    public Guid? InstructionId { get; set; }
    public Guid? OfficeId { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? ServiceChargeAmount { get; set; }

    public DateOnly? AvailableFrom { get; set; }
    public bool VacantPossession { get; set; }
    public bool TenantInSitu { get; set; }
    public int? NoticeRequiredDays { get; set; }
    public bool IsChainFree { get; set; }

    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public List<string> KeyFeatures { get; set; } = [];
    public string? EnergyRating { get; set; }
    public string? CouncilTaxBand { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? RegulatoryPermitNumber { get; set; }

    public bool AllowPortalPublish { get; set; }
    public bool AllowWebsitePublish { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedByName { get; set; }
    public DateTime? WithdrawnAt { get; set; }

    public PropertyDetailDto? Property { get; set; }
    public InstructionDto? Instruction { get; set; }
    public List<PropertyMediaDto> Media { get; set; } = [];
    public List<ListingPriceHistoryDto> PriceHistory { get; set; } = [];
    public List<PortalPublicationDto> Publications { get; set; } = [];
    public List<ViewingListItemDto> Viewings { get; set; } = [];
    public List<OfferListItemDto> Offers { get; set; } = [];

    /// <summary>Media and compliance gaps that block publishing. Enforced before publish, not after a fine.</summary>
    public List<ChecklistItemDto> PublishChecklist { get; set; } = [];
    public bool CanPublish { get; set; }
}

public class ListingUpsertDto
{
    public Guid? Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? InstructionId { get; set; }
    public ListingKind Kind { get; set; }
    public Guid? ListingAgentId { get; set; }
    public Guid? OfficeId { get; set; }

    public decimal? AskingPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool PriceOnApplication { get; set; }
    public RentFrequency? RentFrequency { get; set; }
    public decimal? ServiceChargeAmount { get; set; }
    public string? CurrencyCode { get; set; }

    public DateOnly? AvailableFrom { get; set; }
    public bool VacantPossession { get; set; }
    public bool TenantInSitu { get; set; }
    public int? NoticeRequiredDays { get; set; }
    public bool IsChainFree { get; set; }

    public string? Headline { get; set; }
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public List<string> KeyFeatures { get; set; } = [];
    public string? EnergyRating { get; set; }
    public string? CouncilTaxBand { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? RegulatoryPermitNumber { get; set; }

    public DateOnly? ListedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public bool IsFeatured { get; set; }
    public bool AllowPortalPublish { get; set; } = true;
    public bool AllowWebsitePublish { get; set; } = true;
}

public class ListingPriceHistoryDto
{
    public Guid Id { get; set; }
    public decimal? FromPrice { get; set; }
    public decimal ToPrice { get; set; }
    public decimal ChangePercent { get; set; }
    public DateOnly ChangedOn { get; set; }
    public string? ChangedByName { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }
    public int ViewingsAtPreviousPrice { get; set; }
}

public class ListingPriceChangeDto
{
    public Guid ListingId { get; set; }
    public decimal NewPrice { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? Note { get; set; }

    /// <summary>Tell everyone whose saved search now matches. The fastest re-viewing an agency gets.</summary>
    public bool NotifyMatchedApplicants { get; set; } = true;
}

// ── Instructions ─────────────────────────────────────────────────────────────

public class InstructionDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string? PropertyReference { get; set; }
    public string AddressOneLine { get; set; } = string.Empty;
    public Guid? LandlordId { get; set; }
    public Guid? OwnerPartyId { get; set; }
    public string? OwnerName { get; set; }
    public string? AgentName { get; set; }

    public AgencyBasis Basis { get; set; }
    public ListingKind Kind { get; set; }
    public DateOnly InstructedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public int NoticePeriodDays { get; set; }
    public int TailPeriodDays { get; set; }

    public FeeBasis FeeBasis { get; set; }
    public decimal FeePercent { get; set; }
    public decimal FeeFixedAmount { get; set; }
    public decimal MinimumFee { get; set; }
    public decimal? FeePeriodsOfRent { get; set; }
    public bool FeeIncludesTax { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal WithdrawalFee { get; set; }
    public decimal MarketingBudget { get; set; }
    public ManagementService? ManagementService { get; set; }

    public string? DocumentUrl { get; set; }
    public DateOnly? SignedOn { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? TerminatedOn { get; set; }
    public string? TerminationReason { get; set; }

    /// <summary>Days until the instruction lapses. The renewal conversation an agency forgets to have.</summary>
    public int? DaysToExpiry { get; set; }

    public int ListingCount { get; set; }
    public decimal? EstimatedFee { get; set; }
}

public class InstructionUpsertDto
{
    public Guid? Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? LandlordId { get; set; }
    public Guid? OwnerPartyId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? AgentId { get; set; }
    public AgencyBasis Basis { get; set; }
    public ListingKind Kind { get; set; }
    public DateOnly InstructedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public int NoticePeriodDays { get; set; } = 14;
    public int TailPeriodDays { get; set; }
    public FeeBasis FeeBasis { get; set; }
    public decimal FeePercent { get; set; }
    public decimal FeeFixedAmount { get; set; }
    public decimal MinimumFee { get; set; }
    public decimal? FeePeriodsOfRent { get; set; }
    public bool FeeIncludesTax { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal WithdrawalFee { get; set; }
    public decimal MarketingBudget { get; set; }
    public ManagementService? ManagementService { get; set; }
    public string? DocumentUrl { get; set; }
    public DateOnly? SignedOn { get; set; }
}

// ── Portals ──────────────────────────────────────────────────────────────────

public class PortalChannelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string FeedFormat { get; set; } = "Xml";
    public string? FeedUrl { get; set; }
    public string? CountryCode { get; set; }
    public bool IsActive { get; set; }

    public int ListingQuota { get; set; }
    public int FeaturedQuota { get; set; }
    public int CurrentListingCount { get; set; }
    public int RemainingSlots { get; set; }

    /// <summary>Set when the firm is at or over its plan. Told before the breach, not after.</summary>
    public bool QuotaExceeded { get; set; }

    public decimal MonthlyCost { get; set; }
    public DateOnly? ContractExpiresOn { get; set; }
    public int MinPhotoCount { get; set; }
    public int MaxPhotoCount { get; set; }
    public int MinPhotoWidthPx { get; set; }
    public bool SupportsFloorPlan { get; set; }
    public bool SupportsVirtualTour { get; set; }
    public int RefreshIntervalMinutes { get; set; }

    // Performance, so a renewal decision has numbers behind it.
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public int Leads { get; set; }
    public int Bookings { get; set; }
    public decimal? CostPerLead { get; set; }
    public int FailedCount { get; set; }
}

public class PortalPublicationDto
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid PortalChannelId { get; set; }
    public string PortalName { get; set; } = string.Empty;
    public PortalPublishState State { get; set; }
    public string? PortalListingId { get; set; }
    public string? PortalUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? LastPushedAt { get; set; }
    public bool IsFeatured { get; set; }
    public string? LastError { get; set; }
    public int FailureCount { get; set; }
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public int Leads { get; set; }
}

public class PortalPublishRequestDto
{
    public List<Guid> ListingIds { get; set; } = [];
    public List<Guid> PortalChannelIds { get; set; } = [];

    /// <summary>"publish", "update", "withdraw", "refresh".</summary>
    public string Operation { get; set; } = "publish";

    public bool AsFeatured { get; set; }
}

public class PortalPublishResultDto
{
    public int Attempted { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public List<PortalPublishLogDto> Log { get; set; } = [];
}

public class PortalPublishLogDto
{
    public Guid ListingId { get; set; }
    public string ListingReference { get; set; } = string.Empty;
    public string PortalName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public DateTime AttemptedAt { get; set; }
    public int DurationMs { get; set; }
}

public class PortalMappingDto
{
    public Guid? Id { get; set; }
    public Guid PortalChannelId { get; set; }
    public string MappingKind { get; set; } = string.Empty;
    public string LocalValue { get; set; } = string.Empty;
    public string PortalValue { get; set; } = string.Empty;
    public string? PortalLocationId { get; set; }
}

// ── Viewings & site visits ───────────────────────────────────────────────────

public class ViewingListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public int TravelMinutes { get; set; }
    public ViewingStatus Status { get; set; }

    public string AgentName { get; set; } = string.Empty;
    public string? ApplicantName { get; set; }
    public string? ApplicantPhone { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? PartyId { get; set; }

    public int PropertyCount { get; set; }
    public string PropertySummary { get; set; } = string.Empty;
    public string? FirstAddress { get; set; }
    public decimal? FirstLatitude { get; set; }
    public decimal? FirstLongitude { get; set; }

    public AccessArrangement Access { get; set; }
    public bool ReminderSent { get; set; }
    public bool VendorNotified { get; set; }
    public bool FeedbackReceived { get; set; }
    public InterestLevel? Interest { get; set; }
}

public class ViewingDetailDto : ViewingListItemDto
{
    public string? AccessNote { get; set; }
    public Guid? KeySetId { get; set; }
    public string? KeyLabel { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? CancelReason { get; set; }
    public string? CancelNote { get; set; }
    public string? RouteGeoJson { get; set; }

    public List<ViewingPropertyDto> Properties { get; set; } = [];
    public List<ViewingAttendeeDto> Attendees { get; set; } = [];
    public List<ViewingFeedbackDto> Feedback { get; set; } = [];
}

public class ViewingPropertyDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? UnitId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string AddressOneLine { get; set; } = string.Empty;
    public string? HeroImageUrl { get; set; }
    public decimal? AskingPrice { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public bool WasSeen { get; set; }
    public string? NotSeenReason { get; set; }
    public ViewingFeedbackDto? Feedback { get; set; }
}

public class ViewingAttendeeDto
{
    public Guid? Id { get; set; }
    public Guid? PartyId { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = "Applicant";
    public bool Attended { get; set; }
    public bool IsDecisionMaker { get; set; }
}

public class ViewingUpsertDto
{
    public Guid? Id { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? PartyId { get; set; }
    public Guid AgentId { get; set; }
    public Guid? OfficeId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public int TravelMinutes { get; set; }
    public AccessArrangement Access { get; set; }
    public string? AccessNote { get; set; }
    public Guid? KeySetId { get; set; }
    public List<Guid> PropertyIds { get; set; } = [];
    public List<ViewingAttendeeDto> Attendees { get; set; } = [];
    public bool SendConfirmation { get; set; } = true;
    public bool NotifyVendor { get; set; } = true;

    /// <summary>Serve the statutory notice on a sitting tenant as part of booking the slot.</summary>
    public bool ServeAccessNotice { get; set; }
}

public class ViewingFeedbackDto
{
    public Guid? Id { get; set; }
    public Guid ViewingId { get; set; }
    public Guid? ViewingPropertyId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyReference { get; set; }
    public InterestLevel Interest { get; set; }
    public string? PriceOpinion { get; set; }
    public decimal? WouldOfferAmount { get; set; }
    public string? Liked { get; set; }
    public string? Disliked { get; set; }
    public Guid? ObjectionReasonCodeId { get; set; }
    public string? ObjectionLabel { get; set; }
    public string? NextStep { get; set; }
    public DateTime CapturedAt { get; set; }
    public string? CapturedByName { get; set; }
    public bool SharedWithVendor { get; set; }
    public DateTime? SharedAt { get; set; }
}

public class SiteVisitListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public ViewingStatus Status { get; set; }
    public bool IsRevisit { get; set; }
    public int VisitNumber { get; set; }
    public int GuestCount { get; set; }

    public string? VisitorName { get; set; }
    public string? VisitorPhone { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? PartyId { get; set; }
    public string? PartnerName { get; set; }
    public string? SalesExecutiveName { get; set; }

    public TransportArrangement? Transport { get; set; }
    public string? PickupAddress { get; set; }
    public DateTime? PickupAt { get; set; }
    public string? DriverName { get; set; }
    public string? VehicleNumber { get; set; }

    public DateTime? ArrivedAt { get; set; }
    public bool CostSheetIssued { get; set; }
    public Guid? ResultingBookingId { get; set; }
    public InterestLevel? Interest { get; set; }
    public bool ReminderSent { get; set; }
}

public class SiteVisitDetailDto : SiteVisitListItemDto
{
    public DateTime? LeftAt { get; set; }
    public string? UnitsShown { get; set; }
    public Guid? ShownUnitId { get; set; }
    public string? ShownUnitNumber { get; set; }
    public bool BrochureIssued { get; set; }
    public string? NoShowReason { get; set; }
    public SiteVisitFeedbackDto? Feedback { get; set; }
}

public class SiteVisitUpsertDto
{
    public Guid? Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? PartyId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? SalesExecutiveId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int GuestCount { get; set; } = 1;
    public bool IsRevisit { get; set; }

    public TransportArrangement Transport { get; set; } = TransportArrangement.OwnTransport;
    public string? PickupAddress { get; set; }
    public DateTime? PickupAt { get; set; }
    public decimal? PickupLatitude { get; set; }
    public decimal? PickupLongitude { get; set; }
    public Guid? DriverUserId { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string? VehicleNumber { get; set; }
    public decimal? TransportCost { get; set; }

    public bool SendConfirmation { get; set; } = true;
}

public class SiteVisitFeedbackDto
{
    public Guid? Id { get; set; }
    public Guid SiteVisitId { get; set; }
    public InterestLevel Interest { get; set; }
    public string? PriceOpinion { get; set; }
    public string? PreferredUnitType { get; set; }
    public decimal? BudgetIndicated { get; set; }
    public string? Liked { get; set; }
    public Guid? ObjectionReasonCodeId { get; set; }
    public string? Objection { get; set; }
    public string? NextStep { get; set; }
    public DateOnly? NextStepDate { get; set; }
    public int? SatisfactionRating { get; set; }
    public DateTime CapturedAt { get; set; }
    public string? CapturedByName { get; set; }
}

/// <summary>The diary for one agent or team on one day, with the travel gaps made visible.</summary>
public class DiaryDayDto
{
    public DateOnly Date { get; set; }
    public Guid? AgentId { get; set; }
    public string? AgentName { get; set; }
    public List<DiarySlotDto> Slots { get; set; } = [];
    public int ViewingCount { get; set; }
    public int SiteVisitCount { get; set; }
    public int ConflictCount { get; set; }
    public string? RouteGeoJson { get; set; }
    public int TotalTravelMinutes { get; set; }
}

public class DiarySlotDto
{
    public Guid Id { get; set; }

    /// <summary>"viewing", "sitevisit", "inspection", "workorder", "appointment", "block".</summary>
    public string Kind { get; set; } = "viewing";

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? Tone { get; set; }
    public string? Route { get; set; }

    /// <summary>Overlaps another slot, or the travel gap before it is impossible.</summary>
    public bool HasConflict { get; set; }
    public string? ConflictReason { get; set; }
}

// ── Keys ─────────────────────────────────────────────────────────────────────

public class KeySetDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyReference { get; set; } = string.Empty;
    public string AddressOneLine { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? TagNumber { get; set; }
    public int KeyCount { get; set; }
    public string? KeySafeLocation { get; set; }
    public bool IsOut { get; set; }
    public string? CurrentHolderName { get; set; }
    public DateTime? OutSince { get; set; }
    public DateTime? DueBackAt { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsLost { get; set; }
    public string? Description { get; set; }
}

public class KeyMovementDto
{
    public Guid Id { get; set; }
    public string Movement { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? HolderName { get; set; }
    public DateTime? DueBackAt { get; set; }
    public string? Note { get; set; }
    public Guid? ViewingId { get; set; }
    public Guid? WorkOrderId { get; set; }
}

// ── Campaigns & events ───────────────────────────────────────────────────────

public class CampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public decimal Budget { get; set; }
    public decimal ActualSpend { get; set; }
    public string? LandingPageUrl { get; set; }
    public string? TrackingCode { get; set; }
    public bool IsActive { get; set; }

    public int LeadCount { get; set; }
    public int VisitCount { get; set; }
    public int BookingCount { get; set; }
    public decimal BookingValue { get; set; }
    public decimal? CostPerLead { get; set; }
    public decimal? CostPerVisit { get; set; }
    public decimal? CostPerBooking { get; set; }
    public decimal? ReturnOnAdSpend { get; set; }
}

public class MarketingEventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EventType { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string? Venue { get; set; }
    public decimal Budget { get; set; }
    public decimal ActualSpend { get; set; }
    public int RegisteredCount { get; set; }
    public int AttendedCount { get; set; }
    public int WalkInCount { get; set; }
    public int LeadCount { get; set; }
    public int BookingCount { get; set; }
    public decimal? AttendanceRate { get; set; }
}

public class ContentAssetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int Version { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public bool IsExpired { get; set; }
    public bool IsExternallyShareable { get; set; }
    public string LanguageCode { get; set; } = "en";
    public int DownloadCount { get; set; }
    public bool IsCurrent { get; set; }
}
