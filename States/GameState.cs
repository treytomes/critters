using Critters.Events;
using Critters.Gfx;
using Critters.IO;

namespace Critters.States;

abstract class GameState : IGameComponent
{
	protected EventBus? EventBus { get; private set; }

  public virtual void Load(ResourceManager resources, EventBus eventBus)
	{
		EventBus = eventBus;
	}
  
	public virtual void Unload(ResourceManager resources, EventBus eventBus) {}
  public virtual void Render(RenderingContext rc, GameTime gameTime) {}
  public virtual void Update(GameTime gameTime) {}

	protected void Leave()
	{
		EventBus?.Publish(new LeaveGameStateEventArgs());
	}
}
