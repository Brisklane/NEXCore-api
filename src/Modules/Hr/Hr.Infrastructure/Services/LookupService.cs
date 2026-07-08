using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class LookupService : ILookupService
{
    private readonly ILookupTypeRepository _typeRepository;
    private readonly ILookupValueRepository _valueRepository;
    private readonly ILogger<LookupService> _logger;

    public LookupService(ILookupTypeRepository typeRepository, ILookupValueRepository valueRepository, ILogger<LookupService> logger)
    {
        _typeRepository = typeRepository;
        _valueRepository = valueRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<LookupTypeDto>> GetAllTypesAsync()
    {
        try
        {
            var types = await _typeRepository.GetAllByTenantAsync();
            return types.Where(t => !t.IsDeleted).Select(MapTypeToDto);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving lookup types"); throw; }
    }

    public async Task<LookupTypeDto?> GetTypeByIdAsync(Guid id)
    {
        try
        {
            var type = await _typeRepository.GetByIdAsync(id);
            return type == null ? null : MapTypeToDto(type);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving lookup type: {LookupTypeId}", id); throw; }
    }

    public async Task<LookupTypeDto> CreateTypeAsync(CreateLookupTypeDto request, Guid userId)
    {
        try
        {
            var type = new LookupType
            {
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                ModuleName = request.ModuleName,
                EntityName = request.EntityName,
                SortOrder = request.SortOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _typeRepository.AddAsync(type);
            await _typeRepository.SaveChangesAsync();
            return MapTypeToDto(type);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating lookup type"); throw; }
    }

    public async Task DeleteTypeAsync(Guid id, Guid userId)
    {
        try
        {
            var type = await _typeRepository.GetByIdAsync(id);
            if (type == null || type.IsDeleted) throw new InvalidOperationException("Lookup type not found");

            type.IsDeleted = true;
            type.DeletedAt = DateTime.UtcNow;
            type.DeletedByUserId = userId;
            _typeRepository.Update(type);
            await _typeRepository.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting lookup type: {LookupTypeId}", id); throw; }
    }

    public async Task<IEnumerable<LookupValueDto>> GetValuesByTypeIdAsync(Guid lookupTypeId)
    {
        try
        {
            var values = await _valueRepository.GetByTypeIdAsync(lookupTypeId);
            return values.Where(v => !v.IsDeleted).Select(MapValueToDto);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving lookup values"); throw; }
    }

    public async Task<LookupValueDto?> GetValueByIdAsync(Guid id)
    {
        try
        {
            var value = await _valueRepository.GetByIdAsync(id);
            return value == null ? null : MapValueToDto(value);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving lookup value: {LookupValueId}", id); throw; }
    }

    public async Task<LookupValueDto> CreateValueAsync(CreateLookupValueDto request, Guid userId)
    {
        try
        {
            var value = new LookupValue
            {
                LookupTypeId = request.LookupTypeId,
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                SortOrder = request.SortOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _valueRepository.AddAsync(value);
            await _valueRepository.SaveChangesAsync();
            return MapValueToDto(value);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating lookup value"); throw; }
    }

    public async Task DeleteValueAsync(Guid id, Guid userId)
    {
        try
        {
            var value = await _valueRepository.GetByIdAsync(id);
            if (value == null || value.IsDeleted) throw new InvalidOperationException("Lookup value not found");

            value.IsDeleted = true;
            value.DeletedAt = DateTime.UtcNow;
            value.DeletedByUserId = userId;
            _valueRepository.Update(value);
            await _valueRepository.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting lookup value: {LookupValueId}", id); throw; }
    }

    private static LookupTypeDto MapTypeToDto(LookupType t) => new()
    {
        Id = t.Id,
        CompanyId = t.CompanyId,
        Code = t.Code,
        Name = t.Name,
        Description = t.Description,
        ModuleName = t.ModuleName,
        EntityName = t.EntityName,
        IsActive = t.IsActive,
        IsSystem = t.IsSystem,
        SortOrder = t.SortOrder
    };

    private static LookupValueDto MapValueToDto(LookupValue v) => new()
    {
        Id = v.Id,
        CompanyId = v.CompanyId,
        LookupTypeId = v.LookupTypeId,
        Code = v.Code,
        Name = v.Name,
        Description = v.Description,
        IsActive = v.IsActive,
        SortOrder = v.SortOrder
    };
}
