using Critters.IO;
using OpenTK.Mathematics;

namespace Critters.World;

/// <summary>
/// Represents a fixed-size chunk of tiles in the game world.
/// </summary>
class Chunk
{
	#region Fields

	/// <summary>
	/// The two-dimensional array of tile references that make up this chunk.
	/// </summary>
	private readonly TileRef[,] _tiles;

	#endregion

	#region Properties

	/// <summary>
	/// Gets the size of this chunk (width and height in tiles).
	/// </summary>
	public int Size => _tiles.GetLength(0);

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="Chunk"/> class with the specified size.
	/// </summary>
	/// <param name="size">The size of the chunk (width and height in tiles).</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when size is less than 1.</exception>
	public Chunk(int size)
	{
		if (size < 1)
			throw new ArgumentOutOfRangeException(nameof(size), "Chunk size must be at least 1");

		_tiles = new TileRef[size, size];
		// Initialize all tiles to Empty
		Clear();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Chunk"/> class from a serializable chunk.
	/// </summary>
	/// <param name="chunk">The serializable chunk to create this chunk from.</param>
	/// <exception cref="ArgumentNullException">Thrown when chunk is null.</exception>
	/// <exception cref="ArgumentException">Thrown when the chunk size doesn't match the tiles array dimensions.</exception>
	public Chunk(SerializableChunk chunk)
	{
		if (chunk == null)
			throw new ArgumentNullException(nameof(chunk));

		if (chunk.Size != chunk.Tiles.GetLength(0) || chunk.Size != chunk.Tiles.GetLength(1))
			throw new ArgumentException("Chunk size does not match tiles array dimensions", nameof(chunk));

		_tiles = new TileRef[chunk.Size, chunk.Size];

		// Copy tiles from serializable chunk
		for (int x = 0; x < chunk.Size; x++)
		{
			for (int y = 0; y < chunk.Size; y++)
			{
				_tiles[x, y] = chunk.Tiles[x, y];
			}
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Converts this chunk to a serializable representation.
	/// </summary>
	/// <returns>A serializable representation of this chunk.</returns>
	public SerializableChunk ToSerializable()
	{
		var size = Size;
		var tiles = new TileRef[size, size];

		// Copy tiles to the serializable chunk
		for (int x = 0; x < size; x++)
		{
			for (int y = 0; y < size; y++)
			{
				tiles[x, y] = _tiles[x, y];
			}
		}

		return new SerializableChunk()
		{
			Size = size,
			Tiles = tiles
		};
	}

	/// <summary>
	/// Gets the tile at the specified position within the chunk.
	/// </summary>
	/// <param name="position">The position vector within the chunk.</param>
	/// <returns>The tile reference at the specified position, or TileRef.Empty if out of bounds.</returns>
	public TileRef GetTile(Vector2 position)
	{
		return GetTile((int)position.X, (int)position.Y);
	}

	/// <summary>
	/// Gets the tile at the specified coordinates within the chunk.
	/// </summary>
	/// <param name="x">The X coordinate within the chunk.</param>
	/// <param name="y">The Y coordinate within the chunk.</param>
	/// <returns>The tile reference at the specified coordinates, or TileRef.Empty if out of bounds.</returns>
	public TileRef GetTile(int x, int y)
	{
		if (!IsInBounds(x, y))
			return TileRef.Empty;

		return _tiles[x, y];
	}

	/// <summary>
	/// Sets the tile at the specified position within the chunk.
	/// </summary>
	/// <param name="position">The position vector within the chunk.</param>
	/// <param name="tileId">The ID of the tile to set.</param>
	/// <returns>True if the tile was set; false if the position was out of bounds.</returns>
	public bool SetTile(Vector2 position, int tileId)
	{
		return SetTile((int)position.X, (int)position.Y, tileId);
	}

	/// <summary>
	/// Sets the tile at the specified coordinates within the chunk.
	/// </summary>
	/// <param name="x">The X coordinate within the chunk.</param>
	/// <param name="y">The Y coordinate within the chunk.</param>
	/// <param name="tileId">The ID of the tile to set.</param>
	/// <returns>True if the tile was set; false if the coordinates were out of bounds.</returns>
	public bool SetTile(int x, int y, int tileId)
	{
		if (!IsInBounds(x, y))
			return false;

		// Since TileRef is immutable, we create a new instance
		_tiles[x, y] = new TileRef(tileId);
		return true;
	}

	/// <summary>
	/// Determines whether the specified coordinates are within the bounds of this chunk.
	/// </summary>
	/// <param name="x">The X coordinate to check.</param>
	/// <param name="y">The Y coordinate to check.</param>
	/// <returns>True if the coordinates are within bounds; otherwise, false.</returns>
	public bool IsInBounds(int x, int y)
	{
		return x >= 0 && x < Size && y >= 0 && y < Size;
	}

	/// <summary>
	/// Clears all tiles in this chunk, setting them to TileRef.Empty.
	/// </summary>
	public void Clear()
	{
		int size = Size;
		for (int x = 0; x < size; x++)
		{
			for (int y = 0; y < size; y++)
			{
				_tiles[x, y] = TileRef.Empty;
			}
		}
	}

	/// <summary>
	/// Returns a count of non-empty tiles in this chunk.
	/// </summary>
	/// <returns>The number of non-empty tiles.</returns>
	public int CountNonEmptyTiles()
	{
		int count = 0;
		int size = Size;

		for (int x = 0; x < size; x++)
		{
			for (int y = 0; y < size; y++)
			{
				if (!_tiles[x, y].IsEmpty)
					count++;
			}
		}

		return count;
	}

	/// <summary>
	/// Determines whether this chunk is completely empty (contains only empty tiles).
	/// </summary>
	/// <returns>True if all tiles are empty; otherwise, false.</returns>
	public bool IsEmpty()
	{
		return CountNonEmptyTiles() == 0;
	}

	#endregion
}