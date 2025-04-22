using Critters.Core;
using Critters.Gfx;
using Critters.IO;
using Newtonsoft.Json;
using OpenTK.Mathematics;

namespace Critters.World;

/// <summary>
/// Represents a game level composed of multiple chunks of tiles.
/// </summary>
class Level : Disposable, ILevel
{
	#region Fields

	/// <summary>
	/// Dictionary mapping chunk coordinates to chunk instances.
	/// </summary>
	private readonly Dictionary<Vector2i, Chunk> _chunks;

	/// <summary>
	/// Size of each chunk in tiles.
	/// </summary>
	private readonly int _chunkSize;

	/// <summary>
	/// Size of each tile in pixels.
	/// </summary>
	private readonly int _tileSize;

	/// <summary>
	/// Set of chunk coordinates that have been modified since last save.
	/// </summary>
	private readonly HashSet<Vector2i> _dirtyChunks;

	#endregion

	#region Properties

	/// <summary>
	/// Gets the size of each chunk in tiles.
	/// </summary>
	public int ChunkSize => _chunkSize;

	/// <summary>
	/// Gets the size of each tile in pixels.
	/// </summary>
	public int TileSize => _tileSize;

	/// <summary>
	/// Gets the number of chunks in this level.
	/// </summary>
	public int ChunkCount => _chunks.Count;

