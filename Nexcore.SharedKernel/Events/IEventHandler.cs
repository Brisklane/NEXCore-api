namespace Nexcore.SharedKernel.Events;

/// <summary>
/// Event handler interface for processing domain events
/// Modules implement this to handle specific events
/// </summary>
/// <typeparam name="TEvent">Type of event to handle</typeparam>
public interface IEventHandler<TEvent> where TEvent : class
{
    /// <summary>
    /// Handle the domain event
    /// </summary>
    /// <param name="domainEvent">The event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
