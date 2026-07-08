using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class BenefitsPlan : BaseEntity
{
    public string BenefitsPlanCode { get; set; } = string.Empty;
    public string BenefitsPlanName { get; set; } = string.Empty;
    public ICollection<OfferLetterDetail>? OfferLetterDetails { get; set; }
}
