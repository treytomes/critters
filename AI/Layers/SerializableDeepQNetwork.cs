namespace Critters.AI;

/// <summary>
/// Serializable form of DeepQNetwork for persistence
/// </summary>
class SerializableDeepQNetwork
{
    public int StateSize { get; set; }
    public int ActionSize { get; set; }
    public int BatchSize { get; set; }
    public double Epsilon { get; set; }
    public double EpsilonDecay { get; set; }
    public double EpsilonMin { get; set; }
    public double Gamma { get; set; }
    public double LearningRate { get; set; }
    public int TargetUpdateFrequency { get; set; }
    public int TrainingSteps { get; set; }
    public bool UsePrioritizedReplay { get; set; }
    public bool UseDoubleDQN { get; set; }
    public double AverageLoss { get; set; }
    public double AverageQValue { get; set; }
    public int MetricsUpdateCount { get; set; }
    public required SerializableNeuralNetwork QNetwork { get; set; }
    public required SerializableNeuralNetwork TargetNetwork { get; set; }
    public required SerializableReplayBuffer ReplayBuffer { get; set; }
}