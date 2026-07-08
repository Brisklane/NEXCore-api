using Nexcore.SharedKernel.Enums;
using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

// ── Purchase Contract Read ────────────────────────────────────────────────────

public class PurchaseContractDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid VendorId { get; set; }
    public string? VendorName { get; set; }

    public PurchaseContractType ContractType { get; set; }
    public PurchaseContractStatus Status { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? TerminatedAt { get; set; }

    public bool AutoRenew { get; set; }
    public int RenewalNoticeDays { get; set; }
    public int? RenewalDurationMonths { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal? MaximumContractValue { get; set; }
    public decimal CommittedValue { get; set; }
    public decimal UsedValue { get; set; }
    public decimal RemainingValue { get; set; }

    public PaymentTerms PaymentTerms { get; set; }
    public Incoterm? Incoterm { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? TermsAndConditions { get; set; }
    public string? TerminationReason { get; set; }
    public string? Notes { get; set; }

    public List<PurchaseContractLineDto> Lines { get; set; } = [];
}

public class PurchaseContractLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }

    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string ItemDescription { get; set; } = string.Empty;

    public Guid? ProcurementCategoryId { get; set; }
    public string? ProcurementCategoryName { get; set; }

    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }

    public decimal? MinimumQuantity { get; set; }
    public decimal? MaximumQuantity { get; set; }
    public decimal? CommittedQuantity { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public string? Notes { get; set; }
}

// ── Purchase Contract Create / Update ─────────────────────────────────────────

public class CreatePurchaseContractDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }

    public Guid VendorId { get; set; }
    public PurchaseContractType ContractType { get; set; } = PurchaseContractType.FrameworkAgreement;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool AutoRenew { get; set; }
    public int RenewalNoticeDays { get; set; }
    public int? RenewalDurationMonths { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal? MaximumContractValue { get; set; }

    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public Incoterm? Incoterm { get; set; }

    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }

    public List<CreatePurchaseContractLineDto> Lines { get; set; } = [];
}

public class CreatePurchaseContractLineDto
{
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public required string ItemDescription { get; set; }
    public Guid? ProcurementCategoryId { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public decimal? MinimumQuantity { get; set; }
    public decimal? MaximumQuantity { get; set; }
    public decimal? CommittedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? Notes { get; set; }
}

public class TerminateContractDto
{
    public required string TerminationReason { get; set; }
}

public class UpdatePurchaseContractDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MaximumContractValue { get; set; }
    public bool? AutoRenew { get; set; }
    public int? RenewalNoticeDays { get; set; }
    public int? RenewalDurationMonths { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }
    public List<CreatePurchaseContractLineDto>? Lines { get; set; }
}
