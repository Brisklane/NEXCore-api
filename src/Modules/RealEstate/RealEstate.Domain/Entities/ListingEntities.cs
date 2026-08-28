using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// An offering of a property to the market.
///
/// Separate from the property because one property is routinely listed for sale and for rent at
/// once, at different prices, by different agents, in different currencies — and is listed,
/// withdrawn and re-listed a dozen times across a decade. Folding this into the property would
/// destroy the marketing history that tells an agency which of those attempts worked.
/// </summary>
public class Listing : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    public string Reference { get; set; } = string.Empty;
    public ListingKind Kind { get; set; } = ListingKind.ForSale;
    public ListingStatus Status { get; set; } = ListingStatus.Draft;

    public Guid? OfficeId { get; set; }
    public Guid? ListingAgentId { get; set; }
    public Guid? InstructionId { get; set; }

    // ── Price ────────────────────────────────────────────────────────────────

    public decimal? AskingPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    /// <summary>Deliberately withholding the number. A real marketing choice, not a missing value.</summary>
    public bool PriceOnApplication { get; set; }

    public RentFrequency? RentFrequency { get; set; }
    public decimal? ServiceChargeAmount { get; set; }

    // ── Availability ─────────────────────────────────────────────────────────

    public DateOnly? AvailableFrom { get; set; }
    public bool VacantPossession { get; set; }

    /// <summary>Let with the tenant staying — an investment sale, not a home sale.</summary>
    public bool TenantInSitu { get; set; }

    public int? NoticeRequiredDays { get; set; }
    public bool IsChainFree { get; set; }

    // ── Content ──────────────────────────────────────────────────────────────

    public string? Headline { get; set; }
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }

    /// <summary>Newline-separated bullets. Portals want them as a list, brochures as prose.</summary>
    public string? KeyFeatures { get; set; }

    public string? EnergyRating { get; set; }
    public string? CouncilTaxBand { get; set; }
    public string LanguageCode { get; set; } = "en";

    /// <summary>Permit or agency reference some markets require on every advertisement.</summary>
    public string? RegulatoryPermitNumber { get; set; }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public DateOnly? ListedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public Guid? WithdrawnReasonCodeId { get; set; }

    public bool IsFeatured { get; set; }
    public bool AllowPortalPublish { get; set; } = true;
    public bool AllowWebsitePublish { get; set; } = true;

    // ── Performance counters, maintained by the events that cause them ───────

    public int ViewCount { get; set; }
    public int EnquiryCount { get; set; }
    public int ViewingCount { get; set; }
    public int OfferCount { get; set; }
    public DateTime? LastViewingAt { get; set; }
    public DateTime? LastEnquiryAt { get; set; }

    /// <summary>Days on market. Frozen when the listing completes so history stays true.</summary>
    public int? DaysOnMarket { get; set; }

    public ICollection<ListingPriceHistory> PriceHistory { get; set; } = [];
    public ICollection<PortalPublication> Publications { get; set; } = [];
}

/// <summary>
/// Every price the listing ever carried, with why it moved. A listing that has had no viewing in
/// three weeks at its current price is the report this table exists to produce.
/// </summary>
public class ListingPriceHistory : BaseEntity
{
    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }

    public decimal? FromPrice { get; set; }
    public decimal ToPrice { get; set; }
    public DateOnly ChangedOn { get; set; }
    public Guid ChangedByUserId { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? Note { get; set; }

    /// <summary>Viewings held at the previous price, so the effect of the reduction is measurable.</summary>
    public int ViewingsAtPreviousPrice { get; set; }
}

