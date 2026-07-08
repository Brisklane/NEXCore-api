using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Persistence;
using Sales.Infrastructure.Repositories.Interfaces;
using System.Data;


namespace Sales.Infrastructure.Services;

/// <summary>
/// Thread-safe document sequence service.
///
/// Atomic increment strategy:
///   A serializable transaction locks the DocumentSequences row with UPDLOCK,
///   reads the current counter, increments it, saves, and commits — all in one
///   round-trip. This guarantees no two documents ever get the same number even
///   under concurrent creates.
///
/// Format builder:
///   Reads the sequence's Prefix, Separator, IncludeYear, YearFormat,
///   IncludeMonth, SequencePadding, and Suffix to produce the final string.
///
///   Examples:
///     SO-2026-00001      Prefix=SO,  Sep=-, Year=Full,  Month=false, Pad=5
///     INV/2026/00001     Prefix=INV, Sep=/, Year=Full,  Month=false, Pad=5
///     INV/2026/05/00001  Prefix=INV, Sep=/, Year=Full,  Month=true,  Pad=5
///     PAY-26-00001       Prefix=PAY, Sep=-, Year=Short, Month=false, Pad=5
///     QT-00001           Prefix=QT,  Sep=-, Year=false, Month=false, Pad=5
/// </summary>
public class DocumentSequenceService : IDocumentSequenceService
{
    private readonly IDocumentSequenceRepository _repo;
    private readonly SalesDbContext _db;
    private readonly ILogger<DocumentSequenceService> _logger;

    public DocumentSequenceService(
        IDocumentSequenceRepository repo,
        SalesDbContext db,
        ILogger<DocumentSequenceService> logger)
    {
        _repo   = repo;
        _db     = db;
        _logger = logger;
    }

    // ── Number allocation ─────────────────────────────────────────────────────

    public async Task<SequenceNumber> GetNextNumberAsync(DocumentType documentType)
    {
        var seq = await _repo.GetByDocumentTypeAsync(documentType);

        if (seq == null)
        {
            var name = documentType.ToString();
            _logger.LogWarning(
                "No DocumentSequence configured for type '{DocumentType}' — using random fallback",
                name);
            var fallback = $"{name[..Math.Min(3, name.Length)].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            return new SequenceNumber(fallback, 0);
        }

        return await AllocateAtomicallyAsync(seq);
    }

    // ── Configuration CRUD ────────────────────────────────────────────────────

