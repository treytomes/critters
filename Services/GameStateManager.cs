using Critters.Core;
using Critters.Events;
using Critters.States;
using Microsoft.Extensions.Logging;
using OpenTK.Windowing.Common;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Critters.Services;

/// <summary>
/// Manages the lifecycle and transitions between game states.
/// Handles a stack-based state system where only the top state is active.
/// </summary>
class GameStateManager : Disposable, IGameStateManager
{
	#region Fields

	private readonly ILogger<GameStateManager> _logger;
	private readonly Stack<GameState> _states = new();
	private readonly Dictionary<GameState, IDisposable> _stateSubscriptions = new();
	private readonly ReaderWriterLockSlim _stateLock = new(LockRecursionPolicy.SupportsRecursion);
	private readonly Subject<StateTransitionEventArgs> _stateTransitionSubject = new();
	private GameState? _currentState;
	private bool _isInitialized;

	#endregion

	#region Properties

	/// <summary>
	/// Gets an observable sequence of state transition events.
	/// </summary>
	public IObservable<StateTransitionEventArgs> StateTransitions => _stateTransitionSubject;

	/// <summary>
	/// Gets a value indicating whether there is at least one state in the stack.
	/// </summary>
	public bool HasState
	{
		get
		{
			ThrowIfDisposed();
			_stateLock.EnterReadLock();
			try
			{
				return _states.Count > 0;
			}
			finally
			{
				_stateLock.ExitReadLock();
			}
		}
	}

	/// <summary>
	/// Gets the number of states in the stack.
	/// </summary>
	public int StateCount
	{
		get
		{
			ThrowIfDisposed();
			_stateLock.EnterReadLock();
			try
			{
				return _states.Count;
			}
			finally
			{
				_stateLock.ExitReadLock();
			}
		}
	}

	/// <summary>
	/// Gets the currently active game state.
	/// </summary>
	public GameState? CurrentState
	{
		get
		{
			ThrowIfDisposed();
			_stateLock.EnterReadLock();
			try
			{
				return _currentState;
			}
			finally
			{
				_stateLock.ExitReadLock();
			}
		}
		private set
		{
			_stateLock.EnterWriteLock();
			try
			{
				_currentState = value;
			}
			finally
			{
				_stateLock.ExitWriteLock();
			}
		}
	}

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="GameStateManager"/> class.
	/// </summary>
	/// <param name="logger">The logger for diagnostic information.</param>
	/// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
	public GameStateManager(ILogger<GameStateManager> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_logger.LogDebug("GameStateManager initialized");
	}

	#endregion

	#region Methods

