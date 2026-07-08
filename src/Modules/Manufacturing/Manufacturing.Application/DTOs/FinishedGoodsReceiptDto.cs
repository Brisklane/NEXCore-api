namespace Manufacturing.Application.DTOs;

public class FinishedGoodsReceiptDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityReceived { get; set; }
    public Guid WarehouseId { get; set; }
    public string? BatchNo { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateFinishedGoodsReceiptDto
{
    public Guid ProductionOrderId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityReceived { get; set; }
    public Guid WarehouseId { get; set; }
    public string? BatchNo { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string? Notes { get; set; }
}

public class UpdateFinishedGoodsReceiptDto
{
    public decimal? QuantityReceived { get; set; }
    public string? BatchNo { get; set; }
    public string? Notes { get; set; }
}
