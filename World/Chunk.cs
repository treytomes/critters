using OpenTK.Mathematics;

namespace Critters.World;

class Chunk
{
	#region Fields

	private readonly TileRef[,] _tiles;

	#endregion

	#region Constructors

	public Chunk(int size)
	{
		_tiles = new TileRef[size, size];
	}

	#endregion

	#region Methods

	public TileRef GetTile(Vector2 position)
	{
		return GetTile((int)position.X, (int)position.Y);
	}

	public TileRef GetTile(int x, int y)
	{
		return _tiles[x, y];
	}

	public void SetTile(Vector2 position, int tileId)
	{
		SetTile((int)position.X, (int)position.Y, tileId);
	}

	public void SetTile(int x, int y, int tileId)
	{
		_tiles[x, y].TileId = tileId;
	}

	#endregion
}