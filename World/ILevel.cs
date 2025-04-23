using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;

namespace Critters.World;

/// <summary>
/// Defines the interface for a game level composed of multiple chunks of tiles.
/// </summary>
interface ILevel : IDisposable
{
	#region Properties

	/// <summary>
	/// Gets the size of each chunk in tiles.
	/// </summary>
	int ChunkSize { get; }

	/// <summary>
	/// Gets the size of each tile in pixels.
	/// </summary>
	int TileSize { get; }

	/// <summary>
	/// Gets the number of chunks in this level.
	/// </summary>
	int ChunkCount { get; }

	/// <summary>
	/// Gets a value indicating whether any chunks have been modified since the last save.
	/// </summary>
	bool HasUnsavedChanges { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Saves the level to a JSON file.
	/// </summary>
	/// <param name="path">The path to save the JSON file.</param>
	/// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
	/// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
	void Save(string path);

	/// <summary>
	/// Converts this level to a serializable representation.
	/// </summary>
	/// <returns>A serializable representation of this level.</returns>
	SerializableLevel ToSerializable();

	/// <summary>
	/// Converts world coordinates to chunk coordinates.
	/// </summary>
	/// <param name="worldX">The X coordinate in world space.</param>
	/// <param name="worldY">The Y coordinate in world space.</param>
	/// <returns>The chunk coordinates containing the world position.</returns>
	Vector2i WorldToChunkCoordinates(int worldX, int worldY);

	/// <summary>
	/// Converts world coordinates to local coordinates within a chunk.
	/// </summary>
	/// <param name="worldX">The X coordinate in world space.</param>
	/// <param name="worldY">The Y coordinate in world space.</param>
	/// <returns>The local coordinates within the chunk.</returns>
	Vector2i WorldToLocalCoordinates(int worldX, int worldY);

	/// <summary>
	/// Gets a chunk at the specified chunk coordinates, or null if it doesn't exist.
	/// </summary>
	/// <param name="chunkX">The X coordinate of the chunk.</param>
	/// <param name="chunkY">The Y coordinate of the chunk.</param>
	/// <returns>The chunk at the specified coordinates, or null if it doesn't exist.</returns>
	Chunk? GetChunk(int chunkX, int chunkY);

	/// <summary>
	/// Sets a tile at the specified world position.
	/// </summary>
	/// <param name="worldPosition">The position in world coordinates.</param>
	/// <param name="tileId">The ID of the tile to set.</param>
	/// <returns>True if the tile was set; false if the position was invalid.</returns>
	bool SetTile(Vector2 worldPosition, int tileId);

	/// <summary>
	/// Sets a tile at the specified world coordinates.
	/// </summary>
	/// <param name="worldX">The X coordinate in world space.</param>
	/// <param name="worldY">The Y coordinate in world space.</param>
	/// <param name="tileId">The ID of the tile to set.</param>
	/// <returns>True if the tile was set; false if the coordinates were invalid.</returns>
	bool SetTile(int worldX, int worldY, int tileId);

	/// <summary>
	/// Gets a tile at the specified world position.
	/// </summary>
	/// <param name="worldPosition">The position in world coordinates.</param>
	/// <returns>The tile reference at the specified position.</returns>
	TileRef GetTile(Vector2 worldPosition);

	/// <summary>
	/// Gets a tile at the specified world coordinates.
	/// </summary>
	/// <param name="worldX">The X coordinate in world space.</param>
	/// <param name="worldY">The Y coordinate in world space.</param>
	/// <returns>The tile reference at the specified coordinates.</returns>
	TileRef GetTile(int worldX, int worldY);

	/// <summary>
	/// Renders the level using the provided rendering context, tile repository, and camera.
	/// </summary>
	/// <param name="rc">The rendering context.</param>
	/// <param name="tiles">The tile repository.</param>
	/// <param name="camera">The camera.</param>
	void Render(IRenderingContext rc, TileRepo tiles, Camera camera);

	/// <summary>
	/// Removes all chunks from the level.
	/// </summary>
	void Clear();

	/// <summary>
	/// Removes empty chunks to save memory.
	/// </summary>
	/// <returns>The number of chunks removed.</returns>
	int RemoveEmptyChunks();

	/// <summary>
	/// Gets all chunk coordinates in this level.
	/// </summary>
	/// <returns>An enumerable of all chunk coordinates.</returns>
	IEnumerable<Vector2i> GetChunkCoordinates();

	/// <summary>
	/// Checks if a chunk exists at the specified coordinates.
	/// </summary>
	/// <param name="chunkX">The X coordinate of the chunk.</param>
	/// <param name="chunkY">The Y coordinate of the chunk.</param>
	/// <returns>True if the chunk exists; otherwise, false.</returns>
	bool ChunkExists(int chunkX, int chunkY);

	/// <summary>
	/// Gets the boundaries of the level in world coordinates.
	/// </summary>
	/// <returns>A tuple containing the minimum and maximum world coordinates.</returns>
	(Vector2 min, Vector2 max) GetBoundaries();

	/// <summary>
	/// Gets the total number of non-empty tiles in the level.
	/// </summary>
	/// <returns>The total number of non-empty tiles.</returns>
	int CountNonEmptyTiles();

	/// <summary>
	/// Performs a specified action on each chunk in the level.
	/// </summary>
	/// <param name="action">The action to perform on each chunk.</param>
	void ForEachChunk(Action<Vector2i, Chunk> action);

	/// <summary>
	/// Performs a specified action on each non-empty tile in the level.
	/// </summary>
	/// <param name="action">The action to perform on each tile, providing world coordinates and tile reference.</param>
	void ForEachTile(Action<int, int, TileRef> action);

	#endregion
}
