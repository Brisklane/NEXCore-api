namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for ledger response
/// </summary>
public class LedgerDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string BaseCurrencyCode { get; set; }
    public Guid FiscalCalendarId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
