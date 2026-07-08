using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Configurable document numbering sequence — one row per document type per tenant.
///
/// Controls:
///   • Prefix    (e.g. "SO", "INV", "QT", "PAY")
///   • Separator (e.g. "-", "/")
///   • Whether year and/or month are embedded
///   • How many digits to pad the running counter to
///   • When the counter resets (never / yearly / monthly)
///
/// Generated number examples (all configurable):
///   SO-2026-00001         Prefix=SO,  Sep=-, Year=Full,  Month=false, Day=false, Pad=5, Reset=Yearly
///   INV/2026/00001        Prefix=INV, Sep=/, Year=Full,  Month=false, Day=false, Pad=5, Reset=Yearly
///   INV/2026/05/00001     Prefix=INV, Sep=/, Year=Full,  Month=true,  Day=false, Pad=5, Reset=Monthly
///   INV/2026/05/09/00001  Prefix=INV, Sep=/, Year=Full,  Month=true,  Day=true,  Pad=5, Reset=Daily
///   PAY-26-00001          Prefix=PAY, Sep=-, Year=Short, Month=false, Day=false, Pad=5, Reset=Yearly
///   QT-00001              Prefix=QT,  Sep=-, Year=false, Month=false, Day=false, Pad=5, Reset=Never
///
/// Thread safety: the service uses a serializable-isolation DB transaction to atomically
/// read-and-increment NextSequenceNumber, preventing duplicate numbers under concurrency.
/// </summary>
public class DocumentSequence : BaseEntity
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Document type — stored as its string name in the database.
    /// Unique per tenant.
    /// </summary>
    public DocumentType DocumentType { get; set; }

    // ── Format settings ───────────────────────────────────────────────────────

    /// <summary>Leading text printed before the number. e.g. "SO", "INV", "QT".</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Optional trailing text appended after the number. Rarely used.</summary>
    public string? Suffix { get; set; }

    /// <summary>Character between the parts of the number. Usually "-" or "/".</summary>
    public string Separator { get; set; } = "-";

    /// <summary>Whether to embed the year in the number.</summary>
    public bool IncludeYear { get; set; } = true;

    /// <summary>4-digit (2026) or 2-digit (26) year. Ignored when IncludeYear = false.</summary>
    public SequenceYearFormat YearFormat { get; set; } = SequenceYearFormat.Full;

    /// <summary>Whether to embed the 2-digit month. Only meaningful when IncludeYear = true.</summary>
    public bool IncludeMonth { get; set; }

    /// <summary>Whether to embed the 2-digit day. Only meaningful when IncludeMonth = true.</summary>
    public bool IncludeDay { get; set; }

    /// <summary>How many digits the running counter is padded to. e.g. 5 → "00001".</summary>
    public int SequencePadding { get; set; } = 5;

    // ── Counter ───────────────────────────────────────────────────────────────

    /// <summary>When the counter resets back to 1.</summary>
    public SequenceResetPeriod ResetOn { get; set; } = SequenceResetPeriod.Yearly;

    /// <summary>The next number that will be issued. Incremented atomically on each allocation.</summary>
    public int NextSequenceNumber { get; set; } = 1;

    /// <summary>Year of the last reset — used to detect when yearly/monthly/daily reset is due.</summary>
    public int? LastResetYear { get; set; }

    /// <summary>Month of the last reset — used to detect when monthly/daily reset is due.</summary>
    public int? LastResetMonth { get; set; }

    /// <summary>Day of the last reset — used to detect when daily reset is due.</summary>
    public int? LastResetDay { get; set; }

}

