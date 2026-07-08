using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Helpers;

namespace Hr.Infrastructure.Services;

/// <summary>
/// Full approval lifecycle engine.
///
/// Submit flow:
///   1. Resolve WorkflowConfig by TransactionType (or explicit Id)
///   2. Load WorkflowConditions and evaluate against EntityData snapshot
///      - ADD_STEP  ? injects extra step(s) into the chain at the specified level
///      - REPLACE_CHAIN ? discards default steps and uses a single override approver (e.g. CEO for Critical)
///   3. Build final step list with correct level numbering
///   4. Create ApprovalRequest header (status = IN_PROGRESS)
///   5. Create ApprovalRequestSteps: L1 = PENDING, L2+ = WAITING
///
/// Approve flow:
///   - Marks current step APPROVED
///   - Activates next step (WAITING ? PENDING)
///   - If last step: marks header APPROVED, writes back to the source entity
///
/// Reject flow:
///   - Marks current step REJECTED
///   - Cancels all remaining WAITING steps
///   - Marks header REJECTED, writes back to the source entity
/// </summary>
public class ApprovalService : IApprovalService
{
    // Well-known LookupValue codes
    // Must match the codes seeded for the "APPROVAL_STATUS" LookupType.
    private const string StatusCodePending    = "PENDING";
    private const string StatusCodeWaiting    = "WAITING";
    private const string StatusCodeApproved   = "APPROVED";
    private const string StatusCodeRejected   = "REJECTED";
    private const string StatusCodeCancelled  = "CANCELLED";
    private const string StatusCodeInProgress = "IN_PROGRESS";

    // Condition ActionType constants
    // Must match the ActionType values stored in WorkflowCondition.ActionType.
    private const string ConditionActionAddStep      = "ADD_STEP";
    private const string ConditionActionReplaceChain = "REPLACE_CHAIN";

