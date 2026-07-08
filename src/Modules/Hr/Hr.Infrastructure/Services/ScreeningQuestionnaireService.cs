using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class ScreeningQuestionnaireService : IScreeningQuestionnaireService
{
    private readonly IScreeningQuestionnaireRepository _repo;
    private readonly ILogger<ScreeningQuestionnaireService> _logger;

    public ScreeningQuestionnaireService(IScreeningQuestionnaireRepository repo, ILogger<ScreeningQuestionnaireService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<ScreeningQuestionnaireDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving screening questionnaires"); throw; }
    }

    public async Task<ScreeningQuestionnaireDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving screening questionnaire {Id}", id); throw; }
    }

    public async Task<ScreeningQuestionnaireDto> CreateAsync(CreateScreeningQuestionnaireDto request, Guid userId)
    {
        try
        {
            var entity = new ScreeningQuestionnaire
            {
                QuestionnaireCode = request.QuestionnaireCode, QuestionnaireName = request.QuestionnaireName,
                QuestionsJson = request.QuestionsJson, IsActive = true, VersionNo = 1,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Screening questionnaire created: {Name} ({Id})", entity.QuestionnaireName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating screening questionnaire"); throw; }
    }

    public async Task<ScreeningQuestionnaireDto> UpdateAsync(Guid id, UpdateScreeningQuestionnaireDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Screening questionnaire not found");
            if (!string.IsNullOrWhiteSpace(request.QuestionnaireName)) entity.QuestionnaireName = request.QuestionnaireName;
            if (request.QuestionsJson != null) { entity.QuestionsJson = request.QuestionsJson; entity.VersionNo++; }
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating screening questionnaire {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Screening questionnaire not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting screening questionnaire {Id}", id); throw; }
    }

    private static ScreeningQuestionnaireDto Map(ScreeningQuestionnaire e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, QuestionnaireCode = e.QuestionnaireCode,
        QuestionnaireName = e.QuestionnaireName, QuestionsJson = e.QuestionsJson,
        IsActive = e.IsActive, VersionNo = e.VersionNo, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
