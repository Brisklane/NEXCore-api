using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Customer Return / RMA (Return Merchandise Authorization).
/// Aligned with SAP RE (Returns) order type, Oracle RMA, Dynamics Return Order.
/// </summary>
public class SalesReturn : BaseEntity
{
    public string ReturnNumber { get; set; } = string.Empty;

    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    /// <summary>Cross-module reference to Crm.Contact. ID only.</summary>
    public Guid? ContactId { get; set; }
    /// <summary>Snapshot of contact name for fast reads.</summary>
    public string? ContactName { get; set; }

    public Guid? SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }

    public ReturnStatus Status { get; set; } = ReturnStatus.Requested;

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? CreditIssuedDate { get; set; }

    /// <summary>RMA authorization number provided to the customer.</summary>
    public string? RmaNumber { get; set; }

    public string? ReturnReason { get; set; }
    public string? InspectionNotes { get; set; }

    public decimal TotalRefundAmount { get; set; }

    public Guid? CreditNoteId { get; set; }
    public CreditNote? CreditNote { get; set; }

    public ICollection<SalesReturnLine> Lines { get; set; } = new List<SalesReturnLine>();
}
