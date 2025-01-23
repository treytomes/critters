namespace Critters.World;

class LevelBuilder
{
	private const int CHUNK_SIZE = 64;
	private const int TILE_SIZE = 8;

	public static Level BuildSample()
	{
		var level = new Level(CHUNK_SIZE, TILE_SIZE);

		for (var y = -64; y <= 64; y++)
		{
			for (var x = -64; x <= 64; x++)
			{
				if ((x % 8 == 0) || (y % 8 == 0))
				{
					level.SetTile(x, y, TileRepo.DIRT_ID);
				}
				else
				{
					level.SetTile(x, y, TileRepo.GRASS_ID);
				}
			}
		}

		for (var y = -64; y <= 64; y++)
		{
			level.SetTile(-64, y, TileRepo.ROCK_ID);
			level.SetTile(64, y, TileRepo.ROCK_ID);
		}
		for (var x = -64; x <= 64; x++)
		{
			level.SetTile(x, -64, TileRepo.ROCK_ID);
			level.SetTile(x, 64, TileRepo.ROCK_ID);
		}

		return level;
	}
}