namespace Critters.AI;

class SerializableReplayBuffer
{
	public int Capacity { get; set; }
	public required List<SerializableExperience> Buffer { get; set; }
}
