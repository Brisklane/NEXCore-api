using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class AllowancesProfile : BaseEntity
{
    public string AllowancesProfileCode { get; set; } = string.Empty;
    public string AllowancesProfileName { get; set; } = string.Empty;
    public ICollection<OfferLetterDetail>? OfferLetterDetails { get; set; }
}