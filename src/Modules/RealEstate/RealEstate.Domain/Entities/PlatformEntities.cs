using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A login on one of the four self-service surfaces — customer, tenant, owner or partner.
///
/// Deliberately separate from the ERP user: a buyer must never be an ERP user, and scoping is
/// enforced server-side from this row rather than from anything the browser sends.
/// </summary>
public class PortalUser : BaseEntity
{
    public PortalAudience Audience { get; set; }

    public Guid? PartyId { get; set; }
    public Guid? LandlordId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? PartnerUserId { get; set; }
    public Guid? ResidentId { get; set; }

    public string LoginIdentifier { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Reference into the platform's credential store. No secret material lives here.</summary>
    public string? CredentialRef { get; set; }

    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool MustChangePassword { get; set; }

    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }

    public string PreferredLanguage { get; set; } = "en";
    public bool NotifyByEmail { get; set; } = true;
    public bool NotifyBySms { get; set; }
    public bool NotifyByWhatsApp { get; set; } = true;
    public bool NotifyByPush { get; set; } = true;

    public DateTime? InvitedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
}

public class PortalSession : BaseEntity
{
    public Guid PortalUserId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceType { get; set; }
    public int PageViewCount { get; set; }
    public bool WasTerminated { get; set; }
}

/// <summary>A message sent through a portal. Lands on the internal timeline as an activity too.</summary>
public class PortalMessage : BaseEntity
{
    public Guid PortalUserId { get; set; }
    public Guid? PartyId { get; set; }

    /// <summary>"Inbound" from the portal user, "Outbound" from the firm.</summary>
    public string Direction { get; set; } = "Inbound";

    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }

    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? ComplaintId { get; set; }
    public Guid? ClientBuildContractId { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public Guid? RespondedByUserId { get; set; }
    public Guid? ActivityId { get; set; }
    public Guid? FollowUpTaskId { get; set; }
    public bool RequiresResponse { get; set; }
}

/// <summary>
/// When an alert fires, to whom, and on which channel. Every shipped alert has a row here with a
/// sensible default, and every one can be switched off — an alert nobody can silence gets ignored.
/// </summary>
public class NotificationRule : BaseEntity
{
    /// <summary>Stable key: "hold_expiring", "lead_sla_breach", "instalment_overdue",
    /// "cheque_bounced", "transfer_blocked_dues", "noc_expiring", "possession_at_risk",
    /// "snag_critical_open", "dlp_expiring", "tenancy_expiring", "rent_overdue",
    /// "compliance_expiring", "deposit_registration_due", "workorder_sla_breach", "ppm_due",
    /// "meter_implausible", "escrow_over_entitlement", "qpr_due", "licence_expiring",
    /// "client_money_exception", "partner_registration_conflict".</summary>
    public string RuleKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

    /// <summary>Comma-separated channels. Several, because a critical alert should not rely on email.</summary>
    public string Channels { get; set; } = "InApp";

    /// <summary>Comma-separated role keys the alert goes to.</summary>
    public string? TargetRoles { get; set; }

    public Guid? TargetUserId { get; set; }

    /// <summary>Days before the event to fire, for the ones that look ahead.</summary>
    public int? LeadDays { get; set; }

    public decimal? ThresholdAmount { get; set; }

    /// <summary>Batched into a digest instead of firing individually. Right for the noisy ones.</summary>
    public bool IsDigest { get; set; }

    public string? DigestSchedule { get; set; }

    /// <summary>Honour the company's quiet hours. False only for genuine emergencies.</summary>
    public bool RespectQuietHours { get; set; } = true;

    public int? EscalateAfterHours { get; set; }
    public string? EscalateToRole { get; set; }
    public Guid? MessageTemplateId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }
}

public class NotificationLog : BaseEntity
{
    public Guid? NotificationRuleId { get; set; }
    public string RuleKey { get; set; } = string.Empty;

    public NotificationChannel Channel { get; set; }
    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;

    public Guid? RecipientUserId { get; set; }
    public Guid? RecipientPartyId { get; set; }
    public Guid? PortalUserId { get; set; }
    public string? RecipientAddress { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? DeepLink { get; set; }

    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }

    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public bool Failed { get; set; }
    public string? FailureReason { get; set; }
    public int AttemptCount { get; set; }

    /// <summary>Held back by quiet hours or the daily cap, rather than dropped.</summary>
    public bool WasSuppressed { get; set; }
    public string? SuppressionReason { get; set; }

    public bool IsEscalation { get; set; }
}

