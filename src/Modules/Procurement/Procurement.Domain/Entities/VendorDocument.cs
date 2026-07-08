using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Vendor compliance document — certificates, insurance, registrations, and licences.
/// Tracks expiry dates and drives automated renewal alerts.
/// Aligned with SAP vendor master document management, Oracle Supplier Qualifications.
/// </summary>
public class VendorDocument : BaseEntity
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    // ─── Document Identity ─────────────────────────────────────────────────────
    public VendorDocumentType DocumentType { get; set; }
    public required string DocumentName { get; set; }
    public string? DocumentNumber { get; set; }

    // ─── Issuer ────────────────────────────────────────────────────────────────
    public string? IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }

    // ─── Validity ──────────────────────────────────────────────────────────────
    public DateTime? ExpiryDate { get; set; }
    public bool NeverExpires { get; set; }

    public VendorDocumentStatus Status { get; set; } = VendorDocumentStatus.Active;

    /// <summary>Alert X days before expiry. Default 30 days.</summary>
    public int ReminderDays { get; set; } = 30;

    public bool IsExpired => !NeverExpires && ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

    public int? DaysUntilExpiry => (!NeverExpires && ExpiryDate.HasValue)
        ? (int)(ExpiryDate.Value - DateTime.UtcNow).TotalDays
        : null;

    // ─── File Storage ──────────────────────────────────────────────────────────
    /// <summary>Relative path or blob URL to the stored document file.</summary>
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? FileContentType { get; set; }
    public long? FileSizeBytes { get; set; }

    // ─── Verification ──────────────────────────────────────────────────────────
    public bool IsVerified { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationNotes { get; set; }

    public string? Notes { get; set; }
}
