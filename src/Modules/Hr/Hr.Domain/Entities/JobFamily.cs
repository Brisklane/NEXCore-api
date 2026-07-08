using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class JobFamily : BaseEntity
{
    public string JobFamilyCode { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public ICollection<Designation>? Designations { get; set; }
    public ICollection<Position>? Positions { get; set; }
    public ICollection<JobTemplate>? JobTemplates { get; set; }
    public ICollection<CompetencyFramework>? CompetencyFrameworks { get; set; }
    public ICollection<InterviewFeedbackTemplate>? InterviewFeedbackTemplates { get; set; }
}
