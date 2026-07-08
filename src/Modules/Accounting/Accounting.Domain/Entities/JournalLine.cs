using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;

namespace Accounting.Domain.Entities;

/// <summary>
/// Journal line - individual debit/credit line in a journal entry
/// </summary>
public class JournalLine : BaseEntity
{
    /// <summary>
    /// Journal entry reference
    /// </summary>
    public Guid JournalEntryId { get; set; }

    /// <summary>
    /// Ledger account reference
    /// </summary>
    public Guid LedgerAccountId { get; set; }

    /// <summary>
    /// Debit amount (0 if credit line)
    /// </summary>
    public decimal DebitAmount { get; set; }

    /// <summary>
    /// Credit amount (0 if debit line)
    /// </summary>
    public decimal CreditAmount { get; set; }

    /// <summary>
    /// Currency code for this line
    /// </summary>
    public required string CurrencyCode { get; set; }

    /// <summary>
    /// Exchange rate for this line
    /// </summary>
    public decimal ExchangeRate { get; set; } = 1;

    /// <summary>
    /// Debit amount in base currency
    /// </summary>
    public decimal BaseDebitAmount { get; set; }

    /// <summary>
    /// Credit amount in base currency
    /// </summary>
    public decimal BaseCreditAmount { get; set; }

    /// <summary>
    /// Dimension set reference (optional)
    /// </summary>
    public Guid? DimensionSetId { get; set; }

    /// <summary>
    /// Tax code reference (optional)
    /// </summary>
    public Guid? TaxCodeId { get; set; }

    /// <summary>
    /// Unit of measure reference (optional) - e.g., "Units", "Boxes", "Kilos"
    /// Enables proper quantity tracking for Inventory and Sales integration
    /// </summary>
    public Guid? UnitOfMeasureId { get; set; }

    /// <summary>
    /// Source module type (optional) - identifies which module created this journal line
    /// Replaces hardcoded strings with ModuleType enum
    /// </summary>
    public ModuleType? SourceModuleType { get; set; }

    /// <summary>
    /// Source entity ID (optional) - GUID of the source entity
    /// Enables strong link to source document (SalesInvoice, PurchaseOrder, etc.)
    /// </summary>
    public Guid? SourceEntityId { get; set; }

    /// <summary>
    /// Source entity type (optional) - identifies the type of source entity
    /// Replaces hardcoded strings with EntityType enum
    /// </summary>
    public EntityType? SourceEntityType { get; set; }

    /// <summary>
    /// Customer ID (optional) - reference to Sales/AR customer
    /// Used for AR module integration
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Vendor ID (optional) - reference to Purchasing/AP vendor
    /// Used for AP module integration
    /// </summary>
    public Guid? VendorId { get; set; }

    /// <summary>
    /// Inventory item ID (optional) - reference to inventory master
    /// Used for inventory module integration
    /// </summary>
    public Guid? InventoryItemId { get; set; }

    /// <summary>
    /// Due date (optional) - when payment is due
    /// Used for AR/AP aging analysis
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Paid date (optional) - when payment was received/made
    /// Used for payment reconciliation
    /// </summary>
    public DateTime? PaidDate { get; set; }

    /// <summary>
    /// Payment terms (optional) - standard payment terms
    /// Replaces hardcoded strings with PaymentTerms enum
    /// </summary>
    public PaymentTerms? PaymentTermsCode { get; set; }

    /// <summary>
    /// Line number in the entry
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Quantity (if applicable)
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Unit price (if applicable)
    /// </summary>
    public decimal? UnitPrice { get; set; }

    // Navigation properties
    public JournalEntry? JournalEntry { get; set; }
    public LedgerAccount? LedgerAccount { get; set; }
}
