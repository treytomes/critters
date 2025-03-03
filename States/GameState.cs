using Critters.Events;
using Critters.Gfx;
using Critters.IO;

namespace Critters.States;

abstract class GameState : IGameComponent
{
	protected bool HasFocus { get; private set; }
	protected EventBus? EventBus { get; private set; }

  public virtual void Load(ResourceManager resources, EventBus eventBus)
	{
		EventBus = eventBus;
	}
  
	public virtual void Unload(ResourceManager resources, EventBus eventBus) {}
  public virtual void Render(RenderingContext rc, GameTime gameTime) {}
  public virtual void Update(GameTime gameTime) {}
	
	public virtual void AcquireFocus(EventBus eventBus)
	{
		HasFocus = true;
	}

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
