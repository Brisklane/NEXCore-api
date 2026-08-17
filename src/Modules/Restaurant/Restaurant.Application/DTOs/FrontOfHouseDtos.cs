using Restaurant.Domain.Enums;

namespace Restaurant.Application.DTOs;

// ── Reservations ─────────────────────────────────────────────────────────────

public class ReservationDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public ReservationStatus Status { get; set; }

    public Guid? GuestProfileId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsVipGuest { get; set; }

    public int PartySize { get; set; }
    public DateTime ReservedFor { get; set; }
    public int DurationMinutes { get; set; }

    public Guid? TableId { get; set; }
    public string? TableNumber { get; set; }
    public Guid? SectionId { get; set; }
    public string? SectionName { get; set; }
    public Guid? FloorId { get; set; }

    public string? Occasion { get; set; }
    public string? SpecialRequests { get; set; }
    public string? AllergyNotes { get; set; }
    public bool IsHighChairNeeded { get; set; }
    public bool IsWheelchairAccess { get; set; }

    public decimal DepositAmount { get; set; }
    public bool IsDepositPaid { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? SeatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    public Guid? OrderId { get; set; }
    public bool ReminderSent { get; set; }
    public OrderChannel Source { get; set; }
    public string? Note { get; set; }

    /// <summary>Minutes until the booking. Negative means the party is late.</summary>
    public int MinutesUntil { get; set; }
    public bool IsLate { get; set; }
}

public class SaveReservationDto
{
    public Guid OutletId { get; set; }
    public Guid? GuestProfileId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int PartySize { get; set; } = 2;
    public DateTime ReservedFor { get; set; }
    public int DurationMinutes { get; set; } = 90;
    public Guid? TableId { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? FloorId { get; set; }
    public string? Occasion { get; set; }
    public string? SpecialRequests { get; set; }
    public string? AllergyNotes { get; set; }
    public bool IsHighChairNeeded { get; set; }
    public bool IsWheelchairAccess { get; set; }
    public decimal DepositAmount { get; set; }
    public bool IsDepositPaid { get; set; }
    public OrderChannel Source { get; set; } = OrderChannel.Phone;
    public string? Note { get; set; }
}

public class ChangeReservationStatusDto
{
    public ReservationStatus Status { get; set; }
    public Guid? TableId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>Which tables can actually take a party at a given time, and which are already taken.</summary>
public class ReservationAvailabilityDto
{
    public Guid OutletId { get; set; }
    public DateTime RequestedFor { get; set; }
    public int PartySize { get; set; }
    public int DurationMinutes { get; set; }

    public List<AvailableTableDto> AvailableTables { get; set; } = [];
    public List<ReservationDto> ConflictingReservations { get; set; } = [];

    /// <summary>Nearest times either side that would work, when nothing fits the requested slot.</summary>
    public List<DateTime> AlternativeTimes { get; set; } = [];
}

public class AvailableTableDto
{
    public Guid TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Seats { get; set; }
    public Guid? SectionId { get; set; }
    public string? SectionName { get; set; }
    public string? FloorName { get; set; }
    public TableState CurrentState { get; set; }

    /// <summary>How well the table fits: a party of 2 on a 10-top scores badly.</summary>
    public int FitScore { get; set; }
}

// ── Waitlist ─────────────────────────────────────────────────────────────────

public class WaitlistEntryDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int PartySize { get; set; }
    public WaitlistStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }
    public int QuotedWaitMinutes { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? SeatedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public Guid? TableId { get; set; }
    public string? TableNumber { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? GuestProfileId { get; set; }
    public Guid? PreferredSectionId { get; set; }
    public string? PreferredSectionName { get; set; }
    public string? PagerNumber { get; set; }
    public string? Note { get; set; }

    /// <summary>1-based place in the queue, derived from join order.</summary>
    public int Position { get; set; }

    /// <summary>Minutes actually waited so far — compared against the quote.</summary>
    public int WaitedMinutes { get; set; }
    public bool IsOverQuote { get; set; }
}

public class SaveWaitlistEntryDto
{
    public Guid OutletId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int PartySize { get; set; } = 2;
    public int QuotedWaitMinutes { get; set; }
    public Guid? PreferredSectionId { get; set; }
    public Guid? GuestProfileId { get; set; }
    public string? PagerNumber { get; set; }
    public string? Note { get; set; }
}

public class ChangeWaitlistStatusDto
{
    public WaitlistStatus Status { get; set; }
    public Guid? TableId { get; set; }
    public string? Note { get; set; }
}

// ── Guests ───────────────────────────────────────────────────────────────────

public class GuestProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
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
    public bool IsVip { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public int LoyaltyPoints { get; set; }
    public string? LoyaltyTier { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class SaveGuestProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime? Anniversary { get; set; }
    public string? DietaryPreferences { get; set; }
    public string? Allergies { get; set; }
    public string? FavouriteItems { get; set; }
    public string? PreferredSeating { get; set; }
    public bool IsVip { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public string? LoyaltyTier { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── Feedback ─────────────────────────────────────────────────────────────────

public class CustomerFeedbackDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid? CheckId { get; set; }
    public Guid? GuestProfileId { get; set; }
    public Guid? WaiterId { get; set; }
    public string? WaiterName { get; set; }
    public Guid? TableId { get; set; }
    public string? TableNumber { get; set; }
    public int OverallRating { get; set; }
    public int? FoodRating { get; set; }
    public int? ServiceRating { get; set; }
    public int? AmbienceRating { get; set; }
    public int? ValueRating { get; set; }
    public string? Comment { get; set; }
    public string? GuestName { get; set; }
    public string? Phone { get; set; }
    public DateTime SubmittedAt { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class SaveFeedbackDto
{
    public Guid OutletId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? CheckId { get; set; }
    public Guid? GuestProfileId { get; set; }
    public Guid? WaiterId { get; set; }
    public Guid? TableId { get; set; }
    public int OverallRating { get; set; }
    public int? FoodRating { get; set; }
    public int? ServiceRating { get; set; }
    public int? AmbienceRating { get; set; }
    public int? ValueRating { get; set; }
    public string? Comment { get; set; }
    public string? GuestName { get; set; }
    public string? Phone { get; set; }
}
