using OpenTK.Mathematics;

namespace Critters.Gfx;

class BitmapRef : IImageRef
{
	private Bitmap _bitmap;
	private byte _foregroundColor;
	private byte _backgroundColor;

	public BitmapRef(Bitmap bitmap, byte foregroundColor, byte backgroundColor)
	{
		_bitmap = bitmap;
		_foregroundColor = foregroundColor;
		_backgroundColor = backgroundColor;
	}

	public void Render(RenderingContext rc, Vector2 position)
	{
		_bitmap.Render(rc, position, _foregroundColor, _backgroundColor);
	}
}