/// <summary>
/// The agreement with the owner that lets us market the property, and the fee it earns.
///
/// This is where an agency's money is actually decided: sole agency versus multiple agency
/// determines whether a fee is due when somebody else finds the buyer, and the tail period
/// determines whether it is due after the instruction ends.
/// </summary>
public class Instruction : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid? LandlordId { get; set; }
    public Guid? OwnerPartyId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? AgentId { get; set; }

    public string Reference { get; set; } = string.Empty;
    public AgencyBasis Basis { get; set; } = AgencyBasis.SoleAgency;
    public ListingKind Kind { get; set; } = ListingKind.ForSale;

    public DateOnly InstructedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public int NoticePeriodDays { get; set; } = 14;

    /// <summary>
    /// Days after the instruction ends during which a buyer we introduced still earns us the fee.
    /// The clause agencies forget to enforce and lose money on.
    /// </summary>
    public int TailPeriodDays { get; set; }

    // ── Fee ──────────────────────────────────────────────────────────────────

    public FeeBasis FeeBasis { get; set; } = FeeBasis.PercentOfPrice;
    public decimal FeePercent { get; set; }
    public decimal FeeFixedAmount { get; set; }
    public decimal MinimumFee { get; set; }

    /// <summary>For lettings quoted as "two weeks' rent" rather than a percentage.</summary>
    public decimal? FeePeriodsOfRent { get; set; }

    public bool FeeIncludesTax { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal WithdrawalFee { get; set; }
    public decimal MarketingBudget { get; set; }

    public ManagementService? ManagementService { get; set; }

    public string? DocumentUrl { get; set; }
    public DateOnly? SignedOn { get; set; }
    public DateOnly? TerminatedOn { get; set; }
    public Guid? TerminationReasonCodeId { get; set; }

    public ICollection<InstructionTerm> Terms { get; set; } = [];
}

/// <summary>A negotiated term that departs from the standard agency agreement.</summary>
public class InstructionTerm : BaseEntity
{
    public Guid InstructionId { get; set; }
    public Instruction? Instruction { get; set; }
    public string TermKey { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A place we advertise. One per portal per market, holding the credentials reference, the feed
/// shape and the plan limits — a firm on a ten-slot plan must be told before it breaches it, not
/// after.
/// </summary>
public class PortalChannel : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>"Xml", "Json", "Csv", "Mits", "Api". Decides which serialiser runs.</summary>
    public string FeedFormat { get; set; } = "Xml";

    public string? EndpointUrl { get; set; }
    public string? FeedUrl { get; set; }

    /// <summary>Reference into the platform's secret store. Credentials are never held here.</summary>
    public string? CredentialRef { get; set; }

    public string? CountryCode { get; set; }

    /// <summary>Slots the plan allows. Zero means unlimited.</summary>
    public int ListingQuota { get; set; }
    public int FeaturedQuota { get; set; }
    public int CurrentListingCount { get; set; }

    public decimal MonthlyCost { get; set; }
    public DateOnly? ContractExpiresOn { get; set; }

    public int MinPhotoCount { get; set; }
    public int MaxPhotoCount { get; set; }
    public int MinPhotoWidthPx { get; set; }
    public bool SupportsFloorPlan { get; set; } = true;
    public bool SupportsVirtualTour { get; set; }

    /// <summary>Minutes between automatic refresh pushes. Portals demote stale listings.</summary>
    public int RefreshIntervalMinutes { get; set; }

    public ICollection<PortalMapping> Mappings { get; set; } = [];
}

/// <summary>
/// How our vocabulary becomes theirs. Every portal names property types, features and amenities
/// differently, and "apartment" on one is "flat" on the next and code 24 on a third.
/// </summary>
public class PortalMapping : BaseEntity
{
    public Guid PortalChannelId { get; set; }
    public PortalChannel? Channel { get; set; }

    /// <summary>"PropertySubType", "Feature", "Category", "Facing", "Furnishing".</summary>
    public string MappingKind { get; set; } = string.Empty;

    public string LocalValue { get; set; } = string.Empty;
    public string PortalValue { get; set; } = string.Empty;

    /// <summary>Portal's own area or geography id, where they demand their own location tree.</summary>
    public string? PortalLocationId { get; set; }
}

/// <summary>One listing's life on one portal.</summary>
public class PortalPublication : BaseEntity
{
    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }
    public Guid PortalChannelId { get; set; }

    public PortalPublishState State { get; set; } = PortalPublishState.NotPublished;

    /// <summary>Their id for our listing. Needed to update or withdraw it later.</summary>
    public string? PortalListingId { get; set; }

    public string? PortalUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? LastPushedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public bool IsFeatured { get; set; }

    /// <summary>What the portal complained about. Shown on the syndication desk, not swallowed.</summary>
    public string? LastError { get; set; }

    public int FailureCount { get; set; }

