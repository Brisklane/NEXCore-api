using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Universal order document - single source of truth for ALL channels.
///
// ?
/// ?  CLIENT           ?  BASKET STAGE        ?  ORDER STAGE             ?
// ?
/// ?  WPF / Web POS    ?  Draft (cashier scan)?  PosParked ? PaidAndClosed?
/// ?  Android POS      ?  Draft (cashier scan)?  PosParked ? PaidAndClosed?
/// ?  Customer App     ?  Draft (customer cart)? Confirmed ? Closed       ?
/// ?  B2B / Field Sales?  Draft (sales rep)   ?  Confirmed ? Closed       ?
// ?
///
/// Business logic (promotions, pricing, tax, loyalty) lives in ONE place
/// and is applied to SalesOrderLine regardless of which client created it.
/// </summary>
public class SalesOrder : BaseEntity
{
    // ? Identity
    /// <summary>Auto-generated order number (e.g., SO-2024-00001).</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Order number assigned by the offline Android POS device. Preserved for sync idempotency.</summary>
    public string? OfflineOrderNumber { get; set; }

    public string? OrderName { get; set; }

    // ? Customer
    /// <summary>
    /// Cross-module reference to Crm.Contact - the person placing this order.
    /// ID only - never navigate across module boundaries.
    /// Null only for fully anonymous POS sales with no customer lookup.
    /// </summary>
    public Guid? ContactId { get; set; }

    /// <summary>Snapshot of contact name at order time for fast reads without cross-module query.</summary>
    public string? ContactName { get; set; }

    // ? Customer PO
    public string? CustomerPONumber { get; set; }
    public DateTime? CustomerPODate { get; set; }

    // ? Status & Dates
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the customer tapped "Place Order" or cashier tapped "Charge".
    /// THIS timestamp marks the store visibility boundary.
    /// Null while Status = Draft or PosParked.
    /// </summary>
    public DateTime? PlacedAt { get; set; }

    public DateTime? RequestedDeliveryDate { get; set; }
    public DateTime? ConfirmedDeliveryDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    public DateTime? RejectedDate { get; set; }

    /// <summary>
    /// When this Draft order expires and is auto-abandoned.
    /// POS drafts: typically end of session.
    /// App carts: typically 7 days.
    /// Null = no expiry.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // ? Source / Channel Origin
    public Guid? QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public Guid? SalesAgreementId { get; set; }
    public SalesAgreement? SalesAgreement { get; set; }

    /// <summary>Which client/channel created this order.</summary>
    public SalesChannel SalesChannel { get; set; } = SalesChannel.DirectSales;

    /// <summary>
    /// Branch this POS order originates from (PosWalkIn, PhoneOrder, click-and-collect).
    /// FK to Core.Branch by ID only — never navigate across module boundaries.
    /// </summary>
    public Guid? OriginBranchId { get; set; }

    /// <summary>
    /// POS Terminal where the order was opened.
    /// Set for WPF POS and Android POS channels. Null for app/online.
    /// A parked order can be resumed on a DIFFERENT terminal - update this field on resume.
    /// </summary>
    public Guid? OriginPosTerminalId { get; set; }
    public PosTerminal? OriginPosTerminal { get; set; }

    /// <summary>
    /// Cashier who opened or last touched this order at POS.
    /// Null for self-service app orders.
    /// </summary>
    public Guid? OriginPosCashierId { get; set; }
    public PosCashier? OriginPosCashier { get; set; }

    /// <summary>
    /// The POS Session under which this order was opened.
    /// Used for session-level cash reconciliation reporting.
    /// </summary>
    public Guid? OriginPosSessionId { get; set; }
    public PosSession? OriginPosSession { get; set; }

    /// <summary>
    /// POS Transaction that fully closed this order (immediate payment).
    /// </summary>
    public Guid? ClosingPosTransactionId { get; set; }
    public PosTransaction? ClosingPosTransaction { get; set; }

    // ? Promotions & Pricing
    public Guid? PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    /// <summary>ISO 4217 currency code.</summary>
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>Applied coupon code - same engine for POS and app.</summary>
    public string? CouponCode { get; set; }
    public decimal CouponDiscountAmount { get; set; }

    /// <summary>Loyalty points redeemed against this order (deducted from account).</summary>
    public decimal LoyaltyPointsRedeemed { get; set; }
    /// <summary>Loyalty points that will be earned when this order is paid.</summary>
    public decimal LoyaltyPointsEarned { get; set; }

    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public Incoterm? Incoterm { get; set; }
    public string? IncotermLocation { get; set; }

    // ── Invoicing ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Computed invoice status — updated whenever an invoice is created or paid.
    /// Drives the "Invoice Status" column and the "To Invoice" filter.
    /// </summary>
    public OrderInvoiceStatus InvoiceStatus { get; set; } = OrderInvoiceStatus.NothingToInvoice;

    /// <summary>
    /// Policy controlling which quantity (ordered vs delivered) is used to generate invoice lines.
    /// Default: OnOrder (invoice immediately on confirmation).
    /// </summary>
    public InvoicePolicy InvoicePolicy { get; set; } = InvoicePolicy.OnOrder;

    /// <summary>
    /// When true, the order is locked and cannot be edited. Set after confirmation.
    /// Equivalent to Odoo's "Lock Confirmed Sales" setting.
    /// </summary>
    public bool IsLocked { get; set; }

    // ── Fulfillment Type ──────────────────────────────────────────────────────
    /// <summary>
    /// Immediate (walk-in, counter), Delivery (rider), Pickup (click-and-collect).
    /// Drives post-payment workflow.
    /// </summary>
    public FulfillmentType FulfillmentType { get; set; } = FulfillmentType.Immediate;

    // ? Billing Address
    public string? BillToName { get; set; }
    public string? BillToStreet { get; set; }
    public string? BillToCity { get; set; }
    public string? BillToState { get; set; }
    public string? BillToPostalCode { get; set; }
    public string? BillToCountry { get; set; }

    // ? Delivery (cross-module Crm.ContactAddress reference)
    /// <summary>
    /// Cross-module FK to Crm.ContactAddress. ID only — never navigate.
    /// The full delivery address (street, city, GPS, phone, etc.) lives on the Delivery entity.
    /// </summary>
    public Guid? ShipToAddressId { get; set; }

    // ? Financials
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }

    // ? Credit Check (B2B only)
    public bool CreditCheckPassed { get; set; }
    public DateTime? CreditCheckDate { get; set; }

    // ? Ownership
    public Guid? SalesRepId { get; set; }
    public Guid? SalesTerritoryId { get; set; }
    public SalesTerritory? SalesTerritory { get; set; }

    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? CancellationReason { get; set; }

    // ? Navigation
    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    public ICollection<SalesInvoice> Invoices { get; set; } = new List<SalesInvoice>();
    public ICollection<SalesOrderApproval> Approvals { get; set; } = new List<SalesOrderApproval>();
    public ICollection<SalesOrderAttachment> Attachments { get; set; } = new List<SalesOrderAttachment>();
    public ICollection<SalesPayment> Payments { get; set; } = new List<SalesPayment>();
    public ICollection<SalesOrderStatusHistory> StatusHistory { get; set; } = new List<SalesOrderStatusHistory>();
}
