namespace Nexcore.SharedKernel.Events;

/// <summary>One expense/inventory line on a posted vendor invoice (amount is tax-inclusive).</summary>
public class PurchaseInvoiceAccountingLine
{
    /// <summary>Line total INCLUDING tax (simple model folds tax into the expense line).</summary>
    public decimal Amount { get; init; }
    /// <summary>Preferred debit account (the line's chosen GL account). Null → handler fallback.</summary>
    public Guid? ExpenseAccountId { get; init; }
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Published by Procurement when a Purchase Invoice (vendor bill) is posted. Accounting handles this
/// to recognise the payable:  DR Expense/Inventory (per line)  CR Accounts Payable (total).
/// The vendor-side counterpart of <see cref="SalesInvoicePostedEvent"/>.
/// </summary>
public class PurchaseInvoicePostedEvent
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid VendorId { get; init; }
    public string? VendorName { get; init; }
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid BusinessUnitId { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public IReadOnlyList<PurchaseInvoiceAccountingLine> Lines { get; init; } = [];
}

/// <summary>
/// Published by Procurement when a Vendor Payment clears. Accounting handles this to settle the
/// payable:  DR Accounts Payable  CR Bank/Cash.  Counterpart of <see cref="SalesPaymentReceivedEvent"/>.
/// </summary>
public class VendorPaymentClearedEvent
{
    public Guid PaymentId { get; init; }
    public string PaymentNumber { get; init; } = string.Empty;
    public Guid VendorId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid BusinessUnitId { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTime PaymentDate { get; init; }
    /// <summary>Amount actually applied to invoices (sum of allocations).</summary>
    public decimal AmountPaid { get; init; }
    /// <summary>True = paid from cash on hand; false = paid from the bank.</summary>
    public bool IsCash { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
}
