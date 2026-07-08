namespace Hr.Application.DTOs;

public class CurrencyDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCurrencyDto
{
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string? Symbol { get; set; }
}

public class UpdateCurrencyDto
{
    public string? CurrencyName { get; set; }
    public string? Symbol { get; set; }
    public bool? IsActive { get; set; }
}