/// <summary>A reusable message body per channel and language, with its placeholders declared.</summary>
public class MessageTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public NotificationChannel Channel { get; set; }
    public string LanguageCode { get; set; } = "en";
    public bool IsRightToLeft { get; set; }

    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;

    /// <summary>The provider's approved template name, which WhatsApp requires before you may send.</summary>
    public string? ProviderTemplateName { get; set; }
    public bool IsProviderApproved { get; set; }

    /// <summary>Declared placeholders, validated at render so a merge failure never goes out.</summary>
    public string? MergeFieldsJson { get; set; }

    public int Version { get; set; } = 1;
    public Guid? ApprovalRequestId { get; set; }
    public Guid? ProjectId { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// One thread with one person across every channel. The reason an agent can pick up a WhatsApp
/// conversation somebody else started three weeks ago.
/// </summary>
public class Conversation : BaseEntity
{
    public Guid? PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? ChannelPartnerId { get; set; }

    public NotificationChannel Channel { get; set; } = NotificationChannel.WhatsApp;
    public string? ExternalThreadId { get; set; }
    public string? ContactAddress { get; set; }

    public string? Subject { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }
    public int MessageCount { get; set; }
    public int UnreadCount { get; set; }

    public Guid? AssignedToUserId { get; set; }

    /// <summary>"Open", "Waiting", "Closed", "Snoozed".</summary>
    public string Status { get; set; } = "Open";

    public DateTime? SnoozedUntil { get; set; }

    /// <summary>Nobody has replied and the clock is running. Drives the shared-inbox board.</summary>
    public bool AwaitingReply { get; set; }

    public ICollection<ConversationMessage> Messages { get; set; } = [];
}

public class ConversationMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public ActivityDirection Direction { get; set; } = ActivityDirection.Outbound;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public string? Body { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public string? Caption { get; set; }

    public Guid? SentByUserId { get; set; }
    public Guid? MessageTemplateId { get; set; }
    public string? ExternalMessageId { get; set; }

    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool Failed { get; set; }
    public string? FailureReason { get; set; }

    public Guid? ActivityId { get; set; }
    public bool IsAutomated { get; set; }
}

/// <summary>
/// A call, with the tracking number that identifies where the lead came from. The reason portal
/// ROI can be measured on phone leads rather than only on web forms.
/// </summary>
public class CallLog : BaseEntity
{
    public ActivityDirection Direction { get; set; } = ActivityDirection.Outbound;

    public Guid? PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AgentId { get; set; }

    public string? FromNumber { get; set; }
    public string? ToNumber { get; set; }

    /// <summary>The DID that was dialled. Maps to a portal or a campaign, which is the whole point.</summary>
    public string? TrackingNumber { get; set; }
    public Guid? PortalChannelId { get; set; }
    public Guid? CampaignId { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; }
    public int? RingSeconds { get; set; }

    /// <summary>"Answered", "NoAnswer", "Busy", "Failed", "Voicemail", "Abandoned".</summary>
    public string Outcome { get; set; } = "Answered";

    public string? RecordingUrl { get; set; }
    public string? ProviderCallId { get; set; }
    public Guid? ActivityId { get; set; }
    public Guid? DispositionReasonCodeId { get; set; }
    public string? Note { get; set; }
    public bool CallBackRequested { get; set; }
    public DateTime? CallBackAt { get; set; }
}

/// <summary>A bulk send to a segment, with consent honoured and the result recorded per recipient.</summary>
public class BroadcastRun : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public NotificationChannel Channel { get; set; }
    public Guid? MessageTemplateId { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? AttachmentUrl { get; set; }

    /// <summary>"AllLeads", "ProjectLeads", "Defaulters", "BlockResidents", "Partners",
    /// "Tenants", "Owners", "Custom".</summary>
    public string SegmentKey { get; set; } = "Custom";

    public string? SegmentFilterJson { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }

    public DateTime? ScheduledFor { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int TargetCount { get; set; }
    public int SentCount { get; set; }

    /// <summary>Skipped for want of consent. Reported, because it explains the gap in the numbers.</summary>
    public int SuppressedCount { get; set; }

    public int DeliveredCount { get; set; }
    public int ReadCount { get; set; }
    public int FailedCount { get; set; }
    public int OptOutCount { get; set; }
    public decimal? Cost { get; set; }

    public Guid? RunByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }

    /// <summary>"Draft", "Scheduled", "Running", "Completed", "Cancelled", "Failed".</summary>
    public string Status { get; set; } = "Draft";

    public ICollection<BroadcastRecipient> Recipients { get; set; } = [];
}

public class BroadcastRecipient : BaseEntity
{
    public Guid BroadcastRunId { get; set; }
    public BroadcastRun? Run { get; set; }

    public Guid? PartyId { get; set; }
    public Guid? PortalUserId { get; set; }
    public string? Address { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ClickedAt { get; set; }

    public bool WasSuppressed { get; set; }
    public string? SuppressionReason { get; set; }
    public bool Failed { get; set; }
    public string? FailureReason { get; set; }
    public bool OptedOut { get; set; }
}

/// <summary>
/// A bulk load with a dry run.
///
/// Every real customer arrives with fifteen years of history in spreadsheets — properties, units,
/// price lists, bookings and the instalments already paid against them. An import that cannot take
/// that is a lost deal, and one that writes before it validates is a worse one.
/// </summary>
public class ImportBatch : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public ImportEntityKind EntityKind { get; set; }
    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Uploaded;

    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public long? FileSizeBytes { get; set; }

    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }

    /// <summary>Saved column-to-field mapping, so the next month's file does not need re-mapping.</summary>
    public string? MappingProfileJson { get; set; }

    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    public int WarningRows { get; set; }
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }
    public int UpdatedRows { get; set; }

    /// <summary>Validated and previewed. Nothing is written until a human commits it.</summary>
    public bool DryRunCompleted { get; set; }
    public DateTime? DryRunAt { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? RunByUserId { get; set; }

    /// <summary>Update existing rows on a key match instead of refusing the whole file.</summary>
    public bool AllowUpdates { get; set; }
    public string? MatchKeyField { get; set; }

    /// <summary>Everything written by this batch, so a bad import can be undone in one action.</summary>
    public bool CanRollback { get; set; }
    public bool WasRolledBack { get; set; }
    public DateTime? RolledBackAt { get; set; }

    public string? ErrorSummary { get; set; }

    public ICollection<ImportBatchError> Errors { get; set; } = [];
}

