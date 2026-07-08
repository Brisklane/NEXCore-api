namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for ledger account
/// </summary>
public class LedgerAccountDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string AccountNumber { get; set; }
    public required string AccountName { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? ParentAccountId { get; set; }
    public bool IsPostingAllowed { get; set; }
    public bool IsControlAccount { get; set; }
    public string? CurrencyCode { get; set; }
    public bool AllowManualEntry { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
