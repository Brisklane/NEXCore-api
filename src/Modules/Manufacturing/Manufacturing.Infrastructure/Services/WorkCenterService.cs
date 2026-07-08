using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Infrastructure.Services;

public class WorkCenterService : IWorkCenterService
{
    private readonly IWorkCenterRepository _repo;
    private readonly IWorkCenterShiftRepository _shiftRepo;
    private readonly ILogger<WorkCenterService> _logger;

    public WorkCenterService(IWorkCenterRepository repo, IWorkCenterShiftRepository shiftRepo, ILogger<WorkCenterService> logger)
    {
        _repo = repo; _shiftRepo = shiftRepo; _logger = logger;
    }

    public async Task<WorkCenterDto> CreateAsync(CreateWorkCenterDto request, Guid userId)
    {
        var entity = new WorkCenter
        {
            Code = request.Code, Name = request.Name,
            CapacityPerHour = request.CapacityPerHour, HourlyMachineCost = request.HourlyMachineCost,
            Description = request.Description, IsActive = true,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        _logger.LogInformation("WorkCenter created: {Code}", entity.Code);
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<WorkCenterDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<WorkCenterDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<WorkCenterDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<WorkCenterDto> UpdateAsync(Guid id, UpdateWorkCenterDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("WorkCenter not found");
        if (request.Name != null) e.Name = request.Name;
        if (request.CapacityPerHour.HasValue) e.CapacityPerHour = request.CapacityPerHour.Value;
        if (request.HourlyMachineCost.HasValue) e.HourlyMachineCost = request.HourlyMachineCost.Value;
        if (request.IsActive.HasValue) e.IsActive = request.IsActive.Value;
        if (request.Description != null) e.Description = request.Description;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("WorkCenter not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }

    public async Task<WorkCenterShiftDto> AddShiftAsync(CreateWorkCenterShiftDto request, Guid userId)
    {
        var entity = new WorkCenterShift
        {
            WorkCenterId = request.WorkCenterId, ShiftName = request.ShiftName,
            StartTime = request.StartTime, EndTime = request.EndTime,
            AvailableHours = request.AvailableHours, CapacityUtilizationPercent = request.CapacityUtilizationPercent,
            WorkingDays = request.WorkingDays, IsActive = true,
            EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo,
            Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _shiftRepo.AddAsync(entity);
        await _shiftRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<WorkCenterShiftDto?> GetShiftByIdAsync(Guid shiftId)
    {
        var e = await _shiftRepo.GetByIdAsync(shiftId);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<WorkCenterShiftDto>> GetShiftsByWorkCenterAsync(Guid workCenterId, PaginationParams pagination)
    {
        var (items, total) = await _shiftRepo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: s => s.WorkCenterId == workCenterId);
        return PaginatedResponse<WorkCenterShiftDto>.Ok(items.Select(ManufacturingMapper.ToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<WorkCenterShiftDto> UpdateShiftAsync(Guid shiftId, UpdateWorkCenterShiftDto request, Guid userId)
    {
        var e = await _shiftRepo.GetByIdAsync(shiftId) ?? throw new InvalidOperationException("Shift not found");
        if (request.ShiftName != null) e.ShiftName = request.ShiftName;
        if (request.StartTime.HasValue) e.StartTime = request.StartTime.Value;
        if (request.EndTime.HasValue) e.EndTime = request.EndTime.Value;
        if (request.AvailableHours.HasValue) e.AvailableHours = request.AvailableHours.Value;
        if (request.CapacityUtilizationPercent.HasValue) e.CapacityUtilizationPercent = request.CapacityUtilizationPercent.Value;
        if (request.WorkingDays != null) e.WorkingDays = request.WorkingDays;
        if (request.IsActive.HasValue) e.IsActive = request.IsActive.Value;
        if (request.EffectiveFrom.HasValue) e.EffectiveFrom = request.EffectiveFrom;
        if (request.EffectiveTo.HasValue) e.EffectiveTo = request.EffectiveTo;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _shiftRepo.Update(e);
        await _shiftRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteShiftAsync(Guid shiftId, Guid userId)
    {
        var e = await _shiftRepo.GetByIdAsync(shiftId) ?? throw new InvalidOperationException("Shift not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _shiftRepo.Update(e);
        await _shiftRepo.SaveChangesAsync();
    }
}
