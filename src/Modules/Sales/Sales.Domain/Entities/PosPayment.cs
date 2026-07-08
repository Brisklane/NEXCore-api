using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Payment tender line for a POS transaction.
/// A transaction can have multiple tenders (split payment).
/// </summary>
public class PosPayment : BaseEntity
{
    public Guid PosTransactionId { get; set; }
    public PosTransaction PosTransaction { get; set; } = null!;

    public PosTenderType TenderType { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Card last 4 digits, wallet transaction ID, cheque no., etc.</summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>Card approval / authorization code from payment gateway.</summary>
    public string? AuthorizationCode { get; set; }

    public string? CardScheme { get; set; }     // VISA, Mastercard, Amex…
    public string? CardLast4 { get; set; }

    public bool IsApproved { get; set; } = true;
    public string? FailureReason { get; set; }
}