	/// <summary>
	/// Gets a value indicating whether any chunks have been modified since the last save.
	/// </summary>
	public bool HasUnsavedChanges => _dirtyChunks.Count > 0;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="Level"/> class.
	/// </summary>
	/// <param name="chunkSize">Size of each chunk in tiles.</param>
	/// <param name="tileSize">Size of each tile in pixels.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when chunkSize or tileSize is less than 1.</exception>
	public Level(int chunkSize, int tileSize)
	{
		if (chunkSize < 1)
			throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be at least 1");
		if (tileSize < 1)
			throw new ArgumentOutOfRangeException(nameof(tileSize), "Tile size must be at least 1");

		_chunks = new Dictionary<Vector2i, Chunk>();
		_dirtyChunks = new HashSet<Vector2i>();
		_chunkSize = chunkSize;
		_tileSize = tileSize;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Level"/> class from a serializable level.
	/// </summary>
	/// <param name="level">The serializable level data.</param>
	/// <exception cref="ArgumentNullException">Thrown when level is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when chunk size or tile size is less than 1.</exception>
	private Level(SerializableLevel level)
	{
		if (level == null)
			throw new ArgumentNullException(nameof(level));
		if (level.ChunkSize < 1)
			throw new ArgumentOutOfRangeException(nameof(level.ChunkSize), "Chunk size must be at least 1");
		if (level.TileSize < 1)
			throw new ArgumentOutOfRangeException(nameof(level.TileSize), "Tile size must be at least 1");

		_chunks = new Dictionary<Vector2i, Chunk>();
		_dirtyChunks = new HashSet<Vector2i>();
		_chunkSize = level.ChunkSize;
		_tileSize = level.TileSize;

		// Load chunks from serialized data
		foreach (var chunkData in level.Chunks)
		{
			var position = chunkData.ChunkPosition.ToVector2i();
			_chunks[position] = new Chunk(chunkData.Chunk);
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Loads a level from a JSON file.
	/// </summary>
	/// <param name="path">The path to the JSON file.</param>
	/// <returns>A new Level instance.</returns>
	/// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
	public static Level Load(string path)
	{
		if (!File.Exists(path))
			throw new FileNotFoundException("Level file not found", path);

		try
		{
			var json = File.ReadAllText(path);
			var levelData = JsonConvert.DeserializeObject<SerializableLevel>(json);

			if (levelData == null)
				throw new JsonException("Failed to deserialize level data");

			return new Level(levelData);
		}
		catch (JsonException ex)
		{
			throw new JsonException($"Error parsing level file: {ex.Message}", ex);
		}
		catch (Exception ex)
		{
			throw new Exception($"Error loading level: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Saves the level to a JSON file.
	/// </summary>
	/// <param name="path">The path to save the JSON file.</param>
	/// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
	/// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
	public void Save(string path)
	{
		if (string.IsNullOrEmpty(path))
			throw new ArgumentNullException(nameof(path));

		try
		{
			var serializable = ToSerializable();
			var json = JsonConvert.SerializeObject(serializable, Formatting.Indented);
			File.WriteAllText(path, json);
			_dirtyChunks.Clear(); // Clear dirty flags after successful save
		}
		catch (Exception ex)
		{
			throw new IOException($"Error saving level to {path}: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Converts this level to a serializable representation.
	/// </summary>
	/// <returns>A serializable representation of this level.</returns>
	public SerializableLevel ToSerializable()
	{
		return new SerializableLevel
		{
			ChunkSize = _chunkSize,
			TileSize = _tileSize,
			Chunks = _chunks.Select(kvp => new SerializableLevelChunk
			{
				ChunkPosition = new SerializableVector2i(kvp.Key),
				Chunk = kvp.Value.ToSerializable()
			}).ToList()
		};
	}

	/// <summary>
	/// Converts world coordinates to chunk coordinates.
	/// </summary>
	/// <param name="worldX">The X coordinate in world space.</param>
	/// <param name="worldY">The Y coordinate in world space.</param>
	/// <returns>The chunk coordinates containing the world position.</returns>
	public Vector2i WorldToChunkCoordinates(int worldX, int worldY)
	{
		return new Vector2i(
			MathHelper.FloorDiv(worldX, _chunkSize),
			MathHelper.FloorDiv(worldY, _chunkSize)
		);
	}

	/// <summary>
	/// Converts world coordinates to local coordinates within a chunk.
	/// </summary>
	/// <param name="worldX">The X coordinate in world space.</param>
	/// <param name="worldY">The Y coordinate in world space.</param>
	/// <returns>The local coordinates within the chunk.</returns>
	public Vector2i WorldToLocalCoordinates(int worldX, int worldY)
	{
		// Use modulo for positive coordinates, FloorMod for handling negative coordinates correctly
		int localX = MathHelper.FloorMod(worldX, _chunkSize);
		int localY = MathHelper.FloorMod(worldY, _chunkSize);
		return new Vector2i(localX, localY);
	}

	/// <summary>
	/// Gets a chunk at the specified chunk coordinates, or null if it doesn't exist.
	/// </summary>
	/// <param name="chunkX">The X coordinate of the chunk.</param>
	/// <param name="chunkY">The Y coordinate of the chunk.</param>
	/// <returns>The chunk at the specified coordinates, or null if it doesn't exist.</returns>
	public Chunk? GetChunk(int chunkX, int chunkY)
	{
		var chunkPos = new Vector2i(chunkX, chunkY);
		_chunks.TryGetValue(chunkPos, out var chunk);
		return chunk;
	}

	/// <summary>
	/// Gets or creates a chunk at the specified chunk coordinates.
	/// </summary>
	/// <param name="chunkX">The X coordinate of the chunk.</param>
	/// <param name="chunkY">The Y coordinate of the chunk.</param>
	/// <returns>The chunk at the specified coordinates, creating it if it doesn't exist.</returns>
	private Chunk GetOrCreateChunk(int chunkX, int chunkY)
	{
		var chunkPos = new Vector2i(chunkX, chunkY);
		if (!_chunks.TryGetValue(chunkPos, out var chunk))
		{
			chunk = new Chunk(_chunkSize);
			_chunks[chunkPos] = chunk;
			_dirtyChunks.Add(chunkPos);
		}
		return chunk;
	}

	/// <summary>
	/// Sets a tile at the specified world position.
	/// </summary>
	/// <param name="worldPosition">The position in world coordinates.</param>
	/// <param name="tileId">The ID of the tile to set.</param>
	/// <returns>True if the tile was set; false if the position was invalid.</returns>
	public bool SetTile(Vector2 worldPosition, int tileId)
	{
		return SetTile((int)worldPosition.X, (int)worldPosition.Y, tileId);
	}

	/// <summary>
	/// Sets a tile at the specified world coordinates.
	/// </summary>
	/// <param name="worldX">The X coordinate in world space.</param>
	/// <param name="worldY">The Y coordinate in world space.</param>
	/// <param name="tileId">The ID of the tile to set.</param>
	/// <returns>True if the tile was set; false if the coordinates were invalid.</returns>
	public bool SetTile(int worldX, int worldY, int tileId)
	{
		try
		{
			var chunkPos = WorldToChunkCoordinates(worldX, worldY);
			var localPos = WorldToLocalCoordinates(worldX, worldY);

			var chunk = GetOrCreateChunk(chunkPos.X, chunkPos.Y);
			bool result = chunk.SetTile(localPos.X, localPos.Y, tileId);

			if (result)
				_dirtyChunks.Add(chunkPos);

			return result;
		}
		catch (Exception)
		{
			return false;
		}
	}

	/// <summary>
	/// Gets a tile at the specified world position.
	/// </summary>
	/// <param name="worldPosition">The position in world coordinates.</param>
	/// <returns>The tile reference at the specified position.</returns>
	public TileRef GetTile(Vector2 worldPosition)
	{
		return GetTile((int)worldPosition.X, (int)worldPosition.Y);
	}

	/// <summary>
	/// Gets a tile at the specified world coordinates.
	/// </summary>
	/// <param name="worldX">The X coordinate in world space.</param>
	/// <param name="worldY">The Y coordinate in world space.</param>
	/// <returns>The tile reference at the specified coordinates.</returns>
	public TileRef GetTile(int worldX, int worldY)
	{
		var chunkPos = WorldToChunkCoordinates(worldX, worldY);
		var localPos = WorldToLocalCoordinates(worldX, worldY);

		var chunk = GetChunk(chunkPos.X, chunkPos.Y);
		if (chunk == null)
			return TileRef.Empty;

		return chunk.GetTile(localPos.X, localPos.Y);
	}

	/// <summary>
	/// Renders the level using the provided rendering context, tile repository, and camera.
	/// </summary>
	/// <param name="rc">The rendering context.</param>
	/// <param name="tiles">The tile repository.</param>
	/// <param name="camera">The camera.</param>
	public void Render(IRenderingContext rc, TileRepo tiles, Camera camera)
	{
		if (rc == null) throw new ArgumentNullException(nameof(rc));
		if (tiles == null) throw new ArgumentNullException(nameof(tiles));
		if (camera == null) throw new ArgumentNullException(nameof(camera));

		// Calculate visible tile range in world coordinates
		var viewportHalfSize = rc.ViewportSize / 2;
		var startWorldPos = (camera.Position - viewportHalfSize).Floor();
		var endWorldPos = (camera.Position + viewportHalfSize).Ceiling();

		// Convert to tile coordinates and add padding
		var startTilePos = (startWorldPos / _tileSize).Floor() - Vector2.One;
		var endTilePos = (endWorldPos / _tileSize).Ceiling() + Vector2.One;

		// Render visible tiles
		for (int tileY = (int)startTilePos.Y; tileY <= (int)endTilePos.Y; tileY++)
		{
			for (int tileX = (int)startTilePos.X; tileX <= (int)endTilePos.X; tileX++)
			{
				var tileRef = GetTile(tileX, tileY);
				if (tileRef.IsEmpty)
					continue;

				var worldPos = new Vector2(tileX * _tileSize, tileY * _tileSize);
				var screenPos = camera.WorldToScreen(worldPos).Floor();

				// Render the tile at the correct screen position
				tiles.Get(tileRef.TileId)?.Render(rc, screenPos);
			}
		}
	}

	/// <summary>
	/// Removes all chunks from the level.
	/// </summary>
	public void Clear()
	{
		_chunks.Clear();
		_dirtyChunks.Clear();
	}

	/// <summary>
	/// Removes empty chunks to save memory.
	/// </summary>
	/// <returns>The number of chunks removed.</returns>
	public int RemoveEmptyChunks()
	{
		var emptyChunkKeys = _chunks
			.Where(kvp => kvp.Value.IsEmpty())
			.Select(kvp => kvp.Key)
			.ToList();

		foreach (var key in emptyChunkKeys)
		{
			_chunks.Remove(key);
			_dirtyChunks.Add(key); // Mark as dirty since we're changing the level structure
		}

		return emptyChunkKeys.Count;
	}

	/// <summary>
	/// Gets all chunk coordinates in this level.
	/// </summary>
	/// <returns>An enumerable of all chunk coordinates.</returns>
	public IEnumerable<Vector2i> GetChunkCoordinates()
	{
		return _chunks.Keys;
	}

	/// <summary>
	/// Checks if a chunk exists at the specified coordinates.
	/// </summary>
	/// <param name="chunkX">The X coordinate of the chunk.</param>
	/// <param name="chunkY">The Y coordinate of the chunk.</param>
	/// <returns>True if the chunk exists; otherwise, false.</returns>
	public bool ChunkExists(int chunkX, int chunkY)
	{
		return _chunks.ContainsKey(new Vector2i(chunkX, chunkY));
	}

	/// <summary>
	/// Gets the boundaries of the level in world coordinates.
	/// </summary>
	/// <returns>A tuple containing the minimum and maximum world coordinates.</returns>
	public (Vector2 min, Vector2 max) GetBoundaries()
	{
		if (_chunks.Count == 0)
			return (Vector2.Zero, Vector2.Zero);

		var keys = _chunks.Keys;
		var minChunkX = keys.Min(k => k.X);
		var minChunkY = keys.Min(k => k.Y);
		var maxChunkX = keys.Max(k => k.X);
		var maxChunkY = keys.Max(k => k.Y);

		var min = new Vector2(minChunkX * _chunkSize, minChunkY * _chunkSize);
		var max = new Vector2((maxChunkX + 1) * _chunkSize - 1, (maxChunkY + 1) * _chunkSize - 1);

		return (min, max);
	}

	/// <summary>
	/// Gets the total number of non-empty tiles in the level.
	/// </summary>
	/// <returns>The total number of non-empty tiles.</returns>
	public int CountNonEmptyTiles()
	{
		return _chunks.Values.Sum(chunk => chunk.CountNonEmptyTiles());
	}

	/// <summary>
	/// Performs a specified action on each chunk in the level.
	/// </summary>
	/// <param name="action">The action to perform on each chunk.</param>
	public void ForEachChunk(Action<Vector2i, Chunk> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		foreach (var kvp in _chunks)
		{
			action(kvp.Key, kvp.Value);
		}
	}

	/// <summary>
	/// Performs a specified action on each non-empty tile in the level.
	/// </summary>
	/// <param name="action">The action to perform on each tile, providing world coordinates and tile reference.</param>
	public void ForEachTile(Action<int, int, TileRef> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		foreach (var kvp in _chunks)
		{
			var chunkPos = kvp.Key;
			var chunk = kvp.Value;

			for (int localY = 0; localY < _chunkSize; localY++)
			{
				for (int localX = 0; localX < _chunkSize; localX++)
				{
					var tileRef = chunk.GetTile(localX, localY);
					if (!tileRef.IsEmpty)
					{
						int worldX = chunkPos.X * _chunkSize + localX;
						int worldY = chunkPos.Y * _chunkSize + localY;
						action(worldX, worldY, tileRef);
					}
				}
			}
		}
	}

	/// <summary>
	/// Disposes resources used by this level.
	/// </summary>
	protected override void DisposeManagedResources()
	{
		_chunks.Clear();
		_dirtyChunks.Clear();
		GC.SuppressFinalize(this);
	}

	#endregion
}