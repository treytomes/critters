using Critters.States;
using Microsoft.Extensions.Logging;

namespace Critters.Services;

class GameStateManager : IGameStateManager
{
	#region Fields  

	private readonly IResourceManager _resources;
	private readonly IEventBus _eventBus;
	private readonly ILogger<GameStateManager> _logger;
	private readonly Stack<GameState> _states = new();
	private readonly object _stateLock = new();
	private GameState? _currentState;
	private bool _isDisposed;

	#endregion

	#region Constructors

	public GameStateManager(
		IResourceManager resources,
		IEventBus eventBus,
		ILogger<GameStateManager> logger)
	{
		_resources = resources ?? throw new ArgumentNullException(nameof(resources));
		_eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		_logger.LogDebug("GameStateManager initialized");
	}

	#endregion

	#region Properties  

	public bool HasState
	{
		get
		{
			lock (_stateLock)
			{
				return _states.Count > 0;
			}
		}
	}

	public GameState? CurrentState
	{
		get
		{
			lock (_stateLock)
			{
				return _currentState;
			}
		}
		private set
		{
			lock (_stateLock)
			{
				_currentState = value;
			}
		}
	}

	protected IResourceManager Resources => _resources;

	#endregion

	#region Methods  

	public void Load()
	{
		_logger.LogDebug("Loading GameStateManager");
		_eventBus.Subscribe<LeaveGameStateEventArgs>(OnLeave);
		_eventBus.Subscribe<EnterGameStateEventArgs>(OnEnter);
	}

	public void Unload()
	{
		try
		{
			_logger.LogDebug("Unloading GameStateManager");
			lock (_stateLock)
			{
				while (HasState)
				{
					LeaveState();
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during state manager unload");
		}
		finally
		{
			_eventBus.Unsubscribe<LeaveGameStateEventArgs>(OnLeave);
			_eventBus.Unsubscribe<EnterGameStateEventArgs>(OnEnter);
		}
	}

	public void EnterState(GameState state)
	{
		if (state == null)
		{
			throw new ArgumentNullException(nameof(state));
		}

		try
		{
			_logger.LogInformation("Entering state: {StateType}", state.GetType().Name);

			lock (_stateLock)
			{
				CurrentState?.LostFocus();
				_states.Push(state);
				state.Load();
				CurrentState = state;
				CurrentState.AcquireFocus();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error entering state {StateType}", state.GetType().Name);

			// Try to recover by removing the problematic state  
			lock (_stateLock)
			{
				if (_states.Count > 0 && _states.Peek() == state)
				{
					_states.Pop();
					CurrentState = _states.Count > 0 ? _states.Peek() : null;
				}
			}

			throw;
		}
	}

	public void LeaveState()
	{
		try
		{
			lock (_stateLock)
			{
				if (!HasState)
				{
					_logger.LogWarning("Attempted to leave state when no states exist");
					return;
				}

				var stateType = CurrentState?.GetType().Name;
				_logger.LogInformation("Leaving state: {StateType}", stateType);

				CurrentState?.LostFocus();
				CurrentState?.Unload();
				_states.Pop();
				CurrentState = _states.Count > 0 ? _states.Peek() : null;
				CurrentState?.AcquireFocus();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error leaving state");
			throw;
		}
	}

	public void Render(GameTime gameTime)
	{
		var currentState = CurrentState;
		currentState?.Render(gameTime);
	}

	public void Update(GameTime gameTime)
	{
		var currentState = CurrentState;
		currentState?.Update(gameTime);
	}

	private void OnLeave(LeaveGameStateEventArgs e)
	{
		_logger.LogDebug("Received LeaveGameState event");
		LeaveState();
	}

	private void OnEnter(EnterGameStateEventArgs e)
	{
		if (e.State == null)
		{
			_logger.LogWarning("Received EnterGameState event with null state");
			throw new ArgumentException("State cannot be null", nameof(e));
		}

		_logger.LogDebug("Received EnterGameState event for {StateType}", e.State.GetType().Name);
		EnterState(e.State);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_isDisposed)
		{
			if (disposing)
			{
				// Dispose managed resources  
				try
				{
					_logger.LogDebug("Disposing GameStateManager");

					lock (_stateLock)
					{
						while (HasState)
						{
							LeaveState();
						}
					}

					_eventBus.Unsubscribe<LeaveGameStateEventArgs>(OnLeave);
					_eventBus.Unsubscribe<EnterGameStateEventArgs>(OnEnter);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error during GameStateManager disposal");
				}
			}

			_isDisposed = true;
		}
	}

	#endregion
}