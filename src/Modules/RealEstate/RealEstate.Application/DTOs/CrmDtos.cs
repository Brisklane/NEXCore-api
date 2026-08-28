using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// People, enquiries, matching, and the diary.
// =====================================================================================

public class PartyListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public PartyKind Kind { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? FatherOrGuardianName { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? City { get; set; }
    public string? PhotoUrl { get; set; }

    public List<PartyRoleKind> Roles { get; set; } = [];
    public KycStatus KycStatus { get; set; }
    public RiskRating RiskRating { get; set; }
    public bool IsCautioned { get; set; }

    public decimal TotalInvested { get; set; }
    public decimal TotalOutstanding { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public string? OwnerAgentName { get; set; }
    public int PropertyCount { get; set; }
}

/// <summary>
/// The Person 360. Every enquiry, viewing, booking, tenancy, receipt and message in one payload,
/// because the whole point of the screen is that nobody has to open five others.
/// </summary>
public class PartyDetailDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public PartyKind Kind { get; set; }

    public string? Salutation { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? FatherOrGuardianName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? ResidencyStatus { get; set; }
    public string? Occupation { get; set; }
    public string? Employer { get; set; }
    public string? PhotoUrl { get; set; }
    public string? SignatureSpecimenUrl { get; set; }

    public string? OrganisationName { get; set; }
    public string? TradingName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public DateOnly? IncorporatedOn { get; set; }
    public string? Industry { get; set; }

    public NotificationChannel PreferredChannel { get; set; }
    public string PreferredLanguage { get; set; } = "en";

    public KycStatus KycStatus { get; set; }
    public RiskRating RiskRating { get; set; }
    public DateOnly? KycVerifiedOn { get; set; }
    public DateOnly? KycExpiresOn { get; set; }
    public bool IsPoliticallyExposed { get; set; }
    public bool IsCautioned { get; set; }

    public string? OwnerAgentName { get; set; }
    public string? Notes { get; set; }

    public List<PartyRoleDto> Roles { get; set; } = [];
    public List<PartyContactDto> Contacts { get; set; } = [];
    public List<PartyAddressDto> Addresses { get; set; } = [];
    public List<PartyIdentityDto> Identities { get; set; } = [];
    public List<PartyRelationshipDto> Relationships { get; set; } = [];
    public List<PartyConsentDto> Consents { get; set; } = [];
    public List<CautionListEntryDto> Cautions { get; set; } = [];

    // What they have with us
    public PartyMoneySummaryDto Money { get; set; } = new();
    public List<EnquiryListItemDto> Enquiries { get; set; } = [];
    public List<BookingListItemDto> Bookings { get; set; } = [];
    public List<TenancyListItemDto> Tenancies { get; set; } = [];
    public List<PropertyListItemDto> OwnedProperties { get; set; } = [];
    public List<TimelineEntryDto> Timeline { get; set; } = [];
    public List<GeneratedDocumentDto> Documents { get; set; } = [];
}

public class PartyMoneySummaryDto
{
    public string CurrencyCode { get; set; } = "USD";
    public decimal TotalInvested { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal SurchargeAccrued { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public decimal NextDueAmount { get; set; }
    public decimal RentPaidThisYear { get; set; }
    public decimal RentArrears { get; set; }
    public decimal MaintenanceArrears { get; set; }
    public int DaysOverdue { get; set; }
    public bool IsDefaulter { get; set; }
}

public class PartyUpsertDto
{
    public Guid? Id { get; set; }
    public PartyKind Kind { get; set; } = PartyKind.Individual;

    public string? Salutation { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? FatherOrGuardianName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? ResidencyStatus { get; set; }
    public string? Occupation { get; set; }
    public string? Employer { get; set; }
    public string? PhotoUrl { get; set; }
    public string? SignatureSpecimenUrl { get; set; }

    public string? OrganisationName { get; set; }
    public string? TradingName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public DateOnly? IncorporatedOn { get; set; }
    public string? Industry { get; set; }

    public string? PrimaryPhone { get; set; }
    public string? PrimaryEmail { get; set; }
    public NotificationChannel PreferredChannel { get; set; } = NotificationChannel.WhatsApp;
    public string PreferredLanguage { get; set; } = "en";

    public Guid? OwnerAgentId { get; set; }
    public Guid? OfficeId { get; set; }
    public string? Notes { get; set; }

    public List<PartyContactDto> Contacts { get; set; } = [];
    public List<PartyAddressDto> Addresses { get; set; } = [];
    public List<PartyIdentityDto> Identities { get; set; } = [];
    public List<PartyRelationshipDto> Relationships { get; set; } = [];
    public List<PartyRoleKind> AddRoles { get; set; } = [];
    public bool AcknowledgeDuplicate { get; set; }
}

public class PartyRoleDto
{
    public Guid Id { get; set; }
    public PartyRoleKind Kind { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool IsActive { get; set; }
    public string? ContextType { get; set; }
    public Guid? ContextId { get; set; }
    public string? ContextLabel { get; set; }
}

public class PartyContactDto
{
    public Guid? Id { get; set; }
    public string ContactType { get; set; } = "Phone";
    public string Value { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsWhatsApp { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public bool IsUnreachable { get; set; }
    public string? PersonName { get; set; }
    public string? Designation { get; set; }
    public bool IsAuthorisedSignatory { get; set; }
}

public class PartyAddressDto
{
    public Guid? Id { get; set; }
    public string AddressType { get; set; } = "Correspondence";
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostCode { get; set; }
    public string? CountryCode { get; set; }
    public Guid? GeoAreaId { get; set; }
    public bool IsMailingAddress { get; set; }
    public bool IsVerified { get; set; }
    public string OneLine { get; set; } = string.Empty;
}

public class PartyIdentityDto
{
    public Guid? Id { get; set; }
    public IdentityKind Kind { get; set; }
    public string? LocalLabel { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? IssuingCountry { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? FrontImageUrl { get; set; }
    public string? BackImageUrl { get; set; }
    public DocumentState State { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsExpired { get; set; }
}

public class PartyRelationshipDto
{
    public Guid? Id { get; set; }
    public Guid RelatedPartyId { get; set; }
    public string RelatedPartyName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? PowerScope { get; set; }
    public string? PoaDocumentNumber { get; set; }
    public DateOnly? PoaValidFrom { get; set; }
    public DateOnly? PoaValidTo { get; set; }
    public bool PoaIsRegistered { get; set; }
    public bool PoaIsExpired { get; set; }
    public string? DocumentUrl { get; set; }
    public decimal? SharePercent { get; set; }
    public bool IsVerified { get; set; }
}

public class PartyConsentDto
{
    public Guid? Id { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Purpose { get; set; } = "Marketing";
    public bool IsGranted { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? Source { get; set; }
    public DateTime? WithdrawnAt { get; set; }
}

public class CautionListEntryDto
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public string? PartyName { get; set; }
    public string Category { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? EvidenceUrl { get; set; }
    public string? RaisedByName { get; set; }
    public DateOnly RaisedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public bool BlocksNewBusiness { get; set; }
    public bool IsActive { get; set; }
}

// ── KYC ──────────────────────────────────────────────────────────────────────

public class KycCaseDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public KycStatus Status { get; set; }
    public RiskRating RiskRating { get; set; }
    public DateOnly OpenedOn { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public DateOnly? NextReviewDue { get; set; }
    public string? AssignedToName { get; set; }
    public string? ApprovedByName { get; set; }
    public string? SanctionsScreeningRef { get; set; }
    public DateOnly? SanctionsScreenedOn { get; set; }
    public bool SanctionsHit { get; set; }
    public string? SourceOfFunds { get; set; }
    public decimal? DeclaredNetWorth { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
    public int DaysOpen { get; set; }
    public List<ChecklistItemDto> Documents { get; set; } = [];
}

// ── Enquiries ────────────────────────────────────────────────────────────────

public class EnquiryListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? PartyId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }

    public EnquiryStage Stage { get; set; }
    public EnquiryChannel Channel { get; set; }
    public string? SourceLabel { get; set; }
    public ListingKind Interest { get; set; }

    public string? ProjectName { get; set; }
    public string? PropertyReference { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? CurrencyCode { get; set; }

    public string? AssignedAgentName { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? FirstContactedAt { get; set; }
    public DateTime? ResponseDueAt { get; set; }
    public bool SlaBreached { get; set; }

    /// <summary>Minutes from arrival to first human contact. Null while still unanswered.</summary>
    public int? SpeedToLeadMinutes { get; set; }

    /// <summary>Minutes remaining on the SLA clock. Negative once breached.</summary>
    public int? MinutesToSlaDeadline { get; set; }

    public DateTime? LastActivityAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public bool FollowUpOverdue { get; set; }
    public int Score { get; set; }
    public int ViewingCount { get; set; }
    public int SiteVisitCount { get; set; }
    public string? PartnerName { get; set; }
    public int DaysInStage { get; set; }
}

public class EnquiryDetailDto : EnquiryListItemDto
{
    public string? SubSource { get; set; }
    public Guid? PortalChannelId { get; set; }
    public Guid? CampaignId { get; set; }
    public string? CampaignName { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? ReferredByPartyId { get; set; }
    public string? ReferredByName { get; set; }

    public Guid? ListingId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public BuyingPurpose? Purpose { get; set; }
    public FundingKind? Funding { get; set; }
    public string? Timeline { get; set; }
    public string? Message { get; set; }
    public bool IsQualified { get; set; }

    public string? ScoreBreakdown { get; set; }
    public RequirementProfileDto? Requirement { get; set; }

    public Guid? ConvertedBookingId { get; set; }
    public Guid? ConvertedDealId { get; set; }
    public Guid? ConvertedTenancyId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? LossReason { get; set; }
    public string? LossNote { get; set; }
    public string? LostToCompetitor { get; set; }

    public List<ActivityDto> Activities { get; set; } = [];
    public List<FollowUpTaskDto> Tasks { get; set; } = [];
    public List<ViewingListItemDto> Viewings { get; set; } = [];
    public List<SiteVisitListItemDto> SiteVisits { get; set; } = [];
    public List<MatchResultDto> Matches { get; set; } = [];
    public List<EnquiryStageHistoryDto> StageHistory { get; set; } = [];
}

public class EnquiryStageHistoryDto
{
    public EnquiryStage FromStage { get; set; }
    public EnquiryStage ToStage { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedByName { get; set; }
    public int DaysInPreviousStage { get; set; }
    public string? Note { get; set; }
}

public class EnquiryUpsertDto
{
    public Guid? Id { get; set; }
    public Guid? PartyId { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }

    public EnquiryChannel Channel { get; set; }
    public string? SubSource { get; set; }
    public Guid? PortalChannelId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? ReferredByPartyId { get; set; }
    public Guid? MarketingEventId { get; set; }

    public ListingKind Interest { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public BuyingPurpose? Purpose { get; set; }
    public FundingKind? Funding { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? Timeline { get; set; }
    public string? Message { get; set; }

    public Guid? AssignedAgentId { get; set; }
    public Guid? OfficeId { get; set; }
    public RequirementProfileUpsertDto? Requirement { get; set; }

    /// <summary>Skip the router and hand it straight to the named agent.</summary>
    public bool SkipAutoAssign { get; set; }
    public bool AcknowledgeDuplicate { get; set; }
}

public class EnquiryStageChangeDto
{
    public Guid EnquiryId { get; set; }
    public EnquiryStage ToStage { get; set; }
    public Guid? LossReasonCodeId { get; set; }
    public string? LossNote { get; set; }
    public string? LostToCompetitor { get; set; }
    public string? Note { get; set; }
}

/// <summary>What the enquiry board draws, grouped by stage.</summary>
public class EnquiryBoardDto
{
    public List<EnquiryBoardColumnDto> Columns { get; set; } = [];
    public int TotalCount { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public int BreachingSlaCount { get; set; }
    public int UnassignedCount { get; set; }
}

public class EnquiryBoardColumnDto
{
    public EnquiryStage Stage { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Value { get; set; }
    public List<EnquiryListItemDto> Items { get; set; } = [];

    /// <summary>Set when the column is paged — a New column with four hundred leads in it.</summary>
    public bool HasMore { get; set; }
}

// ── Requirements & matching ──────────────────────────────────────────────────

public class RequirementProfileDto
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public string? Name { get; set; }
    public ListingKind Interest { get; set; }
    public PropertyCategory? Category { get; set; }
    public List<PropertySubType> SubTypes { get; set; } = [];
    public BuyingPurpose? Purpose { get; set; }
    public FundingKind? Funding { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? CurrencyCode { get; set; }
    public AreaDto? MinArea { get; set; }
    public AreaDto? MaxArea { get; set; }
    public int? MinBedrooms { get; set; }
    public int? MaxBedrooms { get; set; }
    public int? MinBathrooms { get; set; }
    public int? MinFloor { get; set; }
    public int? MaxFloor { get; set; }
    public Facing? PreferredFacing { get; set; }
    public FurnishingState? Furnishing { get; set; }
    public List<string> MustHaveFeatures { get; set; } = [];
    public List<string> NiceToHaveFeatures { get; set; } = [];
    public List<LookupDto> PreferredAreas { get; set; } = [];
    public string? PreferredAreasGeoJson { get; set; }
    public DateOnly? AvailableFrom { get; set; }
    public string? Exclusions { get; set; }
    public bool IsActive { get; set; }
    public bool AlertsEnabled { get; set; }
    public DateTime? LastMatchedAt { get; set; }
    public int MatchCount { get; set; }
}

public class RequirementProfileUpsertDto
{
    public Guid? Id { get; set; }
    public Guid? PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public string? Name { get; set; }
    public ListingKind Interest { get; set; }
    public PropertyCategory? Category { get; set; }
    public List<PropertySubType> SubTypes { get; set; } = [];
    public BuyingPurpose? Purpose { get; set; }
    public FundingKind? Funding { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public AreaUnit InputAreaUnit { get; set; } = AreaUnit.SquareFeet;
    public decimal? MinArea { get; set; }
    public decimal? MaxArea { get; set; }
    public int? MinBedrooms { get; set; }
    public int? MaxBedrooms { get; set; }
    public int? MinBathrooms { get; set; }
    public int? MinFloor { get; set; }
    public int? MaxFloor { get; set; }
    public Facing? PreferredFacing { get; set; }
    public FurnishingState? Furnishing { get; set; }
    public List<string> MustHaveFeatures { get; set; } = [];
    public List<string> NiceToHaveFeatures { get; set; } = [];
    public List<Guid> PreferredAreaIds { get; set; } = [];
    public string? PreferredAreasGeoJson { get; set; }
    public DateOnly? AvailableFrom { get; set; }
    public string? Exclusions { get; set; }
    public bool AlertsEnabled { get; set; } = true;
}

/// <summary>One match, with the reasoning shown. A score nobody can interrogate is worthless.</summary>
public class MatchResultDto
{
    public Guid Id { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PropertyId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? AddressOneLine { get; set; }
    public string? HeroImageUrl { get; set; }
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }
    public AreaDto? Area { get; set; }
    public int? Bedrooms { get; set; }
    public PropertySubType? SubType { get; set; }

    public int Score { get; set; }

    /// <summary>Each criterion and what it contributed. Rendered as a list under the score.</summary>
    public List<MatchFactorDto> Factors { get; set; } = [];

    public DateTime MatchedAt { get; set; }
    public bool WasSent { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public bool IsDismissed { get; set; }
    public bool LedToViewing { get; set; }
}

public class MatchFactorDto
{
    public string Criterion { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsMustHave { get; set; }
}

public class SendMatchesDto
{
    public Guid RequirementProfileId { get; set; }
    public List<Guid> MatchResultIds { get; set; } = [];
    public NotificationChannel Channel { get; set; } = NotificationChannel.WhatsApp;
    public string? Message { get; set; }
    public Guid? MessageTemplateId { get; set; }
}

// ── Activities & tasks ───────────────────────────────────────────────────────

public class ActivityDto
{
    public Guid Id { get; set; }
    public ActivityKind Kind { get; set; }
    public ActivityDirection Direction { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? UserName { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Outcome { get; set; }
    public string? RecordingUrl { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool DeliveryFailed { get; set; }
    public string? FailureReason { get; set; }
    public bool IsSystemGenerated { get; set; }
    public bool IsPinned { get; set; }
}

public class ActivityCreateDto
{
    public ActivityKind Kind { get; set; }
    public ActivityDirection Direction { get; set; } = ActivityDirection.Outbound;
    public Guid? PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public DateTime? OccurredAt { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Outcome { get; set; }
    public Guid? OutcomeReasonCodeId { get; set; }
    public string? AttachmentUrl { get; set; }

    /// <summary>Create the next follow-up in the same call. The rule is: never leave a lead without one.</summary>
    public FollowUpTaskCreateDto? NextFollowUp { get; set; }
}

public class FollowUpTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Note { get; set; }
    public ActivityKind SuggestedAction { get; set; }
    public DateTime DueAt { get; set; }
    public TicketPriority Priority { get; set; }
    public TaskState State { get; set; }
    public bool IsOverdue { get; set; }
    public string? AssignedToName { get; set; }
    public bool IsAutoGenerated { get; set; }
    public string? SourceRuleKey { get; set; }
    public int SnoozeCount { get; set; }

    // Context, so My Day can link straight through.
    public Guid? PartyId { get; set; }
    public string? PartyName { get; set; }
    public string? PartyPhone { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? DunningCaseId { get; set; }
    public string? ContextLabel { get; set; }
    public string? Route { get; set; }
    public decimal? Amount { get; set; }
}

public class FollowUpTaskCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Note { get; set; }
    public ActivityKind SuggestedAction { get; set; } = ActivityKind.Call;
    public DateTime DueAt { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public Guid? AssignedToUserId { get; set; }
    public Guid? PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? DunningCaseId { get; set; }
}

public class TaskCompletionDto
{
    public Guid TaskId { get; set; }
    public string? CompletionNote { get; set; }
    public string? Outcome { get; set; }
    public FollowUpTaskCreateDto? NextTask { get; set; }
}

/// <summary>
/// The agent's day: what is overdue, what is due today, and what is booked. A to-do list rather
/// than a database — the only screen most agents open before lunch.
/// </summary>
public class MyDayDto
{
    public DateOnly Date { get; set; }
    public string AgentName { get; set; } = string.Empty;

    public List<FollowUpTaskDto> Overdue { get; set; } = [];
    public List<FollowUpTaskDto> DueToday { get; set; } = [];
    public List<FollowUpTaskDto> Upcoming { get; set; } = [];

    public List<ViewingListItemDto> Viewings { get; set; } = [];
    public List<SiteVisitListItemDto> SiteVisits { get; set; } = [];
    public List<EnquiryListItemDto> UnansweredLeads { get; set; } = [];
    public List<ApprovalRequestDto> AwaitingMyApproval { get; set; } = [];

    public int CompletedToday { get; set; }
    public int CallsMadeToday { get; set; }
    public decimal? CollectedToday { get; set; }
}
