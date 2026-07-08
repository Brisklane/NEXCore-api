using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IBenefitsPlanService
{
    Task<IEnumerable<BenefitsPlanDto>> GetAllAsync();
    Task<BenefitsPlanDto?> GetByIdAsync(Guid id);
    Task<BenefitsPlanDto> CreateAsync(CreateBenefitsPlanDto request, Guid userId);
    Task<BenefitsPlanDto> UpdateAsync(Guid id, UpdateBenefitsPlanDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
