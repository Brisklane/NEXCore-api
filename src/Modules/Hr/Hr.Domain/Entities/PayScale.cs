using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class PayScale : BaseEntity
{
    public string PayScaleCode { get; set; } = string.Empty;
    public string PayScaleName { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public Currency? Currency { get; set; }
    public ICollection<Position>? Positions { get; set; }
}
