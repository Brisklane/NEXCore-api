namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for posting profile
/// </summary>
public class PostingProfileDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string ModuleName { get; set; }
    public required string TransactionType { get; set; }
    public Guid DebitAccountId { get; set; }
    public Guid CreditAccountId { get; set; }
    public Guid? TaxAccountId { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