    public async Task<List<DocumentSequenceDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();
        return list.OrderBy(s => s.DocumentType).Select(MapToDto).ToList();
    }

    public async Task<DocumentSequenceDto?> GetByDocumentTypeAsync(DocumentType documentType)
    {
        var seq = await _repo.GetByDocumentTypeAsync(documentType);
        return seq == null ? null : MapToDto(seq);
    }

    public async Task<DocumentSequenceDto> CreateAsync(CreateDocumentSequenceDto dto)
    {
        var existing = await _repo.GetByDocumentTypeAsync(dto.DocumentType);
        if (existing != null)
            throw new InvalidOperationException($"A sequence for '{dto.DocumentType}' already exists");

        var seq = new DocumentSequence
        {
            DocumentType       = dto.DocumentType,
            Description        = dto.Description,
            Prefix             = dto.Prefix?.Trim().ToUpperInvariant() ?? string.Empty,
            Suffix             = dto.Suffix?.Trim(),
            Separator          = dto.Separator,
            IncludeYear        = dto.IncludeYear,
            YearFormat         = dto.YearFormat,
            IncludeMonth       = dto.IncludeMonth,
            IncludeDay         = dto.IncludeDay,
            SequencePadding    = dto.SequencePadding,
            ResetOn            = dto.ResetOn,
            NextSequenceNumber = dto.StartFrom,
            IsActive           = true,
        };

        await _repo.AddAsync(seq);
        await _repo.SaveChangesAsync();

        _logger.LogInformation(
            "DocumentSequence created: {Type} → preview: {Preview}",
            seq.DocumentType.ToString(), BuildFormat(seq, dto.StartFrom, DateTime.UtcNow));

        return MapToDto(seq);
    }

    public async Task<DocumentSequenceDto> UpdateAsync(Guid id, UpdateDocumentSequenceDto dto)
    {
        var seq = await _repo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Sequence not found");

        if (dto.Description  != null) seq.Description  = dto.Description;
        if (dto.Prefix       != null) seq.Prefix       = dto.Prefix.Trim().ToUpperInvariant();
        if (dto.Suffix       != null) seq.Suffix       = dto.Suffix.Trim();
        if (dto.Separator    != null) seq.Separator    = dto.Separator;
        if (dto.IncludeYear  != null) seq.IncludeYear  = dto.IncludeYear.Value;
        if (dto.YearFormat   != null) seq.YearFormat   = dto.YearFormat.Value;
        if (dto.IncludeMonth != null) seq.IncludeMonth = dto.IncludeMonth.Value;
        if (dto.IncludeDay   != null) seq.IncludeDay   = dto.IncludeDay.Value;
        if (dto.SequencePadding != null) seq.SequencePadding = dto.SequencePadding.Value;
        if (dto.ResetOn      != null) seq.ResetOn      = dto.ResetOn.Value;
        if (dto.IsActive     != null) seq.IsActive     = dto.IsActive.Value;

        _repo.Update(seq);
        await _repo.SaveChangesAsync();
        return MapToDto(seq);
    }

    public async Task<DocumentSequenceDto> ResetCounterAsync(Guid id, ResetSequenceDto dto)
    {
        var seq = await _repo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Sequence not found");

        seq.NextSequenceNumber = dto.ResetTo;
        seq.LastResetYear      = DateTime.UtcNow.Year;
        seq.LastResetMonth     = DateTime.UtcNow.Month;
        seq.LastResetDay       = DateTime.UtcNow.Day;

        _repo.Update(seq);
        await _repo.SaveChangesAsync();

        _logger.LogInformation(
            "DocumentSequence '{Type}' counter reset to {ResetTo}",
            seq.DocumentType, dto.ResetTo);

        return MapToDto(seq);
    }

    // ── Format preview ────────────────────────────────────────────────────────

    public PreviewSequenceFormatResultDto PreviewFormat(PreviewSequenceFormatDto dto)
    {
        var fake = new DocumentSequence
        {
            Prefix          = dto.Prefix.Trim().ToUpperInvariant(),
            Suffix          = dto.Suffix?.Trim(),
            Separator       = dto.Separator,
            IncludeYear     = dto.IncludeYear,
            YearFormat      = dto.YearFormat,
            IncludeMonth    = dto.IncludeMonth,
            IncludeDay      = dto.IncludeDay,
            SequencePadding = dto.SequencePadding,
        };

        var preview     = BuildFormat(fake, dto.SampleNumber, DateTime.UtcNow);
        var pattern     = BuildPatternDescription(fake);
        var codeInt     = BuildCodeInt(fake, dto.SampleNumber, DateTime.UtcNow);

        return new PreviewSequenceFormatResultDto { Preview = preview, Pattern = pattern, CodeIntPreview = codeInt };
    }

    // ── Private: atomic allocation ────────────────────────────────────────────

    /// <summary>
    /// Uses a serializable transaction to atomically read-and-increment the counter.
    /// Handles yearly/monthly/daily resets transparently.
    /// </summary>
    private async Task<SequenceNumber> AllocateAtomicallyAsync(DocumentSequence seq)
    {
        // EnableRetryOnFailure requires explicit transactions to run inside CreateExecutionStrategy.
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                // Re-read inside the transaction with a write lock
                var locked = await _db.DocumentSequences
                    .FromSqlRaw(
                        "SELECT * FROM sales.DocumentSequences WITH (UPDLOCK, ROWLOCK) WHERE Id = {0} AND IsDeleted = 0",
                        seq.Id)
                    .FirstOrDefaultAsync()
                    ?? throw new InvalidOperationException($"Sequence '{seq.DocumentType}' not found");

                var now = DateTime.UtcNow;

                // Detect reset
                var needsReset = locked.ResetOn switch
                {
                    SequenceResetPeriod.Yearly  => locked.LastResetYear  != now.Year,
                    SequenceResetPeriod.Monthly => locked.LastResetYear  != now.Year
                                               || locked.LastResetMonth  != now.Month,
                    SequenceResetPeriod.Daily   => locked.LastResetYear  != now.Year
                                               || locked.LastResetMonth  != now.Month
                                               || locked.LastResetDay    != now.Day,
                    _ => false,
                };

                if (needsReset)
                {
                    locked.NextSequenceNumber = 1;
                    locked.LastResetYear      = now.Year;
                    locked.LastResetMonth     = now.Month;
                    locked.LastResetDay       = now.Day;
                }

                var allocated = locked.NextSequenceNumber;
                locked.NextSequenceNumber++;

                _db.DocumentSequences.Update(locked);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // Detach after save so the locked entity doesn't interfere with subsequent SaveChangesAsync calls
                // in the same scope (e.g. when controllers add other entities after allocating a number).
                _db.Entry(locked).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

                var code    = BuildFormat(locked, allocated, now);
                var codeInt = BuildCodeInt(locked, allocated, now);
                _logger.LogDebug("Allocated {DocumentType}: {Code} / {CodeInt}", locked.DocumentType, code, codeInt);
                return new SequenceNumber(code, codeInt);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    // ── Private: format builder ───────────────────────────────────────────────

    /// <summary>Builds the formatted number string from a sequence's settings.</summary>
    internal static string BuildFormat(DocumentSequence seq, int number, DateTime asOf)
    {
        var parts = new List<string>();

        // Prefix is optional — omit when empty so no leading separator appears
        if (!string.IsNullOrWhiteSpace(seq.Prefix))
            parts.Add(seq.Prefix);

        if (seq.IncludeYear)
        {
            var year = seq.YearFormat == SequenceYearFormat.Short
                ? asOf.Year.ToString()[2..]   // "26"
                : asOf.Year.ToString();        // "2026"
            parts.Add(year);
        }

        if (seq.IncludeYear && seq.IncludeMonth)
            parts.Add(asOf.Month.ToString("00"));

        if (seq.IncludeYear && seq.IncludeMonth && seq.IncludeDay)
            parts.Add(asOf.Day.ToString("00"));

        parts.Add(number.ToString($"D{seq.SequencePadding}"));

        if (!string.IsNullOrEmpty(seq.Suffix))
            parts.Add(seq.Suffix);

        return string.Join(seq.Separator, parts);
    }

    /// <summary>
    /// Builds the pure-numeric equivalent of the document number by concatenating
    /// only the date/counter segments — prefix and separators are excluded.
    /// Example: year=2026, month=06, day=09, number=1, padding=5 → 2026060900001
    /// </summary>
    internal static long BuildCodeInt(DocumentSequence seq, int number, DateTime asOf)
    {
        var sb = new System.Text.StringBuilder();

        if (seq.IncludeYear)
            sb.Append(seq.YearFormat == SequenceYearFormat.Short
                ? asOf.Year.ToString()[2..]
                : asOf.Year.ToString());

        if (seq.IncludeYear && seq.IncludeMonth)
            sb.Append(asOf.Month.ToString("00"));

        if (seq.IncludeYear && seq.IncludeMonth && seq.IncludeDay)
            sb.Append(asOf.Day.ToString("00"));

        sb.Append(number.ToString($"D{seq.SequencePadding}"));

        return long.Parse(sb.ToString());
    }

    private static string BuildPatternDescription(DocumentSequence seq)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(seq.Prefix)) parts.Add("{Prefix}");
        if (seq.IncludeYear)  parts.Add(seq.YearFormat == SequenceYearFormat.Short ? "{YY}" : "{YYYY}");
        if (seq.IncludeYear && seq.IncludeMonth) parts.Add("{MM}");
        if (seq.IncludeYear && seq.IncludeMonth && seq.IncludeDay) parts.Add("{DD}");
        parts.Add($"{{{'0'.ToString().PadRight(seq.SequencePadding, '0')}}}");
        if (!string.IsNullOrEmpty(seq.Suffix)) parts.Add("{Suffix}");
        return string.Join(seq.Separator, parts);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static DocumentSequenceDto MapToDto(DocumentSequence s) => new()
    {
        Id                 = s.Id,
        DocumentType       = s.DocumentType,
        Description        = s.Description,
        Prefix             = s.Prefix,
        Suffix             = s.Suffix,
        Separator          = s.Separator,
        IncludeYear        = s.IncludeYear,
        YearFormat         = s.YearFormat,
        IncludeMonth       = s.IncludeMonth,
        IncludeDay         = s.IncludeDay,
        SequencePadding    = s.SequencePadding,
        ResetOn            = s.ResetOn,
        NextSequenceNumber = s.NextSequenceNumber,
        LastResetYear      = s.LastResetYear,
        LastResetMonth     = s.LastResetMonth,
        LastResetDay       = s.LastResetDay,
        IsActive           = s.IsActive,
        NextNumberPreview  = BuildFormat(s, s.NextSequenceNumber, DateTime.UtcNow),
        NextCodeIntPreview = BuildCodeInt(s, s.NextSequenceNumber, DateTime.UtcNow),
    };
}
