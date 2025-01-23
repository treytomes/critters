using Critters.World;
using OpenTK.Mathematics;

namespace Critters.IO;

class SerializableLevelChunk
{
	public required SerializableVector2i ChunkPosition { get; set; }
	public required SerializableChunk Chunk { get; set; }
}
