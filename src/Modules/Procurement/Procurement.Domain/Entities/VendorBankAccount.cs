using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Vendor bank account details used for payment processing.
/// Aligned with SAP Vendor Bank Details (LFBK), Oracle Supplier Bank Account.
/// </summary>
public class VendorBankAccount : BaseEntity
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public required string BankName { get; set; }
    public string? BankCode { get; set; }
    public string? BranchCode { get; set; }
    public string? BranchName { get; set; }

    public required string AccountHolderName { get; set; }
    public required string AccountNumber { get; set; }
    public string? IBAN { get; set; }
    public string? SWIFTCode { get; set; }
    public string? RoutingNumber { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public bool IsDefault { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByUserId { get; set; }

    public string? Notes { get; set; }
}
