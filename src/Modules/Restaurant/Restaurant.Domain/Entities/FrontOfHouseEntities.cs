using Nexcore.SharedKernel;
using Restaurant.Domain.Enums;

namespace Restaurant.Domain.Entities;

/// <summary>
/// A booking. Held against a time window rather than a single instant, because a table booked
/// at 19:00 is unavailable until roughly 20:30 and the availability engine has to know that.
/// </summary>
public class Reservation : BaseEntity
{
    public Guid OutletId { get; set; }

    public string ReservationNumber { get; set; } = string.Empty;
    public ReservationStatus Status { get; set; } = ReservationStatus.Requested;

    public Guid? GuestProfileId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public int PartySize { get; set; } = 2;
    public DateTime ReservedFor { get; set; }

    /// <summary>How long the table is held. Defaults from the outlet's average dining time.</summary>
    public int DurationMinutes { get; set; } = 90;

    public Guid? TableId { get; set; }
    public DiningTable? Table { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? FloorId { get; set; }

    public string? Occasion { get; set; }
    public string? SpecialRequests { get; set; }
    public string? AllergyNotes { get; set; }
    public bool IsHighChairNeeded { get; set; }
    public bool IsWheelchairAccess { get; set; }

    /// <summary>Money taken to hold the booking, forfeited on a no-show.</summary>
    public decimal DepositAmount { get; set; }
    public bool IsDepositPaid { get; set; }
    public DateTime? DepositPaidAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? SeatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    /// <summary>Set when guests are seated, so the booking links to what they actually spent.</summary>
    public Guid? OrderId { get; set; }

    public bool ReminderSent { get; set; }
    public OrderChannel Source { get; set; } = OrderChannel.Phone;
    public string? Note { get; set; }
}

/// <summary>
/// A walk-in party waiting for a table. Position is derived from <see cref="JoinedAt"/> rather
/// than stored, so removing someone from the middle of the queue cannot leave a gap.
/// </summary>
public class WaitlistEntry : BaseEntity
{
    public Guid OutletId { get; set; }

    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int PartySize { get; set; } = 2;

    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>What the host told the guest. Compared against reality in the wait-time report.</summary>
    public int QuotedWaitMinutes { get; set; }

    public DateTime? NotifiedAt { get; set; }
    public DateTime? SeatedAt { get; set; }
    public DateTime? LeftAt { get; set; }

    public Guid? TableId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? GuestProfileId { get; set; }

    /// <summary>Guest asked for a specific area — patio, non-smoking, booth.</summary>
    public Guid? PreferredSectionId { get; set; }

    public string? PagerNumber { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A guest the venue recognises. Kept in Restaurant rather than only in CRM because a diner is
/// not necessarily a company customer — but <see cref="CustomerId"/> links the two when they are.
/// </summary>
public class GuestProfile : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>CRM contact, when this guest has been promoted to a company customer.</summary>
    public Guid? CustomerId { get; set; }

    public DateTime? Birthday { get; set; }
    public DateTime? Anniversary { get; set; }

    public string? DietaryPreferences { get; set; }
    public string? Allergies { get; set; }
    public string? FavouriteItems { get; set; }
    public string? PreferredSeating { get; set; }

    public int VisitCount { get; set; }
    public decimal LifetimeSpend { get; set; }
    public decimal AverageCheck { get; set; }
    public DateTime? FirstVisitAt { get; set; }
    public DateTime? LastVisitAt { get; set; }

    public int NoShowCount { get; set; }

    /// <summary>Regular, reviewer, or owner's guest — flags the host should act on.</summary>
    public bool IsVip { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }

    public int LoyaltyPoints { get; set; }
    public string? LoyaltyTier { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Guest feedback tied to the check and the waiter, so it is actionable rather than anonymous.</summary>
public class CustomerFeedback : BaseEntity
{
    public Guid OutletId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? CheckId { get; set; }
    public Guid? GuestProfileId { get; set; }
    public Guid? WaiterId { get; set; }
    public Guid? TableId { get; set; }

    /// <summary>1–5.</summary>
    public int OverallRating { get; set; }
    public int? FoodRating { get; set; }
    public int? ServiceRating { get; set; }
    public int? AmbienceRating { get; set; }
    public int? ValueRating { get; set; }

    public string? Comment { get; set; }
    public string? GuestName { get; set; }
    public string? Phone { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Low scores need a manager response; this closes the loop.</summary>
    public bool IsResolved { get; set; }
    public string? ResolutionNote { get; set; }
    public Guid? ResolvedByStaffId { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
