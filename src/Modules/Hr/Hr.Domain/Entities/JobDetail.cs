
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class JobDetail : BaseEntity
{
    public Guid JobId { get; set; }
    public Guid? JobTemplateId { get; set; }

    public Guid? JobLocationId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? JobFamilyId { get; set; }
    public Guid? JobFunctionId { get; set; }
    public Guid? GradeId { get; set; }
    public Guid? PayScaleId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? ShiftId { get; set; }
    public Guid? ReplacedEmployeeId { get; set; }
    public Guid? ReportingManagerEmployeeId { get; set; }

    public string? WorkerCategory { get; set; }
    public string? HiringType { get; set; }
    public string? VacancyReason { get; set; }

    public string? Requirements { get; set; }
    public string? RequiredSkills { get; set; }
    public string? PreferredSkills { get; set; }
    public string? Responsibilities { get; set; }
    public string? EducationRequirements { get; set; }
    public string? LanguagesRequired { get; set; }
    public decimal? MinExperienceYears { get; set; }
    public decimal? MaxExperienceYears { get; set; }

    public bool BonusEligible { get; set; }
    public Guid? AllowancesProfileId { get; set; }
    public decimal? BudgetApprovedAmount { get; set; }
    public decimal? ForecastedHireCost { get; set; }

    public bool PublishExternallyFlag { get; set; }
    public bool CareerSiteVisible { get; set; }
    public bool InternalOnlyFlag { get; set; }

    public bool BackgroundCheckRequired { get; set; }
    public bool DrugTestRequired { get; set; }
    public string? SecurityClearanceLevel { get; set; }
    public bool VisaSponsorshipAvailable { get; set; }
    public bool ConfidentialJobFlag { get; set; }
    public bool DiversityTargetFlag { get; set; }
    public string? EEOCategory { get; set; }
    public bool UnionRoleFlag { get; set; }
    public decimal? TravelRequiredPercent { get; set; }

    public string? SourceCampaignCode { get; set; }
    public bool ReferralBonusEligible { get; set; }
    public string? Tags { get; set; }
    public string? InternalJobTitle { get; set; }
    public string? ExternalJobCode { get; set; }
    public string? LegacySourceSystem { get; set; }
    public string? LegacyRecordId { get; set; }

    public Guid? ScreeningQuestionnaireId { get; set; }
    public Guid? ClosedByEmployeeId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CancelReason { get; set; }

    // Navigation
    public Job? Job { get; set; }
    public JobTemplate? JobTemplate { get; set; }
    public JobLocation? JobLocation { get; set; }
    public Position? Position { get; set; }
    public JobFamily? JobFamily { get; set; }
    public JobFunction? JobFunction { get; set; }
    public Grade? Grade { get; set; }
    public PayScale? PayScale { get; set; }
    public CostCenter? CostCenter { get; set; }
    public Shift? Shift { get; set; }
    public Employee? ReplacedEmployee { get; set; }
    public Employee? ReportingManagerEmployee { get; set; }
    public Employee? ClosedByEmployee { get; set; }
    public AllowancesProfile? AllowancesProfile { get; set; }
    public ScreeningQuestionnaire? ScreeningQuestionnaire { get; set; }
}
