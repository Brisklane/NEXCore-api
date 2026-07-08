using Nexcore.SharedKernel;
using Nexcore.SharedKernel.ValueObjects;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Point of Sale store / outlet configuration.
///
/// IMPORTANT - No address, phone, email, or location fields are stored here.
/// Those are owned by <c>Core.Domain.Entities.Branch</c>.
/// This entity's inherited <c>BranchId</c> (from <see cref="BaseEntity"/>) IS the
/// branch registration for this store.
/// To display store address, phone, GPS etc. join on:
///     Core.Branch WHERE Branch.Id = PosStore.BranchId
///
/// A single Branch can have more than one PosStore
/// (e.g., a "Retail Floor" store and a "Wholesale Counter" store in the same branch).
/// </summary>
public class PosStore : BaseEntity
{
    // ? Identity
    /// <summary>
    /// Trading name shown to customers on receipts and the app.
    /// Falls back to Branch.Name when null.
    /// </summary>
    public string? TradingName { get; set; }

    // ? Geo Reference
    /// <summary>
    /// Cross-module int FK to Core.City.Id.
    /// Used to derive the city-code segment of the auto-generated Code (e.g. ISB in POS-ISB-000001).
    /// </summary>
    public int? CityId { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code (denormalized from Core.Country).</summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Language reference for the native-script name.
    /// Cross-module Guid FK to Core.Language (referenced by ID only).
    /// Determines which script/locale to use for the customer-facing app.
    /// </summary>
    public Guid? NativeLanguageId { get; set; }

    // ? Store Type & Format
    /// <summary>Business / industry category (e.g. Restaurant, Grocery).</summary>
    public PosStoreType StoreType { get; set; } = PosStoreType.Retail;

    /// <summary>Operational format / channel (e.g. Physical, DriveThrough, Virtual).</summary>
    public PosStoreFormat StoreFormat { get; set; } = PosStoreFormat.Physical;

    // ? POS-Specific Config
    /// <summary>
    /// Default warehouse for stock deductions.
    /// FK to Inventory.Domain.Entities.Warehouse - referenced by ID only.
    /// </summary>
    public Guid? DefaultWarehouseId { get; set; }

    /// <summary>Default price list for walk-in / anonymous customers.</summary>
    public Guid? DefaultPriceListId { get; set; }
    public PriceList? DefaultPriceList { get; set; }

    /// <summary>Receipt template applied to all terminals in this store.</summary>
    public Guid? ReceiptTemplateId { get; set; }
    public PosReceiptTemplate? ReceiptTemplate { get; set; }

    /// <summary>Whether this store accepts online orders for customer pickup.</summary>
    public bool AcceptsOnlinePickup { get; set; }

    /// <summary>Whether this store dispatches riders for delivery.</summary>
    public bool HasDelivery { get; set; }

    /// <summary>Whether this store is visible and accepting orders on the customer app.</summary>
    public bool IsOnlineOrderingEnabled { get; set; }

    /// <summary>
    /// Real-time availability override for the customer app.
    /// System sets this automatically based on Schedule/Holidays.
    /// Staff can also manually set to Busy or Closed (e.g., kitchen overloaded).
    /// </summary>
    public StoreOnlineStatus OnlineStatus { get; set; } = StoreOnlineStatus.Closed;

    /// <summary>Reason shown to customer when store is temporarily closed/busy.</summary>
    public string? OnlineStatusNote { get; set; }

    /// <summary>Estimated preparation time in minutes shown on the app.</summary>
    public int? EstimatedPrepTimeMinutes { get; set; }

    /// <summary>Minimum order amount for online orders.</summary>
    public decimal? MinOnlineOrderAmount { get; set; }

    /// <summary>Maximum delivery radius in kilometres from the store.</summary>
    public double? MaxDeliveryRadiusKm { get; set; }

    /// <summary>URL of the store logo/banner shown on the customer app.</summary>
    public string? OnlineLogoUrl { get; set; }
    public string? OnlineBannerUrl { get; set; }

    /// <summary>
    /// GPS coordinates for store-discovery / nearby search.
    /// Falls back to Core.Branch lat/lng when null.
    /// Stored here so multi-store branches can have distinct GPS points.
    /// </summary>
    public GeoCoordinate? Location { get; set; }

    /// <summary>
    /// Set for marketplace / home-chef stores. Null for admin-created POS stores.
    /// Mirrors <see cref="StoreVendorProfile.OnboardingStatus"/> for fast filtering.
    /// </summary>
    public VendorOnboardingStatus? OnboardingStatus { get; set; }

    // ? Navigation
    public ICollection<PosStoreSchedule> Schedule { get; set; } = new List<PosStoreSchedule>();
    public ICollection<PosStoreHoliday> Holidays { get; set; } = new List<PosStoreHoliday>();
    public ICollection<PosTerminal> Terminals { get; set; } = new List<PosTerminal>();
    public ICollection<PosCashier> Cashiers { get; set; } = new List<PosCashier>();
    public ICollection<DeliveryZone> DeliveryZones { get; set; } = new List<DeliveryZone>();
    public ICollection<StoreMenu> Menus { get; set; } = new List<StoreMenu>();
    public StoreVendorProfile? VendorProfile { get; set; }
}
