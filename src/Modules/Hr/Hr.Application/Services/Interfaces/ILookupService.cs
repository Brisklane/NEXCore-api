using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface ILookupService
{
    Task<IEnumerable<LookupTypeDto>> GetAllTypesAsync();
    Task<LookupTypeDto?> GetTypeByIdAsync(Guid id);
    Task<LookupTypeDto> CreateTypeAsync(CreateLookupTypeDto request, Guid userId);
    Task DeleteTypeAsync(Guid id, Guid userId);

    Task<IEnumerable<LookupValueDto>> GetValuesByTypeIdAsync(Guid lookupTypeId);
    Task<LookupValueDto?> GetValueByIdAsync(Guid id);
    Task<LookupValueDto> CreateValueAsync(CreateLookupValueDto request, Guid userId);
    Task DeleteValueAsync(Guid id, Guid userId);
}
