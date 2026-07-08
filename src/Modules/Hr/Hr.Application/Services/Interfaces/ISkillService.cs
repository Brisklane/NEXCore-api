using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface ISkillCategoryService
{
    Task<IEnumerable<SkillCategoryDto>> GetAllAsync();
    Task<SkillCategoryDto?> GetByIdAsync(Guid id);
    Task<SkillCategoryDto> CreateAsync(CreateSkillCategoryDto request, Guid userId);
    Task<SkillCategoryDto> UpdateAsync(Guid id, UpdateSkillCategoryDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface ISkillService
{
    Task<IEnumerable<SkillDto>> GetAllAsync();
    Task<SkillDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<SkillDto>> GetByCategoryAsync(Guid skillCategoryId);
    Task<SkillDto> CreateAsync(CreateSkillDto request, Guid userId);
    Task<SkillDto> UpdateAsync(Guid id, UpdateSkillDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
