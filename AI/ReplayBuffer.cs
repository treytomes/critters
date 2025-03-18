namespace Critters.AI;

public class ReplayBuffer
{
	private readonly int capacity;
	private readonly List<Experience> buffer;
	private readonly Random random;

	public class Experience
	{
		public double[] State { get; }
		public int Action { get; }
		public double Reward { get; }
		public double[] NextState { get; }
		public bool Done { get; }

		public Experience(double[] state, int action, double reward, double[] nextState, bool done)
		{
			State = state;
			Action = action;
			Reward = reward;
			NextState = nextState;
			Done = done;
		}
	}

	public ReplayBuffer(int capacity)
	{
		this.capacity = capacity;
		buffer = new List<Experience>(capacity);
		random = new Random();
	}

	public void Add(double[] state, int action, double reward, double[] nextState, bool done)
	{
		if (buffer.Count >= capacity)
			buffer.RemoveAt(0);
			
		buffer.Add(new Experience(state, action, reward, nextState, done));
	}

	public List<Experience> SampleBatch(int batchSize)
	{
		batchSize = Math.Min(batchSize, buffer.Count);
		var batch = new List<Experience>(batchSize);
		
		for (int i = 0; i < batchSize; i++)
		{
			int index = random.Next(buffer.Count);
			batch.Add(buffer[index]);
		}
		
		return batch;
	}

	public int Count => buffer.Count;
}