using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class ScreeningQuestionnaire : BaseEntity
{
    public string QuestionnaireCode { get; set; } = string.Empty;
    public string QuestionnaireName { get; set; } = string.Empty;
    public string QuestionsJson { get; set; } = string.Empty;
    public int VersionNo { get; set; } = 1;

    public ICollection<JobDetail>? JobDetails { get; set; }
    public ICollection<ApplicationDetail>? ApplicationDetails { get; set; }
}