public class ImportBatchError : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public ImportBatch? Batch { get; set; }

    public int RowNumber { get; set; }
    public string? ColumnName { get; set; }
    public string? CellValue { get; set; }

    /// <summary>"Error" blocks the row; "Warning" imports it and flags it.</summary>
    public string Severity { get; set; } = "Error";

    public string Message { get; set; } = string.Empty;

    /// <summary>What the operator should actually do about it, in their language not the parser's.</summary>
    public string? Suggestion { get; set; }

    public string? RawRowJson { get; set; }
    public bool IsResolved { get; set; }
}

/// <summary>
/// A reason-coded record of something a person overrode.
///
/// This is a market with disputes and court cases; the audit log is evidence. The platform's own
/// audit captures the field-level before and after — this adds the *why*, which no automatic
/// trail can infer.
/// </summary>
public class RealEstateAuditNote : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? EntityReference { get; set; }

    /// <summary>"PriceChange", "DiscountOverride", "SurchargeWaiver", "AllocationOverride",
    /// "StatusForce", "DuesOverride", "LitigationOverride", "EscrowOverride", "BallotOverride",
    /// "DocumentReissue", "CommissionAdjustment", "PossessionOverride".</summary>
    public string ActionKey { get; set; } = string.Empty;

    public string? BeforeValue { get; set; }
    public string? AfterValue { get; set; }
    public decimal? AmountImpact { get; set; }

    public Guid ReasonCodeId { get; set; }
    public string? Note { get; set; }

    public Guid PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public Guid? ApprovalRequestId { get; set; }
    public Guid? AuthorisedByUserId { get; set; }

    public string? IpAddress { get; set; }

    /// <summary>Flagged for the exception report the auditor reads first.</summary>
    public bool IsHighRisk { get; set; }
}

/// <summary>
/// A dashboard "needs attention" item, ordered by how much damage it does if ignored rather than
/// by how many of them there are. Six critical snags matter more than sixty small arrears.
/// </summary>
public class AttentionItem : BaseEntity
{
    public string ItemKey { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? Icon { get; set; }
    public string? Route { get; set; }

    public int Count { get; set; }
    public decimal? Amount { get; set; }

    /// <summary>Ranking weight. Higher sorts first, independent of how many items are behind it.</summary>
    public int Rank { get; set; }

    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }
    public LineOfBusiness? LineOfBusiness { get; set; }

    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    public bool IsDismissed { get; set; }
    public Guid? DismissedByUserId { get; set; }
    public DateTime? DismissedUntil { get; set; }
}
