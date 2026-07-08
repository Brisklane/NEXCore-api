namespace Nexcore.SharedKernel.Events;

// Per-line accounting payload

/// <summary>
/// One product line in a sales accounting event.
/// Carries the item-level GL account IDs that were configured on the
/// Inventory item (SalesAccountId, CogsAccountId, InventoryAccountId).
/// These come from <c>Item.SalesAccountId</c> etc. so every line posts to its
/// own correct revenue / cost account instead of a single posting profile.
/// </summary>
public class SalesAccountingLine
{
    public Guid ProductId         { get; init; }
    public string ProductCode     { get; init; } = string.Empty;
    public string ProductName     { get; init; } = string.Empty;

    public decimal Quantity       { get; init; }
    public decimal UnitPrice      { get; init; }   // selling price (excl. tax)
    public decimal UnitCost       { get; init; }   // inventory cost (for COGS)
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount      { get; init; }   // tax on this line
    public decimal LineTotal      { get; init; }   // net revenue excl. tax

    // GL accounts from Item master
    /// <summary>Item.SalesAccountId - credit account for sales revenue.</summary>
    public Guid? SalesGlAccountId     { get; init; }

    /// <summary>Item.CogsAccountId - debit account for COGS expense.</summary>
    public Guid? CogsGlAccountId      { get; init; }

    /// <summary>Item.InventoryAccountId - credit account relieved on shipment.</summary>
    public Guid? InventoryGlAccountId { get; init; }
}

// Sales Invoice posted (credit sale)

/// <summary>
/// Published by <c>SalesInvoiceController.Confirm</c> when a sales invoice
/// is confirmed (Draft ? Issued). Triggers the AR debit journal entry.
///
/// Accounting posts per-line:
///   DR  Accounts Receivable (1121)    =  TotalAmount  (gross)
///   CR  Line.SalesGlAccountId         =  Line.LineTotal    (net revenue per line)
///   CR  Sales Tax Payable    (2141)   =  TotalTaxAmount    (total tax, one line)
///
/// Falls back to PostingProfile / account "4111" when SalesGlAccountId is null.
/// </summary>
public class SalesInvoicePostedEvent
{
    public Guid InvoiceId         { get; init; }
    public string InvoiceNumber   { get; init; } = string.Empty;
    public Guid SalesOrderId      { get; init; }
    public Guid? ContactId        { get; init; }
    public string? ContactName    { get; init; }

    public decimal SubtotalAmount { get; init; }   // sum of net line totals
    public decimal TaxAmount      { get; init; }   // total tax
    public decimal ShippingAmount { get; init; }   // freight / delivery charged to the customer
    public decimal TotalAmount    { get; init; }   // gross (incl. tax + shipping)

    public string CurrencyCode    { get; init; } = "USD";
    public decimal ExchangeRate   { get; init; } = 1m;
    public DateTime InvoiceDate   { get; init; } = DateTime.UtcNow;
    public DateTime DueDate       { get; init; }

    /// <summary>Per-line detail with item-specific GL account IDs.</summary>
    public IReadOnlyList<SalesAccountingLine> Lines { get; init; } = [];

    public Guid CompanyId         { get; init; }
    public Guid BranchId          { get; init; }
    public Guid BusinessUnitId    { get; init; }
    public Guid CreatedByUserId   { get; init; }
}

// Delivery shipped - COGS entry

/// <summary>
/// Published by <c>DeliveryController.Ship</c> when a delivery is marked as
/// Shipped. Triggers the COGS journal entry (inventory relief).
///
/// Accounting posts per-line:
///   DR  Line.CogsGlAccountId          =  Line.Quantity * Line.UnitCost
///   CR  Line.InventoryGlAccountId     =  Line.Quantity * Line.UnitCost
///
/// Falls back to "5104" (Cost of Inventory Sold) / "1143" (Finished Goods)
/// when item-level accounts are null.
/// </summary>
public class SalesCogsPostedEvent
{
    public Guid DeliveryId        { get; init; }
    public Guid SalesOrderId      { get; init; }
    public Guid? SalesInvoiceId   { get; init; }

    public DateTime ShippedAt     { get; init; } = DateTime.UtcNow;
    public string CurrencyCode    { get; init; } = "USD";

    /// <summary>Per-line detail with item-specific GL account IDs and unit cost.</summary>
    public IReadOnlyList<SalesAccountingLine> Lines { get; init; } = [];

    public Guid CompanyId         { get; init; }
    public Guid BranchId          { get; init; }
    public Guid BusinessUnitId    { get; init; }
    public Guid CreatedByUserId   { get; init; }
}

// Payment received (AR clearing)

