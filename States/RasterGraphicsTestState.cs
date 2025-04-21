using Critters.Gfx;
using Critters.Services;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.States;

class RasterGraphicsTestState : GameState
{
	#region Fields

	private bool _hasMouseHover = false;
	private Box2 _bounds = new Box2(32, 32, 128, 96);

	#endregion

	#region Constructors

	public RasterGraphicsTestState(IResourceManager resources, IRenderingContext rc)
		: base(resources, rc)
	{
	}

	#endregion
	public override void AcquireFocus()
	{
		base.AcquireFocus();
	}

	public override void LostFocus()
	{
		base.LostFocus();
	}

	public override void Render(GameTime gameTime)
	{
		base.Render(gameTime);

		RC.Clear();

		var segments = 32;
		var dx = RC.Width / segments;
		var dy = RC.Height / segments;
		for (var n = 0; n < segments; n++)
		{
			var x1 = 0;
			var y1 = n * dy;
			var x2 = (segments - n) * dx;
			var y2 = 0;
			RC.RenderLine(x1, y1, x2, y2, RC.Palette[0, 5, 0]);
		}

		var fillColor = _hasMouseHover ? RC.Palette[0, 1, 4] : RC.Palette[0, 1, 3];
		var borderColor = _hasMouseHover ? RC.Palette[5, 5, 5] : RC.Palette[4, 4, 4];
		RC.RenderFilledRect(_bounds, fillColor);
		RC.RenderRect(_bounds, borderColor);

		RC.RenderCircle(290, 190, 24, RC.Palette[5, 4, 0]);
		RC.RenderFilledCircle(290, 190, 23, RC.Palette[3, 2, 0]);

		RC.FloodFill(new Vector2(310, 220), RC.Palette[1, 0, 2]);
	}

	public override bool MouseMove(MouseMoveEventArgs e)
	{
		_hasMouseHover = _bounds.ContainsInclusive(e.Position);
		return base.MouseMove(e);
	}
}