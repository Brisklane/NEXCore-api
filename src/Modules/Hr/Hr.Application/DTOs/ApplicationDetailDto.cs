namespace Hr.Application.DTOs;

public class ApplicationDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ApplicationId { get; set; }
    public string? CoverLetterUrl { get; set; }
    public string? MinQualificationsMet { get; set; }
    public string? OverallRating { get; set; }
    public decimal? ResumeParseScore { get; set; }
    public decimal? ScreeningQuestionnaireScore { get; set; }
    public Guid? ScreeningQuestionnaireId { get; set; }
    public Guid? ScreeningStatusLookupValueId { get; set; }
    public DateTime? ScreeningCompletedDate { get; set; }
    public Guid? AssignedHiringManagerEmployeeId { get; set; }
    public Guid? ReviewedByEmployeeId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RecruiterNotes { get; set; }
    public DateTime? NextFollowUpDate { get; set; }
    public DateTime? ExpectedJoinDate { get; set; }
    public DateTime? ActualJoinDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateApplicationDetailDto
{
    public Guid ApplicationId { get; set; }
    public string? CoverLetterUrl { get; set; }
    public string? MinQualificationsMet { get; set; }
    public string? OverallRating { get; set; }
    public Guid? ScreeningQuestionnaireId { get; set; }
    public Guid? AssignedHiringManagerEmployeeId { get; set; }
    public string? RecruiterNotes { get; set; }
    public DateTime? NextFollowUpDate { get; set; }
    public DateTime? ExpectedJoinDate { get; set; }
}

public class UpdateApplicationDetailDto
{
    public string? CoverLetterUrl { get; set; }
    public string? MinQualificationsMet { get; set; }
    public string? OverallRating { get; set; }
    public decimal? ResumeParseScore { get; set; }
    public decimal? ScreeningQuestionnaireScore { get; set; }
    public Guid? ScreeningQuestionnaireId { get; set; }
    public Guid? ScreeningStatusLookupValueId { get; set; }
    public DateTime? ScreeningCompletedDate { get; set; }
    public Guid? AssignedHiringManagerEmployeeId { get; set; }
    public Guid? ReviewedByEmployeeId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RecruiterNotes { get; set; }
    public DateTime? NextFollowUpDate { get; set; }
    public DateTime? ExpectedJoinDate { get; set; }
    public DateTime? ActualJoinDate { get; set; }
}
