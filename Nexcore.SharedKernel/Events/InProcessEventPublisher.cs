using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nexcore.SharedKernel.Events;

/// <summary>
/// Dispatches events to their handlers synchronously, in-process. Each handler runs in its own
/// child DI scope so scoped dependencies (DbContext, repositories) are never pulled from the root
/// provider, and one handler throwing is logged and swallowed so it can't derail the others.
/// </summary>
public class InProcessEventPublisher : IEventPublisher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InProcessEventPublisher> _logger;

    public InProcessEventPublisher(
        IServiceScopeFactory scopeFactory,
        ILogger<InProcessEventPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventTypeName = typeof(TEvent).Name;

        // Discover registered handler concrete types via a short-lived scope
        Type[] handlerTypes;
        using (var discoveryScope = _scopeFactory.CreateScope())
        {
            var discovered = discoveryScope.ServiceProvider
                .GetServices<IEventHandler<TEvent>>()
                .Select(h => h.GetType())
                .ToArray();

            if (discovered.Length == 0)
            {
                _logger.LogDebug("No handlers registered for {EventType}", eventTypeName);
                return;
            }

            handlerTypes = discovered;
        }

        _logger.LogDebug("Publishing {EventType} to {Count} handler(s)", eventTypeName, handlerTypes.Length);

        // Execute each handler in its own isolated scope
        foreach (var handlerType in handlerTypes)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            try
            {
                // Resolve by interface (what IS registered), then match the concrete type
                var handler = scope.ServiceProvider
                    .GetServices<IEventHandler<TEvent>>()
                    .First(h => h.GetType() == handlerType);

                await handler.HandleAsync(domainEvent, cancellationToken);

                _logger.LogDebug("Handler {Handler} completed for {EventType}", handlerType.Name, eventTypeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Handler {Handler} threw an exception processing {EventType}",
                    handlerType.Name, eventTypeName);
            }
        }
    }
}
