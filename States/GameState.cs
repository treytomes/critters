using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.UI;

namespace Critters.States;

abstract class GameState : IGameComponent
{
	#region Fields

	#endregion

	#region Properties

	protected bool HasFocus { get; private set; }
	protected EventBus? EventBus { get; private set; }
	protected List<UIElement> UI { get; } = new List<UIElement>();

	#endregion

	#region Methods

	/// <summary>
	/// Called once when this state is first activated.
	/// Loads all UI elements.  Instantiate your UI before calling this.
	/// </summary>
	/// <param name="resources"></param>
	/// <param name="eventBus"></param>
  public virtual void Load(ResourceManager resources, EventBus eventBus)
	{
		EventBus = eventBus;

		foreach (var ui in UI)
		{
			ui.Load(resources, eventBus);
		}
	}
  
	/// <summary>
	/// Called once when this state so removed.
	/// Unloads all UI.
	/// </summary>
	/// <param name="resources"></param>
	/// <param name="eventBus"></param>
	public virtual void Unload(ResourceManager resources, EventBus eventBus)
	{
		foreach (var ui in UI)
		{
			ui.Unload(resources, eventBus);
		}
	}
  
	/// <summary>
	/// Render all UI elements.
	/// </summary>
	/// <param name="rc"></param>
	/// <param name="gameTime"></param>
	public virtual void Render(RenderingContext rc, GameTime gameTime)
	{
		foreach (var ui in UI)
		{
			ui.Render(rc, gameTime);
		}
	}
  
	/// <summary>
	/// Update all UI elements.
	/// </summary>
	/// <param name="gameTime"></param>
	public virtual void Update(GameTime gameTime)
	{
		foreach (var ui in UI)
		{
			ui.Update(gameTime);
		}
	}
	
	/// <summary>
	/// Called when this state becomes the active state.
	/// 
	/// Attach events here.
	/// </summary>
	/// <param name="eventBus"></param>
	public virtual void AcquireFocus(EventBus eventBus)
	{
		HasFocus = true;
	}

	/// <summary>
	/// Called when this state is no longer the active state.
	/// 
	/// Detach events here.
	/// </summary>
	/// <param name="eventBus"></param>
	public virtual void LostFocus(EventBus eventBus)
	{
		HasFocus = false;
	}

	protected void Leave()
	{
		EventBus?.Publish(new LeaveGameStateEventArgs());
	}

	protected void Enter(GameState state)
	{
		EventBus?.Publish(new EnterGameStateEventArgs(state));
	}

	#endregion
}
