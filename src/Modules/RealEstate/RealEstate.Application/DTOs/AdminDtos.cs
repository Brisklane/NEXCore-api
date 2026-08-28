using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Organisation, portals, messaging, import and the report shapes.
// =====================================================================================

public class RealEstateOfficeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public OfficeType OfficeType { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public AddressDto Address { get; set; } = new();
    public string? TimeZoneId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public AreaUnit? DisplayAreaUnit { get; set; }
    public LinesOfBusinessDto LinesOfBusiness { get; set; } = new();
    public int WorkingDaysMask { get; set; }
    public TimeSpan? OpensAt { get; set; }
    public TimeSpan? ClosesAt { get; set; }
    public bool IsFranchise { get; set; }
    public decimal FranchiseRoyaltyPercent { get; set; }
    public string? ManagerName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public bool IsActive { get; set; }
    public int AgentCount { get; set; }
    public int ActiveListingCount { get; set; }
}

public class GeoAreaDto
{
    public Guid Id { get; set; }
    public Guid? ParentAreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? LevelLabel { get; set; }
    public int Depth { get; set; }
    public string? Path { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? BoundaryGeoJson { get; set; }
    public decimal? AverageRatePerSqFt { get; set; }
    public int PropertyCount { get; set; }
    public bool IsActive { get; set; }
    public List<GeoAreaDto> Children { get; set; } = [];
}

public class TerritoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? OfficeName { get; set; }
    public PropertyCategory? CategoryFilter { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? BoundaryGeoJson { get; set; }
    public bool IsActive { get; set; }
    public List<LookupDto> Areas { get; set; } = [];
    public List<TerritoryAssignmentDto> Assignments { get; set; } = [];
    public int LeadsThisMonth { get; set; }
}

public class TerritoryAssignmentDto
{
    public Guid? Id { get; set; }
    public Guid AgentProfileId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int RoutingWeight { get; set; }
    public bool IsPrimary { get; set; }
}

public class AgentProfileDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }
    public string? JobTitle { get; set; }
    public string? OfficeName { get; set; }
    public string? TeamName { get; set; }
    public DateOnly? JoinedOn { get; set; }
    public DateOnly? LeftOn { get; set; }
    public DateOnly? CapAnniversary { get; set; }
    public string? CommissionPlanName { get; set; }

    public int MaxOpenLeads { get; set; }
    public int MaxLeadsPerDay { get; set; }
    public bool AcceptsNewLeads { get; set; }
    public bool IsOnLeave { get; set; }
    public string? CoveringAgentName { get; set; }
    public string? Languages { get; set; }
    public string? Specialisations { get; set; }
    public bool IsActive { get; set; }

    // Performance
    public int OpenLeads { get; set; }
    public int ListingsTaken { get; set; }
    public int ViewingsHeld { get; set; }
    public int OffersMade { get; set; }
    public int DealsClosed { get; set; }
    public int BookingsMade { get; set; }
    public decimal BookingValue { get; set; }
    public decimal GrossCommission { get; set; }
    public decimal NetCommission { get; set; }
    public decimal ConversionPercent { get; set; }
    public decimal AverageDaysToClose { get; set; }
    public decimal AverageSpeedToLeadMinutes { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public bool HasExpiredLicence { get; set; }
    public List<AgentLicenceDto> Licences { get; set; } = [];
    public AgentCapLedgerDto? CapPosition { get; set; }
}

public class AgentLicenceDto
{
    public Guid? Id { get; set; }
    public string LicenceType { get; set; } = string.Empty;
    public string LicenceNumber { get; set; } = string.Empty;
    public string? IssuingAuthority { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public int? DaysToExpiry { get; set; }
    public bool IsExpired { get; set; }
    public string? DocumentUrl { get; set; }
    public bool BlocksAssignmentWhenExpired { get; set; }
    public bool IsVerified { get; set; }
}

public class SalesTeamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? OfficeName { get; set; }
    public string? LeaderName { get; set; }
    public decimal LeaderOverridePercent { get; set; }
    public string? CommissionPlanName { get; set; }
    public int MemberCount { get; set; }
    public decimal TeamVolume { get; set; }
    public int TeamDeals { get; set; }
    public List<AgentProfileDto> Members { get; set; } = [];
}

