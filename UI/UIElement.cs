using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.States;

namespace Critters.UI;

/// <summary>
/// UI element position and size is measured in tiles.
/// The PixelBounds property will convert to virtual display pixels for things like mouse hover check.
/// </summary>
class UIElement : IGameComponent
{
	#region Properties

	#endregion

	#region Methods

	public virtual void Load(ResourceManager resources, EventBus eventBus)
	{
	}

	public virtual void Render(RenderingContext rc, GameTime gameTime)
	{
	}

	public virtual void Unload(ResourceManager resources, EventBus eventBus)
	{
	}

	public virtual void Update(GameTime gameTime)
	{
	}

	#endregion
}
