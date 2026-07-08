namespace Hr.Application.DTOs;

public class ScreeningQuestionnaireDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string QuestionnaireCode { get; set; } = string.Empty;
    public string QuestionnaireName { get; set; } = string.Empty;
    public string QuestionsJson { get; set; } = "[]";
    public bool IsActive { get; set; }
    public int VersionNo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateScreeningQuestionnaireDto
{
    public string QuestionnaireCode { get; set; } = string.Empty;
    public string QuestionnaireName { get; set; } = string.Empty;
    public string QuestionsJson { get; set; } = "[]";
}

public class UpdateScreeningQuestionnaireDto
{
    public string? QuestionnaireName { get; set; }
    public string? QuestionsJson { get; set; }
    public bool? IsActive { get; set; }
}
