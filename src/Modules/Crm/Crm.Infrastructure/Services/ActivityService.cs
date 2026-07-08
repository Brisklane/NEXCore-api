using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Crm.Infrastructure.Services;

public class ActivityService : IActivityService
{
    private readonly IActivityRepository _repository;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(IActivityRepository repository, ILogger<ActivityService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ActivityDto> CreateAsync(CreateActivityDto dto, Guid userId)
    {
        var entity = new Activity
        {
            Type = dto.Type, Subject = dto.Subject, DueDate = dto.DueDate,
            StartDateTime = dto.StartDateTime, EndDateTime = dto.EndDateTime,
            Status = dto.Status, Priority = dto.Priority, DurationMinutes = dto.DurationMinutes,
            CallType = dto.CallType, CallPurpose = dto.CallPurpose, CallResult = dto.CallResult,
            Description = dto.Description, Comments = dto.Comments,
            RelatedToId = dto.RelatedToId, RelatedToType = dto.RelatedToType,
            NameId = dto.NameId, NameType = dto.NameType,
            AssignedToId = dto.AssignedToId ?? userId,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<ActivityDto?> GetByIdAsync(Guid id)
    {
        var e = await _repository.GetByIdAsync(id);
        return e is null ? null : MapToDto(e);
    }

    public async Task<PaginatedResponse<ActivityDto>> GetPagedAsync(PaginationParams pagination, string? type = null)
    {
        var (items, total) = await _repository.GetPagedByTypeAsync(pagination.PageNumber, pagination.PageSize, type);
        return PaginatedResponse<ActivityDto>.Ok(items.OrderByDescending(x => x.CreatedAt).Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<IEnumerable<ActivityDto>> GetByRelatedEntityAsync(Guid relatedToId, string relatedToType)
    {
        var items = await _repository.GetByRelatedEntityAsync(relatedToId, relatedToType);
        return items.OrderByDescending(x => x.CreatedAt).Select(MapToDto);
    }

    public async Task<ActivityDto> UpdateAsync(Guid id, UpdateActivityDto dto, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Activity not found");
        entity.Type = dto.Type; entity.Subject = dto.Subject; entity.DueDate = dto.DueDate;
        entity.StartDateTime = dto.StartDateTime; entity.EndDateTime = dto.EndDateTime;
        entity.Status = dto.Status; entity.Priority = dto.Priority; entity.DurationMinutes = dto.DurationMinutes;
        entity.CallType = dto.CallType; entity.CallPurpose = dto.CallPurpose; entity.CallResult = dto.CallResult;
        entity.Description = dto.Description; entity.Comments = dto.Comments;
        entity.RelatedToId = dto.RelatedToId; entity.RelatedToType = dto.RelatedToType;
        entity.NameId = dto.NameId; entity.NameType = dto.NameType; entity.AssignedToId = dto.AssignedToId;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Activity not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    private static ActivityDto MapToDto(Activity e) => new()
    {
        Id = e.Id, Type = e.Type, Subject = e.Subject, DueDate = e.DueDate,
        StartDateTime = e.StartDateTime, EndDateTime = e.EndDateTime,
        Status = e.Status, Priority = e.Priority, DurationMinutes = e.DurationMinutes,
        CallType = e.CallType, CallPurpose = e.CallPurpose, CallResult = e.CallResult,
        Description = e.Description, Comments = e.Comments,
        RelatedToId = e.RelatedToId, RelatedToType = e.RelatedToType,
        NameId = e.NameId, NameType = e.NameType,
        AssignedToId = e.AssignedToId, CreatedAt = e.CreatedAt
    };
}
