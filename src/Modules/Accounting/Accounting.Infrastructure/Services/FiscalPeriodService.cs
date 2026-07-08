using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implementation of fiscal period service
/// Handles all business logic for fiscal period operations
/// Coordinates between controllers and repositories
/// Applies domain rules and validations
/// </summary>
public class FiscalPeriodService : IFiscalPeriodService
{
    private readonly IFiscalPeriodRepository _repository;
    private readonly ILogger<FiscalPeriodService> _logger;

    public FiscalPeriodService(IFiscalPeriodRepository repository, ILogger<FiscalPeriodService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<FiscalPeriodDto> CreateAsync(CreateFiscalPeriodDto request, Guid userId)
    {
        try
        {
            var period = new FiscalPeriod
            {
                FiscalCalendarId = request.FiscalCalendarId,
                PeriodName = request.PeriodName,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Description = request.Description,
                IsClosed = false,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(period);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Fiscal period created: {PeriodName} (ID: {PeriodId})", 
                period.PeriodName, period.Id);

            return MapToDto(period);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fiscal period: {PeriodName}", request.PeriodName);
            throw;
        }
    }

    public async Task<FiscalPeriodDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var period = await _repository.GetByIdAsync(id);
            if (period == null || period.IsDeleted)
                return null;

            return MapToDto(period);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal period: {PeriodId}", id);
            throw;
        }
    }

    public async Task<PaginatedResponse<FiscalPeriodDto>> GetAllAsync(PaginationParams pagination)
    {
        try
        {
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                orderBy: q => q.OrderBy(p => p.StartDate));
            return PaginatedResponse<FiscalPeriodDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal periods");
            throw;
        }
    }

    public async Task<PaginatedResponse<FiscalPeriodDto>> GetByCalendarIdAsync(Guid calendarId, PaginationParams pagination)
    {
        try
        {
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: p => p.FiscalCalendarId == calendarId,
                orderBy: q => q.OrderBy(p => p.StartDate));
            return PaginatedResponse<FiscalPeriodDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal periods for calendar: {CalendarId}", calendarId);
            throw;
        }
    }

    public async Task<FiscalPeriodDto?> GetCurrentPeriodAsync(Guid calendarId)
    {
        try
        {
            var period = await _repository.GetCurrentPeriodAsync(calendarId);
            if (period == null || period.IsDeleted)
                return null;

            return MapToDto(period);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current fiscal period: {CalendarId}", calendarId);
            throw;
        }
    }

    public async Task<FiscalPeriodDto> UpdateAsync(Guid id, UpdateFiscalPeriodDto request, Guid userId)
    {
        try
        {
            var period = await _repository.GetByIdAsync(id);
            if (period == null || period.IsDeleted)
                throw new InvalidOperationException("Fiscal period not found");

            if (!string.IsNullOrWhiteSpace(request.PeriodName))
                period.PeriodName = request.PeriodName;

            if (request.StartDate.HasValue)
                period.StartDate = request.StartDate.Value;

            if (request.EndDate.HasValue)
                period.EndDate = request.EndDate.Value;

            if (request.Description != null)
                period.Description = request.Description;

            period.UpdatedAt = DateTime.UtcNow;
            period.UpdatedByUserId = userId;

            _repository.Update(period);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Fiscal period updated: {PeriodName} (ID: {PeriodId})", 
                period.PeriodName, period.Id);

            return MapToDto(period);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fiscal period: {PeriodId}", id);
            throw;
        }
    }

    public async Task CloseAsync(Guid id, Guid userId)
    {
        try
        {
            var period = await _repository.GetByIdAsync(id);
            if (period == null || period.IsDeleted)
                throw new InvalidOperationException("Fiscal period not found");

            if (period.IsClosed)
                throw new InvalidOperationException("Fiscal period is already closed");

            period.IsClosed = true;
            period.ClosedByUserId = userId;
            period.ClosedAt = DateTime.UtcNow;
            period.UpdatedAt = DateTime.UtcNow;
            period.UpdatedByUserId = userId;

            _repository.Update(period);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Fiscal period closed: {PeriodName} (ID: {PeriodId})", 
                period.PeriodName, period.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing fiscal period: {PeriodId}", id);
            throw;
        }
    }

    public async Task ReopenAsync(Guid id, Guid userId)
    {
        try
        {
            var period = await _repository.GetByIdAsync(id);
            if (period == null || period.IsDeleted)
                throw new InvalidOperationException("Fiscal period not found");

            if (!period.IsClosed)
                throw new InvalidOperationException("Fiscal period is not closed");

            period.IsClosed = false;
            period.ClosedByUserId = null;
            period.ClosedAt = null;
            period.UpdatedAt = DateTime.UtcNow;
            period.UpdatedByUserId = userId;

            _repository.Update(period);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Fiscal period reopened: {PeriodName} (ID: {PeriodId})", 
                period.PeriodName, period.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening fiscal period: {PeriodId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var period = await _repository.GetByIdAsync(id);
            if (period == null || period.IsDeleted)
                throw new InvalidOperationException("Fiscal period not found");

            period.IsDeleted = true;
            period.DeletedAt = DateTime.UtcNow;
            period.DeletedByUserId = userId;

            _repository.Update(period);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Fiscal period deleted: {PeriodName} (ID: {PeriodId})", 
                period.PeriodName, period.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fiscal period: {PeriodId}", id);
            throw;
        }
    }

    private static FiscalPeriodDto MapToDto(FiscalPeriod period)
    {
        return new FiscalPeriodDto
        {
            Id = period.Id,
            CompanyId = period.CompanyId,
            FiscalCalendarId = period.FiscalCalendarId,
            PeriodName = period.PeriodName,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            IsClosed = period.IsClosed,
            Description = period.Description
        };
    }
}
