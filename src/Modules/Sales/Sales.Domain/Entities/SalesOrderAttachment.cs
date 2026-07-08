using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// File attachment for a Sales Order (PO documents, specs, signed contracts).
/// </summary>
public class SalesOrderAttachment : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
}
