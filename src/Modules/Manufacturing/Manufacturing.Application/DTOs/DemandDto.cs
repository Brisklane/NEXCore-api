namespace Manufacturing.Application.DTOs;

public class DemandDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal FulfilledQty { get; set; }
    public DateTime DueDate { get; set; }
    public required string SourceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public required string Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateDemandDto
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime DueDate { get; set; }
    public required string SourceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateDemandDto
{
    public decimal? Quantity { get; set; }
    public decimal? FulfilledQty { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}
