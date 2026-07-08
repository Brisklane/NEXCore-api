namespace Crm.Application.DTOs;

public class DealProductDto
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ListPrice { get; set; }
    public decimal? Discount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
}

public class CreateDealProductDto
{
    public Guid ProductId { get; set; }
    public Guid? PricebookEntryId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal? Discount { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
}

public class UpdateDealProductDto : CreateDealProductDto { }

public class DealContactDto
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public Guid ContactId { get; set; }
    public string? ContactName { get; set; }
    public string? Role { get; set; }
    public bool IsPrimary { get; set; }
}

public class CreateDealContactDto
{
    public Guid ContactId { get; set; }
    public string? Role { get; set; }
    public bool IsPrimary { get; set; }
}
