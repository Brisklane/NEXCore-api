using Sales.Application.DTOs;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Service layer for Rider business logic.
/// Handles rider registration, availability queries, and assignment dispatch.
/// </summary>
public interface IRiderService
{
    Task<List<RiderDto>> GetAllAsync();
    Task<RiderDto?> GetByIdAsync(Guid id);
    Task<List<RiderDto>> GetAvailableAsync(Guid? branchId);
    Task<List<RiderDto>> GetByBranchAsync(Guid branchId);
    Task<RiderDto> CreateAsync(CreateRiderDto dto);
    Task<RiderDto> UpdateAsync(Guid id, UpdateRiderDto dto);
    Task<RiderAssignmentDto> AssignAsync(CreateRiderAssignmentDto dto);
    Task<RiderAssignmentDto?> GetActiveAssignmentAsync(Guid salesOrderId);
    Task DeleteAsync(Guid id);
}
