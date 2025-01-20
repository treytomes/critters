using System.Collections;

namespace Critter.Gfx
{
	class TileSet<TImage> : IReadOnlyList<TImage>
		where TImage : IImage<TImage>
	{
		#region Fields

		public readonly int TileWidth;
		public readonly int TileHeight;

		private readonly List<TImage> _tiles;

		#endregion

		#region Constructors

		public TileSet(TImage image, int tileWidth, int tileHeight)
		{
			TileWidth = tileWidth;
			TileHeight = tileHeight;

			_tiles = new List<TImage>();
			for (int y = 0; y < image.Height; y += tileHeight)
			{
				for (int x = 0; x < image.Width; x += tileWidth)
				{
					_tiles.Add(image.Crop(x, y, tileWidth, tileHeight));
				}
			}
		}

		#endregion

		#region Properties

		public TImage this[int index] => _tiles[index];

		public int Count => _tiles.Count;

		#endregion

		#region Methods

		public IEnumerator<TImage> GetEnumerator()
		{
				return _tiles.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
				return _tiles.GetEnumerator();
		}

		#endregion
	}
}