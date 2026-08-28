using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// An appointment to show property.
///
/// Models both an agency viewing and a multi-property tour: one appointment, several properties in
/// sequence, one confirmation to the client. Splitting those into two entities would mean two
/// diaries, and an agent only has one afternoon.
/// </summary>
public class Viewing : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? EnquiryId { get; set; }
    public Guid? PartyId { get; set; }
    public Guid AgentId { get; set; }
    public Guid? OfficeId { get; set; }

    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;

    /// <summary>Padding before the slot so the diary does not book two viewings across a city.</summary>
    public int TravelMinutes { get; set; }

    public ViewingStatus Status { get; set; } = ViewingStatus.Scheduled;

    public AccessArrangement Access { get; set; } = AccessArrangement.AgentHasKeys;
    public string? AccessNote { get; set; }
    public Guid? KeySetId { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public Guid? CancelReasonCodeId { get; set; }
    public string? CancelNote { get; set; }
    public Guid? RescheduledToViewingId { get; set; }

    /// <summary>Notice served on a sitting tenant. Statutory in several markets, so it is recorded.</summary>
    public Guid? AccessNoticeId { get; set; }

    public bool VendorNotified { get; set; }
    public bool ReminderSent { get; set; }
    public bool FeedbackRequested { get; set; }
    public bool FeedbackReceived { get; set; }

    /// <summary>Optimised order and timings for a multi-property tour.</summary>
    public string? RouteGeoJson { get; set; }

    public ICollection<ViewingProperty> Properties { get; set; } = [];
    public ICollection<ViewingAttendee> Attendees { get; set; } = [];
}

/// <summary>One stop on a viewing appointment.</summary>
public class ViewingProperty : BaseEntity
{
    public Guid ViewingId { get; set; }
    public Viewing? Viewing { get; set; }

    public Guid PropertyId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? UnitId { get; set; }

    public int SequenceNumber { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public bool WasSeen { get; set; }

    /// <summary>Ran out of time, could not get in, tenant refused. Not the same as "did not like it".</summary>
    public string? NotSeenReason { get; set; }
}

public class ViewingAttendee : BaseEntity
{
    public Guid ViewingId { get; set; }
    public Viewing? Viewing { get; set; }

    public Guid? PartyId { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }

    /// <summary>"Applicant", "Partner", "Family", "Adviser", "Vendor", "Tenant", "Agent", "Surveyor".</summary>
    public string Role { get; set; } = "Applicant";

    public bool Attended { get; set; }
    public bool IsDecisionMaker { get; set; }
}

/// <summary>
/// What the viewer thought, in a shape the vendor can be told.
///
/// Vendors leave agencies because they never hear back after a viewing. This closes that loop, and
/// it is structured rather than free text so that "four viewings, all said the price" is a report
/// rather than an anecdote.
/// </summary>
public class ViewingFeedback : BaseEntity
{
    public Guid ViewingId { get; set; }
    public Guid? ViewingPropertyId { get; set; }
    public Guid? PropertyId { get; set; }

    public InterestLevel Interest { get; set; } = InterestLevel.Lukewarm;

    /// <summary>"TooHigh", "AboutRight", "GoodValue". The most actionable field on the record.</summary>
    public string? PriceOpinion { get; set; }

    public decimal? WouldOfferAmount { get; set; }

    public string? Liked { get; set; }
    public string? Disliked { get; set; }
    public Guid? ObjectionReasonCodeId { get; set; }

    /// <summary>"SecondViewing", "MakeOffer", "SendMore", "NotProceeding", "Thinking".</summary>
    public string? NextStep { get; set; }

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public Guid? CapturedByUserId { get; set; }

    public bool SharedWithVendor { get; set; }
    public DateTime? SharedAt { get; set; }
}

/// <summary>
/// A developer's site visit. Different enough from an agency viewing to be its own record: it has
/// transport, a pickup point, a driver, a sales executive at the site office, and the visit-to-
/// booking ratio it produces is the number a project head is judged on.
/// </summary>
public class SiteVisit : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? PartyId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? SalesExecutiveId { get; set; }

    public DateTime ScheduledAt { get; set; }
    public ViewingStatus Status { get; set; } = ViewingStatus.Scheduled;

    /// <summary>Second and later visits convert far better, and are counted separately for that reason.</summary>
    public bool IsRevisit { get; set; }
    public int VisitNumber { get; set; } = 1;

    public int GuestCount { get; set; } = 1;
    public DateTime? ArrivedAt { get; set; }
    public DateTime? LeftAt { get; set; }

    public string? UnitsShown { get; set; }
    public Guid? ShownUnitId { get; set; }

    public bool CostSheetIssued { get; set; }
    public bool BrochureIssued { get; set; }

    public Guid? ResultingBookingId { get; set; }
    public Guid? ResultingReservationId { get; set; }
    public Guid? NoShowReasonCodeId { get; set; }

