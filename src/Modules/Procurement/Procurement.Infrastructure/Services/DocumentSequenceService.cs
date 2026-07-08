using Microsoft.EntityFrameworkCore;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;
using Procurement.Infrastructure.Repositories.Interfaces;

namespace Procurement.Infrastructure.Services;

public interface IDocumentSequenceService
{
    Task<string> GetNextNumberAsync(ProcurementDocumentType documentType);
}

public class DocumentSequenceService : IDocumentSequenceService
{
    private readonly ProcurementDbContext _ctx;

    public DocumentSequenceService(ProcurementDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<string> GetNextNumberAsync(ProcurementDocumentType documentType)
    {
        // Serializable isolation prevents duplicate numbers under concurrency
        await using var tx = await _ctx.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        var seq = await _ctx.DocumentSequences
            .FirstOrDefaultAsync(s => s.DocumentType == documentType && s.IsActive);

        if (seq is null)
            return $"{documentType}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;

        // Reset counter if period rolled over
        var needsReset = seq.ResetOn switch
        {
            SequenceResetPeriod.Yearly  => seq.LastResetYear != year,
            SequenceResetPeriod.Monthly => seq.LastResetYear != year || seq.LastResetMonth != month,
            _                           => false
        };

        if (needsReset)
        {
            seq.NextSequenceNumber = 1;
            seq.LastResetYear  = year;
            seq.LastResetMonth = month;
        }

        var number = seq.NextSequenceNumber++;
        _ctx.DocumentSequences.Update(seq);
        await _ctx.SaveChangesAsync();
        await tx.CommitAsync();

        return BuildNumber(seq, number, now);
    }

    private static string BuildNumber(Domain.Entities.DocumentSequence seq, int number, DateTime now)
    {
        var parts = new List<string> { seq.Prefix };

        if (seq.IncludeYear)
        {
            var year = seq.YearFormat == SequenceYearFormat.Full
                ? now.Year.ToString()
                : now.ToString("yy");
            parts.Add(year);
        }

        if (seq.IncludeYear && seq.IncludeMonth)
            parts.Add(now.ToString("MM"));

        parts.Add(number.ToString().PadLeft(seq.SequencePadding, '0'));

        if (!string.IsNullOrEmpty(seq.Suffix))
            parts.Add(seq.Suffix);

        return string.Join(seq.Separator, parts);
    }
}
