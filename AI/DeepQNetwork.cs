using Critters.AI.Layers;

namespace Critters.AI;

/// <summary>
/// Deep-Q Network
/// </summary>
class DeepQNetwork : ICloneable<DeepQNetwork>
{
	#region Fields

	private NeuralNetwork _qNetwork;
	private NeuralNetwork _targetNetwork;
	private ReplayBuffer _replayBuffer;
	private readonly int _stateSize;
	private readonly int _actionSize;
	private int _trainingSteps;
	
	#endregion

	#region Constructors

	public DeepQNetwork(int stateSize, int actionSize, int hiddenSize = 64)
	{
		_stateSize = stateSize;
		_actionSize = actionSize;
		_trainingSteps = 0;
		
		// Initialize replay buffer.
		_replayBuffer = new ReplayBuffer(capacity: 10000);

		// Hyperparameters
		Epsilon = 1.0;
		EpsilonMin = 0.1;
		EpsilonDecay = 0.995;
		Gamma = 0.99;
		LearningRate = 0.001;
		TargetUpdateFrequency = 1000;
		BatchSize = 64;
		
		//
		// Initialize networks.
		//

		// Q-Network.
		_qNetwork = new NeuralNetwork();
		_qNetwork.AddLayer(new InputLayer(_stateSize));
		_qNetwork.AddLayer(new DenseLayer(_stateSize, hiddenSize));
		_qNetwork.AddLayer(new ActivationLayer(hiddenSize, ActivationType.ReLU));
		_qNetwork.AddLayer(new DenseLayer(hiddenSize, hiddenSize));
		_qNetwork.AddLayer(new ActivationLayer(hiddenSize, ActivationType.ReLU));
		_qNetwork.AddLayer(new DenseLayer(hiddenSize, _actionSize));
		
		// Target Network (clone of Q-Network).
		_targetNetwork = _qNetwork.Clone();
	}

	private DeepQNetwork(DeepQNetwork other)
	{
		_stateSize = other._stateSize;
		_actionSize = other._actionSize;
		_trainingSteps = other._trainingSteps;
		
		// Initialize replay buffer.
		_replayBuffer = other._replayBuffer.Clone();

		// Hyperparameters
		Epsilon = other.Epsilon;
		EpsilonMin = other.EpsilonMin;
		EpsilonDecay = other.EpsilonDecay;
		Gamma = other.Gamma;
		LearningRate = other.LearningRate;
		TargetUpdateFrequency = other.TargetUpdateFrequency;
		BatchSize = other.BatchSize;
		
		//
		// Initialize networks.
		//

		// Q-Network.
		_qNetwork = other._qNetwork.Clone();
		
		// Target Network (clone of Q-Network).
		_targetNetwork = other._targetNetwork.Clone();
	}

	private DeepQNetwork(SerializableDeepQNetwork other)
	{
		_stateSize = other.StateSize;
		_actionSize = other.ActionSize;
		_trainingSteps = other.TrainingSteps;
		
		// Initialize replay buffer.
		_replayBuffer = ReplayBuffer.Deserialize(other.ReplayBuffer);

		// Hyperparameters
		Epsilon = other.Epsilon;
		EpsilonMin = other.EpsilonMin;
		EpsilonDecay = other.EpsilonDecay;
		Gamma = other.Gamma;
		LearningRate = other.LearningRate;
		TargetUpdateFrequency = other.TargetUpdateFrequency;
		BatchSize = other.BatchSize;
		
		//
		// Initialize networks.
		//

		// Q-Network.
		_qNetwork = NeuralNetwork.Deserialize(other.QNetwork);
		
		// Target Network (clone of Q-Network).
		_targetNetwork = NeuralNetwork.Deserialize(other.TargetNetwork);
	}

	#endregion

	#region Properties

	// Hyperparameters

	/// <summary>
	/// Exploration rate.
	/// </summary>
	public double Epsilon { get; set; }

