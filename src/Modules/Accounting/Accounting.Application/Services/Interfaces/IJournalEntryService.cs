using Accounting.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Accounting.Application.Services.Interfaces;

/// <summary>
/// Service layer for JournalEntry business logic
/// </summary>
public interface IJournalEntryService
{
    Task<JournalEntryDto> CreateAsync(CreateJournalEntryDto request, Guid userId);
    Task<JournalEntryDto?> GetByIdAsync(Guid id);
    Task<JournalEntryDto?> GetByIdWithLinesAsync(Guid id);
    Task<PaginatedResponse<JournalEntryDto>> GetByLedgerIdAsync(Guid ledgerId, PaginationParams pagination);
    Task<PaginatedResponse<JournalEntryDto>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, PaginationParams pagination);
    Task<PaginatedResponse<JournalEntryDto>> GetByStatusAsync(string status, PaginationParams pagination);
    Task<JournalEntryDto> UpdateAsync(Guid id, UpdateJournalEntryDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<JournalEntryDto> SubmitAsync(Guid id, Guid userId);
    Task<JournalEntryDto> PostAsync(Guid id, Guid userId);
    Task<JournalEntryDto> ReverseAsync(Guid id, Guid userId);
}
