
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class InterviewFeedbackTemplate : BaseEntity
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Guid InterviewTypeLookupValueId { get; set; }
    public string QuestionsJson { get; set; } = "[]";
    public string RatingScale { get; set; } = "1-5";
    public int VersionNo { get; set; } = 1;
    public Guid? CompetencyFrameworkId { get; set; }
    public Guid? JobFamilyId { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsMandatory { get; set; }
    public decimal? PassingScore { get; set; }
    public decimal? WeightInOverallScore { get; set; }
    public string? InstructionsForInterviewer { get; set; }
    public string? InstructionsForCandidate { get; set; }
    public string? SkillCriteriaJson { get; set; }
}
