namespace Critters.AI;

interface IExperience
{
	double[] State { get; }
	int Action { get; }
	double Reward { get; }
	double[] NextState { get; }
	bool Done { get; }
}

class SerializableExperience : IExperience
{
	public required double[] State { get; set; }
	public int Action { get; set; }
	public double Reward { get; set; }
	public required double[] NextState { get; set; }
	public bool Done { get; set; }
}

class Experience : IExperience, ICloneable<Experience>
{
	#region Constructors

	public Experience(double[] state, int action, double reward, double[] nextState, bool done)
	{
		State = state.ToArray();
		Action = action;
		Reward = reward;
		NextState = nextState.ToArray();
		Done = done;
	}

	private Experience(IExperience other)
		: this(other.State, other.Action, other.Reward, other.NextState, other.Done)
	{
	}

	#endregion

	#region Properties

	public double[] State { get; }
	public int Action { get; }
	public double Reward { get; }
	public double[] NextState { get; }
	public bool Done { get; }

	#endregion

	#region Methods

	public static Experience Deserialize(SerializableExperience other)
	{
		return new Experience(other);
	}

	public SerializableExperience Serialize()
	{
		return new SerializableExperience()
		{
			Action = Action,
			Done = Done,
			NextState = NextState.ToArray(),
			Reward = Reward,
			State = State.ToArray(),
		};
	}

	public Experience Clone()
	{
		return new Experience(this);
	}
	
	object ICloneable.Clone()
	{
		return Clone();
	}

	#endregion
}
