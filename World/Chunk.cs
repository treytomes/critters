using OpenTK.Mathematics;

namespace Critters.World;

class Chunk
{
	#region Fields

	private readonly Tile[,] _tiles;

	#endregion

	#region Constructors

	public Chunk(int size)
	{
		_tiles = new Tile[size, size];
	}

	#endregion

	#region Methods

	public Tile GetTile(Vector2 position)
	{
		return GetTile((int)position.X, (int)position.Y);
	}

	public Tile GetTile(int x, int y)
	{
		return _tiles[x, y];
	}

	public void SetTile(Vector2 position, Tile tile)
	{
		SetTile((int)position.X, (int)position.Y, tile);
	}

	public void SetTile(int x, int y, Tile tile)
	{
		_tiles[x, y] = tile;
	}

	#endregion
}