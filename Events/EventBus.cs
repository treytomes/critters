namespace Critters.Events;

public class EventBus
{
	#region Fields

	private readonly Dictionary<Type, List<Delegate>> _handlers = new();
	private readonly object _lock = new();

	#endregion

	#region Methods

	/// <summary>
	/// Register a handler for a specific event type.
	/// </summary>
	/// <typeparam name="TEvent"></typeparam>
	/// <param name="handler"></param>
	public void Subscribe<TEvent>(Action<TEvent> handler)
	{
		lock (_lock)
		{
			var eventType = typeof(TEvent);
			if (!_handlers.ContainsKey(eventType))
			{
				_handlers[eventType] = new List<Delegate>();
			}
			_handlers[eventType].Add(handler);
		}
	}

	/// <summary>
	/// Remove a handler for a specific event type.
	/// </summary>
	public void Unsubscribe<TEvent>(Action<TEvent> handler)
	{
		lock (_lock)
		{
			var eventType = typeof(TEvent);
			if (_handlers.ContainsKey(eventType))
			{
				_handlers[eventType].Remove(handler);
				if (_handlers[eventType].Count == 0)
				{
					_handlers.Remove(eventType);
				}
			}
		}
	}

	/// <summary>
	/// Publish an event to all registered handlers.
	/// </summary>
	public void Publish<TEvent>(TEvent eventData)
	{
		lock (_lock)
		{
			var eventType = typeof(TEvent);
			if (!_handlers.ContainsKey(eventType))
			{
				return;
			}

			var handlers = _handlers[eventType].ToList();
			foreach (var handler in handlers)
			{
				try
				{
					((Action<TEvent>)handler)(eventData);
				}
				catch (Exception ex)
				{
					// Log or handle the exception as needed
					Console.WriteLine($"Error handling event: {ex.Message}");
				}
			}
		}
	}

	/// <summary>
	/// Async version of Publish.
	/// </summary>
	public async Task PublishAsync<TEvent>(TEvent eventData)
	{
		List<Delegate> handlers;
		lock (_lock)
		{
			var eventType = typeof(TEvent);
			if (!_handlers.ContainsKey(eventType))
			{
				return;
			}
			handlers = _handlers[eventType].ToList();
		}

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
					// Log or handle the exception as needed
					Console.WriteLine($"Error handling event: {ex.Message}");
				}
			}));
		}

		await Task.WhenAll(tasks);
	}

	#endregion
}