	/// <summary>
	/// Minimum exploration rate.
	/// </summary>
	public double EpsilonMin { get; set; }

	/// <summary>
	/// Decay rate for exploration.
	/// </summary>
	public double EpsilonDecay { get; set; }

	/// <summary>
	/// Discount factor.
	/// </summary>
	public double Gamma { get; set; }

	/// <summary>
	/// Learning rate.
	/// </summary>
	public double LearningRate { get; set; }

	/// <summary>
	/// How often to update target network.
	/// </summary>
	public int TargetUpdateFrequency { get; set; }

	/// <summary>
	/// Batch size for training.
	/// </summary>
	public int BatchSize { get; set; }

	#endregion

	#region Methods

	public static DeepQNetwork Deserialize(SerializableDeepQNetwork other)
	{
		return new DeepQNetwork(other);
	}

	public SerializableDeepQNetwork Serialize()
	{
		return new SerializableDeepQNetwork()
		{
			ActionSize = _actionSize,
			BatchSize = BatchSize,
			Epsilon = Epsilon,
			EpsilonDecay = EpsilonDecay,
			EpsilonMin = EpsilonMin,
			Gamma = Gamma,
			LearningRate = LearningRate,
			QNetwork = _qNetwork.Serialize(),
			ReplayBuffer = _replayBuffer.Serialize(),
			StateSize = _stateSize,
			TargetNetwork = _targetNetwork.Serialize(),
			TargetUpdateFrequency = TargetUpdateFrequency,
			TrainingSteps = _trainingSteps,
		};
	}

	public int SelectAction(double[] state)
	{
		// Epsilon-greedy action selection
		if (Random.Shared.NextDouble() < Epsilon)
		{
			return Random.Shared.Next(_actionSize);
		}
		else
		{
			// Choose action with highest Q-value
			var qValues = _qNetwork.Forward(state);
			return Array.IndexOf(qValues, qValues.Max());
		}
	}

	public void StoreExperience(double[] state, int action, double reward, double[] nextState, bool done)
	{
		_replayBuffer.Add(state, action, reward, nextState, done);
	}

	public void Train()
	{
		// Don't train until we have enough samples.
		if (_replayBuffer.Count < BatchSize)
		{
			return;
		}
				
		// Sample a batch of experiences.
		var batch = _replayBuffer.SampleBatch(BatchSize);
		
		// Create training data from batch
		var trainingData = new List<(double[] input, double[] target)>();
		
		foreach (var experience in batch)
		{
			// Current Q-values.
			var currentQValues = _qNetwork.Forward(experience.State);
			
			// Next Q-values from target network.
			var nextQValues = _targetNetwork.Forward(experience.NextState);
			
			// Copy current Q-values as targets.
			var targetQValues = currentQValues.ToArray();
			
			// Update target for the chosen action.
			if (experience.Done)
			{
				targetQValues[experience.Action] = experience.Reward;
			}
			else
			{
				// Q-Learning update: Q(s,a) = r + γ * max Q'(s',a')
				targetQValues[experience.Action] = experience.Reward + Gamma * nextQValues.Max();
			}
			
			trainingData.Add((experience.State, targetQValues));
		}
		
		// Train the Q-network on batch.
		_qNetwork.Train(trainingData, LearningRate, epochs: 1);
		
		// Update target network periodically.
		_trainingSteps++;
		if ((_trainingSteps % TargetUpdateFrequency) == 0)
		{
			_targetNetwork = _qNetwork.Clone();
		}
		
		// Decay exploration rate.
		if (Epsilon > EpsilonMin)
		{
			Epsilon *= EpsilonDecay;
		}
	}

	public double GetMaxQ(double[] state)
	{
		var qValues = _qNetwork.Forward(state);
		return qValues.Max();
	}

	public DeepQNetwork Clone()
	{
		return new DeepQNetwork(this);
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	#endregion
}