    // Performance, imported from the portal where it reports back.
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public int Leads { get; set; }

    public ICollection<PortalPublishLog> Logs { get; set; } = [];
}

public class PortalPublishLog : BaseEntity
{
    public Guid PortalPublicationId { get; set; }
    public PortalPublication? Publication { get; set; }

    /// <summary>"Publish", "Update", "Withdraw", "Refresh".</summary>
    public string Operation { get; set; } = string.Empty;

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public bool Succeeded { get; set; }
    public string? ResponseCode { get; set; }
    public string? Message { get; set; }
    public int DurationMs { get; set; }
}

/// <summary>
/// A lead that arrived from a portal, kept as its own record so portal ROI survives the enquiry
/// being merged into an existing person.
/// </summary>
public class PortalLeadRef : BaseEntity
{
    public Guid PortalChannelId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? EnquiryId { get; set; }

    public string? PortalLeadId { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public string? RawPayload { get; set; }
    public bool WasDuplicate { get; set; }
}

/// <summary>A marketing spend with a tracking code, so a booking can be traced back to what paid for it.</summary>
public class Campaign : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>"Portal", "Google", "Meta", "Print", "Hoarding", "Sms", "Email", "Event", "Referral".</summary>
    public string Channel { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }

    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public decimal Budget { get; set; }
    public decimal ActualSpend { get; set; }

    public string? LandingPageUrl { get; set; }
    public string? TrackingCode { get; set; }
    public string? CreativeUrl { get; set; }

    // Outcome counters, recomputed nightly from attribution rather than incremented ad hoc.
    public int LeadCount { get; set; }
    public int VisitCount { get; set; }
    public int BookingCount { get; set; }
    public decimal BookingValue { get; set; }

    public ICollection<CampaignCost> Costs { get; set; } = [];
}

public class CampaignCost : BaseEntity
{
    public Guid CampaignId { get; set; }
    public Campaign? Campaign { get; set; }
    public DateOnly IncurredOn { get; set; }
    public decimal Amount { get; set; }
    public Guid? SupplierPartyId { get; set; }
    public string? InvoiceReference { get; set; }
}

/// <summary>
/// One touch on the path to a booking. Several rows per enquiry: first touch and last touch are
/// both interesting, and a simple multi-touch view falls out of having them all.
/// </summary>
public class CampaignAttribution : BaseEntity
{
    public Guid? CampaignId { get; set; }
    public Guid? PortalChannelId { get; set; }
    public Guid EnquiryId { get; set; }
    public Guid? BookingId { get; set; }

    /// <summary>"First", "Last", "Assist".</summary>
    public string TouchType { get; set; } = "Assist";

    public DateTime TouchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Share of the credit. Rows for one booking sum to 100.</summary>
    public decimal WeightPercent { get; set; }

    public decimal AttributedValue { get; set; }
}

/// <summary>A launch, open house, exhibition or roadshow — and the leads and bookings it produced.</summary>
public class MarketingEvent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? EventType { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? CampaignId { get; set; }

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

    public ICollection<EventRegistration> Registrations { get; set; } = [];
}

public class EventRegistration : BaseEntity
{
    public Guid MarketingEventId { get; set; }
    public MarketingEvent? Event { get; set; }

    public Guid? PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public DateTime? RegisteredAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public bool IsWalkIn { get; set; }
    public int GuestCount { get; set; } = 1;
    public string? SlotLabel { get; set; }
    public bool FollowUpCreated { get; set; }
}

/// <summary>
/// A brochure, price list, floor plan or social asset — versioned with an expiry, so nobody
/// forwards last year's price list to a customer.
/// </summary>
public class ContentAsset : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>"Brochure", "PriceList", "FloorPlan", "Video", "SocialCard", "PaymentPlan", "Presentation".</summary>
    public string AssetType { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public Guid? ListingId { get; set; }

    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int Version { get; set; } = 1;
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ExpiresOn { get; set; }

    /// <summary>Partners and portals only see assets cleared for outside use.</summary>
    public bool IsExternallyShareable { get; set; } = true;

    public string LanguageCode { get; set; } = "en";
    public int DownloadCount { get; set; }
    public bool IsCurrent { get; set; } = true;
}
