using Critters.Gfx;
using Critters.Services;

namespace Critters.States;

class PatternTestState : GameState
{
	public PatternTestState(IResourceManager resources, IEventBus eventBus, IRenderingContext rc)
		: base(resources, eventBus, rc)
	{
	}

	public override void Render(GameTime gameTime)
	{
		base.Render(gameTime);

		for (int y = 0; y < RC.Height; y++)
		{
			for (int x = 0; x < RC.Width; x++)
			{
				var r = (byte)((x | y) % 6);
				var g = (byte)((x ^ y) % 6);
				var b = (byte)((x & y) % 6);
				RC.SetPixel(x, y, RC.Palette[r, g, b]);
			}
		}
	}
}
