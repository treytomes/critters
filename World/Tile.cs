using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.World;

class Tile
{
	#region Fields

	private byte _foregroundColor;
	private byte _backgroundColor;
	private Bitmap _bitmap;

	#endregion

	#region Constructors

	public Tile(byte foregroundColor, byte backgroundColor, Bitmap bitmap)
	{
		_foregroundColor = foregroundColor;
		_backgroundColor = backgroundColor;
		_bitmap = bitmap;
	}

	#endregion

	#region Methods

	public void Render(RenderingContext rc, Vector2 position)
	{
		_bitmap.Render(rc, position, _foregroundColor, _backgroundColor);
	}

	#endregion
}