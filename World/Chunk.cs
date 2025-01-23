using Critters.IO;
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

	public Chunk(SerializableChunk chunk)
	{
		if (chunk.Size != chunk.Tiles.GetLength(0))
		{
			throw new Exception("Invalid chunk size");
		}

		_tiles = (TileRef[,])chunk.Tiles.Clone();
	}

	#endregion

	#region Methods

	public SerializableChunk ToSerializable()
	{
		return new SerializableChunk()
		{
			Size = _tiles.GetLength(0),
			Tiles = (TileRef[,])_tiles.Clone()
		};
	}

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