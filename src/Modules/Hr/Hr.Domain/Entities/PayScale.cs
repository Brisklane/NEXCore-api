using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class PayScale : BaseEntity
{
    public string PayScaleCode { get; set; } = string.Empty;
    public string PayScaleName { get; set; } = string.Empty;

    /// <summary>
    /// ISO 4217 alpha-3 code, e.g. "USD". The currency catalogue is owned by the Core module;
    /// HR stores the stable natural key rather than a foreign key into another module's schema.
    /// </summary>
    public string? CurrencyCode { get; set; }

    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public ICollection<Position>? Positions { get; set; }
}
