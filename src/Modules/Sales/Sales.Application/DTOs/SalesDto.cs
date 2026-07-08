using Sales.Domain.Enums;

namespace Sales.Application.DTOs;

// POS Store

public class PosStoreDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string? Code { get; set; }
    public long? CodeInt { get; set; }
    public int? CityId { get; set; }
    public string? CountryCode { get; set; }
    public string? TradingName { get; set; }
    public Guid? NativeLanguageId { get; set; }
    public PosStoreType StoreType { get; set; }
    public PosStoreFormat StoreFormat { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public Guid? DefaultPriceListId { get; set; }
    public bool AcceptsOnlinePickup { get; set; }
    public bool HasDelivery { get; set; }
    public bool IsOnlineOrderingEnabled { get; set; }
    public StoreOnlineStatus OnlineStatus { get; set; }
    public string? OnlineStatusNote { get; set; }
    public int? EstimatedPrepTimeMinutes { get; set; }
    public decimal? MinOnlineOrderAmount { get; set; }
    public double? MaxDeliveryRadiusKm { get; set; }
    public string? OnlineLogoUrl { get; set; }
    public string? OnlineBannerUrl { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; }
}

public class PosStoreNearbyDto : PosStoreDto
{
    public double DistanceKm { get; set; }
}

public class CreatePosStoreDto
{
    public int? CityId { get; set; }
    public string? CountryCode { get; set; }
    public string? TradingName { get; set; }
    public Guid? NativeLanguageId { get; set; }
    public PosStoreType StoreType { get; set; } = PosStoreType.Retail;
    public PosStoreFormat StoreFormat { get; set; } = PosStoreFormat.Physical;
    public Guid? DefaultWarehouseId { get; set; }
    public Guid? DefaultPriceListId { get; set; }
    public bool AcceptsOnlinePickup { get; set; }
    public bool HasDelivery { get; set; }
    public bool IsOnlineOrderingEnabled { get; set; }
    public int? EstimatedPrepTimeMinutes { get; set; }
    public decimal? MinOnlineOrderAmount { get; set; }
    public double? MaxDeliveryRadiusKm { get; set; }
    public string? OnlineLogoUrl { get; set; }
    public string? OnlineBannerUrl { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class UpdatePosStoreDto
{
    public string? TradingName { get; set; }
    public Guid? NativeLanguageId { get; set; }
    public PosStoreType? StoreType { get; set; }
    public PosStoreFormat? StoreFormat { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public Guid? DefaultPriceListId { get; set; }
    public StoreOnlineStatus? OnlineStatus { get; set; }
    public string? OnlineStatusNote { get; set; }
    public bool? IsOnlineOrderingEnabled { get; set; }
    public int? EstimatedPrepTimeMinutes { get; set; }
    public decimal? MinOnlineOrderAmount { get; set; }
    public double? MaxDeliveryRadiusKm { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool? IsActive { get; set; }
}

public class SetStoreOnlineStatusDto
{
    public StoreOnlineStatus OnlineStatus { get; set; }
    public string? Note { get; set; }
}

// Vendor Profile

public class StoreVendorProfileDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }

    // Owner identity
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerCnic { get; set; }
    public string? OwnerPhone { get; set; }
    public string? OwnerEmail { get; set; }
    public string? CnicFrontDocUrl { get; set; }
    public string? CnicBackDocUrl { get; set; }

    // Business
    public string? BusinessName { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? FoodLicenseNumber { get; set; }
    public DateOnly? FoodLicenseExpiry { get; set; }
    public string? FoodLicenseDocUrl { get; set; }
    public string? BusinessDescription { get; set; }

    // Banking
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? AccountTitle { get; set; }
    public string? AccountNumber { get; set; }
    public string? IbanNumber { get; set; }

    // Social media
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TiktokUrl { get; set; }
    public string? WhatsappNumber { get; set; }

    // Photos
    public string? StoreFrontPhotoUrl { get; set; }
    public List<string> KitchenPhotos { get; set; } = [];

    // Onboarding
    public VendorOnboardingStatus OnboardingStatus { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}

public class UpsertVendorProfileDto
{
    // Owner identity
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerCnic { get; set; }
    public string? OwnerPhone { get; set; }
    public string? OwnerEmail { get; set; }
    public string? CnicFrontDocUrl { get; set; }
    public string? CnicBackDocUrl { get; set; }

    // Business
    public string? BusinessName { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? FoodLicenseNumber { get; set; }
    public DateOnly? FoodLicenseExpiry { get; set; }
    public string? FoodLicenseDocUrl { get; set; }
    public string? BusinessDescription { get; set; }

    // Banking
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? AccountTitle { get; set; }
    public string? AccountNumber { get; set; }
    public string? IbanNumber { get; set; }

    // Social media
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TiktokUrl { get; set; }
    public string? WhatsappNumber { get; set; }

    // Photos
    public string? StoreFrontPhotoUrl { get; set; }
    public List<string> KitchenPhotos { get; set; } = [];
}

public class ReviewVendorApplicationDto
{
    public VendorOnboardingStatus Decision { get; set; }
    /// <summary>Required when Decision = Rejected.</summary>
    public string? RejectionReason { get; set; }
    /// <summary>Internal reviewer notes (not shown to seller).</summary>
    public string? ReviewNotes { get; set; }
}

// Store Offer

public class StoreOfferDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public StoreOfferType OfferType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
    public string? CallToAction { get; set; }
    public string? DeepLinkUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public Guid? PromotionId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? ItemCategoryId { get; set; }
    /// <summary>Promotion summary included when the offer links to one.</summary>
    public LinkedPromotionDto? Promotion { get; set; }
}

/// <summary>Lightweight promotion summary embedded inside a StoreOfferDto.</summary>
public class LinkedPromotionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PromotionCode { get; set; }
    public PromotionDiscountType? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public DateOnly EndDate { get; set; }
}

public class CreateStoreOfferDto
{
    public Guid StoreId { get; set; }
    public StoreOfferType OfferType { get; set; } = StoreOfferType.Featured;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
    public string? CallToAction { get; set; }
    public string? DeepLinkUrl { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public Guid? PromotionId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? ItemCategoryId { get; set; }
}

public class UpdateStoreOfferDto
{
    public StoreOfferType? OfferType { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
    public string? CallToAction { get; set; }
    public string? DeepLinkUrl { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsActive { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public Guid? PromotionId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? ItemCategoryId { get; set; }
}

// Store Menu

public class StoreMenuDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public TimeOnly? AvailableFrom { get; set; }
    public TimeOnly? AvailableTo { get; set; }
    /// <summary>
    /// Sections of this menu - each section references an Inventory ItemCategory by ID.
    /// The app resolves category name/image and items from Inventory using ItemCategoryId.
    /// </summary>
    public List<StoreMenuSectionDto> Sections { get; set; } = [];
}

/// <summary>
/// Represents one category section within a StoreMenu.
/// ItemCategoryId is a cross-module reference to Inventory.ItemCategory.
/// The app queries Inventory to get the category name, image, and its active items
/// (filtered by ItemChannelListing where Channel = "MOBILE_APP" and ListingStatus = "Active").
/// </summary>
public class StoreMenuSectionDto
{
    public Guid Id { get; set; }
    public Guid ItemCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateStoreMenuDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public TimeOnly? AvailableFrom { get; set; }
    public TimeOnly? AvailableTo { get; set; }
    public List<CreateStoreMenuSectionDto> Sections { get; set; } = [];
}

public class CreateStoreMenuSectionDto
{
    /// <summary>FK to Inventory.ItemCategory.</summary>
    public Guid ItemCategoryId { get; set; }
    public int DisplayOrder { get; set; }
}

// Delivery

public class DeliveryDto
{
    public Guid Id { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string? SalesOrderNumber { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public DeliveryStatus Status { get; set; }
    public DateTime PlannedDeliveryDate { get; set; }
    public DateTime? ActualShipDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string? Carrier { get; set; }
    public string? ShippingMethod { get; set; }
    public string? TrackingNumber { get; set; }
    public Incoterm? Incoterm { get; set; }
    public string? IncotermLocation { get; set; }
    public Guid? WarehouseId { get; set; }
    // Delivery Address
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientWhatsApp { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public string? DeliveryNotes { get; set; }
    // Assigned Rider
    public Guid? AssignedRiderId { get; set; }
    public string? RiderName { get; set; }
    // Packaging
    public int? NumberOfPackages { get; set; }
    public decimal? TotalWeight { get; set; }
    public string? WeightUnit { get; set; }
    public decimal? TotalVolume { get; set; }
    public string? VolumeUnit { get; set; }
    public string? Notes { get; set; }
    public List<DeliveryLineDto> Lines { get; set; } = [];
}

public class DeliveryLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public string? BinLocation { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class CreateDeliveryDto
{
    public Guid SalesOrderId { get; set; }
    public Guid? ContactId { get; set; }
    public DateTime PlannedDeliveryDate { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Carrier { get; set; }
    public string? ShippingMethod { get; set; }
    public Incoterm? Incoterm { get; set; }
    public string? IncotermLocation { get; set; }
    // Delivery Address
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientWhatsApp { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public string? DeliveryNotes { get; set; }
    // Assigned Rider
    public Guid? AssignedRiderId { get; set; }
    // Packaging
    public int? NumberOfPackages { get; set; }
    public decimal? TotalWeight { get; set; }
    public string? WeightUnit { get; set; }
    public string? Notes { get; set; }
    public List<CreateDeliveryLineDto> Lines { get; set; } = [];
}

public class CreateDeliveryLineDto
{
    public Guid SalesOrderLineId { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public string? BinLocation { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
}

public class ShipDeliveryDto
{
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
}

// Sales Invoice

public class SalesInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public Guid? DeliveryId { get; set; }
    public InvoiceStatus Status { get; set; }
    /// <summary>Payment state — drives the "IN PAYMENT" ribbon and payment status column.</summary>
    public InvoicePaymentStatus PaymentStatus { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public PaymentTerms PaymentTerms { get; set; }
    // Financials
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    // Billing Address
    public string? BillToName { get; set; }
    public string? BillToStreet { get; set; }
    public string? BillToCity { get; set; }
    public string? BillToState { get; set; }
    public string? BillToPostalCode { get; set; }
    public string? BillToCountry { get; set; }
    public string? CustomerReference { get; set; }
    // Other Info — Invoice section
    public Guid? SalesRepId { get; set; }
    public string? SalesRepName { get; set; }
    /// <summary>Payment communication printed on the invoice. Auto-set to InvoiceNumber on confirm.</summary>
    public string? PaymentReference { get; set; }
    public string? RecipientBankAccount { get; set; }
    public DateTime? DeliveryDate { get; set; }
    // Other Info — Accounting section
    public Guid? FiscalPositionId { get; set; }
    public string? PaymentMethod { get; set; }
    public InvoiceAutoPost AutoPost { get; set; }
    // Reminders
    public DateTime? LastReminderDate { get; set; }
    public int ReminderCount { get; set; }
    // Accounting
    public Guid? AccountingJournalEntryId { get; set; }
    public string? Notes { get; set; }
    public List<SalesInvoiceLineDto> Lines { get; set; } = [];
}

public class SalesInvoiceLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineAmount { get; set; }
    public TaxCategory TaxCategory { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    /// <summary>True for down-payment lines — amount is positive (deposit) or negative (deduction).</summary>
    public bool IsDownPayment { get; set; }
}

public class CreateSalesInvoiceDto
{
    public Guid SalesOrderId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? DeliveryId { get; set; }
    public DateTime DueDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    // Billing Address
    public string? BillToName { get; set; }
    public string? BillToStreet { get; set; }
    public string? BillToCity { get; set; }
    public string? BillToState { get; set; }
    public string? BillToPostalCode { get; set; }
    public string? BillToCountry { get; set; }
    public string? CustomerReference { get; set; }
    // Other Info
    public Guid? SalesRepId { get; set; }
    public string? SalesRepName { get; set; }
    public string? RecipientBankAccount { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public Guid? FiscalPositionId { get; set; }
    public string? PaymentMethod { get; set; }
    public InvoiceAutoPost AutoPost { get; set; } = InvoiceAutoPost.No;
    public string? Notes { get; set; }
    public List<CreateSalesInvoiceLineDto> Lines { get; set; } = [];
}

public class CreateSalesInvoiceLineDto
{
    public Guid SalesOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public TaxCategory TaxCategory { get; set; } = TaxCategory.Standard;
    public decimal TaxRate { get; set; }
}

public class CancelInvoiceDto
{
    public string? Reason { get; set; }
}

// Sales Payment

public class SalesPaymentDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid? PosTransactionId { get; set; }
    public Guid? ContactId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? BankName { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? GatewayResponse { get; set; }
    public bool IsRefunded { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTime? RefundedAt { get; set; }
    public Guid? AccountingReceiptId { get; set; }
    public string? Notes { get; set; }
    /// <summary>Invoices this payment is applied to.</summary>
    public List<PaymentAllocationDto> Allocations { get; set; } = [];
}

public class PaymentAllocationDto
{
    public Guid Id { get; set; }
    public Guid SalesInvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateTime AllocatedAt { get; set; }
}

public class CreateSalesPaymentDto
{
    public Guid SalesOrderId { get; set; }
    public Guid? ContactId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? BankName { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? Notes { get; set; }
    /// <summary>
    /// Optional: specify which invoices to allocate to and how much.
    /// If empty, the system auto-allocates FIFO across unpaid invoices.
    /// For POS walk-in sales with no invoices, leave this empty.
    /// </summary>
    public List<CreatePaymentAllocationDto> Allocations { get; set; } = [];
}

public class CreatePaymentAllocationDto
{
    public Guid SalesInvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
}

// Quotation

public class QuotationDto
{
    public Guid Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public string? QuotationName { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public QuotationStatus Status { get; set; }
    public DateTime QuotationDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? AcceptedDate { get; set; }
    public Guid? PriceListId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public PaymentTerms PaymentTerms { get; set; }
    public Incoterm? Incoterm { get; set; }
    public string? IncotermLocation { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public Guid? ShipToAddressId { get; set; }
    public string? ShippingMethod { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid? SalesRepId { get; set; }
    public Guid? SalesTerritoryId { get; set; }
    public Guid? CrmDealId { get; set; }
    public Guid? ConvertedToSalesOrderId { get; set; }
    public string? CustomerPONumber { get; set; }
    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }
    public List<QuotationLineDto> Lines { get; set; } = [];
}

public class QuotationLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetUnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public TaxCategory TaxCategory { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public DateTime? ConfirmedDeliveryDate { get; set; }
    public string? Notes { get; set; }
}

public class CreateQuotationDto
{
    public string? QuotationName { get; set; }

    /// <summary>Cross-module Crm.Contact reference. Mandatory — every quotation must have a customer.</summary>
    [System.ComponentModel.DataAnnotations.Required]
    public Guid? ContactId { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(1)]
    public string ContactName { get; set; } = string.Empty;
    public DateTime? ValidUntil { get; set; }
    public Guid? PriceListId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public Incoterm? Incoterm { get; set; }
    public string? IncotermLocation { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public Guid? ShipToAddressId { get; set; }
    public string? ShippingMethod { get; set; }
    public Guid? SalesRepId { get; set; }
    public Guid? SalesTerritoryId { get; set; }
    public Guid? CrmDealId { get; set; }
    public string? CustomerPONumber { get; set; }
    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }
    public List<CreateQuotationLineDto> Lines { get; set; } = [];
}

public class CreateQuotationLineDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public TaxCategory TaxCategory { get; set; } = TaxCategory.Standard;
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }
}

// Sales Return (RMA)

public class SalesReturnDto
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public ReturnStatus Status { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? CreditIssuedDate { get; set; }
    public string? RmaNumber { get; set; }
    public string? ReturnReason { get; set; }
    public string? InspectionNotes { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public Guid? CreditNoteId { get; set; }
    public List<SalesReturnLineDto> Lines { get; set; } = [];
}

public class SalesReturnLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal ReturnedQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }
    public string? ConditionOnReturn { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public class CreateSalesReturnDto
{
    public Guid SalesOrderId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public string? ReturnReason { get; set; }
    public List<CreateSalesReturnLineDto> Lines { get; set; } = [];
}

public class CreateSalesReturnLineDto
{
    public Guid SalesOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal ReturnedQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? ConditionOnReturn { get; set; }
    public string? Reason { get; set; }
}

// Credit Note

public class CreditNoteDto
{
    public Guid Id { get; set; }
    public string CreditNoteNumber { get; set; } = string.Empty;
    public Guid SalesInvoiceId { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public DateTime CreditNoteDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }
    public Guid? AccountingJournalEntryId { get; set; }
    public string? Notes { get; set; }
    public List<CreditNoteLineDto> Lines { get; set; } = [];
}

public class CreditNoteLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public TaxCategory TaxCategory { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }
}

// Sales Agreement

public class SalesAgreementDto
{
    public Guid Id { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;
    public string AgreementName { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public SalesAgreementStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? PriceListId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public PaymentTerms PaymentTerms { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal ReleasedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }
    public List<SalesAgreementLineDto> Lines { get; set; } = [];
}

public class SalesAgreementLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal CommittedQuantity { get; set; }
    public decimal ReleasedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal AgreedUnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string? Notes { get; set; }
}

public class CreateSalesAgreementDto
{
    public string AgreementName { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? PriceListId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public decimal CommittedAmount { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }
    public List<CreateSalesAgreementLineDto> Lines { get; set; } = [];
}

public class CreateSalesAgreementLineDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal CommittedQuantity { get; set; }
    public decimal AgreedUnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string? Notes { get; set; }
}

// Rider

public class RiderDto
{
    public Guid Id { get; set; }
    public string RiderCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ProfileImageUrl { get; set; }
    public VehicleType VehicleType { get; set; }
    public string? VehiclePlateNumber { get; set; }
    public RiderStatus Status { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public int TotalDeliveries { get; set; }
    public decimal AverageRating { get; set; }
    public bool IsActive { get; set; }
    public Guid? HomeBranchId { get; set; }
}

public class CreateRiderDto
{
    public string RiderCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public VehicleType VehicleType { get; set; } = VehicleType.Motorcycle;
    public string? VehiclePlateNumber { get; set; }
    public string? VehicleModel { get; set; }
    public string ContractType { get; set; } = "Employee";
    public Guid? HrEmployeeId { get; set; }
    public Guid? HomeBranchId { get; set; }
    public Guid? ZoneId { get; set; }
}

public class UpdateRiderDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public VehicleType? VehicleType { get; set; }
    public string? VehiclePlateNumber { get; set; }
    public Guid? HomeBranchId { get; set; }
    public bool? IsActive { get; set; }
}

// Rider Assignment

public class RiderAssignmentDto
{
    public Guid Id { get; set; }
    public string AssignmentNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid RiderId { get; set; }
    public string? RiderName { get; set; }
    public RiderAssignmentStatus Status { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public string? ProofImageUrl { get; set; }
    public string? FailureReason { get; set; }
}

public class CreateRiderAssignmentDto
{
    public Guid SalesOrderId { get; set; }
    public Guid RiderId { get; set; }
    public Guid? PickupBranchId { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public double? EstimatedDistanceKm { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
}

// Loyalty

public class LoyaltyAccountDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public LoyaltyTier Tier { get; set; }
    public decimal PointsBalance { get; set; }
    public decimal LifetimePointsEarned { get; set; }
    public decimal LifetimePointsRedeemed { get; set; }
    public DateTime? TierExpiryDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
}

// Price List

public class PriceListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PriceListType ListType { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsActive { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int ItemCount { get; set; }
}

public class CreatePriceListDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PriceListType ListType { get; set; } = PriceListType.Standard;
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePriceListDto
{
    public string? Name { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? ExchangeRate { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

// Promotion

public class PromotionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PromotionCode { get; set; }
    public bool IsAutoApplied { get; set; }
    public PromotionStatus Status { get; set; }
    public int Priority { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public ScheduledDays ScheduledDays { get; set; }
    public int? MaxUsageCount { get; set; }
    public int? MaxUsagePerCustomer { get; set; }
    public int CurrentUsageCount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public PromotionTargetType TargetType { get; set; }
    public LoyaltyTier? RequiredLoyaltyTier { get; set; }
    public Guid? RequiredPriceListId { get; set; }
    public Guid? TargetContactId { get; set; }
    public bool IsStackable { get; set; }
    public string? Notes { get; set; }
    public List<PromotionItemDto> Items { get; set; } = [];
}

public class PromotionItemDto
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? ItemCategoryId { get; set; }
    public PromotionDiscountType DiscountType { get; set; }
    public PromotionPriceTarget PriceTarget { get; set; }
    public decimal Value { get; set; }
    public bool IsConditional { get; set; }
    public PromotionConditionType ConditionType { get; set; }
    public decimal? ConditionQuantity { get; set; }
    public decimal? ConditionAmount { get; set; }
    public decimal? MaxDiscountedQuantity { get; set; }
    public decimal? BuyQuantity { get; set; }
    public decimal? GetQuantity { get; set; }
    public Guid? FreeItemId { get; set; }
}

public class CreatePromotionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PromotionCode { get; set; }
    public bool IsAutoApplied { get; set; } = true;
    public int Priority { get; set; } = 0;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public ScheduledDays ScheduledDays { get; set; } = ScheduledDays.EveryDay;
    public int? MaxUsageCount { get; set; }
    public int? MaxUsagePerCustomer { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public PromotionTargetType TargetType { get; set; } = PromotionTargetType.AllCustomers;
    public LoyaltyTier? RequiredLoyaltyTier { get; set; }
    public Guid? RequiredPriceListId { get; set; }
    public Guid? TargetContactId { get; set; }
    public bool IsStackable { get; set; } = false;
    public string? Notes { get; set; }
    public List<CreatePromotionItemDto> Items { get; set; } = [];
}

public class UpdatePromotionDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? PromotionCode { get; set; }
    public bool? IsAutoApplied { get; set; }
    public int? Priority { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public ScheduledDays? ScheduledDays { get; set; }
    public int? MaxUsageCount { get; set; }
    public int? MaxUsagePerCustomer { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public PromotionTargetType? TargetType { get; set; }
    public LoyaltyTier? RequiredLoyaltyTier { get; set; }
    public Guid? RequiredPriceListId { get; set; }
    public Guid? TargetContactId { get; set; }
    public bool? IsStackable { get; set; }
    public string? Notes { get; set; }
}

public class CreatePromotionItemDto
{
    public Guid? ItemId { get; set; }
    public Guid? ItemCategoryId { get; set; }
    public PromotionDiscountType DiscountType { get; set; }
    public PromotionPriceTarget PriceTarget { get; set; } = PromotionPriceTarget.AnyPrice;
    public decimal Value { get; set; }
    public bool IsConditional { get; set; } = false;
    public PromotionConditionType ConditionType { get; set; } = PromotionConditionType.None;
    public decimal? ConditionQuantity { get; set; }
    public decimal? ConditionAmount { get; set; }
    public decimal? MaxDiscountedQuantity { get; set; }
    public decimal? BuyQuantity { get; set; }
    public decimal? GetQuantity { get; set; }
    public Guid? FreeItemId { get; set; }
}

// Coupon

public class CouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CouponStatus Status { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCouponDto
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int? PerCustomerLimit { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

// Cashier Session DTOs

public class PosCashierDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? BadgeNumber { get; set; }
    public Guid BranchId { get; set; }
    public bool CanApplyManualDiscount { get; set; }
    public decimal MaxManualDiscountPercentage { get; set; }
    public bool CanVoidTransaction { get; set; }
    public bool CanIssueRefund { get; set; }
    public bool CanOpenDrawer { get; set; }
    public bool CanOverridePrices { get; set; }
    public bool CanApplyCoupons { get; set; }
    public bool CanAccessReports { get; set; }
    public bool IsActive { get; set; }
}

public class CreatePosCashierDto
{
    public Guid EmployeeId { get; set; }
    public Guid PosStoreId { get; set; }
    public string? DisplayName { get; set; }
    public string? BadgeNumber { get; set; }
    public bool? CanApplyManualDiscount { get; set; }
    public decimal? MaxManualDiscountPercentage { get; set; }
    public bool? CanVoidTransaction { get; set; }
    public bool? CanIssueRefund { get; set; }
    public bool? CanOpenDrawer { get; set; }
    public bool? CanOverridePrices { get; set; }
    public bool? CanApplyCoupons { get; set; }
    public bool? CanAccessReports { get; set; }
}

public class UpdatePosCashierDto
{
    public string? DisplayName { get; set; }
    public string? BadgeNumber { get; set; }
    public Guid? BranchId { get; set; }
    public bool? CanApplyManualDiscount { get; set; }
    public decimal? MaxManualDiscountPercentage { get; set; }
    public bool? CanVoidTransaction { get; set; }
    public bool? CanIssueRefund { get; set; }
    public bool? CanOpenDrawer { get; set; }
    public bool? CanOverridePrices { get; set; }
    public bool? CanApplyCoupons { get; set; }
    public bool? CanAccessReports { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>Initial PIN set by a manager when creating or resetting a cashier PIN.</summary>
public class SetCashierPinDto
{
    /// <summary>4-6 digit numeric PIN.</summary>
    public string Pin { get; set; } = string.Empty;
}

/// <summary>Cashier self-service PIN change - requires current PIN for verification.</summary>
public class ChangeCashierPinDto
{
    public string CurrentPin { get; set; } = string.Empty;
    public string NewPin { get; set; } = string.Empty;
}

public class CashierPinLoginDto
{
    public string Pin { get; set; } = string.Empty;
    public Guid StoreId { get; set; }
}

/// <summary>
/// A single denomination line in a cash count.
/// e.g. { Denomination: 1000, Count: 5, IsCoin: false } = PKR 5,000 in 1,000-note bills.
/// </summary>
public class DenominationCountDto
{
    /// <summary>Face value of the note or coin (e.g. 1000, 500, 10, 1).</summary>
    public decimal Denomination { get; set; }
    /// <summary>Number of pieces of this denomination.</summary>
    public int Count { get; set; }
    /// <summary>True = coin, False = banknote/bill.</summary>
    public bool IsCoin { get; set; }
    /// <summary>Computed total for this line (Denomination × Count).</summary>
    public decimal LineTotal => Denomination * Count;
}

public class CashierCheckInDto
{
    public Guid CashierId { get; set; }
    public Guid TerminalId { get; set; }
    /// <summary>
    /// Optional opening note visible in the session report (e.g. "Float received from manager").
    /// </summary>
    public string? OpeningNotes { get; set; }
    /// <summary>
    /// Denomination breakdown of the opening float.
    /// When provided the OpeningFloat total is calculated from these counts.
    /// </summary>
    public List<DenominationCountDto> Denominations { get; set; } = [];
    /// <summary>
    /// Override total — used when Denominations list is empty.
    /// Ignored when Denominations are supplied (total is derived from them).
    /// </summary>
    public decimal OpeningFloat { get; set; }
}

public class CashierCheckOutDto
{
    /// <summary>Optional notes for the end-of-shift report.</summary>
    public string? Notes { get; set; }
    /// <summary>
    /// Denomination breakdown of the closing cash count.
    /// When provided the ClosingFloat total is calculated from these counts.
    /// </summary>
    public List<DenominationCountDto> Denominations { get; set; } = [];
    /// <summary>
    /// Override total — used when Denominations list is empty.
    /// Ignored when Denominations are supplied (total is derived from them).
    /// </summary>
    public decimal ClosingFloat { get; set; }
}

public class PosSessionDto
{
    public Guid Id { get; set; }
    public string SessionNumber { get; set; } = string.Empty;
    public Guid PosTerminalId { get; set; }
    public Guid PosCashierId { get; set; }
    public Sales.Domain.Enums.PosSessionStatus Status { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningFloat { get; set; }
    public string? OpeningNotes { get; set; }
    public List<DenominationCountDto> OpeningDenominations { get; set; } = [];
    public decimal ClosingFloat { get; set; }
    public decimal ExpectedClosingFloat { get; set; }
    public decimal FloatVariance { get; set; }
    public List<DenominationCountDto> ClosingDenominations { get; set; } = [];
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalRefundsAmount { get; set; }
    public decimal TotalDiscountsAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal NetSalesAmount { get; set; }
    public decimal CashCollected { get; set; }
    public decimal CardCollected { get; set; }
    public decimal WalletCollected { get; set; }
    public decimal OtherCollected { get; set; }
    public int TransactionCount { get; set; }
    public string? ClosingNotes { get; set; }
}

public class CashMovementDto
{
    public Guid CashierId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public class PosCashMovementDto
{
    public Guid Id { get; set; }
    public Guid PosSessionId { get; set; }
    public Guid PosCashierId { get; set; }
    public Sales.Domain.Enums.PosCashMovementType MovementType { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public DateTime MovementDate { get; set; }
}

// POS Terminal DTOs

public class PosTerminalDto
{
    public Guid Id { get; set; }
    public string TerminalCode { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public Guid PosStoreId { get; set; }
    public string? DeviceIdentifier { get; set; }
    public string? IpAddress { get; set; }
    public Guid? CashDrawerId { get; set; }
    public Guid? ReceiptTemplateId { get; set; }
    public bool IsActive { get; set; }
    public bool IsOnline { get; set; }
    public Guid? CurrentSessionId { get; set; }
}

public class CreatePosTerminalDto
{
    public string TerminalCode { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public Guid PosStoreId { get; set; }
    public string? DeviceIdentifier { get; set; }
    public string? IpAddress { get; set; }
    public Guid? CashDrawerId { get; set; }
    public Guid? ReceiptTemplateId { get; set; }
}

public class UpdatePosTerminalDto
{
    public string? TerminalName { get; set; }
    public string? DeviceIdentifier { get; set; }
    public string? IpAddress { get; set; }
    public Guid? CashDrawerId { get; set; }
    public Guid? ReceiptTemplateId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsOnline { get; set; }
}

public class TerminalHeartbeatDto
{
    public string? IpAddress { get; set; }
    public string? DeviceIdentifier { get; set; }
}

// POS Receipt Template DTOs

// Templates are branch-scoped (one branch can have several — e.g. a Thermal80mm, a Thermal58mm
// and an A4). A store/terminal points to one via ReceiptTemplateId; one template per branch is the
// IsDefault used when nothing more specific is configured. PaperSize ("Thermal58mm"/"Thermal80mm"/"A4")
// distinguishes a thermal receipt template from a full-page receipt/invoice template.

public class PosReceiptTemplateDto
{
    public Guid Id { get; set; }
    public string TemplateName { get; set; } = string.Empty;

    // Header
    public string? HeaderBusinessName { get; set; }
    public string? HeaderAddressLine1 { get; set; }
    public string? HeaderAddressLine2 { get; set; }
    public string? HeaderPhone { get; set; }
    public string? HeaderEmail { get; set; }
    public string? HeaderWebsite { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public string? LogoUrl { get; set; }
    public string? HeaderMessage { get; set; }

    // Footer
    public string? FooterMessage { get; set; }
    public string? ReturnPolicy { get; set; }

    // Display options
    public bool ShowBarcode { get; set; }
    public bool ShowQrCode { get; set; }
    public bool ShowCashierName { get; set; }
    public bool ShowCustomerName { get; set; }
    public bool ShowDiscountLine { get; set; }
    public bool ShowTaxBreakdown { get; set; }
    public bool ShowLoyaltyPoints { get; set; }
    public bool ShowSavingsAmount { get; set; }

    public string PaperSize { get; set; } = "Thermal80mm";
    public bool IsDefault { get; set; }

    // Print behaviour
    public int PrintCopies { get; set; }
    public bool AutoCutPaper { get; set; }
    public bool OpenCashDrawer { get; set; }
    public string BarcodeSymbology { get; set; } = "Code128";
}

public class CreatePosReceiptTemplateDto
{
    public string TemplateName { get; set; } = string.Empty;

    public string? HeaderBusinessName { get; set; }
    public string? HeaderAddressLine1 { get; set; }
    public string? HeaderAddressLine2 { get; set; }
    public string? HeaderPhone { get; set; }
    public string? HeaderEmail { get; set; }
    public string? HeaderWebsite { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public string? LogoUrl { get; set; }
    public string? HeaderMessage { get; set; }

    public string? FooterMessage { get; set; }
    public string? ReturnPolicy { get; set; }

    public bool ShowBarcode { get; set; } = true;
    public bool ShowQrCode { get; set; }
    public bool ShowCashierName { get; set; } = true;
    public bool ShowCustomerName { get; set; } = true;
    public bool ShowDiscountLine { get; set; } = true;
    public bool ShowTaxBreakdown { get; set; } = true;
    public bool ShowLoyaltyPoints { get; set; } = true;
    public bool ShowSavingsAmount { get; set; } = true;

    /// <summary>"Thermal58mm", "Thermal80mm", or "A4".</summary>
    public string PaperSize { get; set; } = "Thermal80mm";
    public bool IsDefault { get; set; }

    // Print behaviour
    public int PrintCopies { get; set; } = 1;
    public bool AutoCutPaper { get; set; } = true;
    public bool OpenCashDrawer { get; set; }
    public string BarcodeSymbology { get; set; } = "Code128";
}

public class UpdatePosReceiptTemplateDto
{
    public string? TemplateName { get; set; }

    public string? HeaderBusinessName { get; set; }
    public string? HeaderAddressLine1 { get; set; }
    public string? HeaderAddressLine2 { get; set; }
    public string? HeaderPhone { get; set; }
    public string? HeaderEmail { get; set; }
    public string? HeaderWebsite { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public string? LogoUrl { get; set; }
    public string? HeaderMessage { get; set; }

    public string? FooterMessage { get; set; }
    public string? ReturnPolicy { get; set; }

    public bool? ShowBarcode { get; set; }
    public bool? ShowQrCode { get; set; }
    public bool? ShowCashierName { get; set; }
    public bool? ShowCustomerName { get; set; }
    public bool? ShowDiscountLine { get; set; }
    public bool? ShowTaxBreakdown { get; set; }
    public bool? ShowLoyaltyPoints { get; set; }
    public bool? ShowSavingsAmount { get; set; }

    public string? PaperSize { get; set; }
    public bool? IsDefault { get; set; }

    // Print behaviour
    public int? PrintCopies { get; set; }
    public bool? AutoCutPaper { get; set; }
    public bool? OpenCashDrawer { get; set; }
    public string? BarcodeSymbology { get; set; }
}

// ── POS Barcode Label Template DTOs ──────────────────────────────────────────

public class PosBarcodeLabelTemplateDto
{
    public Guid Id { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public double LabelWidthMm { get; set; }
    public double LabelHeightMm { get; set; }
    public string BarcodeSymbology { get; set; } = "Code128";
    public string? HeaderText { get; set; }
    public bool ShowProductName { get; set; }
    public bool ShowPrice { get; set; }
    public bool ShowSku { get; set; }
    public bool ShowBarcodeValue { get; set; }
    public string? CurrencySymbol { get; set; }
    public double ProductNameFontPt { get; set; }
    public double PriceFontPt { get; set; }
    public double BarcodeHeightPt { get; set; }
    public bool ShowBorders { get; set; }
    public bool RollPaper { get; set; }
    public bool IsDefault { get; set; }
}

public class CreatePosBarcodeLabelTemplateDto
{
    public string TemplateName { get; set; } = string.Empty;
    public double LabelWidthMm { get; set; } = 50;
    public double LabelHeightMm { get; set; } = 30;
    /// <summary>Code128, Code39, EAN-13, EAN-8, UPC-A, QR.</summary>
    public string BarcodeSymbology { get; set; } = "Code128";
    public string? HeaderText { get; set; }
    public bool ShowProductName { get; set; } = true;
    public bool ShowPrice { get; set; } = true;
    public bool ShowSku { get; set; } = true;
    public bool ShowBarcodeValue { get; set; } = true;
    public string? CurrencySymbol { get; set; }
    public double ProductNameFontPt { get; set; } = 8;
    public double PriceFontPt { get; set; } = 11;
    public double BarcodeHeightPt { get; set; } = 12;
    public bool ShowBorders { get; set; } = true;
    public bool RollPaper { get; set; }
    public bool IsDefault { get; set; }
}

public class UpdatePosBarcodeLabelTemplateDto
{
    public string? TemplateName { get; set; }
    public double? LabelWidthMm { get; set; }
    public double? LabelHeightMm { get; set; }
    public string? BarcodeSymbology { get; set; }
    public string? HeaderText { get; set; }
    public bool? ShowProductName { get; set; }
    public bool? ShowPrice { get; set; }
    public bool? ShowSku { get; set; }
    public bool? ShowBarcodeValue { get; set; }
    public string? CurrencySymbol { get; set; }
    public double? ProductNameFontPt { get; set; }
    public double? PriceFontPt { get; set; }
    public double? BarcodeHeightPt { get; set; }
    public bool? ShowBorders { get; set; }
    public bool? RollPaper { get; set; }
    public bool? IsDefault { get; set; }
}

/// <summary>Request to render one or more product barcode labels to a PDF.</summary>
public class RenderBarcodeLabelDto
{
    /// <summary>Label template to use. Null = the branch's default label template (or built-in defaults).</summary>
    public Guid? TemplateId { get; set; }

    /// <summary>The value encoded in the barcode (product barcode / SKU).</summary>
    public string BarcodeValue { get; set; } = string.Empty;

    public string? ProductName { get; set; }
    public string? Sku { get; set; }
    public decimal? Price { get; set; }

    /// <summary>How many copies of the label to place in the PDF (one per page).</summary>
    public int Copies { get; set; } = 1;

    /// <summary>Override the symbology for this render (else the template's).</summary>
    public string? Symbology { get; set; }
}

/// <summary>One product line in a batch label print.</summary>
public class BarcodeLabelItemDto
{
    public string BarcodeValue { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? Sku { get; set; }
    public decimal? Price { get; set; }
    /// <summary>Number of label copies to print for this product.</summary>
    public int Copies { get; set; } = 1;
}

/// <summary>Render labels for several products in one PDF (price-tag designer "Print" / "Generate Preview").</summary>
public class RenderBarcodeLabelBatchDto
{
    /// <summary>Label template to use. Null = the branch's default (or built-in defaults).</summary>
    public Guid? TemplateId { get; set; }
    /// <summary>Override the symbology for all labels (else the template's).</summary>
    public string? Symbology { get; set; }
    public List<BarcodeLabelItemDto> Items { get; set; } = [];
}

/// <summary>A label paper / stock preset for the price-tag designer's "Paper type" dropdown.</summary>
public class LabelPaperPresetDto
{
    public string Name { get; set; } = string.Empty;
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    /// <summary>True for continuous roll stock (height grows with content; no fixed page height).</summary>
    public bool RollPaper { get; set; }
}

/// <summary>Min/max/default/step for a numeric designer control (slider) — keyed by Field.</summary>
public class LabelFieldRangeDto
{
    /// <summary>The setting this range applies to, e.g. "ProductNameFontPt", "PriceFontPt", "BarcodeHeightPt", "Copies".</summary>
    public string Field { get; set; } = string.Empty;
    public double Min { get; set; }
    public double Max { get; set; }
    public double Default { get; set; }
    public double Step { get; set; } = 1;
}

/// <summary>All dropdown option sets the barcode-label / price-tag designer needs in one call.</summary>
public class LabelDesignerOptionsDto
{
    /// <summary>Supported barcode symbologies (value = code to send back, label = display name).</summary>
    public List<LookupItemDto> BarcodeSymbologies { get; set; } = [];

    /// <summary>Paper / label stock presets (name + dimensions).</summary>
    public List<LabelPaperPresetDto> PaperPresets { get; set; } = [];

    /// <summary>Slider min/max/default/step for the numeric designer controls (no hardcoded UI limits).</summary>
    public List<LabelFieldRangeDto> SliderRanges { get; set; } = [];
}

// POS Cash Drawer DTOs

public class PosCashDrawerDto
{
    public Guid Id { get; set; }
    public string DrawerCode { get; set; } = string.Empty;
    public string? DrawerLabel { get; set; }
    public Guid PosStoreId { get; set; }
    public bool IsActive { get; set; }
}

public class CreatePosCashDrawerDto
{
    public string DrawerCode { get; set; } = string.Empty;
    public string? DrawerLabel { get; set; }
    public Guid PosStoreId { get; set; }
}

public class UpdatePosCashDrawerDto
{
    public string? DrawerLabel { get; set; }
    public bool? IsActive { get; set; }
}

// Phone Order (Cashier-Created Sales Order Form)

/// <summary>
/// DTO used when a cashier creates a sales order from a phone/walk-in call.
/// Streamlined vs CreateSalesOrderDto - pre-sets channel to PhoneOrder/PosWalkIn
/// and requires POS context (store + cashier + session).
/// </summary>
public class CreatePhoneOrderDto
{
    // Who is ordering
    /// <summary>
    /// Cross-module Crm.Contact ID. Null for anonymous callers.
    /// Cashier can search by phone or name and select from CRM, or leave null.
    /// </summary>
    public Guid? ContactId { get; set; }

    /// <summary>Name spoken by caller - used as snapshot when ContactId is null.</summary>
    public string? ContactName { get; set; }

    /// <summary>Caller's phone number - for callback and delivery confirmation.</summary>
    public string? ContactPhone { get; set; }

    // POS Context (required)
    public Guid PosStoreId { get; set; }
    public Guid PosCashierId { get; set; }
    public Guid? PosTerminalId { get; set; }
    public Guid? PosSessionId { get; set; }

    // Fulfillment
    /// <summary>Defaults to PhoneOrder channel. Cashier can switch to PosWalkIn if customer walks in later.</summary>
    public SalesChannel SalesChannel { get; set; } = SalesChannel.PhoneOrder;
    public FulfillmentType FulfillmentType { get; set; } = FulfillmentType.Delivery;

    // Delivery Address (passed to the Delivery record created at dispatch)
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientWhatsApp { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public string? DeliveryNotes { get; set; }

    // Pricing
    public Guid? PriceListId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? CouponCode { get; set; }

    // Notes
    public string? Notes { get; set; }

    // Lines (required - at least one)
    public List<CreatePhoneOrderLineDto> Lines { get; set; } = [];
}

public class CreatePhoneOrderLineDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<CreateSalesOrderLineAddonDto> Addons { get; set; } = [];
}

// POS Dashboard

public enum PosBranchSessionStatus { NoSession, Open, ToClose }

/// <summary>
/// Current POS session state for one branch — returned by the dashboard endpoint.
/// Branch name/logo comes from Core; Sales only supplies the session state and today's stats.
/// </summary>
public class PosBranchStatusDto
{
    public Guid BranchId { get; set; }
    public PosBranchSessionStatus Status { get; set; }
    public int OpenSessionCount { get; set; }
    public Guid? ActiveSessionId { get; set; }
    public string? ActiveSessionNumber { get; set; }
    public Guid? ActiveCashierId { get; set; }
    public string? ActiveCashierName { get; set; }
    public DateTime? SessionOpenedAt { get; set; }
    public decimal TodayTotalSales { get; set; }
    public int TodayTransactionCount { get; set; }
    public decimal TodayCashCollected { get; set; }
    public decimal TodayCardCollected { get; set; }
    public decimal TodayWalletCollected { get; set; }
    public List<PosDailySalesDto> RecentDailySales { get; set; } = [];
}

/// <summary>One day's aggregated sales — used for the branch dashboard mini chart.</summary>
public class PosDailySalesDto
{
    public DateOnly Date { get; set; }
    public decimal TotalSales { get; set; }
    public int TransactionCount { get; set; }
}

/// <summary>Request body for the bulk dashboard endpoint.</summary>
public class PosDashboardBulkRequestDto
{
    public List<Guid> BranchIds { get; set; } = [];
}

/// <summary>Per-terminal POS status + today's sales — returned by the terminals dashboard endpoint.</summary>
public class PosTerminalStatusDto
{
    public Guid   TerminalId          { get; set; }
    public string TerminalName        { get; set; } = string.Empty;
    public string TerminalCode        { get; set; } = string.Empty;
    public Guid   StoreId             { get; set; }
    public string StoreName           { get; set; } = string.Empty;
    public bool   IsActive            { get; set; }
    // Session state
    public PosBranchSessionStatus Status       { get; set; }
    public Guid?   ActiveSessionId             { get; set; }
    public string? ActiveSessionNumber         { get; set; }
    public string? ActiveCashierName           { get; set; }
    public DateTime? SessionOpenedAt           { get; set; }
    public decimal OpeningFloat                { get; set; }
    // Today's totals
    public decimal TodayTotalSales             { get; set; }
    public int     TodayTransactionCount       { get; set; }
    public decimal TodayCashCollected          { get; set; }
    public decimal TodayCardCollected          { get; set; }
    public decimal TodayWalletCollected        { get; set; }
}

// ── Register Payment from Invoice (Odoo "Pay" button) ────────────────────────

/// <summary>
/// Body for POST /api/sales/salesinvoice/{id}/register-payment.
/// Mirrors Odoo's "Pay" dialog fields.
/// </summary>
public class RegisterInvoicePaymentDto
{
    /// <summary>Cash, Card, BankTransfer, Cheque, Wallet, etc.</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Accounting journal / bank account label (e.g. "Bank", "Cash").</summary>
    public string? Journal { get; set; }

    /// <summary>Amount to pay. Defaults to full BalanceDue when null.</summary>
    public decimal? Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    /// <summary>Free-text memo (defaults to invoice number).</summary>
    public string? Memo { get; set; }

    public string? BankName { get; set; }
    public string? ReferenceNumber { get; set; }

    /// <summary>GL account number for cash receipts (from PosSettings). Null = use PostingProfile default.</summary>
    public string? CashGlAccountNumber { get; set; }
    /// <summary>GL account number for card/bank/mobile receipts (from PosSettings). Null = use PostingProfile default.</summary>
    public string? BankGlAccountNumber { get; set; }
}

// ── Send Invoice ──────────────────────────────────────────────────────────────

public class SendInvoiceDto
{
    public List<string> ToEmails { get; set; } = [];
    public string? Subject { get; set; }
    public string? Body { get; set; }
}

// ── Credit Note from Invoice ──────────────────────────────────────────────────

public class CreateCreditNoteFromInvoiceDto
{
    public string? Reason { get; set; }
    public DateTime? CreditNoteDate { get; set; }
    /// <summary>When true, creates a full reversal credit note for the entire invoice amount.</summary>
    public bool FullReversal { get; set; } = true;
    public string? Notes { get; set; }
}

// ── Sales Team ────────────────────────────────────────────────────────────────

public class SalesTeamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public Guid? TeamLeaderUserId { get; set; }
    public string? TeamLeaderName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<SalesTeamMemberDto> Members { get; set; } = [];
}

public class SalesTeamMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSalesTeamDto
{
    public string Name { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public Guid? TeamLeaderUserId { get; set; }
    public string? TeamLeaderName { get; set; }
    public string? Description { get; set; }
}

public class UpdateSalesTeamDto
{
    public string? Name { get; set; }
    public string? Alias { get; set; }
    public Guid? TeamLeaderUserId { get; set; }
    public string? TeamLeaderName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class AddSalesTeamMemberDto
{
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
}

// ── POS Settings DTOs ─────────────────────────────────────────────────────────

public class PosSettingsDto
{
    public Guid    Id                    { get; set; }

    // General
    public bool    RequireCashierPin     { get; set; }
    public int     AutoLockMinutes       { get; set; }
    public bool    AllowPriceOverride    { get; set; }
    public bool    AllowDiscount         { get; set; }
    public decimal MaxDiscountPercent    { get; set; }
    public bool    RequireCustomer       { get; set; }
    public Guid?   DefaultCustomerId     { get; set; }
    public bool    AllowNegativeStock    { get; set; }
    public bool    TaxInclusivePricing   { get; set; }
    public bool    AutoApplyTax          { get; set; }
    public string? DefaultCurrencyCode   { get; set; }
    public decimal RoundingValue         { get; set; }
    public string  RoundingMode          { get; set; } = "None";

    // Payment
    public bool    AcceptCash            { get; set; }
    public bool    AcceptCard            { get; set; }
    public bool    AcceptMobilePayment   { get; set; }
    public bool    AcceptCreditOnAccount { get; set; }
    public bool    AllowSplitPayment     { get; set; }
    public bool    AllowPartialPayment   { get; set; }
    public string? DefaultPaymentMethod  { get; set; }
    public bool    AutoOpenCashDrawer    { get; set; }
    public decimal? MinOrderAmount       { get; set; }
    public decimal? MaxOrderAmount       { get; set; }
    public string?  CashGlAccountNumber  { get; set; }
    public string?  BankGlAccountNumber  { get; set; }

    // Cart & Products
    public bool    ShowProductImages     { get; set; }
    public bool    ShowProductDescription{ get; set; }
    public bool    AllowItemNotes        { get; set; }
    public bool    AllowDecimalQuantity  { get; set; }
    public bool    BarcodeScanSound      { get; set; }
    public bool    ShowStockLevel        { get; set; }
    public int     LowStockThreshold     { get; set; }
    public int     ItemsPerPage          { get; set; }
    public bool    ShowCategoryFilter    { get; set; }
    public bool    TouchMode             { get; set; }

    // Connectivity / Operating Mode
    /// <summary>Online | OfflineFallback | LocalFirst</summary>
    public string  OperatingMode          { get; set; } = "Online";

    // Session
    public bool    RequireOpeningFloat   { get; set; }
    public bool    RequireCashCountOnClose { get; set; }
    public bool    AutoCloseSession      { get; set; }
    public string? AutoCloseTime         { get; set; }

    // Loyalty & Promotions
    public bool    EnableLoyaltyPoints   { get; set; }
    public bool    EnablePromotions      { get; set; }
    public bool    EnableCoupons         { get; set; }
    public bool    AutoApplyPromotions   { get; set; }

    // Receipt
    public string  DefaultPaperSize         { get; set; } = "Thermal80mm";
    public int     ReceiptCopies            { get; set; }
    public bool    AutoPrintReceipt         { get; set; }
    public bool    AskToPrintReceipt        { get; set; }
    public bool    SkipReceiptScreen        { get; set; }
    public bool    ReceiptShowLogo          { get; set; }
    public bool    ReceiptShowBusinessName  { get; set; }
    public bool    ReceiptShowAddress       { get; set; }
    public bool    ReceiptShowContact       { get; set; }
    public bool    ReceiptShowCashierName   { get; set; }
    public bool    ReceiptShowCustomerName  { get; set; }
    public bool    ReceiptShowOrderNumber   { get; set; }
    public bool    ReceiptShowDateTime      { get; set; }
    public bool    ReceiptShowItemCodes     { get; set; }
    public bool    ReceiptShowUnitPrice     { get; set; }
    public bool    ReceiptShowLineDiscount  { get; set; }
    public bool    ReceiptShowTaxBreakdown  { get; set; }
    public bool    ReceiptShowDiscountLine  { get; set; }
    public bool    ReceiptShowBarcode       { get; set; }
    public bool    ReceiptShowQrCode        { get; set; }
    public string  ReceiptBarcodeSymbology  { get; set; } = "Code128";
    public bool    ReceiptAutoCutPaper      { get; set; }
    public string? ReceiptHeaderNote        { get; set; }
    public string? ReceiptFooterMessage     { get; set; }
    public string? ReceiptReturnPolicy      { get; set; }

    // UI / Display
    public string  Theme                 { get; set; } = "light";
    public string? PrimaryColor          { get; set; }
    public string  DefaultProductView    { get; set; } = "grid";
    public bool    ShowNumpad            { get; set; }
    public bool    ShowFavouritesBar     { get; set; }
}

/// <summary>All fields are optional — only supplied fields are updated (PATCH semantics).</summary>
public class UpdatePosSettingsDto
{
    // General
    public bool?    RequireCashierPin     { get; set; }
    public int?     AutoLockMinutes       { get; set; }
    public bool?    AllowPriceOverride    { get; set; }
    public bool?    AllowDiscount         { get; set; }
    public decimal? MaxDiscountPercent    { get; set; }
    public bool?    RequireCustomer       { get; set; }
    public Guid?    DefaultCustomerId     { get; set; }
    public bool?    AllowNegativeStock    { get; set; }
    public bool?    TaxInclusivePricing   { get; set; }
    public bool?    AutoApplyTax          { get; set; }
    public string?  DefaultCurrencyCode   { get; set; }
    public decimal? RoundingValue         { get; set; }
    public string?  RoundingMode          { get; set; }

    // Payment
    public bool?    AcceptCash            { get; set; }
    public bool?    AcceptCard            { get; set; }
    public bool?    AcceptMobilePayment   { get; set; }
    public bool?    AcceptCreditOnAccount { get; set; }
    public bool?    AllowSplitPayment     { get; set; }
    public bool?    AllowPartialPayment   { get; set; }
    public string?  DefaultPaymentMethod  { get; set; }
    public bool?    AutoOpenCashDrawer    { get; set; }
    public decimal? MinOrderAmount        { get; set; }
    public decimal? MaxOrderAmount        { get; set; }
    public string?  CashGlAccountNumber   { get; set; }
    public string?  BankGlAccountNumber   { get; set; }

    // Cart & Products
    public bool?    ShowProductImages     { get; set; }
    public bool?    ShowProductDescription{ get; set; }
    public bool?    AllowItemNotes        { get; set; }
    public bool?    AllowDecimalQuantity  { get; set; }
    public bool?    BarcodeScanSound      { get; set; }
    public bool?    ShowStockLevel        { get; set; }
    public int?     LowStockThreshold     { get; set; }
    public int?     ItemsPerPage          { get; set; }
    public bool?    ShowCategoryFilter    { get; set; }
    public bool?    TouchMode             { get; set; }

    // Connectivity / Operating Mode
    public string?  OperatingMode            { get; set; }

    // Session
    public bool?    RequireOpeningFloat      { get; set; }
    public bool?    RequireCashCountOnClose  { get; set; }
    public bool?    AutoCloseSession         { get; set; }
    public string?  AutoCloseTime            { get; set; }

    // Loyalty & Promotions
    public bool?    EnableLoyaltyPoints   { get; set; }
    public bool?    EnablePromotions      { get; set; }
    public bool?    EnableCoupons         { get; set; }
    public bool?    AutoApplyPromotions   { get; set; }

    // Receipt
    public string?  DefaultPaperSize         { get; set; }
    public int?     ReceiptCopies            { get; set; }
    public bool?    AutoPrintReceipt         { get; set; }
    public bool?    AskToPrintReceipt        { get; set; }
    public bool?    SkipReceiptScreen        { get; set; }
    public bool?    ReceiptShowLogo          { get; set; }
    public bool?    ReceiptShowBusinessName  { get; set; }
    public bool?    ReceiptShowAddress       { get; set; }
    public bool?    ReceiptShowContact       { get; set; }
    public bool?    ReceiptShowCashierName   { get; set; }
    public bool?    ReceiptShowCustomerName  { get; set; }
    public bool?    ReceiptShowOrderNumber   { get; set; }
    public bool?    ReceiptShowDateTime      { get; set; }
    public bool?    ReceiptShowItemCodes     { get; set; }
    public bool?    ReceiptShowUnitPrice     { get; set; }
    public bool?    ReceiptShowLineDiscount  { get; set; }
    public bool?    ReceiptShowTaxBreakdown  { get; set; }
    public bool?    ReceiptShowDiscountLine  { get; set; }
    public bool?    ReceiptShowBarcode       { get; set; }
    public bool?    ReceiptShowQrCode        { get; set; }
    public string?  ReceiptBarcodeSymbology  { get; set; }
    public bool?    ReceiptAutoCutPaper      { get; set; }
    public string?  ReceiptHeaderNote        { get; set; }
    public string?  ReceiptFooterMessage     { get; set; }
    public string?  ReceiptReturnPolicy      { get; set; }

    // UI / Display
    public string?  Theme                 { get; set; }
    public string?  PrimaryColor          { get; set; }
    public string?  DefaultProductView    { get; set; }
    public bool?    ShowNumpad            { get; set; }
    public bool?    ShowFavouritesBar     { get; set; }
}

