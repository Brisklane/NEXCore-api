namespace Nexcore.SharedKernel.Events;

/// <summary>
/// Published by any module that needs the 3-letter city code for a city.
/// Core.Infrastructure handles this event and completes <see cref="Result"/>.
/// Returns null when the city is not found or has no code configured.
/// Uses the TCS request/response pattern — the caller awaits <see cref="Result.Task"/>
/// within the same request without polling.
/// </summary>
public class CityCodeLookupEvent
{
    public int CityId { get; init; }

    /// <summary>Completed by the Core handler with the city code, or null if not found.</summary>
    public TaskCompletionSource<string?> Result { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
