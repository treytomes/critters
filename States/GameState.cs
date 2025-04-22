using Critters.Core;
using Critters.Gfx;
using Critters.Services;
using Critters.UI;
using OpenTK.Windowing.Common;
using System.Reactive.Subjects;

namespace Critters.States;

/// <summary>
/// Base class for all game states that can be managed by the GameStateManager.
/// Provides common functionality for UI management and state transitions.
/// </summary>
abstract class GameState : Disposable, IGameState
{
	#region Fields

	private readonly IResourceManager _resources;
	private readonly IRenderingContext _rc;
	private readonly Subject<GameStateTransitionRequest> _transitionRequests = new();

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="GameState"/> class.
	/// </summary>
	/// <param name="resources">Resource manager for loading assets.</param>
	/// <param name="rc">Rendering context for drawing.</param>
	/// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
	public GameState(
		IResourceManager resources,
		IRenderingContext rc)
	{
		_resources = resources ?? throw new ArgumentNullException(nameof(resources));
		_rc = rc ?? throw new ArgumentNullException(nameof(rc));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets an observable sequence of state transition requests.
	/// </summary>
	public IObservable<GameStateTransitionRequest> TransitionRequests => _transitionRequests;

	/// <summary>
	/// Gets a value indicating whether this state currently has focus.
	/// </summary>
	protected bool HasFocus { get; private set; }

	/// <summary>
	/// Gets the resource manager for loading game assets.
	/// </summary>
	protected IResourceManager Resources => _resources;

	/// <summary>
	/// Gets the rendering context for drawing operations.
	/// </summary>
	protected IRenderingContext RC => _rc;

	/// <summary>
	/// Gets the collection of UI elements in this state.
	/// </summary>
	protected List<UIElement> UI { get; } = new();

	#endregion

	#region Methods

	/// <summary>
	/// Called once when this state is first activated.
	/// Loads all UI elements. Instantiate your UI before calling this.
	/// </summary>
	public virtual void Load()
	{
		foreach (var ui in UI)
		{
			ui.Load();
		}
	}

	/// <summary>
	/// Called once when this state is removed.
	/// Unloads all UI.
	/// </summary>
	public virtual void Unload()
	{
		foreach (var ui in UI)
		{
			ui.Unload();
		}

		// Complete the subject when the state is unloaded
		try
		{
			_transitionRequests.OnCompleted();
		}
		catch (Exception)
		{
			// Subject might already be completed or disposed
		}
	}

	/// <summary>
	/// Renders all UI elements.
	/// </summary>
	/// <param name="gameTime">Timing values for the current frame.</param>
	public virtual void Render(GameTime gameTime)
	{
		foreach (var ui in UI)
		{
			ui.Render(gameTime);
		}
	}

	/// <summary>
	/// Updates all UI elements.
	/// </summary>
	/// <param name="gameTime">Timing values for the current frame.</param>
	public virtual void Update(GameTime gameTime)
	{
		foreach (var ui in UI)
		{
			ui.Update(gameTime);
		}
	}

	/// <summary>
	/// Called when this state becomes the active state.
	/// Override to attach event handlers.
	/// </summary>
	public virtual void AcquireFocus()
	{
		HasFocus = true;
	}

	/// <summary>
	/// Called when this state is no longer the active state.
	/// Override to detach event handlers.
	/// </summary>
	public virtual void LostFocus()
	{
		HasFocus = false;
	}

	/// <summary>
	/// Requests to leave the current state.
	/// </summary>
	protected void Leave()
	{
		_transitionRequests.OnNext(new GameStateTransitionRequest(TransitionType.Leave, null));
	}

	/// <summary>
	/// Requests to enter a new state.
	/// </summary>
	/// <param name="state">The state to enter.</param>
	/// <exception cref="ArgumentNullException">Thrown when state is null.</exception>
	protected void Enter(GameState state)
	{
		if (state == null)
		{
			throw new ArgumentNullException(nameof(state));
		}

		_transitionRequests.OnNext(new GameStateTransitionRequest(TransitionType.Enter, state));
	}

	/// <summary>
	/// Requests to clear all states.
	/// </summary>
	protected void ClearAllStates()
	{
		_transitionRequests.OnNext(new GameStateTransitionRequest(TransitionType.ClearAll, null));
	}

	/// <summary>
	/// Disposes resources used by this state.
	/// </summary>
	protected override void DisposeManagedResources()
	{
		try
		{
			// Complete and dispose the subject
			_transitionRequests.OnCompleted();
			_transitionRequests.Dispose();
		}
		catch (Exception)
		{
			// Subject might already be completed or disposed
		}
	}

	public virtual bool KeyDown(KeyboardKeyEventArgs e)
	{
		foreach (var ui in UI)
		{
			if (ui.KeyDown(e))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool KeyUp(KeyboardKeyEventArgs e)
	{
		foreach (var ui in UI)
		{
			if (ui.KeyUp(e))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool MouseDown(MouseButtonEventArgs e)
	{
		foreach (var ui in UI)
		{
			if (ui.MouseDown(e))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool MouseUp(MouseButtonEventArgs e)
	{
		foreach (var ui in UI)
		{
			if (ui.MouseUp(e))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool MouseMove(MouseMoveEventArgs e)
	{
		foreach (var ui in UI)
		{
			if (ui.MouseMove(e))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool MouseWheel(MouseWheelEventArgs e)
	{
		foreach (var ui in UI)
		{
			if (ui.MouseWheel(e))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool TextInput(TextInputEventArgs e)
	{
		foreach (var ui in UI)
		{
			if (ui.TextInput(e))
			{
				return true;
			}
		}
		return false;
	}

	#endregion
}
