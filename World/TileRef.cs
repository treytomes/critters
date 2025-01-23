
using Newtonsoft.Json;

namespace Critters.World;

/// <summary>
/// Represents a reference to a Tile in a Chunk.
/// </summary>
public struct TileRef
{
	public static readonly TileRef Empty = new TileRef(0);

	public TileRef(int tileId)
	{
		TileId = tileId;
	}
	
	public int TileId { get; set; }

	[JsonIgnore]
	public bool IsEmpty => TileId == 0;
}