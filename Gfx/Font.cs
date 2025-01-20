using Critter.Gfx;

namespace Critters.Gfx
{
	class Font
	{
		#region Fields

		private readonly TileSet<Bitmap> _tiles;

		#endregion

		#region Constructors

		public Font(TileSet<Bitmap> tiles)
		{
			_tiles = tiles;
		}

		#endregion

		#region Methods

		public void WriteString(RenderingContext rc, string text, int x, int y, byte fg, byte bg)
		{
			for (int i = 0; i < text.Length; i++)
			{
				_tiles[text[i]].Draw(rc, x + i * 8, y, fg, bg);
			}
		}

		#endregion
	}
}