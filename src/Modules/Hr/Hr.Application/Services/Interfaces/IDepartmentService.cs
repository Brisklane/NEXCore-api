using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentDto>> GetAllAsync();
    Task<DepartmentDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<DepartmentDto>> GetByParentAsync(Guid parentDepartmentId);
    Task<DepartmentDto> CreateAsync(CreateDepartmentDto request, Guid userId);
    Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    /// <summary>Returns active departments as lightweight id/label pairs for dropdowns.</summary>
    Task<IEnumerable<HrLookupItemDto>> GetLookupListAsync();
}
