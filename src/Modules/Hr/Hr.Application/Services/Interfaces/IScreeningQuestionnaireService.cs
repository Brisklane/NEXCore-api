using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IScreeningQuestionnaireService
{
    Task<IEnumerable<ScreeningQuestionnaireDto>> GetAllAsync();
    Task<ScreeningQuestionnaireDto?> GetByIdAsync(Guid id);
    Task<ScreeningQuestionnaireDto> CreateAsync(CreateScreeningQuestionnaireDto request, Guid userId);
    Task<ScreeningQuestionnaireDto> UpdateAsync(Guid id, UpdateScreeningQuestionnaireDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
