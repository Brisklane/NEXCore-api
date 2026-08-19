using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>
/// A return authorisation — permission for goods to come back.
///
/// The authorisation exists separately from the receipt because in distribution the two are days
/// and kilometres apart: a rep agrees a return at the counter on Tuesday, a van collects it on
/// Thursday, and the warehouse inspects it on Friday. Without the authorisation, the goods arrive
/// unannounced and nobody can say what was agreed.
/// </summary>
public class ReturnAuthorisation : BaseEntity
{
    public string ReturnNumber { get; set; } = string.Empty;

    public ReturnKind Kind { get; set; } = ReturnKind.SaleableMarketReturn;
    public ReturnStatus Status { get; set; } = ReturnStatus.Requested;

    public Guid? OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }
    public Guid? PartnerId { get; set; }

    public Guid? VisitId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? RouteId { get; set; }

    /// <summary>The invoice being returned against, where there is one.</summary>
    public Guid? OriginalOrderId { get; set; }
    public Guid? OriginalInvoiceId { get; set; }
    public string? OriginalInvoiceNumber { get; set; }

    public DateTime RequestedOn { get; set; }

    /// <summary>Authorisation lapses if the goods are not collected; stops stale returns arriving.</summary>
    public DateTime? ValidUntil { get; set; }

    public Guid? ReasonCodeId { get; set; }
    public string? ReasonNote { get; set; }

    public ReturnValuationBasis ValuationBasis { get; set; } = ReturnValuationBasis.OriginalInvoicePrice;

    /// <summary>Used when the basis is a policy percentage — expired goods often credit at 80%.</summary>
    public decimal ValuationPercent { get; set; } = 100;

    public string CurrencyCode { get; set; } = "USD";
    public decimal ClaimedValue { get; set; }
    public decimal ApprovedValue { get; set; }
    public decimal CreditedValue { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>How the goods come back — on the van, on the next trip, or delivered by the outlet.</summary>
    public Guid? CollectionTripId { get; set; }
    public Guid? CollectionVanUnitId { get; set; }
    public DateTime? CollectedAt { get; set; }

    /// <summary>The claim raised on the principal once the return is credited to the outlet.</summary>
    public Guid? LinkedClaimId { get; set; }
    public Guid? CreditNoteId { get; set; }

    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }

    public ICollection<ReturnAuthorisationLine> Lines { get; set; } = [];
}

/// <summary>One SKU on a return authorisation, batch-specific because expiry is usually the reason.</summary>
public class ReturnAuthorisationLine : BaseEntity
{
    public Guid ReturnId { get; set; }
    public ReturnAuthorisation? Return { get; set; }

    public int DisplayOrder { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }

    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string Uom { get; set; } = "PCS";
    public decimal UomFactor { get; set; } = 1;

    public decimal RequestedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineValue { get; set; }

    public Guid? ReasonCodeId { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Goods physically arriving back at a warehouse, and what was decided about them.
///
/// Kept apart from the authorisation because what comes back is routinely not what was agreed,
/// and the difference between the two is a number worth reporting.
/// </summary>
public class ReturnReceipt : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;

    public Guid? ReturnId { get; set; }
    public ReturnAuthorisation? Return { get; set; }

    public Guid? WarehouseId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? TripId { get; set; }

    public DateTime ReceivedAt { get; set; }
    public Guid? ReceivedByUserId { get; set; }

    public DateTime? InspectedAt { get; set; }
    public Guid? InspectedByUserId { get; set; }

    public decimal TotalValue { get; set; }
    public decimal RestockedValue { get; set; }
    public decimal ScrappedValue { get; set; }

    /// <summary>Regulated categories require a certificate before expired goods can be destroyed.</summary>
    public string? DestructionCertificateNumber { get; set; }
    public string? DestructionCertificateUrl { get; set; }
    public DateTime? DestroyedOn { get; set; }

    public string? Note { get; set; }

    public ICollection<ReturnReceiptLine> Lines { get; set; } = [];
}

/// <summary>One SKU received back, with its inspected condition.</summary>
public class ReturnReceiptLine : BaseEntity
{
    public Guid ReceiptId { get; set; }
    public ReturnReceipt? Receipt { get; set; }

    public Guid? ReturnLineId { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string Uom { get; set; } = "PCS";
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineValue { get; set; }

    public ReturnDispositionKind Disposition { get; set; } = ReturnDispositionKind.Restock;

    /// <summary>Where accepted goods went — sellable bin, quarantine, scrap.</summary>
    public Guid? PutawayBinId { get; set; }

    public Guid? ReasonCodeId { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// What was done with returned goods, as a separate decision record.
///
/// One receipt line can be split three ways — half restocked, some discounted, the rest scrapped —
/// and each split has its own value and its own accounting consequence.
/// </summary>
public class ReturnDisposition : BaseEntity
{
    public Guid ReceiptLineId { get; set; }
    public ReturnReceiptLine? ReceiptLine { get; set; }

    public ReturnDispositionKind Kind { get; set; }
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }

    public DateTime DecidedAt { get; set; }
    public Guid? DecidedByUserId { get; set; }

    /// <summary>Where it went — a bin for restock, a supplier for RTV, a scheme for discount-and-sell.</summary>
    public Guid? TargetBinId { get; set; }
    public Guid? TargetSupplierId { get; set; }
    public Guid? LiquidationSchemeId { get; set; }

    /// <summary>Claim raised on the supplier or the principal to recover the loss.</summary>
    public Guid? LinkedClaimId { get; set; }

    public string? Note { get; set; }
}
