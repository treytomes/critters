using Critters.Gfx;
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

	#endregion

	#region Methods

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

	// TODO: If this doesn't work for negatives, use FloorDiv.
	public void SetTile(Vector2 worldPosition, Tile? tile) => SetTile((int)worldPosition.X, (int)worldPosition.Y, tile);

	/// <summary>
	/// Set a tile at a specific world position.
	/// </summary>
	public void SetTile(int worldX, int worldY, Tile? tile)
	{
		var chunkX = MathHelper.FloorDiv(worldX, _chunkSize);
		var chunkY = MathHelper.FloorDiv(worldY, _chunkSize);
		var localX = Math.Abs(worldX % _chunkSize);
		var localY = Math.Abs(worldY % _chunkSize);

		var chunk = GetOrCreateChunk(chunkX, chunkY);
		chunk.SetTile(localX, localY, tile);
	}

	// TODO: If this doesn't work for negatives, use FloorDiv.
	public Tile? GetTile(Vector2 worldPosition) => GetTile((int)worldPosition.X, (int)worldPosition.Y);

	public Tile? GetTile(float worldX, float worldY) => GetTile((int)worldX, (int)worldY);

	/// <summary>
	/// Get a tile at a specific world position.
	/// </summary>
	public Tile? GetTile(int worldX, int worldY)
	{
		var chunkX = MathHelper.FloorDiv(worldX, _chunkSize);
		var chunkY = MathHelper.FloorDiv(worldY, _chunkSize);
		var localX = Math.Abs(worldX % _chunkSize);
		var localY = Math.Abs(worldY % _chunkSize);

		if (_chunks.TryGetValue((chunkX, chunkY), out var chunk))
		{
				return chunk.GetTile(localX, localY);
		}

		return null; // No tile exists.
	}

	public void Render(RenderingContext rc, Camera camera)
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
				var tile = GetTile(tileX, tileY);
				tile?.Render(rc, screenPos); // Render the tile at the correct screen position.
			}
		}

		#endregion
	}
}