    public bool ReminderSent { get; set; }

    public Guid? TransportId { get; set; }
    public Guid? FeedbackId { get; set; }
}

/// <summary>Getting the customer to site, which the developer usually pays for.</summary>
public class SiteVisitTransport : BaseEntity
{
    public Guid SiteVisitId { get; set; }
    public TransportArrangement Arrangement { get; set; } = TransportArrangement.OwnTransport;

    public string? PickupAddress { get; set; }
    public DateTime? PickupAt { get; set; }
    public decimal? PickupLatitude { get; set; }
    public decimal? PickupLongitude { get; set; }

    public Guid? DriverUserId { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string? VehicleNumber { get; set; }

    public decimal? Cost { get; set; }
    public bool WasProvided { get; set; }
    public string? Note { get; set; }
}

public class SiteVisitFeedback : BaseEntity
{
    public Guid SiteVisitId { get; set; }

    public InterestLevel Interest { get; set; } = InterestLevel.Lukewarm;
    public string? PriceOpinion { get; set; }
    public string? PreferredUnitType { get; set; }
    public decimal? BudgetIndicated { get; set; }

    public string? Liked { get; set; }
    public Guid? ObjectionReasonCodeId { get; set; }
    public string? Objection { get; set; }

    /// <summary>"Booking", "Revisit", "Discuss", "SendPlan", "NotProceeding".</summary>
    public string? NextStep { get; set; }

    public DateOnly? NextStepDate { get; set; }
    public int? SatisfactionRating { get; set; }
    public Guid? CapturedByUserId { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Physical keys to a property. Mundane and constantly wrong in practice — an agency that cannot
/// say who has the keys to number 14 cannot do a viewing this afternoon.
/// </summary>
public class KeySet : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? TagNumber { get; set; }
    public int KeyCount { get; set; } = 1;

    /// <summary>Held only as a reference to the platform secret store, never in clear text here.</summary>
    public string? KeySafeCodeRef { get; set; }

    public string? KeySafeLocation { get; set; }

    public bool IsOut { get; set; }
    public Guid? CurrentHolderUserId { get; set; }
    public Guid? CurrentHolderPartyId { get; set; }
    public DateTime? OutSince { get; set; }
    public DateTime? DueBackAt { get; set; }
    public bool IsLost { get; set; }

    public ICollection<KeyMovement> Movements { get; set; } = [];
}

public class KeyMovement : BaseEntity
{
    public Guid KeySetId { get; set; }
    public KeySet? KeySet { get; set; }

    /// <summary>"Out", "In", "Lost", "Replaced", "HandedToOwner".</summary>
    public string Movement { get; set; } = "Out";

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? HolderUserId { get; set; }
    public Guid? HolderPartyId { get; set; }
    public string? HolderName { get; set; }
    public Guid? ViewingId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public DateTime? DueBackAt { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Statutory notice to a sitting tenant before entering. Recorded because the period is set by law
/// and computed from the office's working calendar, not guessed.
/// </summary>
public class AccessNotice : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? ViewingId { get; set; }
    public Guid? WorkOrderId { get; set; }

    /// <summary>"Viewing", "Inspection", "Repair", "Valuation", "Compliance".</summary>
    public string Purpose { get; set; } = "Viewing";

    public DateTime ServedAt { get; set; } = DateTime.UtcNow;
    public NotificationChannel ServedVia { get; set; } = NotificationChannel.Email;
    public DateTime AccessAt { get; set; }
    public int NoticePeriodHours { get; set; } = 24;

    public bool TenantConsented { get; set; }
    public DateTime? ConsentedAt { get; set; }
    public bool TenantRefused { get; set; }
    public string? RefusalReason { get; set; }
    public string? DocumentUrl { get; set; }
}

/// <summary>A scheduled open house or launch day, with a slot grid so arrivals are spread out.</summary>
public class OpenHouse : BaseEntity
{
    public Guid? PropertyId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public Guid? HostAgentId { get; set; }

    public int SlotMinutes { get; set; } = 15;
    public int MaxPerSlot { get; set; } = 4;
    public bool AllowWalkIns { get; set; } = true;

    public int RegisteredCount { get; set; }
    public int AttendedCount { get; set; }
    public int OfferCount { get; set; }
    public bool IsCancelled { get; set; }

    public ICollection<OpenHouseSlot> Slots { get; set; } = [];
}

public class OpenHouseSlot : BaseEntity
{
    public Guid OpenHouseId { get; set; }
    public OpenHouse? OpenHouse { get; set; }

    public DateTime StartsAt { get; set; }
    public int Capacity { get; set; }
    public int BookedCount { get; set; }

    public Guid? PartyId { get; set; }
    public string? AttendeeName { get; set; }
    public string? AttendeePhone { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public bool IsWalkIn { get; set; }
    public Guid? ResultingEnquiryId { get; set; }
}
