namespace Critters.AI;

class SerializableDeepQNetwork
{
	public required SerializableNeuralNetwork QNetwork { get; set; }
	public required SerializableNeuralNetwork TargetNetwork { get; set; }
	public required SerializableReplayBuffer ReplayBuffer { get; set; }
	public int StateSize { get; set; }
	public int ActionSize { get; set; }
	public int TrainingSteps { get; set; }
	public double Epsilon { get; set; }
	public double EpsilonMin { get; set; }
	public double EpsilonDecay { get; set; }
	public double Gamma { get; set; }
	public double LearningRate { get; set; }
	public int TargetUpdateFrequency { get; set; }
	public int BatchSize { get; set; }
}
