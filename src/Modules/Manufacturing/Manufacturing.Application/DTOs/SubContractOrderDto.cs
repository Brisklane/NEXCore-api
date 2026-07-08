namespace Manufacturing.Application.DTOs;

public class SubContractOrderDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid ProductionOrderOperationId { get; set; }
    public Guid VendorId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public decimal QuantitySent { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityRejected { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public required string Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateSubContractOrderDto
{
    public Guid ProductionOrderId { get; set; }
    public Guid ProductionOrderOperationId { get; set; }
    public Guid VendorId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public decimal QuantitySent { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }
}

public class UpdateSubContractOrderDto
{
    public decimal? QuantityReceived { get; set; }
    public decimal? QuantityRejected { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}
