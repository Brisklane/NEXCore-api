namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for journal entry approval
/// </summary>
public class ApprovalDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid ApprovingUserId { get; set; }
    public string? ApprovingUserName { get; set; }
    public DateTime ApprovedAt { get; set; }
    public required string ApprovalDecision { get; set; }
    public string? Comments { get; set; }
    public int ApprovalSequence { get; set; }
    public string? RequiredApprovalRole { get; set; }
    public bool IsFinalApproval { get; set; }
}
