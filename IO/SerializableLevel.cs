namespace Critters.IO;

class SerializableLevel
{
	public required List<SerializableLevelChunk> Chunks { get; set; }
	public int ChunkSize { get; set;}
	public int TileSize { get; set; }
}
