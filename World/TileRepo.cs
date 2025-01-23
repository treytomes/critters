using Critters.Events;
using Critters.Gfx;
using Critters.IO;

namespace Critters.World;

class TileRepo
{
	public const int GRASS_ID = 1;
	public const int DIRT_ID = 2;
	public const int ROCK_ID = 3;

	private Dictionary<int, Tile> _tiles = new();

	public void Load(ResourceManager resources, EventBus eventBus)
	{
		var image = resources.Load<Image>("oem437_8.png");
		var bmp = new Bitmap(image);
		var tiles = new GlyphSet<Bitmap>(bmp, 8, 8);

		_tiles[GRASS_ID] = new Tile(GRASS_ID, new BitmapRef(tiles[176], Palette.GetIndex(0, 4, 0), Palette.GetIndex(0, 2, 0)));
		_tiles[DIRT_ID] = new Tile(DIRT_ID, new BitmapRef(tiles[176], Palette.GetIndex(1, 2, 0), Palette.GetIndex(1, 1, 0)));
		_tiles[ROCK_ID] = new Tile(ROCK_ID, new BitmapRef(tiles[178], Palette.GetIndex(3, 3, 3), Palette.GetIndex(1, 1, 1)));
	}

	public Tile Get(int id)
	{
		return _tiles[id];
	}

	public Tile Get(TileRef tileRef)
	{
		return _tiles[tileRef.TileId];
	}
}