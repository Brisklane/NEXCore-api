namespace Hr.Application.DTOs;

public class OfferLetterDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
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
    public bool IsDigitallySigned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateOfferLetterDetailDto
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
}

public class UpdateOfferLetterDetailDto
{
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
    public bool? IsDigitallySigned { get; set; }
}
