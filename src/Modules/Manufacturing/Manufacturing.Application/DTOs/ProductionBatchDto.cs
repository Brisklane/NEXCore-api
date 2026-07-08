namespace Manufacturing.Application.DTOs;

public class ProductionBatchDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid ProductId { get; set; }
    public required string BatchNumber { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ReTestDate { get; set; }
    public decimal Quantity { get; set; }
    public required string UnitOfMeasure { get; set; }
    public required string Status { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? CertificateOfAnalysis { get; set; }
    public string? VendorBatchNumber { get; set; }
    public bool QualityApproved { get; set; }
    public Guid? InspectionId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductionBatchDto
{
    public Guid ProductionOrderId { get; set; }
    public Guid ProductId { get; set; }
    public required string BatchNumber { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ReTestDate { get; set; }
    public decimal Quantity { get; set; }
    public required string UnitOfMeasure { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? CertificateOfAnalysis { get; set; }
    public string? VendorBatchNumber { get; set; }
    public Guid? InspectionId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateProductionBatchDto
{
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ReTestDate { get; set; }
    public string? Status { get; set; }
    public string? CertificateOfAnalysis { get; set; }
    public bool? QualityApproved { get; set; }
    public Guid? InspectionId { get; set; }
    public string? Notes { get; set; }
}
