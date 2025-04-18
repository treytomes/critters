// Services/EventBus.cs

using Microsoft.Extensions.Logging;

namespace Critters.Services;

/// <summary>  
/// Provides a central event dispatching mechanism for the application.  
/// </summary>  
public class EventBus : IEventBus
{
	#region Fields  

	private readonly Dictionary<Type, List<Delegate>> _handlers = new();
	private readonly object _lock = new();
	private readonly ILogger<EventBus> _logger;

	#endregion

	#region Constructor  

	/// <summary>  
	/// Initializes a new instance of the <see cref="EventBus"/> class.  
	/// </summary>  
	/// <param name="logger">The logger for this class.</param>  
	public EventBus(ILogger<EventBus> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_logger.LogDebug("EventBus initialized");
	}

	#endregion

	#region IEventBus Implementation  

	/// <summary>  
	/// Registers a handler for a specific event type.  
	/// </summary>  
	/// <typeparam name="TEvent">The type of event to subscribe to.</typeparam>  
	/// <param name="handler">The handler to invoke when the event is published.</param>  
	public void Subscribe<TEvent>(Action<TEvent> handler)
	{
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}

		lock (_lock)
		{
			var eventType = typeof(TEvent);
			if (!_handlers.ContainsKey(eventType))
			{
				_handlers[eventType] = new List<Delegate>();
			}
			_handlers[eventType].Add(handler);
			_logger.LogDebug("Handler subscribed for event type {EventType}", eventType.Name);
		}
	}

	/// <summary>  
	/// Removes a handler for a specific event type.  
	/// </summary>  
	/// <typeparam name="TEvent">The type of event to unsubscribe from.</typeparam>  
	/// <param name="handler">The handler to remove.</param>  
	public void Unsubscribe<TEvent>(Action<TEvent> handler)
	{
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}

		lock (_lock)
		{
			var eventType = typeof(TEvent);
			if (_handlers.ContainsKey(eventType))
			{
				if (_handlers[eventType].Remove(handler))
				{
					_logger.LogDebug("Handler unsubscribed for event type {EventType}", eventType.Name);

					if (_handlers[eventType].Count == 0)
					{
						_handlers.Remove(eventType);
						_logger.LogDebug("Removed empty handler list for event type {EventType}", eventType.Name);
					}
				}
			}
		}
	}

	/// <summary>  
	/// Publishes an event to all registered handlers.  
	/// </summary>  
	/// <typeparam name="TEvent">The type of event being published.</typeparam>  
	/// <param name="eventData">The event data to publish.</param>  
	public void Publish<TEvent>(TEvent eventData)
	{
		if (eventData == null)
		{
			throw new ArgumentNullException(nameof(eventData));
		}

		List<Delegate> handlers;
		var eventType = typeof(TEvent);

		lock (_lock)
		{
			if (!_handlers.ContainsKey(eventType))
			{
				_logger.LogTrace("No handlers registered for event type {EventType}", eventType.Name);
				return;
			}

			handlers = new List<Delegate>(_handlers[eventType]);
		}

		_logger.LogTrace("Publishing event {EventType} to {HandlerCount} handlers", eventType.Name, handlers.Count);

		foreach (var handler in handlers)
		{
			try
			{
				((Action<TEvent>)handler)(eventData);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling event {EventType}", eventType.Name);
			}
		}
	}

	/// <summary>  
	/// Asynchronously publishes an event to all registered handlers.  
	/// </summary>  
	/// <typeparam name="TEvent">The type of event being published.</typeparam>  
	/// <param name="eventData">The event data to publish.</param>  
	/// <returns>A task representing the asynchronous operation.</returns>  
	public async Task PublishAsync<TEvent>(TEvent eventData)
	{
		if (eventData == null)
		{
			throw new ArgumentNullException(nameof(eventData));
		}

		List<Delegate> handlers;
		var eventType = typeof(TEvent);

		lock (_lock)
		{
			if (!_handlers.ContainsKey(eventType))
			{
				_logger.LogTrace("No handlers registered for event type {EventType}", eventType.Name);
				return;
			}

			handlers = new List<Delegate>(_handlers[eventType]);
		}

		_logger.LogTrace("Asynchronously publishing event {EventType} to {HandlerCount} handlers", eventType.Name, handlers.Count);

		var tasks = new List<Task>();
		foreach (var handler in handlers)
		{
			tasks.Add(Task.Run(() =>
			{
				try
				{
					((Action<TEvent>)handler)(eventData);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error handling event {EventType} asynchronously", eventType.Name);
				}
			}));
		}

		await Task.WhenAll(tasks);
	}

	#endregion
}