public class ApprovalMatrixDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? OfficeName { get; set; }
    public string? ProjectName { get; set; }
    public decimal MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int Level { get; set; }
    public Guid? ApproverRoleId { get; set; }
    public string? ApproverRoleName { get; set; }
    public Guid? ApproverUserId { get; set; }
    public string? ApproverUserName { get; set; }
    public bool AutoApproveBelowMin { get; set; }
    public int? EscalateAfterHours { get; set; }
}

// ── Portals ──────────────────────────────────────────────────────────────────

public class PortalUserDto
{
    public Guid Id { get; set; }
    public PortalAudience Audience { get; set; }
    public Guid? PartyId { get; set; }
    public Guid? LandlordId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? ResidentId { get; set; }
    public string LoginIdentifier { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? InvitedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public bool IsLocked { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public bool NotifyByEmail { get; set; }
    public bool NotifyBySms { get; set; }
    public bool NotifyByWhatsApp { get; set; }
    public bool NotifyByPush { get; set; }
}

/// <summary>The customer portal's home screen — everything a buyer wants without asking anyone.</summary>
public class CustomerPortalHomeDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public List<BookingListItemDto> Bookings { get; set; } = [];
    public List<ClientBuildContractListItemDto> Builds { get; set; } = [];
    public decimal TotalInvested { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public decimal NextDueAmount { get; set; }
    public List<DemandListItemDto> OpenDemands { get; set; } = [];
    public List<ReceiptListItemDto> RecentReceipts { get; set; } = [];
    public List<GeneratedDocumentDto> Documents { get; set; } = [];
    public List<ProjectMilestoneDto> ConstructionProgress { get; set; } = [];
    public List<string> ProgressPhotoUrls { get; set; } = [];
    public List<SocietyNoticeDto> Notices { get; set; } = [];
    public int UnreadMessages { get; set; }
}

/// <summary>The tenant and resident portal's home screen.</summary>
public class TenantPortalHomeDto
{
    public string TenantName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public List<TenancyListItemDto> Tenancies { get; set; } = [];
    public decimal RentDue { get; set; }
    public DateOnly? NextRentDate { get; set; }
    public decimal Arrears { get; set; }
    public SecurityDepositDto? Deposit { get; set; }
    public List<MaintenanceBillDto> MaintenanceBills { get; set; } = [];
    public List<ComplaintListItemDto> OpenRequests { get; set; } = [];
    public List<AmenityBookingDto> AmenityBookings { get; set; } = [];
    public List<VisitorPassDto> ExpectedVisitors { get; set; } = [];
    public List<ComplianceCertificateDto> Certificates { get; set; } = [];
    public List<SocietyNoticeDto> Notices { get; set; } = [];
    public List<GeneratedDocumentDto> Documents { get; set; } = [];
}

/// <summary>The landlord portal's home screen.</summary>
public class OwnerPortalHomeDto
{
    public string OwnerName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public int PropertyCount { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public decimal Arrears { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal NextPayoutAmount { get; set; }
    public DateOnly? NextPayoutDate { get; set; }
    public List<PropertyListItemDto> Properties { get; set; } = [];
    public List<TenancyListItemDto> Tenancies { get; set; } = [];
    public List<OwnerStatementDto> Statements { get; set; } = [];
    public List<WorkOrderListItemDto> WorkOrders { get; set; } = [];
    public List<WorkOrderListItemDto> AwaitingMyApproval { get; set; } = [];
    public List<ComplianceCertificateDto> ExpiringCertificates { get; set; } = [];
}

/// <summary>The channel partner portal's home screen.</summary>
public class PartnerPortalHomeDto
{
    public string PartnerName { get; set; } = string.Empty;
    public string? TierName { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public List<ProjectListItemDto> AuthorisedProjects { get; set; } = [];
    public int ActiveRegistrations { get; set; }
    public int ExpiringRegistrations { get; set; }
    public int SiteVisitsThisMonth { get; set; }
    public int BookingsThisMonth { get; set; }
    public decimal BookingValue { get; set; }
    public decimal CommissionEarned { get; set; }
    public decimal CommissionPaid { get; set; }
    public decimal CommissionPending { get; set; }
    public List<LeadRegistrationDto> Registrations { get; set; } = [];
    public List<SiteVisitListItemDto> UpcomingVisits { get; set; } = [];
    public List<PartnerCommissionEntryDto> CommissionEntries { get; set; } = [];
    public List<PartnerStatementDto> Statements { get; set; } = [];
    public List<ContentAssetDto> Collateral { get; set; } = [];
    public List<PartnerContestDto> Contests { get; set; } = [];
}

// ── Messaging ────────────────────────────────────────────────────────────────

public class ConversationDto
{
    public Guid Id { get; set; }
    public Guid? PartyId { get; set; }
    public string? PartyName { get; set; }
    public string? PartyPhone { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public NotificationChannel Channel { get; set; }
    public string? Subject { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int MessageCount { get; set; }
    public int UnreadCount { get; set; }
    public string? AssignedToName { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime? SnoozedUntil { get; set; }
    public bool AwaitingReply { get; set; }
    public int? MinutesAwaitingReply { get; set; }
    public string? LastMessagePreview { get; set; }
    public List<ConversationMessageDto> Messages { get; set; } = [];
}

public class ConversationMessageDto
{
    public Guid Id { get; set; }
    public ActivityDirection Direction { get; set; }
    public DateTime SentAt { get; set; }
    public string? Body { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public string? Caption { get; set; }
    public string? SentByName { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool Failed { get; set; }
    public string? FailureReason { get; set; }
    public bool IsAutomated { get; set; }
}

public class SendMessageDto
{
    public Guid? ConversationId { get; set; }
    public Guid? PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.WhatsApp;
    public string? To { get; set; }
    public Guid? MessageTemplateId { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? MediaUrl { get; set; }
    public Guid? AttachDocumentId { get; set; }
    public Dictionary<string, string> MergeValues { get; set; } = [];
}

public class MessageTemplateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string LanguageCode { get; set; } = "en";
    public bool IsRightToLeft { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? ProviderTemplateName { get; set; }
    public bool IsProviderApproved { get; set; }
    public List<string> MergeFields { get; set; } = [];
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string? Category { get; set; }
    public int SentCount { get; set; }
}

public class BroadcastRunDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string? MessageTemplateName { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string SegmentKey { get; set; } = "Custom";
    public string? ProjectName { get; set; }
    public string? SocietyName { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TargetCount { get; set; }
    public int SentCount { get; set; }
    public int SuppressedCount { get; set; }
    public int DeliveredCount { get; set; }
    public int ReadCount { get; set; }
    public int FailedCount { get; set; }
    public int OptOutCount { get; set; }
    public decimal? Cost { get; set; }
    public string Status { get; set; } = "Draft";
    public string? RunByName { get; set; }
    public decimal DeliveryRatePercent { get; set; }
    public decimal ReadRatePercent { get; set; }
}

public class BroadcastRequestDto
{
    public string Name { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public Guid? MessageTemplateId { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? AttachmentUrl { get; set; }
    public string SegmentKey { get; set; } = "Custom";
    public string? SegmentFilterJson { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }
    public List<Guid> ExplicitPartyIds { get; set; } = [];
    public DateTime? ScheduledFor { get; set; }
    public bool DryRun { get; set; } = true;
}

public class NotificationRuleDto
{
    public Guid Id { get; set; }
    public string RuleKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public AlertSeverity Severity { get; set; }
    public List<NotificationChannel> Channels { get; set; } = [];
    public List<string> TargetRoles { get; set; } = [];
    public Guid? TargetUserId { get; set; }
    public string? TargetUserName { get; set; }
    public int? LeadDays { get; set; }
    public decimal? ThresholdAmount { get; set; }
    public bool IsDigest { get; set; }
    public string? DigestSchedule { get; set; }
    public bool RespectQuietHours { get; set; }
    public int? EscalateAfterHours { get; set; }
    public string? EscalateToRole { get; set; }
    public Guid? MessageTemplateId { get; set; }
    public string? ProjectName { get; set; }
    public int FiredLast30Days { get; set; }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string RuleKey { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? DeepLink { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead { get; set; }
    public bool Failed { get; set; }
    public bool IsEscalation { get; set; }
}

// ── Import ───────────────────────────────────────────────────────────────────

public class ImportBatchDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public ImportEntityKind EntityKind { get; set; }
    public ImportBatchStatus Status { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ProjectName { get; set; }

    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    public int WarningRows { get; set; }
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }
    public int UpdatedRows { get; set; }

    public bool DryRunCompleted { get; set; }
    public DateTime? DryRunAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RunByName { get; set; }
    public bool AllowUpdates { get; set; }
    public string? MatchKeyField { get; set; }
    public bool CanRollback { get; set; }
    public bool WasRolledBack { get; set; }
    public string? ErrorSummary { get; set; }
    public List<ImportBatchErrorDto> Errors { get; set; } = [];
}

public class ImportBatchErrorDto
{
    public Guid Id { get; set; }
    public int RowNumber { get; set; }
    public string? ColumnName { get; set; }
    public string? CellValue { get; set; }
    public string Severity { get; set; } = "Error";
    public string Message { get; set; } = string.Empty;

    /// <summary>What the operator should do about it, in their language not the parser's.</summary>
    public string? Suggestion { get; set; }

    public bool IsResolved { get; set; }
}

public class ImportRequestDto
{
    public ImportEntityKind EntityKind { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }
    public string? MappingProfileJson { get; set; }
    public bool AllowUpdates { get; set; }
    public string? MatchKeyField { get; set; }
    public bool DryRun { get; set; } = true;

    /// <summary>Rows as parsed by the client, so the server never has to own a spreadsheet parser.</summary>
    public List<Dictionary<string, string?>> Rows { get; set; } = [];
}

// ── Reports ──────────────────────────────────────────────────────────────────

/// <summary>
/// A generic tabular report envelope. Every report in the module returns this shape so one screen,
/// one exporter and one scheduler can serve all of them.
/// </summary>
public class ReportResultDto
{
    public string ReportKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public Dictionary<string, string> AppliedFilters { get; set; } = [];

    public List<ReportColumnDto> Columns { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    public Dictionary<string, object?> Totals { get; set; } = [];

    public List<BreakdownSliceDto> Breakdown { get; set; } = [];
    public List<TrendPointDto> Trend { get; set; } = [];
    public int RowCount { get; set; }
    public bool IsTruncated { get; set; }
}

public class ReportColumnDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    /// <summary>"text", "number", "money", "percent", "date", "area", "status", "link".</summary>
    public string Type { get; set; } = "text";

    public string? Align { get; set; }
    public bool IsSortable { get; set; } = true;
    public bool IsTotalled { get; set; }
    public string? Format { get; set; }
    public int Width { get; set; }
}

public class ReportRequestDto
{
    public string ReportKey { get; set; } = string.Empty;
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? AgentId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? LandlordId { get; set; }
    public string? GroupBy { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = [];
    public int? Top { get; set; }
}

/// <summary>The catalogue the reports screen renders its menu from.</summary>
public class ReportDefinitionDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>"Sales", "Collections", "Brokerage", "Leasing", "Construction", "Financial", "Society", "Compliance".</summary>
    public string Category { get; set; } = string.Empty;

    public LineOfBusiness? LineOfBusiness { get; set; }
    public string? Icon { get; set; }
    public List<string> Parameters { get; set; } = [];
    public bool SupportsGrouping { get; set; }
    public bool SupportsTrend { get; set; }
    public int SortOrder { get; set; }
}
