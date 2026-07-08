using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

public class LandedCostDto
{
    public Guid Id { get; set; }
    public string LandedCostNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public LandedCostStatus Status { get; set; }
    public DateTime DocumentDate { get; set; }
    public DateTime? PostedAt { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; }
    public decimal TotalLandedCostAmount { get; set; }
    public string? Notes { get; set; }

    public List<LandedCostLineDto> CostLines { get; set; } = [];
    public List<LandedCostGoodsReceiptDto> GoodsReceipts { get; set; } = [];
    public List<LandedCostAllocationDto> Allocations { get; set; } = [];
}

public class LandedCostLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public LandedCostType CostType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public LandedCostAllocationMethod AllocationMethod { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public string? Notes { get; set; }
}

public class LandedCostGoodsReceiptDto
{
    public Guid Id { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public string? GoodsReceiptNumber { get; set; }
}

public class LandedCostAllocationDto
{
    public Guid Id { get; set; }
    public Guid LandedCostLineId { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public string? ItemDescription { get; set; }
    public LandedCostAllocationMethod AllocationMethod { get; set; }
    public decimal AllocationBasisValue { get; set; }
    public decimal TotalBasisValue { get; set; }
    public decimal AllocationPercent { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal AllocatedAmountPerUnit { get; set; }
}

// ── Create ──────────────────────────────────────────────────────────────────────

public class CreateLandedCostDto
{
    public string? Description { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime DocumentDate { get; set; } = DateTime.UtcNow;
    public string CurrencyCode { get; set; } = "USD";
    public string? Notes { get; set; }
    public List<Guid> GoodsReceiptIds { get; set; } = [];
    public List<CreateLandedCostLineDto> CostLines { get; set; } = [];
}

public class CreateLandedCostLineDto
{
    public LandedCostType CostType { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public LandedCostAllocationMethod AllocationMethod { get; set; } = LandedCostAllocationMethod.ByValue;
    public decimal TaxPercent { get; set; }
    public Guid? LedgerAccountId { get; set; }
    public string? Notes { get; set; }
}
