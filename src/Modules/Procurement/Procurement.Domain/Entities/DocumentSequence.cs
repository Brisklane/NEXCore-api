using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Configurable document numbering sequence — one row per document type per tenant.
///
/// Generated number examples (all configurable):
///   PO-2026-00001      Prefix=PO,   Sep=-, Year=Full,  Month=false, Pad=5, Reset=Yearly
///   RFQ/2026/00001     Prefix=RFQ,  Sep=/, Year=Full,  Month=false, Pad=5, Reset=Yearly
///   BILL/2026/05/00001 Prefix=BILL, Sep=/, Year=Full,  Month=true,  Pad=5, Reset=Monthly
///   GRN-26-00001       Prefix=GRN,  Sep=-, Year=Short, Month=false, Pad=5, Reset=Yearly
///
/// Thread safety: the service uses a serializable-isolation DB transaction to atomically
/// read-and-increment NextSequenceNumber, preventing duplicate numbers under concurrency.
/// </summary>
public class DocumentSequence : BaseEntity
{
    /// <summary>Document type — unique per tenant. Stored as string in database.</summary>
    public ProcurementDocumentType DocumentType { get; set; }

    // ─── Format Settings ───────────────────────────────────────────────────────
    /// <summary>Leading text printed before the number (e.g. "PO", "RFQ", "GRN").</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Optional trailing text appended after the number.</summary>
    public string? Suffix { get; set; }

    /// <summary>Character between the parts of the number. Usually "-" or "/".</summary>
    public string Separator { get; set; } = "-";

    /// <summary>Whether to embed the year in the number.</summary>
    public bool IncludeYear { get; set; } = true;

    /// <summary>4-digit (2026) or 2-digit (26) year. Ignored when IncludeYear = false.</summary>
    public SequenceYearFormat YearFormat { get; set; } = SequenceYearFormat.Full;

    /// <summary>Whether to embed the 2-digit month. Only meaningful when IncludeYear = true.</summary>
    public bool IncludeMonth { get; set; }

    /// <summary>How many digits the running counter is padded to (e.g. 5 → "00001").</summary>
    public int SequencePadding { get; set; } = 5;

    // ─── Counter ───────────────────────────────────────────────────────────────
    /// <summary>When the counter resets back to 1.</summary>
    public SequenceResetPeriod ResetOn { get; set; } = SequenceResetPeriod.Yearly;

    /// <summary>The next number to be issued. Incremented atomically on each allocation.</summary>
    public int NextSequenceNumber { get; set; } = 1;

    /// <summary>Year of the last reset — used to detect when yearly/monthly reset is due.</summary>
    public int? LastResetYear { get; set; }

    /// <summary>Month of the last reset — used to detect when monthly reset is due.</summary>
    public int? LastResetMonth { get; set; }

}
