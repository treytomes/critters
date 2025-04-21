
using Critters.Gfx;
using Critters.World;
using OpenTK.Mathematics;

namespace Critters.States.TileMapTest;

class MapCursor
{
	#region Constants

	private const int MAP_CURSOR_BLINK_SPEED_MS = 300;

	#endregion

	#region Fields

	private Vector2 _position = Vector2.Zero;

	#endregion

	#region Methods

	public void Update(GameTime gameTime)
	{
	}

	public void Render(IRenderingContext rc, GameTime gameTime, Camera camera)
	{
		var mapCursorScreenPosition = camera.WorldToScreen(_position);

		if (rc.Bounds.ContainsExclusive(mapCursorScreenPosition))
		{
			rc.RenderRect(mapCursorScreenPosition, mapCursorScreenPosition + new Vector2(7, 7), rc.Palette[5, 0, 0]);
			if ((int)(gameTime.TotalTime.TotalMilliseconds / MAP_CURSOR_BLINK_SPEED_MS) % 2 == 0)
			{
				rc.RenderRect(mapCursorScreenPosition - new Vector2(2, 2), mapCursorScreenPosition + new Vector2(7, 7) + new Vector2(2, 2), rc.Palette[5, 0, 0]);
			}
		}
	}

	public void MoveTo(Vector2 position)
	{
		_position = position;
	}

	public void MoveBy(Vector2 delta)
	{
		_position += delta;
	}

	#endregion
}
