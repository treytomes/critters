using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.States;

class MainMenuState : GameState
{
	private bool _hasMouseHover = false;
	private Box2 _bounds = new Box2(32, 32, 128, 96);

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);

		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);

		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);
		
		rc.Clear();

		var segments = 32;
		var dx = rc.Width / segments;
		var dy = rc.Height / segments;
		for (var n = 0; n < segments; n++)
		{
			var x1 = 0;
			var y1 = n * dy;
			var x2 = (segments - n) * dx;
			var y2 = 0;
			rc.RenderLine(x1, y1, x2, y2, rc.Palette[0, 5, 0]);
		}

		var fillColor = _hasMouseHover ? rc.Palette[0, 1, 4] : rc.Palette[0, 1, 3];
		var borderColor = _hasMouseHover ? rc.Palette[5, 5, 5] : rc.Palette[4, 4, 4];
		rc.RenderFilledRect(_bounds, fillColor);
		rc.RenderRect(_bounds, borderColor);

		rc.RenderCircle(290, 190, 24, rc.Palette[5, 4, 0]);
		rc.RenderFilledCircle(290, 190, 23, rc.Palette[3, 2, 0]);

		rc.FloodFill(new Vector2(310, 220), rc.Palette[1, 0, 2]);
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_hasMouseHover = _bounds.ContainsInclusive(e.Position);
	}
}