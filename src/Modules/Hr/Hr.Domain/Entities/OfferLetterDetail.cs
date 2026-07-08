
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class OfferLetterDetail : BaseEntity
{
    public Guid OfferLetterId { get; set; }
    public string? AllowancesBreakdown { get; set; }
    public decimal? HousingAllowance { get; set; }
    public decimal? TransportAllowance { get; set; }
    public decimal? MedicalAllowance { get; set; }
    public decimal? OtherAllowances { get; set; }
    public decimal? BonusTarget { get; set; }
    public decimal? BonusPercent { get; set; }
    public string? EquityGrant { get; set; }
    public Guid? GradeId { get; set; }
    public Guid? PayScaleId { get; set; }
    public Guid? BenefitsPlanId { get; set; }
    public Guid? AllowancesProfileId { get; set; }
    public string? BenefitsSummary { get; set; }
    public string? TermsDocumentUrl { get; set; }
    public Guid? CounterSignedByEmployeeId { get; set; }
    public DateTime? CounterSignedAt { get; set; }
    public string? DigitalSignatureUrl { get; set; }
    public bool IsDigitallySigned { get; set; }
    public string? LegacyOfferId { get; set; }

    public OfferLetter? OfferLetter { get; set; }
    public Grade? Grade { get; set; }
    public PayScale? PayScale { get; set; }
    public BenefitsPlan? BenefitsPlan { get; set; }
    public AllowancesProfile? AllowancesProfile { get; set; }
    public Employee? CounterSignedByEmployee { get; set; }
}
