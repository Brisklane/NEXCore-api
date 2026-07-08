namespace Manufacturing.Application.DTOs;

public class MaterialIssueDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal QuantityIssued { get; set; }
    public decimal QuantityReturned { get; set; }
    public DateTime IssuedAt { get; set; }
    public Guid IssuedById { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateMaterialIssueDto
{
    public Guid ProductionOrderId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal QuantityIssued { get; set; }
    public DateTime IssuedAt { get; set; }
    public string? Notes { get; set; }
}

public class UpdateMaterialIssueDto
{
    public decimal? QuantityReturned { get; set; }
    public string? Notes { get; set; }
}