/// <summary>
/// Published by <c>SalesPaymentController.Create</c> when a customer payment
/// is recorded. Triggers the AR clearing / cash receipt entry.
///
/// Cash sale (POS / COD):
///   DR  Cash on Hand (1111) / Bank (1112)  =  AmountPaid
///   CR  Accounts Receivable (1121)          =  AmountPaid
///
/// Card / gateway sale:
///   DR  Bank Account - Primary (1112)       =  AmountPaid
///   CR  Accounts Receivable (1121)          =  AmountPaid
///
/// Account resolved via PostingProfile ModuleName="Sales", TransactionType="SalesPayment",
/// or falls back to account numbers above.
/// </summary>
public class SalesPaymentReceivedEvent
{
    public Guid PaymentId         { get; init; }
    public string PaymentNumber   { get; init; } = string.Empty;
    public Guid SalesOrderId      { get; init; }
    public Guid? ContactId        { get; init; }

    public decimal AmountPaid     { get; init; }
    /// <summary>Cash | Card | BankTransfer | Cheque | Wallet | GiftCard | COD</summary>
    public string PaymentMethod   { get; init; } = string.Empty;
    public string CurrencyCode    { get; init; } = "USD";
    public decimal ExchangeRate   { get; init; } = 1m;
    public DateTime PaymentDate   { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// How this payment is split across invoices.
    /// Empty for POS walk-in sales where no invoice is raised.
    /// </summary>
    public List<PaymentInvoiceAllocation> InvoiceAllocations { get; init; } = [];

    public Guid CompanyId         { get; init; }
    public Guid BranchId          { get; init; }
    public Guid BusinessUnitId    { get; init; }
    public Guid CreatedByUserId   { get; init; }

    /// <summary>
    /// Optional GL account number override for cash payments (e.g. "1111").
    /// Set from PosSettings.CashGlAccountNumber. Null = fall back to PostingProfile / default.
    /// </summary>
    public string? CashAccountNumber { get; init; }
    /// <summary>
    /// Optional GL account number override for card/mobile/bank payments (e.g. "1112").
    /// Set from PosSettings.BankGlAccountNumber. Null = fall back to PostingProfile / default.
    /// </summary>
    public string? BankAccountNumber { get; init; }
}

/// <summary>How much of a payment is applied to one invoice.</summary>
public record PaymentInvoiceAllocation(Guid InvoiceId, decimal AllocatedAmount);

// Invoice sent (email notification)

/// <summary>
/// Published by <c>SalesInvoiceController.Send</c> when a user clicks "Send".
/// The notification layer handles email + PDF attachment generation.
/// </summary>
public class SalesInvoiceSentEvent
{
    public Guid InvoiceId         { get; init; }
    public string InvoiceNumber   { get; init; } = string.Empty;
    public Guid? ContactId        { get; init; }
    public string? ContactName    { get; init; }
    public decimal TotalAmount    { get; init; }
    public string CurrencyCode    { get; init; } = "USD";
    public DateTime DueDate       { get; init; }
    public List<string> ToEmails  { get; init; } = [];
    public string? Subject        { get; init; }
    public string? Body           { get; init; }
}

// POS immediate cash / card sale

/// <summary>
/// Published by <c>PosTransactionController</c> when a POS transaction is
/// completed (paid immediately, no separate invoice raised).
///
/// Accounting posts per-line revenue AND COGS in one compound journal:
///   DR  Cash on Hand / Bank             =  TotalAmount  (gross)
///   CR  Line.SalesGlAccountId           =  Line.LineTotal  (per line)
///   CR  Sales Tax Payable (2141)        =  TotalTaxAmount
///   DR  Line.CogsGlAccountId            =  Line.Quantity * Line.UnitCost  (per line)
///   CR  Line.InventoryGlAccountId       =  Line.Quantity * Line.UnitCost  (per line)
/// </summary>
public class PosTransactionAccountingEvent
{
    public Guid PosTransactionId  { get; init; }
    public Guid PosStoreId        { get; init; }
    public Guid? ContactId        { get; init; }

    public decimal SubtotalAmount { get; init; }
    public decimal TaxAmount      { get; init; }
    public decimal TotalAmount    { get; init; }
    public string PaymentMethod   { get; init; } = "Cash";
    public string CurrencyCode    { get; init; } = "USD";
    public DateTime CompletedAt   { get; init; } = DateTime.UtcNow;

    /// <summary>Per-line detail with item-specific GL account IDs and unit cost.</summary>
    public IReadOnlyList<SalesAccountingLine> Lines { get; init; } = [];

    public Guid CompanyId         { get; init; }
    public Guid BranchId          { get; init; }
    public Guid BusinessUnitId    { get; init; }
    public Guid CreatedByUserId   { get; init; }
}
