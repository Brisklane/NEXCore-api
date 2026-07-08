using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class JobLocationService : IJobLocationService
{
    private readonly IJobLocationRepository _repo;
    private readonly ILogger<JobLocationService> _logger;

    public JobLocationService(IJobLocationRepository repo, ILogger<JobLocationService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<JobLocationDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving job locations"); throw; }
    }

    public async Task<JobLocationDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving job location {Id}", id); throw; }
    }

    public async Task<JobLocationDto> CreateAsync(CreateJobLocationDto request, Guid userId)
    {
        try
        {
            var entity = new JobLocation
            {
                LocationCode = request.LocationCode, LocationName = request.LocationName,
                Address = request.Address, City = request.City, StateProvince = request.StateProvince,
                PostalCode = request.PostalCode, Country = request.Country,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Job location created: {Name} ({Id})", entity.LocationName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating job location"); throw; }
    }

    public async Task<JobLocationDto> UpdateAsync(Guid id, UpdateJobLocationDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Job location not found");
            if (!string.IsNullOrWhiteSpace(request.LocationName)) entity.LocationName = request.LocationName;
            if (request.Address != null) entity.Address = request.Address;
            if (request.City != null) entity.City = request.City;
            if (request.StateProvince != null) entity.StateProvince = request.StateProvince;
            if (request.PostalCode != null) entity.PostalCode = request.PostalCode;
            if (request.Country != null) entity.Country = request.Country;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating job location {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Job location not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting job location {Id}", id); throw; }
    }

    private static JobLocationDto Map(JobLocation e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, LocationCode = e.LocationCode,
        LocationName = e.LocationName, Address = e.Address, City = e.City,
        StateProvince = e.StateProvince, PostalCode = e.PostalCode, Country = e.Country,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
