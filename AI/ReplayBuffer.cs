namespace Critters.AI;

class SerializableReplayBuffer
{
	public int Capacity { get; set; }
	public required List<SerializableExperience> Buffer { get; set; }
}

class ReplayBuffer : ICloneable<ReplayBuffer>
{
	#region Fields

	private readonly List<Experience> _buffer;

	#endregion

	#region Constructors

	public ReplayBuffer(int capacity)
	{
		Capacity = capacity;
		_buffer = new List<Experience>(capacity);
	}

	private ReplayBuffer(ReplayBuffer other)
		: this(other.Capacity)
	{
		_buffer.AddRange(other._buffer.Select(x => x.Clone()));
	}

	private ReplayBuffer(SerializableReplayBuffer other)
		: this(other.Capacity)
	{
		_buffer.AddRange(other.Buffer.Select(x => Experience.Deserialize(x)));
	}

	#endregion

	#region Properties

	public int Capacity { get; }
	public int Count => _buffer.Count;

	#endregion

	#region Methods

	public static ReplayBuffer Deserialize(SerializableReplayBuffer data)
	{
		return new ReplayBuffer(data);
	}

	public SerializableReplayBuffer Serialize()
	{
		return new SerializableReplayBuffer()
		{
			Capacity = Capacity,
			Buffer = _buffer.Select(x => x.Serialize()).ToList(),
		};
	}

	public void Add(double[] state, int action, double reward, double[] nextState, bool done)
	{
		if (_buffer.Count >= Capacity)
		{
			_buffer.RemoveAt(0);
		}
		_buffer.Add(new Experience(state, action, reward, nextState, done));
	}

	public List<Experience> SampleBatch(int batchSize)
	{
		batchSize = Math.Min(batchSize, _buffer.Count);
		var batch = new List<Experience>(batchSize);
		
		for (var i = 0; i < batchSize; i++)
		{
			var index = Random.Shared.Next(_buffer.Count);
			batch.Add(_buffer[index]);
		}
		
		return batch;
	}

	public ReplayBuffer Clone()
	{
		return new ReplayBuffer(this);
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	#endregion
}