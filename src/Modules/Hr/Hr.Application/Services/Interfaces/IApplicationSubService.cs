using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IApplicationDetailService
{
    Task<IEnumerable<ApplicationDetailDto>> GetAllAsync();
    Task<ApplicationDetailDto?> GetByIdAsync(Guid id);
    Task<ApplicationDetailDto?> GetByApplicationIdAsync(Guid applicationId);
    Task<ApplicationDetailDto> CreateAsync(CreateApplicationDetailDto request, Guid userId);
    Task<ApplicationDetailDto> UpdateAsync(Guid id, UpdateApplicationDetailDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IApplicationComplianceService
{
    Task<IEnumerable<ApplicationComplianceDto>> GetAllAsync();
    Task<ApplicationComplianceDto?> GetByIdAsync(Guid id);
    Task<ApplicationComplianceDto?> GetByApplicationIdAsync(Guid applicationId);
    Task<ApplicationComplianceDto> CreateAsync(CreateApplicationComplianceDto request, Guid userId);
    Task<ApplicationComplianceDto> UpdateAsync(Guid id, UpdateApplicationComplianceDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
