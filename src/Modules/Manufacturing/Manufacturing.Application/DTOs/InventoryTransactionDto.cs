namespace Manufacturing.Application.DTOs;

public class InventoryTransactionDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public required string TransactionType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateInventoryTransactionDto
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public required string TransactionType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateInventoryTransactionDto
{
    public string? Notes { get; set; }
}