    private readonly IApprovalRequestRepository _requestRepo;
    private readonly IApprovalRequestStepRepository _stepRepo;
    private readonly HrDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        IApprovalRequestRepository requestRepo,
        IApprovalRequestStepRepository stepRepo,
        HrDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApprovalService> logger)
    {
        _requestRepo         = requestRepo;
        _stepRepo            = stepRepo;
        _context             = context;
        _httpContextAccessor = httpContextAccessor;
        _logger              = logger;
    }

    // Queries

    public async Task<IEnumerable<ApprovalRequestDto>> GetAllAsync()
    {
        try
        {
            var all = await _requestRepo.GetAllByTenantAsync();
            return all.Where(a => !a.IsDeleted).Select(MapHeader).ToList();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving approval requests"); throw; }
    }

    public async Task<ApprovalRequestDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await _requestRepo.GetByIdWithStepsAsync(id);
            return entity is null ? null : MapFull(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving approval request {Id}", id); throw; }
    }

    public async Task<IEnumerable<ApprovalRequestDto>> GetByEntityAsync(string entityType, Guid entityId)
    {
        try
        {
            var list = await _requestRepo.GetByEntityAsync(entityType, entityId);
            return list.Select(MapFull).ToList();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving approvals for {EntityType}/{EntityId}", entityType, entityId); throw; }
    }

    public async Task<IEnumerable<ApprovalRequestDto>> GetPendingForApproverAsync(Guid approverEmployeeId)
    {
        try
        {
            var list = await _requestRepo.GetPendingForApproverAsync(approverEmployeeId);
            return list.Select(MapFull).ToList();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pending approvals for employee {Id}", approverEmployeeId); throw; }
    }

    // Commands

    public async Task<ApprovalRequestDto> SubmitAsync(SubmitApprovalRequestDto request, Guid userId)
    {
        try
        {
            // 1. Resolve WorkflowConfig
            WorkflowConfig workflow;
            if (request.WorkflowConfigId.HasValue)
            {
                workflow = await _context.WorkflowConfigs
                    .Include(w => w.Steps!.Where(s => !s.IsDeleted))
                    .Include(w => w.Conditions!.Where(c => !c.IsDeleted))
                    .FirstOrDefaultAsync(w => w.Id == request.WorkflowConfigId.Value && w.IsActive && !w.IsDeleted)
                    ?? throw new InvalidOperationException(
                        $"WorkflowConfig {request.WorkflowConfigId} not found or inactive.");
            }
            else
            {
                workflow = await _context.WorkflowConfigs
                    .Include(w => w.Steps!.Where(s => !s.IsDeleted))
                    .Include(w => w.Conditions!.Where(c => !c.IsDeleted))
                    .Where(w =>
                        w.TransactionType == request.EntityType &&
                        w.IsActive &&
                        !w.IsDeleted &&
                        (w.EffectiveFrom == null || w.EffectiveFrom <= DateTime.UtcNow) &&
                        (w.EffectiveTo   == null || w.EffectiveTo   >= DateTime.UtcNow))
                    .OrderByDescending(w => w.VersionNo)
                    .FirstOrDefaultAsync()
                    ?? throw new InvalidOperationException(
                        $"No active WorkflowConfig found for entity type '{request.EntityType}'. " +
                        "Please configure a workflow first.");
            }

            var baseSteps = (workflow.Steps ?? [])
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.SortOrder)
                .ToList();

            if (baseSteps.Count == 0)
                throw new InvalidOperationException(
                    $"WorkflowConfig '{workflow.WorkflowCode}' has no configured steps.");

            // 2. Evaluate WorkflowConditions ? build final step chain
            var finalSteps = BuildFinalStepChain(baseSteps, workflow.Conditions?.ToList() ?? [], request.EntityData);

            // 3. Resolve status IDs
            var pendingStatusId    = await ResolveLookupStatusIdAsync(StatusCodePending);
            var waitingStatusId    = await ResolveLookupStatusIdAsync(StatusCodeWaiting);
            var inProgressStatusId = await ResolveLookupStatusIdAsync(StatusCodeInProgress);

            // 4. Create ApprovalRequest header
            var code = $"APR-{request.EntityType.ToUpper()}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var approval = new ApprovalRequest
            {
                ApprovalRequestCode        = code,
                EntityType                 = request.EntityType,
                EntityId                   = request.EntityId,
                WorkflowConfigId           = workflow.Id,
                RequestedByEmployeeId      = request.RequestedByEmployeeId,
                CurrentLevel               = 1,
                TotalLevels                = finalSteps.Count,
                OverallStatusLookupValueId = inProgressStatusId,
                PriorityLookupValueId      = request.PriorityLookupValueId,
                RequestedAt                = DateTime.UtcNow,
                Comments                   = request.Comments,
                ApprovalSubjectCode        = request.ApprovalSubjectCode,
                ApprovalSubjectTitle       = request.ApprovalSubjectTitle,
                ApprovalSummary            = request.ApprovalSummary,
                ApprovalDisplayName        = request.ApprovalDisplayName,
                CreatedAt                  = DateTime.UtcNow,
                CreatedByUserId            = userId
            };

            await _requestRepo.AddAsync(approval);

            // 5. Create steps: L1 = PENDING, L2+ = WAITING
            for (var i = 0; i < finalSteps.Count; i++)
            {
                var wfStep = finalSteps[i];
                var isFirstLevel = i == 0;

                var approvalStep = new ApprovalRequestStep
                {
                    ApprovalRequestId    = approval.Id,
                    WorkflowConfigStepId = wfStep.Id,
                    StepLevel            = i + 1,        // re-number sequentially after condition injection
                    ApproverType         = wfStep.ApproverType,
                    ApproverValue        = wfStep.ApproverValue,
                    Mandatory            = wfStep.Mandatory,
                    SLAHours             = wfStep.SLAHours,
                    ExecutionType        = wfStep.ExecutionType,
                    IsConditional        = wfStep.IsConditional,
                    StatusLookupValueId  = isFirstLevel ? pendingStatusId : waitingStatusId,
                    CreatedAt            = DateTime.UtcNow,
                    CreatedByUserId      = userId,
                    CompanyId            = approval.CompanyId,
                    BranchId             = approval.BranchId,
                    BusinessUnitId       = approval.BusinessUnitId
                };

                await _stepRepo.AddAsync(approvalStep);
            }

            await _requestRepo.SaveChangesAsync();

            _logger.LogInformation(
                "Approval {Code} submitted for {EntityType}/{EntityId} — {Total} level(s) after condition evaluation",
                code, request.EntityType, request.EntityId, finalSteps.Count);

            return MapFull(await _requestRepo.GetByIdWithStepsAsync(approval.Id)
                ?? throw new InvalidOperationException("Approval request not found after save."));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error submitting approval request"); throw; }
    }

    public async Task<ApprovalRequestDto> ApproveStepAsync(Guid approvalRequestId, ApproveStepDto dto, Guid userId)
    {
        try
        {
            var approval = await _requestRepo.GetByIdWithStepsAsync(approvalRequestId)
                ?? throw new InvalidOperationException("Approval request not found.");

            var currentStep = GetCurrentStep(approval);
            if (!CanAct(currentStep, dto.ApproverEmployeeId))
                throw new InvalidOperationException("You are not authorised to act on this step.");

            var approvedStatusId = await ResolveLookupStatusIdAsync(StatusCodeApproved);
            var pendingStatusId  = await ResolveLookupStatusIdAsync(StatusCodePending);

            // Mark current step APPROVED
            currentStep.StatusLookupValueId = approvedStatusId;
            currentStep.ApproverEmployeeId  = dto.ApproverEmployeeId;
            currentStep.Comments            = dto.Comments;
            currentStep.ActionDate          = DateTime.UtcNow;
            currentStep.UpdatedAt          = DateTime.UtcNow;
            currentStep.UpdatedByUserId    = userId;
            _stepRepo.Update(currentStep);

            var nextLevel = approval.CurrentLevel + 1;
            var nextStep  = approval.Steps!.FirstOrDefault(s => s.StepLevel == nextLevel && !s.IsDeleted);

            if (nextStep != null)
            {
                // Activate next step: WAITING ? PENDING
                nextStep.StatusLookupValueId = pendingStatusId;
                nextStep.UpdatedAt          = DateTime.UtcNow;
                nextStep.UpdatedByUserId    = userId;
                _stepRepo.Update(nextStep);

                approval.CurrentLevel = nextLevel;

                _logger.LogInformation(
                    "Approval {Code} advanced to level {Level}", approval.ApprovalRequestCode, nextLevel);
            }
            else
            {
                // All levels approved ? fully approved
                approval.OverallStatusLookupValueId = approvedStatusId;
                approval.CompletedAt                = DateTime.UtcNow;

                // Write back to the source entity
                await FinaliseEntityAsync(approval.EntityType, approval.EntityId, approved: true, userId);

                _logger.LogInformation(
                    "Approval {Code} fully approved — entity {EntityType}/{EntityId} updated",
                    approval.ApprovalRequestCode, approval.EntityType, approval.EntityId);
            }

            approval.UpdatedAt       = DateTime.UtcNow;
            approval.UpdatedByUserId = userId;
            _requestRepo.Update(approval);
            await _requestRepo.SaveChangesAsync();

            return MapFull((await _requestRepo.GetByIdWithStepsAsync(approvalRequestId))!);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error approving step for request {Id}", approvalRequestId); throw; }
    }

    public async Task<ApprovalRequestDto> RejectStepAsync(Guid approvalRequestId, RejectStepDto dto, Guid userId)
    {
        try
        {
            var approval = await _requestRepo.GetByIdWithStepsAsync(approvalRequestId)
                ?? throw new InvalidOperationException("Approval request not found.");

            var currentStep = GetCurrentStep(approval);
            if (!CanAct(currentStep, dto.ApproverEmployeeId))
                throw new InvalidOperationException("You are not authorised to act on this step.");

            var rejectedStatusId  = await ResolveLookupStatusIdAsync(StatusCodeRejected);
            var cancelledStatusId = await ResolveLookupStatusIdAsync(StatusCodeCancelled);

            // Mark current step REJECTED
            currentStep.StatusLookupValueId = rejectedStatusId;
            currentStep.ApproverEmployeeId  = dto.ApproverEmployeeId;
            currentStep.RejectionReason     = dto.RejectionReason;
            currentStep.RejectionCategory   = dto.RejectionCategory;
            currentStep.Comments            = dto.Comments;
            currentStep.ActionDate          = DateTime.UtcNow;
            currentStep.UpdatedAt          = DateTime.UtcNow;
            currentStep.UpdatedByUserId    = userId;
            _stepRepo.Update(currentStep);

            // Cancel all remaining WAITING steps
            var waitingSteps = approval.Steps!
                .Where(s => !s.IsDeleted && s.StepLevel > approval.CurrentLevel)
                .ToList();

            foreach (var ws in waitingSteps)
            {
                ws.StatusLookupValueId = cancelledStatusId;
                ws.UpdatedAt          = DateTime.UtcNow;
                ws.UpdatedByUserId    = userId;
                _stepRepo.Update(ws);
            }

            // Mark the whole request REJECTED
            approval.OverallStatusLookupValueId = rejectedStatusId;
            approval.CompletedAt                = DateTime.UtcNow;
            approval.UpdatedAt                 = DateTime.UtcNow;
            approval.UpdatedByUserId           = userId;
            _requestRepo.Update(approval);

            // Write back to the source entity
            await FinaliseEntityAsync(approval.EntityType, approval.EntityId, approved: false, userId);

            await _requestRepo.SaveChangesAsync();

            _logger.LogInformation(
                "Approval {Code} rejected at level {Level} by employee {EmployeeId}",
                approval.ApprovalRequestCode, approval.CurrentLevel, dto.ApproverEmployeeId);

            return MapFull((await _requestRepo.GetByIdWithStepsAsync(approvalRequestId))!);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error rejecting step for request {Id}", approvalRequestId); throw; }
    }

    public async Task<ApprovalRequestDto> DelegateStepAsync(Guid approvalRequestId, DelegateStepDto dto, Guid userId)
    {
        try
        {
            var approval = await _requestRepo.GetByIdWithStepsAsync(approvalRequestId)
                ?? throw new InvalidOperationException("Approval request not found.");

            var currentStep = GetCurrentStep(approval);
            if (!CanAct(currentStep, dto.FromEmployeeId))
                throw new InvalidOperationException("You are not authorised to delegate this step.");

            currentStep.DelegatedToEmployeeId = dto.ToEmployeeId;
            currentStep.Comments              = dto.Comments;
            currentStep.UpdatedAt            = DateTime.UtcNow;
            currentStep.UpdatedByUserId      = userId;
            _stepRepo.Update(currentStep);

            approval.UpdatedAt       = DateTime.UtcNow;
            approval.UpdatedByUserId = userId;
            _requestRepo.Update(approval);
            await _requestRepo.SaveChangesAsync();

            _logger.LogInformation(
                "Approval step {Level} for {Code} delegated from {From} to {To}",
                currentStep.StepLevel, approval.ApprovalRequestCode,
                dto.FromEmployeeId, dto.ToEmployeeId);

            return MapFull((await _requestRepo.GetByIdWithStepsAsync(approvalRequestId))!);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error delegating step for request {Id}", approvalRequestId); throw; }
    }

    public async Task<ApprovalRequestDto> CancelAsync(Guid approvalRequestId, string? reason, Guid userId)
    {
        try
        {
            var approval = await _requestRepo.GetByIdWithStepsAsync(approvalRequestId)
                ?? throw new InvalidOperationException("Approval request not found.");

            var cancelledStatusId = await ResolveLookupStatusIdAsync(StatusCodeCancelled);
            var approvedStatusId  = await ResolveLookupStatusIdAsync(StatusCodeApproved);
            var rejectedStatusId  = await ResolveLookupStatusIdAsync(StatusCodeRejected);

            if (approval.OverallStatusLookupValueId == approvedStatusId ||
                approval.OverallStatusLookupValueId == rejectedStatusId)
                throw new InvalidOperationException("Cannot cancel an already completed approval request.");

            approval.OverallStatusLookupValueId = cancelledStatusId;
            approval.CompletedAt                = DateTime.UtcNow;
            approval.Comments                   = reason ?? approval.Comments;
            approval.UpdatedAt                 = DateTime.UtcNow;
            approval.UpdatedByUserId           = userId;
            _requestRepo.Update(approval);

            foreach (var step in approval.Steps!.Where(s => !s.IsDeleted &&
                s.StatusLookupValueId != approvedStatusId && s.StatusLookupValueId != rejectedStatusId))
            {
                step.StatusLookupValueId = cancelledStatusId;
                step.UpdatedAt          = DateTime.UtcNow;
                step.UpdatedByUserId    = userId;
                _stepRepo.Update(step);
            }

            await _requestRepo.SaveChangesAsync();
            _logger.LogInformation("Approval request {Code} cancelled", approval.ApprovalRequestCode);

            return MapFull((await _requestRepo.GetByIdWithStepsAsync(approvalRequestId))!);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling approval request {Id}", approvalRequestId); throw; }
    }

    public async Task<ApprovalRequestDto> UpdateAsync(Guid id, UpdateApprovalRequestDto request, Guid userId)
    {
        try
        {
            var entity = await _requestRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException("Approval request not found.");

            if (request.PriorityLookupValueId.HasValue) entity.PriorityLookupValueId = request.PriorityLookupValueId.Value;
            if (request.Comments != null)             entity.Comments             = request.Comments;
            if (request.ApprovalSubjectTitle != null) entity.ApprovalSubjectTitle = request.ApprovalSubjectTitle;
            if (request.ApprovalSummary != null)      entity.ApprovalSummary      = request.ApprovalSummary;
            if (request.ApprovalDisplayName != null)  entity.ApprovalDisplayName  = request.ApprovalDisplayName;

            entity.UpdatedAt       = DateTime.UtcNow;
            entity.UpdatedByUserId = userId;
            _requestRepo.Update(entity);
            await _requestRepo.SaveChangesAsync();

            return MapFull((await _requestRepo.GetByIdWithStepsAsync(id))!);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating approval request {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _requestRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException("Approval request not found.");

            entity.IsDeleted       = true;
            entity.DeletedAt       = DateTime.UtcNow;
            entity.DeletedByUserId = userId;
            _requestRepo.Update(entity);
            await _requestRepo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting approval request {Id}", id); throw; }
    }

    // Condition engine

    /// <summary>
    /// Evaluates WorkflowConditions against the entity data snapshot and returns
    /// the final ordered list of WorkflowConfigSteps that will become approval steps.
    ///
    /// Supported ActionTypes:
    ///   ADD_STEP      — Inserts extra step(s) when the condition is met.
    ///                   ActionValue = "ApproverType:ApproverValue:LevelInsertAfter"
    ///                   Example: "EMPLOYEE:finance-mgr-guid:2" inserts a Finance step after L2.
    ///   REPLACE_CHAIN — Discards all default steps and replaces with a single override.
    ///                   ActionValue = "ApproverType:ApproverValue"
    ///                   Example: "EMPLOYEE:ceo-guid" — only CEO approves.
    /// </summary>
    private List<WorkflowConfigStep> BuildFinalStepChain(
        List<WorkflowConfigStep> baseSteps,
        List<WorkflowCondition> conditions,
        Dictionary<string, string> entityData)
    {
        // Check for REPLACE_CHAIN first (highest priority override)
        foreach (var condition in conditions.Where(c => c.ActionType == ConditionActionReplaceChain))
        {
            if (!EvaluateCondition(condition, entityData)) continue;

            _logger.LogInformation(
                "Condition REPLACE_CHAIN triggered for field '{Field}' {Op} '{Value}'",
                condition.FieldName, condition.Operator, condition.FieldValue);

            // Build a synthetic override step using the first base step as template
            var template = baseSteps[0];
            var parts    = (condition.ActionValue ?? "").Split(':', 2);
            var overrideStep = CloneStep(template, overrideApproverType: parts.ElementAtOrDefault(0),
                                                    overrideApproverValue: parts.ElementAtOrDefault(1));
            return [overrideStep];
        }

        // Process ADD_STEP conditions — may inject extra steps into the chain
        var result = new List<WorkflowConfigStep>(baseSteps);

        foreach (var condition in conditions.Where(c => c.ActionType == ConditionActionAddStep))
        {
            if (!EvaluateCondition(condition, entityData)) continue;

            // ActionValue format: "ApproverType:ApproverValue:InsertAfterLevel"
            var parts           = (condition.ActionValue ?? "").Split(':', 3);
            var approverType    = parts.ElementAtOrDefault(0) ?? "EMPLOYEE";
            var approverValue   = parts.ElementAtOrDefault(1) ?? string.Empty;
            var insertAfterLevel = int.TryParse(parts.ElementAtOrDefault(2), out var lvl) ? lvl : result.Count;

            _logger.LogInformation(
                "Condition ADD_STEP triggered — injecting {ApproverType}/{ApproverValue} after level {Level}",
                approverType, approverValue, insertAfterLevel);

            var template   = baseSteps[0];
            var extraStep  = CloneStep(template, overrideApproverType: approverType,
                                                  overrideApproverValue: approverValue);
            // Insert at the correct position (after insertAfterLevel, 0-based index)
            var insertAt = Math.Min(insertAfterLevel, result.Count);
            result.Insert(insertAt, extraStep);
        }

        return result;
    }

    /// <summary>
    /// Evaluates a single WorkflowCondition against the entity data dictionary.
    /// Supported Operators: ==, !=, >, >=, &lt;, &lt;=, CONTAINS, NOT_CONTAINS
    /// </summary>
    private static bool EvaluateCondition(WorkflowCondition condition, Dictionary<string, string> entityData)
    {
        if (!entityData.TryGetValue(condition.FieldName, out var actualValue))
            return false;

        var expected = condition.FieldValue;

        return condition.Operator switch
        {
            "==" or "EQ"           => string.Equals(actualValue, expected, StringComparison.OrdinalIgnoreCase),
            "!=" or "NEQ"          => !string.Equals(actualValue, expected, StringComparison.OrdinalIgnoreCase),
            ">"  or "GT"           => TryCompareDecimal(actualValue, expected, (a, b) => a > b),
            ">=" or "GTE"          => TryCompareDecimal(actualValue, expected, (a, b) => a >= b),
            "<"  or "LT"           => TryCompareDecimal(actualValue, expected, (a, b) => a < b),
            "<=" or "LTE"          => TryCompareDecimal(actualValue, expected, (a, b) => a <= b),
            "CONTAINS"             => actualValue.Contains(expected, StringComparison.OrdinalIgnoreCase),
            "NOT_CONTAINS"         => !actualValue.Contains(expected, StringComparison.OrdinalIgnoreCase),
            _                      => false
        };
    }

    private static bool TryCompareDecimal(string a, string b, Func<decimal, decimal, bool> compare)
        => decimal.TryParse(a, out var da) && decimal.TryParse(b, out var db) && compare(da, db);

    /// <summary>Creates a shallow copy of a WorkflowConfigStep with optional approver overrides.</summary>
    private static WorkflowConfigStep CloneStep(
        WorkflowConfigStep template,
        string? overrideApproverType  = null,
        string? overrideApproverValue = null) => new()
    {
        Id               = template.Id,     // same config step reference for traceability
        WorkflowConfigId = template.WorkflowConfigId,
        LevelNo          = template.LevelNo,
        ApproverType     = overrideApproverType  ?? template.ApproverType,
        ApproverValue    = overrideApproverValue ?? template.ApproverValue,
        Mandatory        = template.Mandatory,
        SLAHours         = template.SLAHours,
        ExecutionType    = template.ExecutionType,
        SortOrder        = template.SortOrder,
        IsConditional    = true
    };

    // Entity write-back

    /// <summary>
    /// Called after the final approve/reject to update the source entity's status.
    /// Currently handles: Requisition (Job entity).
    /// Extend the switch for OfferLetter, PromotionRequest, etc.
    /// </summary>
    private async Task FinaliseEntityAsync(string entityType, Guid entityId, bool approved, Guid userId)
    {
        try
        {
            switch (entityType)
            {
                case "Requisition":
                    var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == entityId && !j.IsDeleted);
                    if (job == null) break;

                    var statusCode  = approved ? "JOB_APPROVED" : "JOB_REJECTED";
                    var statusValue = await _context.LookupValues
                        .AsNoTracking()
                        .FirstOrDefaultAsync(v => v.Code == statusCode && v.IsActive && !v.IsDeleted);

                    if (statusValue != null)
                        job.StatusLookupValueId = statusValue.Id;

                    job.UpdatedAt       = DateTime.UtcNow;
                    job.RecordType = Nexcore.SharedKernel.Enums.JobRecordType.Job;
                    job.UpdatedByUserId = userId;
                    _context.Jobs.Update(job);

                    _logger.LogInformation(
                        "Job Requisition {JobId} status set to {Status} after approval finalisation",
                        entityId, statusCode);
                    break;

                default:
                    _logger.LogDebug(
                        "No entity write-back configured for EntityType '{EntityType}'", entityType);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Write-back failure must not roll back the approval itself — log and continue
            _logger.LogError(ex, "Error finalising entity {EntityType}/{EntityId}", entityType, entityId);
        }
    }

    // Private helpers

    private async Task<Guid> ResolveLookupStatusIdAsync(string code)
    {
        // Resolve tenant context so the query only matches values belonging to this tenant.
        var user = _httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("HTTP context not available when resolving lookup status.");

        var (companyId, branchId, businessUnitId) = TenantContextHelper.ExtractTenantContext(user);
        var resolvedBusinessUnitId = businessUnitId ?? Guid.Empty;

        var val = await _context.LookupValues
            .AsNoTracking()
            .FirstOrDefaultAsync(v =>
                v.Code == code &&
                v.IsActive &&
                !v.IsDeleted &&
                v.CompanyId      == companyId &&
                v.BranchId       == branchId &&
                v.BusinessUnitId == resolvedBusinessUnitId);

        if (val == null)
            throw new InvalidOperationException(
                $"Required LookupValue with code '{code}' was not found for this tenant. " +
                $"Please run POST /api/v1/hr/seed/lookup-values to initialise the approval status lookup values.");

        return val.Id;
    }

    private static ApprovalRequestStep GetCurrentStep(ApprovalRequest approval)
        => approval.Steps?
            .Where(s => s.StepLevel == approval.CurrentLevel && !s.IsDeleted)
            .FirstOrDefault()
           ?? throw new InvalidOperationException(
               $"No step found at level {approval.CurrentLevel} for approval {approval.ApprovalRequestCode}.");

    /// <summary>
    /// An employee can act if:
    ///   - They are the explicitly assigned approver, OR
    ///   - The step was delegated to them.
    /// A null ApproverEmployeeId means the step is role-based — any bearer of that role can act,
    /// so the controller/caller is responsible for role validation before calling this service.
    /// </summary>
    private static bool CanAct(ApprovalRequestStep step, Guid employeeId)
        => step.DelegatedToEmployeeId == employeeId ||
           step.ApproverEmployeeId    == employeeId ||
           step.ApproverEmployeeId    == null;

    // Mappers

    private static ApprovalRequestDto MapHeader(ApprovalRequest a) => new()
    {
        Id                         = a.Id,
        CompanyId                  = a.CompanyId,
        ApprovalRequestCode        = a.ApprovalRequestCode,
        EntityType                 = a.EntityType,
        EntityId                   = a.EntityId,
        WorkflowConfigId           = a.WorkflowConfigId,
        RequestedByEmployeeId      = a.RequestedByEmployeeId,
        CurrentLevel               = a.CurrentLevel,
        TotalLevels                = a.TotalLevels,
        OverallStatusLookupValueId = a.OverallStatusLookupValueId,
        PriorityLookupValueId      = a.PriorityLookupValueId,
        RequestedAt                = a.RequestedAt,
        CompletedAt                = a.CompletedAt,
        Comments                   = a.Comments,
        ApprovalSubjectCode        = a.ApprovalSubjectCode,
        ApprovalSubjectTitle       = a.ApprovalSubjectTitle,
        ApprovalSummary            = a.ApprovalSummary,
        ApprovalDisplayName        = a.ApprovalDisplayName,
        CreatedAt                  = a.CreatedAt,
        UpdatedAt                 = a.UpdatedAt,
        Steps                      = []
    };

    private static ApprovalRequestDto MapFull(ApprovalRequest a)
    {
        var dto   = MapHeader(a);
        dto.Steps = (a.Steps ?? [])
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.StepLevel)
            .Select(s => new ApprovalRequestStepDto
            {
                Id                    = s.Id,
                ApprovalRequestId     = s.ApprovalRequestId,
                WorkflowConfigStepId  = s.WorkflowConfigStepId,
                StepLevel             = s.StepLevel,
                ApproverType          = s.ApproverType,
                ApproverValue         = s.ApproverValue,
                ApproverEmployeeId    = s.ApproverEmployeeId,
                Mandatory             = s.Mandatory,
                SLAHours              = s.SLAHours,
                ExecutionType         = s.ExecutionType,
                StatusLookupValueId   = s.StatusLookupValueId,
                Comments              = s.Comments,
                RejectionReason       = s.RejectionReason,
                RejectionCategory     = s.RejectionCategory,
                ActionDate            = s.ActionDate,
                DelegatedToEmployeeId = s.DelegatedToEmployeeId,
                EscalatedFlag         = s.EscalatedFlag,
                EscalatedToEmployeeId = s.EscalatedToEmployeeId,
                CreatedAt             = s.CreatedAt,
                UpdatedAt            = s.UpdatedAt
            }).ToList();
        return dto;
    }
}


/// <summary>
/// Handles the full approval lifecycle:
///   Submit ? creates ApprovalRequest + one ApprovalRequestStep per WorkflowConfigStep
///   Approve ? marks current step Approved, advances CurrentLevel or completes the request
///   Reject  ? marks current step Rejected, marks whole request Rejected
///   Delegate ? reassigns the current step to a different employee
///   Cancel  ? cancels a pending request
/// </summary>
