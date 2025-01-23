using Critters.Gfx;
using Critters.IO;
using Newtonsoft.Json;
using OpenTK.Mathematics;

namespace Critters.World;

class Level
{
	#region Fields

	private readonly Dictionary<Vector2i, Chunk> _chunks;
	private readonly int _chunkSize;
	private readonly int _tileSize;

	#endregion

	#region Constructors

	public Level(int chunkSize, int tileSize)
	{
		_chunks = new Dictionary<Vector2i, Chunk>();
		_chunkSize = chunkSize;
		_tileSize = tileSize;
	}

	private Level(SerializableLevel level)
	{
		_chunks = level.Chunks.ToDictionary(kvp => kvp.ChunkPosition.ToVector2i(), kvp => new Chunk(kvp.Chunk));
		_chunkSize = level.ChunkSize;
		_tileSize = level.TileSize;
	}

	#endregion

	#region Methods

	public static Level Load(string path)
	{
		var json = File.ReadAllText(path);
		var info = JsonConvert.DeserializeObject<SerializableLevel>(json);
		return new Level(info!);
	}

	public void Save(string path)
	{
		var json = JsonConvert.SerializeObject(ToSerializable());
		File.WriteAllText(path, json);
	}

	public SerializableLevel ToSerializable()
	{
		return new SerializableLevel()
		{
			Chunks = _chunks.Select(kvp => new SerializableLevelChunk()
			{
				ChunkPosition = new SerializableVector2i(kvp.Key),
				Chunk = kvp.Value.ToSerializable()
			}).ToList(),
			ChunkSize = _chunkSize,
			TileSize = _tileSize,
		};
	}

	/// <summary>
	/// Access or create a chunk at a given chunk coordinate.
	/// </summary>
	private Chunk GetOrCreateChunk(int chunkX, int chunkY)
	{
		if (!_chunks.TryGetValue((chunkX, chunkY), out var chunk))
		{
			chunk = new Chunk(_chunkSize);
			_chunks[(chunkX, chunkY)] = chunk;
		}
		return chunk;
	}

	// TODO: If this doesn't work for negative fractions, use FloorDiv.
	public void SetTile(Vector2 worldPosition, int tileId) => SetTile((int)worldPosition.X, (int)worldPosition.Y, tileId);

	/// <summary>
	/// Set a tile at a specific world position.
	/// </summary>
	public void SetTile(int worldX, int worldY, int tileId)
	{
		var chunkX = MathHelper.FloorDiv(worldX, _chunkSize);
		var chunkY = MathHelper.FloorDiv(worldY, _chunkSize);
		var localX = Math.Abs(worldX % _chunkSize);
		var localY = Math.Abs(worldY % _chunkSize);

		var chunk = GetOrCreateChunk(chunkX, chunkY);
		chunk.SetTile(localX, localY, tileId);
	}

	// TODO: If this doesn't work for negatives, use FloorDiv.
	public TileRef GetTile(Vector2 worldPosition) => GetTile((int)worldPosition.X, (int)worldPosition.Y);

	public TileRef GetTile(float worldX, float worldY) => GetTile((int)worldX, (int)worldY);

	/// <summary>
	/// Get a tile at a specific world position.
	/// </summary>
	public TileRef GetTile(int worldX, int worldY)
	{
		var chunkX = MathHelper.FloorDiv(worldX, _chunkSize);
		var chunkY = MathHelper.FloorDiv(worldY, _chunkSize);
		var localX = Math.Abs(worldX % _chunkSize);
		var localY = Math.Abs(worldY % _chunkSize);

		if (_chunks.TryGetValue((chunkX, chunkY), out var chunk))
		{
				return chunk.GetTile(localX, localY);
		}

		return TileRef.Empty;
	}

	public void Render(RenderingContext rc, TileRepo tiles, Camera camera)
	{
		// Calculate visible tile range.
		var startPos = ((camera.Position - rc.ViewportSize / 2) / _tileSize).Floor() * _tileSize;

		// Render one extra tile around the edges.
		var endPos = startPos + rc.ViewportSize + Vector2.One * _tileSize;

		for (var y = startPos.Y; y < endPos.Y; y += _tileSize)
		{
			for (var x = startPos.X; x < endPos.X; x += _tileSize)
			{
				var tileX = x / _tileSize;
				var tileY = y / _tileSize;

				var screenPos = camera.WorldToScreen(new Vector2(x, y)).Floor();
				var tileRef = GetTile(tileX, tileY);
				if (!tileRef.IsEmpty)
				{
					tiles.Get(tileRef.TileId).Render(rc, screenPos); // Render the tile at the correct screen position.
				}
			}
		}

		#endregion
	}
}