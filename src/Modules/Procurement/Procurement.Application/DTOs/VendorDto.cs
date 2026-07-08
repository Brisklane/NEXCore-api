using Nexcore.SharedKernel.Enums;
using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

// ── Vendor Read ───────────────────────────────────────────────────────────────

public class VendorDto
{
    public Guid Id { get; set; }
    public string VendorNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public VendorType Type { get; set; }
    public VendorStatus Status { get; set; }
    public VendorOnboardingStatus OnboardingStatus { get; set; }

    public string? TaxRegistrationNumber { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? VATNumber { get; set; }
    public string? Website { get; set; }

    public Guid? VendorCategoryId { get; set; }
    public string? VendorCategoryName { get; set; }

    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? PrimaryMobile { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public PaymentTerms PaymentTerms { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal CreditLimit { get; set; }
    public bool IsPreferredVendor { get; set; }

    public Guid? ApLedgerAccountId { get; set; }
    public SubledgerType SubledgerType { get; set; }

    public decimal? OverallRating { get; set; }
    public decimal? OnTimeDeliveryRate { get; set; }
    public decimal? QualityScore { get; set; }

    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime? BlockedAt { get; set; }

    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    public List<VendorContactDto> Contacts { get; set; } = [];
    public List<VendorAddressDto> Addresses { get; set; } = [];
    public List<VendorBankAccountDto> BankAccounts { get; set; } = [];
}

public class VendorContactDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? JobTitle { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public bool IsPrimary { get; set; }
}

public class VendorAddressDto
{
    public Guid Id { get; set; }
    public VendorAddressType AddressType { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public bool IsDefault { get; set; }
}

public class VendorBankAccountDto
{
    public Guid Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BranchName { get; set; }
    public string? IBAN { get; set; }
    public string? SwiftCode { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

// ── Vendor Create / Update ────────────────────────────────────────────────────

public class CreateVendorDto
{
    public required string Name { get; set; }
    public string? ShortName { get; set; }
    public VendorType Type { get; set; } = VendorType.Company;

    public string? TaxRegistrationNumber { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? VATNumber { get; set; }
    public string? Website { get; set; }

    public Guid? VendorCategoryId { get; set; }

    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? PrimaryMobile { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public int LeadTimeDays { get; set; } = 7;
    public decimal CreditLimit { get; set; }
    public bool IsPreferredVendor { get; set; }

    public Guid? ApLedgerAccountId { get; set; }

    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
}

public class UpdateVendorDto
{
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public VendorType? Type { get; set; }

    public string? TaxRegistrationNumber { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? VATNumber { get; set; }
    public string? Website { get; set; }

    public Guid? VendorCategoryId { get; set; }

    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? PrimaryMobile { get; set; }

    public string? CurrencyCode { get; set; }
    public PaymentTerms? PaymentTerms { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? CreditLimit { get; set; }
    public bool? IsPreferredVendor { get; set; }

    public Guid? ApLedgerAccountId { get; set; }

    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
}

public class BlockVendorDto
{
    public required string BlockReason { get; set; }
}
