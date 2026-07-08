using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implementation of journal entry service
/// </summary>
public class JournalEntryService : IJournalEntryService
{
    private readonly IJournalEntryRepository _repository;
    private readonly IJournalLineRepository _lineRepository;
    private readonly ILogger<JournalEntryService> _logger;

    public JournalEntryService(
        IJournalEntryRepository repository,
        IJournalLineRepository lineRepository,
        ILogger<JournalEntryService> logger)
    {
        _repository = repository;
        _lineRepository = lineRepository;
        _logger = logger;
    }

    public async Task<JournalEntryDto> CreateAsync(CreateJournalEntryDto request, Guid userId)
    {
        try
        {
            // Validate debit/credit balance
            var totalDebit = request.Lines?.Sum(l => l.DebitAmount) ?? 0;
            var totalCredit = request.Lines?.Sum(l => l.CreditAmount) ?? 0;

            if (Math.Abs(totalDebit - totalCredit) > 0.01m)
                throw new InvalidOperationException("Journal entry must balance (debits = credits)");

            var entry = new JournalEntry
            {
                LedgerId = request.LedgerId,
                JournalNumber = $"JE-{DateTime.UtcNow:yyyyMMddHHmmss}",
                DocumentType = request.DocumentType,
                PostingDate = request.PostingDate,
                DocumentDate = request.DocumentDate,
                Description = request.Description,
                CurrencyCode = request.CurrencyCode,
                ReferenceNumber = request.ReferenceNumber,
                Status = JournalEntryStatus.Draft,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                Lines = BuildLines(request.Lines, request.CurrencyCode, userId)
            };

            await _repository.AddAsync(entry);

            // Stamp tenant context onto the child lines from the parent (repository only stamps the root)
            foreach (var line in entry.Lines)
            {
                line.CompanyId = entry.CompanyId;
                line.BranchId = entry.BranchId;
                line.BusinessUnitId = entry.BusinessUnitId;
                line.JournalEntryId = entry.Id;
            }

            await _repository.SaveChangesAsync();

            _logger.LogInformation("Journal entry created: {JournalNumber} (ID: {EntryId})", 
                entry.JournalNumber, entry.Id);

            return MapToDto(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating journal entry");
            throw;
        }
    }

    public async Task<JournalEntryDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var entry = await _repository.GetByIdWithLinesAsync(id);
            return entry == null ? null : MapToDto(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journal entry: {EntryId}", id);
            throw;
        }
    }

