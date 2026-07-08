using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class ShiftService : IShiftService
{
    private readonly IShiftRepository _repo;
    private readonly ILogger<ShiftService> _logger;

    public ShiftService(IShiftRepository repo, ILogger<ShiftService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<ShiftDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving shifts"); throw; }
    }

    public async Task<ShiftDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving shift {Id}", id); throw; }
    }

    public async Task<ShiftDto> CreateAsync(CreateShiftDto request, Guid userId)
    {
        try
        {
            var entity = new Shift
            {
                ShiftCode = request.ShiftCode, ShiftName = request.ShiftName,
                StartTime = request.StartTime, EndTime = request.EndTime, TimeZone = request.TimeZone,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Shift created: {Name} ({Id})", entity.ShiftName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating shift"); throw; }
    }

    public async Task<ShiftDto> UpdateAsync(Guid id, UpdateShiftDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Shift not found");
            if (!string.IsNullOrWhiteSpace(request.ShiftName)) entity.ShiftName = request.ShiftName;
            if (request.StartTime.HasValue) entity.StartTime = request.StartTime.Value;
            if (request.EndTime.HasValue) entity.EndTime = request.EndTime.Value;
            if (request.TimeZone != null) entity.TimeZone = request.TimeZone;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating shift {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Shift not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting shift {Id}", id); throw; }
    }

    private static ShiftDto Map(Shift e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, ShiftCode = e.ShiftCode,
        ShiftName = e.ShiftName, StartTime = e.StartTime, EndTime = e.EndTime,
        TimeZone = e.TimeZone, IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
