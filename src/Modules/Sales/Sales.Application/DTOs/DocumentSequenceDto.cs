using Sales.Domain.Enums;

namespace Sales.Application.DTOs;

/// <summary>
/// The result of allocating a document number.
/// <see cref="Code"/> is the full formatted string (e.g. "SO-2026-06-09-00001").
/// <see cref="CodeInt"/> is the pure-numeric concatenation of the date/counter parts,
/// with the prefix and separators stripped (e.g. 2026060900001).
/// Implicit conversion to <c>string</c> returns <see cref="Code"/>, so existing callers
/// that assign the result to a <c>string</c> continue to compile unchanged.
/// </summary>
public record SequenceNumber(string Code, long CodeInt)
{
    public static implicit operator string(SequenceNumber s) => s.Code;
    public override string ToString() => Code;
}

public class DocumentSequenceDto
{
    public Guid Id { get; set; }
    public DocumentType DocumentType { get; set; }
    public string? Description { get; set; }

    // ── Format ────────────────────────────────────────────────────────────────
    public string Prefix { get; set; } = string.Empty;
    public string? Suffix { get; set; }
    public string Separator { get; set; } = "-";
    public bool IncludeYear { get; set; }
    public SequenceYearFormat YearFormat { get; set; }
    public bool IncludeMonth { get; set; }
    public bool IncludeDay { get; set; }
    public int SequencePadding { get; set; }
    public SequenceResetPeriod ResetOn { get; set; }

    // ── Counter state ─────────────────────────────────────────────────────────
    public int NextSequenceNumber { get; set; }
    public int? LastResetYear { get; set; }
    public int? LastResetMonth { get; set; }
    public int? LastResetDay { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Live preview of what the NEXT number will look like.</summary>
    public string NextNumberPreview { get; set; } = string.Empty;

    /// <summary>Numeric-only preview — prefix and separators stripped (e.g. 2026060900001).</summary>
    public long NextCodeIntPreview { get; set; }
}

public class CreateDocumentSequenceDto
{
    [System.ComponentModel.DataAnnotations.Required]
    public DocumentType DocumentType { get; set; }

    public string? Description { get; set; }

    /// <summary>Leading text before the number. Leave empty for a pure-numeric sequence.</summary>
    public string Prefix { get; set; } = string.Empty;

    public string? Suffix { get; set; }

    public string Separator { get; set; } = "-";

    public bool IncludeYear { get; set; } = true;

    public SequenceYearFormat YearFormat { get; set; } = SequenceYearFormat.Full;

    public bool IncludeMonth { get; set; } = false;

    public bool IncludeDay { get; set; } = false;

    [System.ComponentModel.DataAnnotations.Range(1, 10)]
    public int SequencePadding { get; set; } = 5;

    public SequenceResetPeriod ResetOn { get; set; } = SequenceResetPeriod.Yearly;

    /// <summary>Starting number (default 1). Use to resume an existing sequence.</summary>
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int StartFrom { get; set; } = 1;
}

public class UpdateDocumentSequenceDto
{
    public string? Description { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public string? Separator { get; set; }
    public bool? IncludeYear { get; set; }
    public SequenceYearFormat? YearFormat { get; set; }
    public bool? IncludeMonth { get; set; }
    public bool? IncludeDay { get; set; }
    public int? SequencePadding { get; set; }
    public SequenceResetPeriod? ResetOn { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request to reset the counter on a sequence (e.g. after migrating from a legacy system).
/// </summary>
public class ResetSequenceDto
{
    /// <summary>The counter will be set to this value. The next document gets this number.</summary>
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int ResetTo { get; set; } = 1;
}

/// <summary>
/// Preview what a number would look like with the given settings, without actually allocating it.
/// </summary>
public class PreviewSequenceFormatDto
{
    public string Prefix { get; set; } = string.Empty;
    public string? Suffix { get; set; }
    public string Separator { get; set; } = "-";
    public bool IncludeYear { get; set; } = true;
    public SequenceYearFormat YearFormat { get; set; } = SequenceYearFormat.Full;
    public bool IncludeMonth { get; set; }
    public bool IncludeDay { get; set; }
    public int SequencePadding { get; set; } = 5;
    /// <summary>Which counter number to show in the preview (default: 1).</summary>
    public int SampleNumber { get; set; } = 1;
}

public class PreviewSequenceFormatResultDto
{
    public string Preview { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    /// <summary>The pure-numeric equivalent of Preview (prefix and separators stripped).</summary>
    public long CodeIntPreview { get; set; }
}
