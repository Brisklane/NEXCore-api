using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implementation of fiscal calendar service
/// </summary>
public class FiscalCalendarService : IFiscalCalendarService
{
    private readonly IFiscalCalendarRepository _repository;
    private readonly IFiscalPeriodRepository _periodRepository;
    private readonly IJournalEntryRepository _journalRepository;
    private readonly ILogger<FiscalCalendarService> _logger;

    public FiscalCalendarService(
        IFiscalCalendarRepository repository,
        IFiscalPeriodRepository periodRepository,
        IJournalEntryRepository journalRepository,
        ILogger<FiscalCalendarService> logger)
    {
        _repository = repository;
        _periodRepository = periodRepository;
        _journalRepository = journalRepository;
        _logger = logger;
    }

    /// <summary>
    /// A fiscal year "has transactions" when any journal entry's posting date falls within its range.
    /// JournalEntry is tenant-scoped and soft-delete filtered by the repository.
    /// </summary>
    private async Task<bool> HasTransactionsAsync(FiscalCalendar calendar)
    {
        var count = await _journalRepository.CountAsync(
            e => e.PostingDate >= calendar.StartDate && e.PostingDate <= calendar.EndDate);
        return count > 0;
    }

    public async Task<FiscalCalendarDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var calendar = await _repository.GetByIdAsync(id);
            return calendar == null ? null : await ToDtoAsync(calendar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal calendar: {CalendarId}", id);
            throw;
        }
    }

    public async Task<PaginatedResponse<FiscalCalendarDto>> GetAllAsync(PaginationParams pagination)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: c => string.IsNullOrEmpty(search) ||
                    c.Name.ToLower().Contains(search) ||
                    (c.Description != null && c.Description.ToLower().Contains(search)),
                orderBy: q => q.ApplyOrderNewestFirst(
                    pagination.SortBy,
                    pagination.SortDirection,
                    "StartDate"));
            var dtos = new List<FiscalCalendarDto>();
            foreach (var c in items)
                dtos.Add(await ToDtoAsync(c));
            return PaginatedResponse<FiscalCalendarDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal calendars");
            throw;
        }
    }

    public async Task<FiscalCalendarDto> CreateAsync(CreateFiscalCalendarDto request, Guid userId)
    {
        try
        {
            if (request.EndDate <= request.StartDate)
                throw new InvalidOperationException("End date must be after the start date.");

            // Reject a year whose range overlaps an existing (non-deleted) fiscal year
            var existing = await _repository.GetAllAsync();
            if (existing.Any(c => request.StartDate <= c.EndDate && request.EndDate >= c.StartDate))
                throw new InvalidOperationException("The date range overlaps an existing fiscal year.");

            var calendar = new FiscalCalendar
            {
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Description = request.Description,
                IsActive = true, // open
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(calendar);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Fiscal calendar created: {Name} (ID: {Id})", calendar.Name, calendar.Id);

            return await ToDtoAsync(calendar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fiscal calendar: {Name}", request.Name);
            throw;
        }
    }

    public async Task<FiscalCalendarDto> UpdateAsync(Guid id, UpdateFiscalCalendarDto request, Guid userId)
    {
        try
        {
            var calendar = await _repository.GetByIdAsync(id);
            if (calendar == null)
                throw new InvalidOperationException("Fiscal calendar not found");

            if (!calendar.IsActive)
                throw new InvalidOperationException("A closed fiscal year is read-only and cannot be edited. Reopen it first.");

            if (await HasTransactionsAsync(calendar))
                throw new InvalidOperationException("This fiscal year has transactions and can no longer be edited.");

            var newStart = request.StartDate ?? calendar.StartDate;
            var newEnd = request.EndDate ?? calendar.EndDate;
            if (newEnd <= newStart)
                throw new InvalidOperationException("End date must be after the start date.");

            // If the date range changed, ensure the new range still has no transactions
            if (newStart != calendar.StartDate || newEnd != calendar.EndDate)
            {
                var probe = new FiscalCalendar { Name = calendar.Name, StartDate = newStart, EndDate = newEnd };
                if (await HasTransactionsAsync(probe))
                    throw new InvalidOperationException("The new date range would include existing transactions.");
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
                calendar.Name = request.Name;
            calendar.StartDate = newStart;
            calendar.EndDate = newEnd;
            if (request.Description != null)
                calendar.Description = request.Description;

            calendar.UpdatedAt = DateTime.UtcNow;
            calendar.UpdatedByUserId = userId;

            _repository.Update(calendar);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Fiscal calendar updated: {Name} (ID: {Id})", calendar.Name, calendar.Id);

            return await ToDtoAsync(calendar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fiscal calendar: {Id}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var calendar = await _repository.GetByIdAsync(id);
            if (calendar == null)
                throw new InvalidOperationException("Fiscal calendar not found");

            if (!calendar.IsActive)
                throw new InvalidOperationException("A closed fiscal year is read-only and cannot be deleted. Reopen it first.");

            if (await HasTransactionsAsync(calendar))
                throw new InvalidOperationException("This fiscal year has transactions and cannot be deleted.");

            calendar.IsDeleted = true;
            calendar.DeletedAt = DateTime.UtcNow;
            calendar.DeletedByUserId = userId;

            _repository.Update(calendar);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Fiscal calendar deleted: {Name} (ID: {Id})", calendar.Name, calendar.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fiscal calendar: {Id}", id);
            throw;
        }
    }

    public async Task<FiscalCalendarDto> CloseAsync(Guid id, Guid userId)
    {
        try
        {
            var calendar = await _repository.GetByIdAsync(id);
            if (calendar == null)
                throw new InvalidOperationException("Fiscal calendar not found");
            if (!calendar.IsActive)
                throw new InvalidOperationException("Fiscal year is already closed.");

            calendar.IsActive = false; // closed
            calendar.UpdatedAt = DateTime.UtcNow;
            calendar.UpdatedByUserId = userId;

            _repository.Update(calendar);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Fiscal calendar closed: {Name} (ID: {Id})", calendar.Name, calendar.Id);

            return await ToDtoAsync(calendar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing fiscal calendar: {Id}", id);
            throw;
        }
    }

    public async Task<FiscalCalendarDto> ReopenAsync(Guid id, Guid userId)
    {
        try
        {
            var calendar = await _repository.GetByIdAsync(id);
            if (calendar == null)
                throw new InvalidOperationException("Fiscal calendar not found");
            if (calendar.IsActive)
                throw new InvalidOperationException("Fiscal year is not closed.");

            calendar.IsActive = true; // reopened
            calendar.UpdatedAt = DateTime.UtcNow;
            calendar.UpdatedByUserId = userId;

            _repository.Update(calendar);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Fiscal calendar reopened: {Name} (ID: {Id})", calendar.Name, calendar.Id);

            return await ToDtoAsync(calendar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening fiscal calendar: {Id}", id);
            throw;
        }
    }

    public async Task<PaginatedResponse<FiscalPeriodDto>> GetCalendarPeriodsAsync(Guid calendarId, PaginationParams pagination)
    {
        try
        {
            var calendar = await _repository.GetByIdAsync(calendarId);
            if (calendar == null)
                throw new InvalidOperationException("Fiscal calendar not found");

            var (items, total) = await _periodRepository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: p => p.FiscalCalendarId == calendarId,
                orderBy: q => q.OrderBy(p => p.StartDate));
            return PaginatedResponse<FiscalPeriodDto>.Ok(items.Select(MapPeriodToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal periods for calendar: {CalendarId}", calendarId);
            throw;
        }
    }

    private async Task<FiscalCalendarDto> ToDtoAsync(FiscalCalendar calendar)
    {
        var dto = MapToDto(calendar);
        dto.HasTransactions = await HasTransactionsAsync(calendar);
        return dto;
    }

    private static FiscalCalendarDto MapToDto(FiscalCalendar calendar)
    {
        return new FiscalCalendarDto
        {
            Id = calendar.Id,
            CompanyId = calendar.CompanyId,
            Name = calendar.Name,
            StartDate = calendar.StartDate,
            EndDate = calendar.EndDate,
            IsActive = calendar.IsActive,
            Description = calendar.Description,
            IsClosed = !calendar.IsActive
        };
    }

    private static FiscalPeriodDto MapPeriodToDto(FiscalPeriod period)
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
