// Services/IEventBus.cs  
namespace Critters.Services;

/// <summary>  
/// Defines the contract for an event bus that enables communication between components.  
/// </summary>  
public interface IEventBus
{
	/// <summary>  
	/// Registers a handler for a specific event type.  
	/// </summary>  
	/// <typeparam name="TEvent">The type of event to subscribe to.</typeparam>  
	/// <param name="handler">The handler to invoke when the event is published.</param>  
	void Subscribe<TEvent>(Action<TEvent> handler);

	/// <summary>  
	/// Removes a handler for a specific event type.  
	/// </summary>  
	/// <typeparam name="TEvent">The type of event to unsubscribe from.</typeparam>  
	/// <param name="handler">The handler to remove.</param>  
	void Unsubscribe<TEvent>(Action<TEvent> handler);

	/// <summary>  
	/// Publishes an event to all registered handlers.  
	/// </summary>  
	/// <typeparam name="TEvent">The type of event being published.</typeparam>  
	/// <param name="eventData">The event data to publish.</param>  
	void Publish<TEvent>(TEvent eventData);

	/// <summary>  
	/// Asynchronously publishes an event to all registered handlers.  
	/// </summary>  
	/// <typeparam name="TEvent">The type of event being published.</typeparam>  
	/// <param name="eventData">The event data to publish.</param>  
	/// <returns>A task representing the asynchronous operation.</returns>  
	Task PublishAsync<TEvent>(TEvent eventData);
}