    public async Task<JournalEntryDto?> GetByIdWithLinesAsync(Guid id)
    {
        try
        {
            var entry = await _repository.GetByIdWithLinesAsync(id);
            return entry == null ? null : MapToDto(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journal entry with lines: {EntryId}", id);
            throw;
        }
    }

    private static List<JournalLine> BuildLines(IEnumerable<CreateJournalLineDto>? lines, string headerCurrency, Guid userId)
    {
        var result = new List<JournalLine>();
        if (lines == null) return result;
        foreach (var l in lines)
        {
            result.Add(new JournalLine
            {
                Id = Guid.NewGuid(),
                LedgerAccountId = l.LedgerAccountId,
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                CurrencyCode = string.IsNullOrWhiteSpace(l.CurrencyCode) ? headerCurrency : l.CurrencyCode,
                ExchangeRate = 1,
                BaseDebitAmount = l.DebitAmount,
                BaseCreditAmount = l.CreditAmount,
                LineNumber = l.LineNumber,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });
        }
        return result;
    }

    public async Task<PaginatedResponse<JournalEntryDto>> GetByLedgerIdAsync(Guid ledgerId, PaginationParams pagination)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: e => e.LedgerId == ledgerId &&
                    (string.IsNullOrEmpty(search) ||
                        e.JournalNumber.ToLower().Contains(search) ||
                        (e.ReferenceNumber != null && e.ReferenceNumber.ToLower().Contains(search)) ||
                        e.DocumentType.ToLower().Contains(search) ||
                        e.Description.ToLower().Contains(search)),
                orderBy: q => q.ApplyOrderNewestFirst(
                    pagination.SortBy,
                    pagination.SortDirection,
                    "PostingDate"));
            return PaginatedResponse<JournalEntryDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journal entries for ledger: {LedgerId}", ledgerId);
            throw;
        }
    }

    public async Task<PaginatedResponse<JournalEntryDto>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, PaginationParams pagination)
    {
        try
        {
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: e => e.PostingDate >= fromDate && e.PostingDate <= toDate,
                orderBy: q => q.OrderByDescending(e => e.PostingDate));
            return PaginatedResponse<JournalEntryDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journal entries by date range");
            throw;
        }
    }

    public async Task<PaginatedResponse<JournalEntryDto>> GetByStatusAsync(string status, PaginationParams pagination)
    {
        try
        {
            if (!Enum.TryParse<JournalEntryStatus>(status, out var statusEnum))
                throw new InvalidOperationException($"Invalid journal entry status: {status}");

            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: e => e.Status == statusEnum,
                orderBy: q => q.OrderByDescending(e => e.PostingDate));
            return PaginatedResponse<JournalEntryDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journal entries by status: {Status}", status);
            throw;
        }
    }

    public async Task<JournalEntryDto> UpdateAsync(Guid id, UpdateJournalEntryDto request, Guid userId)
    {
        try
        {
            // Load the entry (tenant-filtered, detached) — header is updated via Update().
            var entry = await _repository.GetByIdAsync(id);
            if (entry == null)
                throw new InvalidOperationException("Journal entry not found");

            if (entry.Status != JournalEntryStatus.Draft)
                throw new InvalidOperationException("Can only update journal entries in Draft status");

            if (!string.IsNullOrWhiteSpace(request.Description))
                entry.Description = request.Description;

            if (request.ReferenceNumber != null)
                entry.ReferenceNumber = request.ReferenceNumber;

            if (request.PostingDate.HasValue)
                entry.PostingDate = request.PostingDate.Value;

            // Replace lines wholesale when provided (Draft only)
            if (request.Lines != null)
            {
                var totalDebit = request.Lines.Sum(l => l.DebitAmount);
                var totalCredit = request.Lines.Sum(l => l.CreditAmount);
                if (Math.Abs(totalDebit - totalCredit) > 0.01m)
                    throw new InvalidOperationException("Journal entry must balance (debits = credits)");

                // Remove existing lines (hard delete — they are child rows of a draft)
                var existing = (await _lineRepository.GetByJournalEntryIdAsync(id)).ToList();
                if (existing.Count > 0)
                    _lineRepository.DeleteRange(existing);

                // Add the new lines. AddRangeAsync forces EF 'Added' state regardless of key value.
                var newLines = BuildLines(request.Lines, entry.CurrencyCode, userId);
                foreach (var line in newLines)
                {
                    line.CompanyId = entry.CompanyId;
                    line.BranchId = entry.BranchId;
                    line.BusinessUnitId = entry.BusinessUnitId;
                    line.JournalEntryId = entry.Id;
                }
                await _lineRepository.AddRangeAsync(newLines);

                entry.TotalDebit = totalDebit;
                entry.TotalCredit = totalCredit;
            }

            entry.UpdatedAt = DateTime.UtcNow;
            entry.UpdatedByUserId = userId;

            _repository.Update(entry);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Journal entry updated: {JournalNumber} (ID: {EntryId})",
                entry.JournalNumber, entry.Id);

            return MapToDto(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating journal entry: {EntryId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entry = await _repository.GetByIdAsync(id);
            if (entry == null)
                throw new InvalidOperationException("Journal entry not found");

            if (entry.Status != JournalEntryStatus.Draft)
                throw new InvalidOperationException("Can only delete journal entries in Draft status");

            entry.IsDeleted = true;
            entry.DeletedAt = DateTime.UtcNow;
            entry.DeletedByUserId = userId;

            _repository.Update(entry);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Journal entry deleted: {JournalNumber} (ID: {EntryId})",
                entry.JournalNumber, entry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting journal entry: {EntryId}", id);
            throw;
        }
    }

    public async Task<JournalEntryDto> SubmitAsync(Guid id, Guid userId)
    {
        try
        {
            var entry = await _repository.GetByIdAsync(id);
            if (entry == null)
                throw new InvalidOperationException("Journal entry not found");

            if (entry.Status != JournalEntryStatus.Draft)
                throw new InvalidOperationException("Can only submit journal entries in Draft status");

            entry.Status = JournalEntryStatus.Submitted;
            entry.UpdatedAt = DateTime.UtcNow;
            entry.UpdatedByUserId = userId;

            _repository.Update(entry);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Journal entry submitted: {JournalNumber} (ID: {EntryId})", 
                entry.JournalNumber, entry.Id);

            return MapToDto(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting journal entry: {EntryId}", id);
            throw;
        }
    }

    public async Task<JournalEntryDto> PostAsync(Guid id, Guid userId)
    {
        try
        {
            var entry = await _repository.GetByIdAsync(id);
            if (entry == null)
                throw new InvalidOperationException("Journal entry not found");

            if (entry.Status != JournalEntryStatus.ApprovedByFinance)
                throw new InvalidOperationException("Can only post journal entries that are ApprovedByFinance");

            entry.Status = JournalEntryStatus.Posted;
            entry.PostedAt = DateTime.UtcNow;
            entry.PostedByUserId = userId;

            _repository.Update(entry);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Journal entry posted: {JournalNumber} (ID: {EntryId})", 
                entry.JournalNumber, entry.Id);

            return MapToDto(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting journal entry: {EntryId}", id);
            throw;
        }
    }

    public async Task<JournalEntryDto> ReverseAsync(Guid id, Guid userId)
    {
        try
        {
            var originalEntry = await _repository.GetByIdAsync(id);
            if (originalEntry == null)
                throw new InvalidOperationException("Journal entry not found");

            if (originalEntry.Status != JournalEntryStatus.Posted)
                throw new InvalidOperationException("Can only reverse posted journal entries");

            // Create reversal entry
            var reversalEntry = new JournalEntry
            {
                LedgerId = originalEntry.LedgerId,
                JournalNumber = $"JE-REV-{DateTime.UtcNow:yyyyMMddHHmmss}",
                DocumentType = originalEntry.DocumentType,
                PostingDate = DateTime.UtcNow,
                DocumentDate = DateTime.UtcNow,
                Description = $"Reversal of {originalEntry.JournalNumber}",
                CurrencyCode = originalEntry.CurrencyCode,
                ReferenceNumber = originalEntry.ReferenceNumber,
                ReversalOfJournalId = id,
                Status = JournalEntryStatus.Reversed,
                TotalDebit = originalEntry.TotalCredit,
                TotalCredit = originalEntry.TotalDebit,
                PostedAt = DateTime.UtcNow,
                PostedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            originalEntry.Status = JournalEntryStatus.Reversed;
            originalEntry.UpdatedAt = DateTime.UtcNow;
            originalEntry.UpdatedByUserId = userId;

            _repository.Update(originalEntry);
            await _repository.AddAsync(reversalEntry);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Journal entry reversed: {JournalNumber} -> {ReversalNumber}", 
                originalEntry.JournalNumber, reversalEntry.JournalNumber);

            return MapToDto(reversalEntry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reversing journal entry: {EntryId}", id);
            throw;
        }
    }

    private static JournalEntryDto MapToDto(JournalEntry entry)
    {
        return new JournalEntryDto
        {
            Id = entry.Id,
            CompanyId = entry.CompanyId,
            JournalNumber = entry.JournalNumber,
            DocumentType = entry.DocumentType,
            DocumentDate = entry.DocumentDate,
            PostingDate = entry.PostingDate,
            Description = entry.Description,
            CurrencyCode = entry.CurrencyCode,
            TotalDebit = entry.TotalDebit,
            TotalCredit = entry.TotalCredit,
            Status = entry.Status.ToString(),
            ReferenceNumber = entry.ReferenceNumber,
            Lines = entry.Lines?
                .OrderBy(l => l.LineNumber)
                .Select(l => new JournalLineDto
                {
                    Id = l.Id,
                    CompanyId = l.CompanyId,
                    JournalEntryId = l.JournalEntryId,
                    LedgerAccountId = l.LedgerAccountId,
                    DebitAmount = l.DebitAmount,
                    CreditAmount = l.CreditAmount,
                    LineNumber = l.LineNumber
                }).ToList() ?? []
        };
    }
}
