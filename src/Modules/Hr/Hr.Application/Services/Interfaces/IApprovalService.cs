using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IApprovalService
{
    // Queries

    /// <summary>All approval requests for the tenant.</summary>
    Task<IEnumerable<ApprovalRequestDto>> GetAllAsync();

    /// <summary>Single request with its steps.</summary>
    Task<ApprovalRequestDto?> GetByIdAsync(Guid id);

    /// <summary>All approval requests linked to a specific entity instance.</summary>
    Task<IEnumerable<ApprovalRequestDto>> GetByEntityAsync(string entityType, Guid entityId);

    /// <summary>All requests currently pending action by a specific approver employee.</summary>
    Task<IEnumerable<ApprovalRequestDto>> GetPendingForApproverAsync(Guid approverEmployeeId);

    // Commands

    /// <summary>
    /// Submit a new approval request.
    /// Looks up the matching active WorkflowConfig, creates header + all steps (Pending),
    /// and returns the full request.
    /// </summary>
    Task<ApprovalRequestDto> SubmitAsync(SubmitApprovalRequestDto request, Guid userId);

    /// <summary>Approve the current pending step. Advances to the next level or marks the request Approved.</summary>
    Task<ApprovalRequestDto> ApproveStepAsync(Guid approvalRequestId, ApproveStepDto dto, Guid userId);

    /// <summary>Reject the current pending step. Marks the whole request Rejected.</summary>
    Task<ApprovalRequestDto> RejectStepAsync(Guid approvalRequestId, RejectStepDto dto, Guid userId);

    /// <summary>Delegate the current pending step to another employee.</summary>
    Task<ApprovalRequestDto> DelegateStepAsync(Guid approvalRequestId, DelegateStepDto dto, Guid userId);

    /// <summary>Cancel a pending approval request (only allowed while not yet completed).</summary>
    Task<ApprovalRequestDto> CancelAsync(Guid approvalRequestId, string? reason, Guid userId);

    /// <summary>Update mutable header fields (priority, comments, display labels).</summary>
    Task<ApprovalRequestDto> UpdateAsync(Guid id, UpdateApprovalRequestDto request, Guid userId);

    /// <summary>Soft-delete an approval request.</summary>
    Task DeleteAsync(Guid id, Guid userId);
}
