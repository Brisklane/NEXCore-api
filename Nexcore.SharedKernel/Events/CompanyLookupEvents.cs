namespace Nexcore.SharedKernel.Events;

/// <summary>
/// Published by any module that needs the raw bytes of a company's logo.
/// Core.Infrastructure handles this event and completes <see cref="Result"/>.
/// Uses the same TCS request/response pattern as <see cref="ItemGlLookupEvent"/>.
/// </summary>
public class CompanyLogoLookupEvent
{
    public Guid CompanyId { get; init; }

    /// <summary>Completed by the Core handler with the logo bytes, or null if no logo is set.</summary>
    public TaskCompletionSource<byte[]?> Result { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
