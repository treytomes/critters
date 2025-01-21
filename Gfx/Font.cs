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

	public void WriteString(RenderingContext rc, string text, int x, int y, byte fg, byte bg)
	{
		for (int i = 0; i < text.Length; i++)
		{
			_tiles[text[i]].Render(rc, x + i * 8, y, fg, bg);
		}
	}

	#endregion
}