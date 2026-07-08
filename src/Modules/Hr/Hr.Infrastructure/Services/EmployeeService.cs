using Hr.Application.DTOs;
using Hr.Application.Enums;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repo;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IEmployeeRepository repo, ILogger<EmployeeService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving employees"); throw; }
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving employee {Id}", id); throw; }
    }

    public async Task<IEnumerable<EmployeeDto>> GetByDepartmentAsync(Guid departmentId)
    {
        try { return (await _repo.GetByDepartmentAsync(departmentId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving employees for department {DeptId}", departmentId); throw; }
    }

    public async Task<IEnumerable<EmployeeDto>> GetByDesignationAsync(Guid designationId)
    {
        try { return (await _repo.GetByDesignationAsync(designationId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving employees for designation {DesignationId}", designationId); throw; }
    }

    public async Task<IEnumerable<EmployeeDto>> GetDirectReportsAsync(Guid managerId)
    {
        try { return (await _repo.GetDirectReportsAsync(managerId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving direct reports for manager {ManagerId}", managerId); throw; }
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto request, Guid userId)
    {
        try
        {
            var entity = new Employee
            {
                EmployeeCode = request.EmployeeCode, FirstName = request.FirstName,
                LastName = request.LastName, MiddleName = request.MiddleName,
                Email = request.Email, Phone = request.Phone,
                DepartmentId = request.DepartmentId, DesignationId = request.DesignationId,
                PositionId = request.PositionId, ReportingManagerId = request.ReportingManagerId,
                JobLocationId = request.JobLocationId,
                Status = request.Status?.ToString(), JoinDate = request.JoinDate,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Employee created: {Code} ({Id})", entity.EmployeeCode, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating employee"); throw; }
    }

    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Employee not found");
            if (!string.IsNullOrWhiteSpace(request.FirstName)) entity.FirstName = request.FirstName;
            if (!string.IsNullOrWhiteSpace(request.LastName)) entity.LastName = request.LastName;
            if (request.MiddleName != null) entity.MiddleName = request.MiddleName;
            if (request.Phone != null) entity.Phone = request.Phone;
            if (request.DepartmentId.HasValue) entity.DepartmentId = request.DepartmentId;
            if (request.DesignationId.HasValue) entity.DesignationId = request.DesignationId;
            if (request.PositionId.HasValue) entity.PositionId = request.PositionId;
            if (request.ReportingManagerId.HasValue) entity.ReportingManagerId = request.ReportingManagerId;
            if (request.JobLocationId.HasValue) entity.JobLocationId = request.JobLocationId;
            if (request.Status.HasValue) entity.Status = request.Status.Value.ToString();
            if (request.ExitDate.HasValue) entity.ExitDate = request.ExitDate;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating employee {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Employee not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting employee {Id}", id); throw; }
    }

    private static EmployeeDto Map(Employee e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, EmployeeCode = e.EmployeeCode,
        FirstName = e.FirstName, LastName = e.LastName, MiddleName = e.MiddleName,
        Email = e.Email, Phone = e.Phone, DepartmentId = e.DepartmentId,
        DesignationId = e.DesignationId, PositionId = e.PositionId,
        ReportingManagerId = e.ReportingManagerId, JobLocationId = e.JobLocationId,
        Status = Enum.TryParse<EmployeeStatus>(e.Status, out var s) ? s : null,
        JoinDate = e.JoinDate, ExitDate = e.ExitDate,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
