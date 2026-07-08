using Accounting.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Accounting.Application.Services.Interfaces;

/// <summary>
/// Service layer for FiscalPeriod business logic
/// </summary>
public interface IFiscalPeriodService
{
    Task<FiscalPeriodDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<FiscalPeriodDto>> GetByCalendarIdAsync(Guid calendarId, PaginationParams pagination);
    Task<PaginatedResponse<FiscalPeriodDto>> GetAllAsync(PaginationParams pagination);
    Task<FiscalPeriodDto> CreateAsync(CreateFiscalPeriodDto request, Guid userId);
    Task<FiscalPeriodDto> UpdateAsync(Guid id, UpdateFiscalPeriodDto request, Guid userId);
    Task CloseAsync(Guid id, Guid userId);
    Task ReopenAsync(Guid id, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
