using Critters.World;

namespace Critters.IO;

class SerializableChunk
{
	public int Size { get; set; }
	public required TileRef[,] Tiles { get; set; }
}
