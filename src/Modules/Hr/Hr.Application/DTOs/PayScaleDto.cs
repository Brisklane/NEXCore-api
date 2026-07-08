namespace Hr.Application.DTOs;

public class PayScaleDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string PayScaleCode { get; set; } = string.Empty;
    public string PayScaleName { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreatePayScaleDto
{
    public string PayScaleCode { get; set; } = string.Empty;
    public string PayScaleName { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}

public class UpdatePayScaleDto
{
    public string? PayScaleName { get; set; }
    public Guid? CurrencyId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public bool? IsActive { get; set; }
}
