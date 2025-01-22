using Critters.Events;
using Critters.Gfx;
using Critters.IO;

namespace Critters.World;

class TileRepo
{
	public Tile? Grass { get; private set; }
	public Tile? Dirt { get; private set; }
	public Tile? Rock { get; private set; }

	public void Load(ResourceManager resources, EventBus eventBus)
	{
		var image = resources.Load<Image>("oem437_8.png");
		var bmp = new Bitmap(image);
		var tiles = new GlyphSet<Bitmap>(bmp, 8, 8);

		Grass = new Tile(1, new BitmapRef(tiles[176], Palette.GetIndex(0, 4, 0), Palette.GetIndex(0, 2, 0)));
		Dirt = new Tile(2, new BitmapRef(tiles[176], Palette.GetIndex(1, 2, 0), Palette.GetIndex(1, 1, 0)));
		Rock = new Tile(3, new BitmapRef(tiles[178], Palette.GetIndex(3, 3, 3), Palette.GetIndex(1, 1, 1)));
	}
}