
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class ApplicationDetail : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public string? CoverLetterUrl { get; set; }
    public string? MinQualificationsMet { get; set; }
    public string? OverallRating { get; set; }
    public decimal? ResumeParseScore { get; set; }
    public decimal? ScreeningQuestionnaireScore { get; set; }
    public Guid? ScreeningQuestionnaireId { get; set; }
    public Guid? ScreeningStatusLookupValueId { get; set; }
    public DateTime? ScreeningCompletedDate { get; set; }
    public Guid? ScreeningCompletedByEmployeeId { get; set; }
    public Guid? AssignedHiringManagerEmployeeId { get; set; }
    public Guid? ReviewedByEmployeeId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? HiringCommitteeNotes { get; set; }
    public string? RecruiterNotes { get; set; }
    public string? ApplicationPortalNotes { get; set; }
    public string? TagsList { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime? NextFollowUpDate { get; set; }
    public DateTime? CandidateResponseDeadline { get; set; }
    public DateTime? LastContactedDate { get; set; }
    public Guid? LastContactedByEmployeeId { get; set; }
    public decimal? CandidateSatisfactionScore { get; set; }
    public Guid? RejectionReasonLookupValueId { get; set; }
    public Guid? WithdrawalReasonLookupValueId { get; set; }
    public Guid? RejectionSentByEmployeeId { get; set; }
    public bool RejectionNotificationSent { get; set; }
    public DateTime? RejectedDate { get; set; }
    public DateTime? WithdrawnDate { get; set; }
    public DateTime? ExpectedJoinDate { get; set; }
    public DateTime? ActualJoinDate { get; set; }
    public string? LegacyApplicationId { get; set; }
    public string? LegacySourceSystem { get; set; }

    public Application? Application { get; set; }
    public ScreeningQuestionnaire? ScreeningQuestionnaire { get; set; }
    public Employee? ScreeningCompletedByEmployee { get; set; }
    public Employee? AssignedHiringManagerEmployee { get; set; }
    public Employee? ReviewedByEmployee { get; set; }
    public Employee? LastContactedByEmployee { get; set; }
    public Employee? RejectionSentByEmployee { get; set; }
    public LookupValue? ScreeningStatusLookupValue { get; set; }
    public LookupValue? RejectionReasonLookupValue { get; set; }
    public LookupValue? WithdrawalReasonLookupValue { get; set; }
}
