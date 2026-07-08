using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Tracks which vendors were invited to respond to an RFQ and their response status.
/// </summary>
public class RFQVendor : BaseEntity
{
    public Guid RFQId { get; set; }
    public RequestForQuotation RFQ { get; set; } = null!;

    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResponseDeadline { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public RFQVendorStatus Status { get; set; } = RFQVendorStatus.Invited;

    public string? DeclineReason { get; set; }
    public string? Notes { get; set; }
}
