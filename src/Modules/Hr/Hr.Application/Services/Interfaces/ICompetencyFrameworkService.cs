using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface ICompetencyFrameworkService
{
    Task<IEnumerable<CompetencyFrameworkDto>> GetAllAsync();
    Task<CompetencyFrameworkDto?> GetByIdAsync(Guid id);
    Task<CompetencyFrameworkDto> CreateAsync(CreateCompetencyFrameworkDto request, Guid userId);
    Task<CompetencyFrameworkDto> UpdateAsync(Guid id, UpdateCompetencyFrameworkDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface ICompetencyFrameworkItemService
{
    Task<IEnumerable<CompetencyFrameworkItemDto>> GetAllAsync();
    Task<CompetencyFrameworkItemDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<CompetencyFrameworkItemDto>> GetByFrameworkIdAsync(Guid frameworkId);
    Task<CompetencyFrameworkItemDto> CreateAsync(CreateCompetencyFrameworkItemDto request, Guid userId);
    Task<CompetencyFrameworkItemDto> UpdateAsync(Guid id, UpdateCompetencyFrameworkItemDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