	/// <summary>
	/// Loads the state manager and initializes it.
	/// </summary>
	public void Load()
	{
		ThrowIfDisposed();

		_logger.LogDebug("Loading GameStateManager");

		_stateLock.EnterWriteLock();
		try
		{
			if (_isInitialized)
			{
				_logger.LogWarning("GameStateManager is already initialized");
				return;
			}

			_isInitialized = true;
		}
		finally
		{
			_stateLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Unloads the state manager and clears all states.
	/// </summary>
	public void Unload()
	{
		ThrowIfDisposed();
		_logger.LogDebug("Unloading GameStateManager");
		CleanupStates();

		_stateLock.EnterWriteLock();
		try
		{
			_isInitialized = false;
		}
		finally
		{
			_stateLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Pushes a new state onto the stack and makes it the active state.
	/// </summary>
	/// <param name="state">The state to enter.</param>
	/// <exception cref="ArgumentNullException">Thrown when state is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the state is already in the stack.</exception>
	public void EnterState(GameState state)
	{
		ThrowIfDisposed();

		if (state == null)
		{
			throw new ArgumentNullException(nameof(state));
		}

		GameState? previousState = null;
		IDisposable? subscription = null;

		try
		{
			_logger.LogInformation("Entering state: {StateType}", state.GetType().Name);

			// Subscribe to the state's transition requests
			subscription = state.TransitionRequests.Subscribe(request =>
			{
				switch (request.Type)
				{
					case TransitionType.Enter:
						if (request.TargetState != null)
						{
							EnterState(request.TargetState);
						}
						break;
					case TransitionType.Leave:
						LeaveState();
						break;
					case TransitionType.ClearAll:
						ClearAllStates();
						break;
				}
			});

			_stateLock.EnterWriteLock();
			try
			{
				// Validate the state isn't already in our stack
				if (_states.Contains(state))
				{
					throw new InvalidOperationException($"State {state.GetType().Name} is already in the state stack");
				}

				previousState = CurrentState;
				previousState?.LostFocus();

				_states.Push(state);
				state.Load();
				CurrentState = state;

				// Store the subscription with the state
				_stateSubscriptions[state] = subscription;

				CurrentState.AcquireFocus();
			}
			finally
			{
				_stateLock.ExitWriteLock();
			}

			// Notify about the transition outside of the lock
			NotifyStateTransition(new StateTransitionEventArgs(
				TransitionType.Enter,
				previousState,
				state));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error entering state {StateType}", state.GetType().Name);

			// Clean up the subscription if we failed
			subscription?.Dispose();

			// Try to recover by removing the problematic state
			_stateLock.EnterWriteLock();
			try
			{
				if (_states.Count > 0 && _states.Peek() == state)
				{
					_states.Pop();
					CurrentState = _states.Count > 0 ? _states.Peek() : null;

					// Restore focus to the previous state
					if (previousState != null && CurrentState == previousState)
					{
						CurrentState.AcquireFocus();
					}
				}
			}
			finally
			{
				_stateLock.ExitWriteLock();
			}

			throw;
		}
	}

	/// <summary>
	/// Removes the top state from the stack and activates the next state.
	/// </summary>
	/// <returns>True if a state was removed; otherwise, false.</returns>
	public bool LeaveState()
	{
		ThrowIfDisposed();

		try
		{
			GameState? stateToLeave = null;
			GameState? nextState = null;

			_stateLock.EnterWriteLock();
			try
			{
				if (_states.Count == 0)
				{
					_logger.LogWarning("Attempted to leave state when no states exist");
					return false;
				}

				stateToLeave = CurrentState;
				string stateType = stateToLeave?.GetType().Name ?? "Unknown";
				_logger.LogInformation("Leaving state: {StateType}", stateType);

				stateToLeave?.LostFocus();
				stateToLeave?.Unload();
				_states.Pop();

				// Unsubscribe from the state's transition requests
				if (stateToLeave != null && _stateSubscriptions.TryGetValue(stateToLeave, out var subscription))
				{
					subscription.Dispose();
					_stateSubscriptions.Remove(stateToLeave);
				}

				CurrentState = _states.Count > 0 ? _states.Peek() : null;
				nextState = CurrentState;
				nextState?.AcquireFocus();
			}
			finally
			{
				_stateLock.ExitWriteLock();
			}

			// Notify about the transition outside of the lock
			if (stateToLeave != null)
			{
				NotifyStateTransition(new StateTransitionEventArgs(
					TransitionType.Leave,
					stateToLeave,
					nextState));
			}

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error leaving state");
			throw;
		}
	}

	/// <summary>
	/// Clears all states from the stack.
	/// </summary>
	public void ClearAllStates()
	{
		ThrowIfDisposed();

		_logger.LogInformation("Clearing all states");

		_stateLock.EnterWriteLock();
		try
		{
			while (_states.Count > 0)
			{
				var state = _states.Pop();
				try
				{
					state.LostFocus();
					state.Unload();

					// Unsubscribe from the state's transition requests
					if (_stateSubscriptions.TryGetValue(state, out var subscription))
					{
						subscription.Dispose();
						_stateSubscriptions.Remove(state);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error unloading state {StateType} during clear operation",
						state.GetType().Name);
				}
			}

			CurrentState = null;
		}
		finally
		{
			_stateLock.ExitWriteLock();
		}

		NotifyStateTransition(new StateTransitionEventArgs(
			TransitionType.ClearAll,
			null,
			null));
	}

	/// <summary>
	/// Returns the state at the specified depth in the stack without modifying the stack.
	/// </summary>
	/// <param name="depth">The depth to peek at (0 is the top).</param>
	/// <returns>The state at the specified depth, or null if the depth is invalid.</returns>
	public GameState? PeekState(int depth = 0)
	{
		ThrowIfDisposed();

		if (depth < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(depth), "Depth cannot be negative");
		}

		_stateLock.EnterReadLock();
		try
		{
			if (depth >= _states.Count)
			{
				return null;
			}

			// Convert stack to array to access by index
			var statesArray = _states.ToArray();
			return statesArray[depth];
		}
		finally
		{
			_stateLock.ExitReadLock();
		}
	}

	public bool KeyDown(KeyboardKeyEventArgs e)
	{
		return CurrentState?.KeyDown(e) ?? false;
	}

	public bool KeyUp(KeyboardKeyEventArgs e)
	{
		return CurrentState?.KeyUp(e) ?? false;
	}

	public bool MouseDown(MouseButtonEventArgs e)
	{
		return CurrentState?.MouseDown(e) ?? false;
	}

	public bool MouseUp(MouseButtonEventArgs e)
	{
		return CurrentState?.MouseUp(e) ?? false;
	}

	public bool MouseMove(MouseMoveEventArgs e)
	{
		return CurrentState?.MouseMove(e) ?? false;
	}

	public bool MouseWheel(MouseWheelEventArgs e)
	{
		return CurrentState?.MouseWheel(e) ?? false;
	}

	public bool TextInput(TextInputEventArgs e)
	{
		return CurrentState?.TextInput(e) ?? false;
	}

	/// <summary>
	/// Renders the current state.
	/// </summary>
	/// <param name="gameTime">The game timing information.</param>
	public void Render(GameTime gameTime)
	{
		ThrowIfDisposed();

		GameState? stateToRender;

		_stateLock.EnterReadLock();
		try
		{
			stateToRender = _currentState;
		}
		finally
		{
			_stateLock.ExitReadLock();
		}

		stateToRender?.Render(gameTime);
	}

	/// <summary>
	/// Updates the current state.
	/// </summary>
	/// <param name="gameTime">The game timing information.</param>
	public void Update(GameTime gameTime)
	{
		ThrowIfDisposed();

		GameState? stateToUpdate;

		_stateLock.EnterReadLock();
		try
		{
			stateToUpdate = _currentState;
		}
		finally
		{
			_stateLock.ExitReadLock();
		}

		stateToUpdate?.Update(gameTime);
	}

	private void CleanupStates()
	{
		try
		{
			_stateLock.EnterWriteLock();
			try
			{
				while (_states.Count > 0)
				{
					var state = _states.Pop();
					try
					{
						state.LostFocus();
						state.Unload();

						// Dispose the subscription for this state
						if (_stateSubscriptions.TryGetValue(state, out var subscription))
						{
							subscription.Dispose();
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error unloading state {StateType}", state.GetType().Name);
					}
				}

				// Clear all subscriptions
				_stateSubscriptions.Clear();
				CurrentState = null;
			}
			finally
			{
				_stateLock.ExitWriteLock();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during state cleanup");
		}
	}

	/// <summary>
	/// Notifies subscribers about a state transition.
	/// </summary>
	/// <param name="args">The state transition event arguments.</param>
	private void NotifyStateTransition(StateTransitionEventArgs args)
	{
		if (!IsDisposed)
		{
			try
			{
				_stateTransitionSubject.OnNext(args);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error publishing state transition event");
			}
		}
	}




	/// <summary>
	/// Disposes managed resources used by the GameStateManager.
	/// </summary>
	protected override void DisposeManagedResources()
	{
		_logger.LogDebug("Disposing GameStateManager");

		// Clean up states and subscriptions
		CleanupStates();

		// Dispose the state lock
		_stateLock.Dispose();

		// Complete and dispose the subject
		try
		{
			_stateTransitionSubject.OnCompleted();
			_stateTransitionSubject.Dispose();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error disposing state transition subject");
		}
	}

	#endregion
}