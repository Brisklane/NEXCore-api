namespace Manufacturing.Application.DTOs;

public class PlannedOrderDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal PlannedQty { get; set; }
    public DateTime RequiredDate { get; set; }
    public required string SourceType { get; set; }
    public required string Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreatePlannedOrderDto
{
    public Guid ProductId { get; set; }
    public decimal PlannedQty { get; set; }
    public DateTime RequiredDate { get; set; }
    public required string SourceType { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePlannedOrderDto
{
    public decimal? PlannedQty { get; set; }
    public DateTime? RequiredDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}
