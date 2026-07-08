using Accounting.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Accounting.Application.Services.Interfaces;

/// <summary>
/// Service layer for FiscalCalendar business logic
/// </summary>
public interface IFiscalCalendarService
{
    Task<FiscalCalendarDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<FiscalCalendarDto>> GetAllAsync(PaginationParams pagination);
    Task<PaginatedResponse<FiscalPeriodDto>> GetCalendarPeriodsAsync(Guid calendarId, PaginationParams pagination);
    Task<FiscalCalendarDto> CreateAsync(CreateFiscalCalendarDto request, Guid userId);
    Task<FiscalCalendarDto> UpdateAsync(Guid id, UpdateFiscalCalendarDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<FiscalCalendarDto> CloseAsync(Guid id, Guid userId);
    Task<FiscalCalendarDto> ReopenAsync(Guid id, Guid userId);
}
