using Critters.Events;
using Critters.Gfx;
using Critters.IO;

namespace Critters.States;

abstract class GameState : IGameComponent
{
	protected bool HasFocus { get; private set; }
	protected EventBus? EventBus { get; private set; }

	/// <summary>
	/// Called once when this state is first activated.
	/// </summary>
	/// <param name="resources"></param>
	/// <param name="eventBus"></param>
  public virtual void Load(ResourceManager resources, EventBus eventBus)
	{
		EventBus = eventBus;
	}
  
	/// <summary>
	/// Called once when this state so removed.
	/// </summary>
	/// <param name="resources"></param>
	/// <param name="eventBus"></param>
	public virtual void Unload(ResourceManager resources, EventBus eventBus) {}
  
	public virtual void Render(RenderingContext rc, GameTime gameTime) {}
  
	public virtual void Update(GameTime gameTime) {}
	
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
}
