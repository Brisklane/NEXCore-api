using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<EmployeeDto>> GetByDepartmentAsync(Guid departmentId);
    Task<IEnumerable<EmployeeDto>> GetByDesignationAsync(Guid designationId);
    Task<IEnumerable<EmployeeDto>> GetDirectReportsAsync(Guid managerId);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto request, Guid userId);
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
