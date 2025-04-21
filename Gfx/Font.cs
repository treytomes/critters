using OpenTK.Mathematics;

namespace Critters.Gfx;

class Font
{
	#region Fields

	private readonly GlyphSet<Bitmap> _tiles;

	#endregion

	#region Constructors

	public Font(GlyphSet<Bitmap> tiles)
	{
		_tiles = tiles;
	}

	#endregion

	#region Methods

	public void WriteString(IRenderingContext rc, string text, Vector2 position, RadialColor fg, RadialColor? bg = null)
	{
		WriteString(rc, text, (int)position.X, (int)position.Y, fg.Index, bg?.Index ?? 255);
	}

	public void WriteString(IRenderingContext rc, string text, Vector2 position, byte fg, byte bg = 255)
	{
		WriteString(rc, text, (int)position.X, (int)position.Y, fg, bg);
	}

	public void WriteString(IRenderingContext rc, string text, int x, int y, byte fg, byte bg = 255)
	{
		for (int i = 0; i < text.Length; i++)
		{
			_tiles[text[i]].Render(rc, x + i * 8, y, fg, bg);
		}
	}

	public Vector2 MeasureString(string text)
	{
		return new Vector2(text.Length * _tiles.TileWidth, _tiles.TileHeight);
	}

	#endregion
}