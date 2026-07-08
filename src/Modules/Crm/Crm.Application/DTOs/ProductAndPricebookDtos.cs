namespace Crm.Application.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ProductFamily { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? QuantityUnit { get; set; }
    public decimal? QuantityUnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ProductFamily { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? QuantityUnit { get; set; }
    public decimal? QuantityUnitPrice { get; set; }
}

public class UpdateProductDto : CreateProductDto { }

public class PricebookDto
{
    public Guid Id { get; set; }
    public string PricebookName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsStandard { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
}

public class CreatePricebookDto
{
    public string PricebookName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string CurrencyCode { get; set; } = "USD";
}

public class UpdatePricebookDto : CreatePricebookDto { }

public class PricebookEntryDto
{
    public Guid Id { get; set; }
    public Guid PricebookId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
    public bool UseStandardPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

public class CreatePricebookEntryDto
{
    public Guid ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public bool UseStandardPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

public class UpdatePricebookEntryDto
{
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
    public bool UseStandardPrice { get; set; }
}
