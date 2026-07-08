namespace Nexcore.SharedKernel.Events;

/// <summary>
/// Event publisher interface for domain events
/// Allows modules to publish events in a decoupled manner
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publish a domain event
    /// </summary>
    /// <typeparam name="TEvent">Type of event to publish</typeparam>
    /// <param name="domainEvent">The event instance to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
        where TEvent : class;
}
