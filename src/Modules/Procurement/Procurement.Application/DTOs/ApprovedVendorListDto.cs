namespace Procurement.Application.DTOs;

// ── Approved Vendor List (AVL) Read ─────────────────────────────────────────────

public class ApprovedVendorListDto
{
    public Guid Id { get; set; }

    // Scope — item or category
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemDescription { get; set; }
    public Guid? ProcurementCategoryId { get; set; }
    public string? ProcurementCategoryName { get; set; }

    // Vendor
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string VendorNumber { get; set; } = string.Empty;

    // Validity
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }

    // Routing rules
    public bool IsPreferred { get; set; }
    public bool IsExclusive { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }

    // Commercial defaults
    public decimal? DefaultUnitPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal? MinimumOrderQuantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }

    // Quality / approval
    public bool RequiresQualityInspection { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }
}

// ── Create / Update ─────────────────────────────────────────────────────────────

public class CreateApprovedVendorListDto
{
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemDescription { get; set; }
    public Guid? ProcurementCategoryId { get; set; }

    public required Guid VendorId { get; set; }

    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }

    public bool IsPreferred { get; set; }
    public bool IsExclusive { get; set; }

    public decimal? DefaultUnitPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal? MinimumOrderQuantity { get; set; }
    public Guid? UnitOfMeasureId { get; set; }

    public bool RequiresQualityInspection { get; set; }
    public string? Notes { get; set; }
}

public class UpdateApprovedVendorListDto
{
    public string? ItemCode { get; set; }
    public string? ItemDescription { get; set; }
    public Guid? ProcurementCategoryId { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool? IsActive { get; set; }

    public bool? IsPreferred { get; set; }
    public bool? IsExclusive { get; set; }

    public decimal? DefaultUnitPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? MinimumOrderQuantity { get; set; }

    public bool? RequiresQualityInspection { get; set; }
    public string? Notes { get; set; }
}

public class BlockApprovedVendorDto
{
    public required string BlockReason { get; set; }
}
