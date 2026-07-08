using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IDesignationService
{
    Task<IEnumerable<DesignationDto>> GetAllAsync();
    Task<DesignationDto?> GetByIdAsync(Guid id);
    Task<DesignationDto> CreateAsync(CreateDesignationDto request, Guid userId);
    Task<DesignationDto> UpdateAsync(Guid id, UpdateDesignationDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    /// <summary>Returns active designations as lightweight id/label pairs for dropdowns.</summary>
    Task<IEnumerable<HrLookupItemDto>> GetLookupListAsync();
}
