using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class Currency : BaseEntity
{
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public ICollection<PayScale>? PayScales { get; set; }
    public ICollection<Job>? Jobs { get; set; }
    public ICollection<OfferLetter>? OfferLetters { get; set; }
    public ICollection<OfferLetterDetail>? OfferLetterDetails { get; set; }
}
