namespace Nexcore.SharedKernel.Events;

/// <summary>
/// Outcome of a context-switch validation. <see cref="IsValid"/> is true only when the company
/// belongs to the requested tenant, the branch belongs to that company, and the business unit
/// belongs to that branch. CompanyName/CompanySlug are echoed back so the Auth module can build
/// the scoped token without its own access to Core data.
/// </summary>
public record ContextValidationResult(bool IsValid, string? CompanyName, string? CompanySlug)
{
    public static readonly ContextValidationResult Invalid = new(false, null, null);
}

/// <summary>
/// Published by the Auth module when a user switches their active company / branch / business unit.
/// Core.Infrastructure handles this event and completes <see cref="Result"/> after verifying the
/// company → branch → business unit chain is valid within the caller's tenant.
/// Uses the same TCS request/response pattern as <see cref="CompanyLogoLookupEvent"/>.
/// </summary>
public class ContextValidationRequestedEvent
{
    public Guid TenantId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid BusinessUnitId { get; init; }

    /// <summary>Completed by the Core handler with the validation outcome.</summary>
    public TaskCompletionSource<ContextValidationResult> Result { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
