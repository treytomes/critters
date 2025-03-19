using Critters.AI.Layers;

namespace Critters.AI;

/// <summary>
/// Deep-Q Network implementation with advanced reinforcement learning features
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
    private readonly Random _random;
    
    // Learning metrics
    private double _averageLoss = 0;
    private double _averageQValue = 0;
    private int _metricsUpdateCount = 0;
    
    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new Deep Q-Network
    /// </summary>
    /// <param name="stateSize">Size of the input state vector</param>
    /// <param name="actionSize">Number of possible actions</param>
    /// <param name="hiddenSize">Size of hidden layers</param>
    /// <param name="bufferCapacity">Capacity of experience replay buffer</param>
    /// <param name="networkArchitecture">Optional custom network architecture</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public DeepQNetwork(
        int stateSize, 
        int actionSize, 
        int hiddenSize = 64, 
        int bufferCapacity = 10000,
        NetworkArchitecture? networkArchitecture = null)
    {
        if (stateSize <= 0) throw new ArgumentException("State size must be positive", nameof(stateSize));
        if (actionSize <= 0) throw new ArgumentException("Action size must be positive", nameof(actionSize));
        if (hiddenSize <= 0) throw new ArgumentException("Hidden size must be positive", nameof(hiddenSize));
        if (bufferCapacity <= 0) throw new ArgumentException("Buffer capacity must be positive", nameof(bufferCapacity));
        
        _stateSize = stateSize;
        _actionSize = actionSize;
        _trainingSteps = 0;
        _random = new Random();
        
        // Initialize replay buffer
        _replayBuffer = new ReplayBuffer(capacity: bufferCapacity);

        // Hyperparameters with reasonable defaults
        Epsilon = 1.0;
        EpsilonMin = 0.1;
        EpsilonDecay = 0.995;
        Gamma = 0.99;
        LearningRate = 0.001;
        TargetUpdateFrequency = 1000;
        BatchSize = 64;
        UsePrioritizedReplay = false;
        UseDoubleDQN = false;
        
        // Initialize networks
        if (networkArchitecture != null)
        {
            // Use custom architecture if provided
            _qNetwork = networkArchitecture.CreateNetwork(stateSize, actionSize);
        }
        else
        {
            // Use default architecture
            _qNetwork = new NeuralNetwork();
            _qNetwork.AddLayer(new InputLayer(_stateSize));
            _qNetwork.AddLayer(new DenseLayer(_stateSize, hiddenSize));
            _qNetwork.AddLayer(new ActivationLayer(hiddenSize, ActivationType.ReLU));
            _qNetwork.AddLayer(new DenseLayer(hiddenSize, hiddenSize));
            _qNetwork.AddLayer(new ActivationLayer(hiddenSize, ActivationType.ReLU));
            _qNetwork.AddLayer(new DenseLayer(hiddenSize, _actionSize));
        }
        
        // Target Network (clone of Q-Network)
        _targetNetwork = _qNetwork.Clone();
    }

    private DeepQNetwork(DeepQNetwork other)
    {
        _stateSize = other._stateSize;
        _actionSize = other._actionSize;
        _trainingSteps = other._trainingSteps;
        _random = new Random();
        _averageLoss = other._averageLoss;
        _averageQValue = other._averageQValue;
        _metricsUpdateCount = other._metricsUpdateCount;
        
        // Initialize replay buffer
        _replayBuffer = other._replayBuffer.Clone();

        // Hyperparameters
        Epsilon = other.Epsilon;
        EpsilonMin = other.EpsilonMin;
        EpsilonDecay = other.EpsilonDecay;
        Gamma = other.Gamma;
        LearningRate = other.LearningRate;
        TargetUpdateFrequency = other.TargetUpdateFrequency;
        BatchSize = other.BatchSize;
        UsePrioritizedReplay = other.UsePrioritizedReplay;
        UseDoubleDQN = other.UseDoubleDQN;
        
        // Networks
        _qNetwork = other._qNetwork.Clone();
        _targetNetwork = other._targetNetwork.Clone();
    }

    private DeepQNetwork(SerializableDeepQNetwork other)
    {
        _stateSize = other.StateSize;
        _actionSize = other.ActionSize;
        _trainingSteps = other.TrainingSteps;
        _random = new Random();
        _averageLoss = other.AverageLoss;
        _averageQValue = other.AverageQValue;
        _metricsUpdateCount = other.MetricsUpdateCount;
        
        // Initialize replay buffer
        _replayBuffer = ReplayBuffer.Deserialize(other.ReplayBuffer);

        // Hyperparameters
        Epsilon = other.Epsilon;
        EpsilonMin = other.EpsilonMin;
        EpsilonDecay = other.EpsilonDecay;
        Gamma = other.Gamma;
        LearningRate = other.LearningRate;
        TargetUpdateFrequency = other.TargetUpdateFrequency;
        BatchSize = other.BatchSize;
        UsePrioritizedReplay = other.UsePrioritizedReplay;
        UseDoubleDQN = other.UseDoubleDQN;
        
        // Networks
        _qNetwork = NeuralNetwork.Deserialize(other.QNetwork);
        _targetNetwork = NeuralNetwork.Deserialize(other.TargetNetwork);
    }

    #endregion

    #region Properties

    // Hyperparameters

    /// <summary>
    /// Exploration rate (probability of taking a random action)
    /// </summary>
    public double Epsilon { get; set; }

    /// <summary>
    /// Minimum exploration rate
    /// </summary>
    public double EpsilonMin { get; set; }

    /// <summary>
    /// Decay rate for exploration
    /// </summary>
    public double EpsilonDecay { get; set; }

    /// <summary>
    /// Discount factor for future rewards
    /// </summary>
    public double Gamma { get; set; }

    /// <summary>
    /// Learning rate for neural network updates
    /// </summary>
    public double LearningRate { get; set; }

    /// <summary>
    /// How often to update target network (in training steps)
    /// </summary>
    public int TargetUpdateFrequency { get; set; }

    /// <summary>
    /// Batch size for training
    /// </summary>
    public int BatchSize { get; set; }
    
    /// <summary>
    /// Use prioritized experience replay for better sample efficiency
    /// </summary>
    public bool UsePrioritizedReplay { get; set; }
    
    /// <summary>
    /// Use Double DQN algorithm for more stable learning
    /// </summary>
    public bool UseDoubleDQN { get; set; }
    
    /// <summary>
    /// Count of stored experiences in the replay buffer
    /// </summary>
    public int ExperienceCount => _replayBuffer.Count;
    
    /// <summary>
    /// Average loss from recent training steps
    /// </summary>
    public double AverageLoss => _metricsUpdateCount > 0 ? _averageLoss / _metricsUpdateCount : 0;
    
    /// <summary>
    /// Average Q-value from recent actions
    /// </summary>
    public double AverageQValue => _metricsUpdateCount > 0 ? _averageQValue / _metricsUpdateCount : 0;
    
    /// <summary>
    /// Number of completed training steps
    /// </summary>
    public int TrainingSteps => _trainingSteps;

    #endregion

    #region Methods
    
    /// <summary>
    /// Deserializes a DeepQNetwork from its serializable form
    /// </summary>
    public static DeepQNetwork Deserialize(SerializableDeepQNetwork other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));
        return new DeepQNetwork(other);
    }

    /// <summary>
    /// Serializes the DeepQNetwork to a serializable form
    /// </summary>
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
            UsePrioritizedReplay = UsePrioritizedReplay,
            UseDoubleDQN = UseDoubleDQN,
            AverageLoss = _averageLoss,
            AverageQValue = _averageQValue,
            MetricsUpdateCount = _metricsUpdateCount
        };
    }

    /// <summary>
    /// Selects an action based on the current state using epsilon-greedy strategy
    /// </summary>
    /// <param name="state">Current state vector</param>
    /// <returns>Index of selected action</returns>
    /// <exception cref="ArgumentNullException">Thrown when state is null</exception>
    /// <exception cref="ArgumentException">Thrown when state size is invalid</exception>
    public int SelectAction(double[] state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (state.Length != _stateSize) throw new ArgumentException($"State size mismatch. Expected {_stateSize}, got {state.Length}", nameof(state));
        
        // Epsilon-greedy action selection
        if (_random.NextDouble() < Epsilon)
        {
            return _random.Next(_actionSize);
        }
        else
        {
            // Choose action with highest Q-value
            var qValues = _qNetwork.Forward(state);
            
            // Find index of max Q-value in a single pass
            int maxIndex = 0;
            double maxValue = qValues[0];
            for (int i = 1; i < qValues.Length; i++)
            {
                if (qValues[i] > maxValue)
                {
                    maxValue = qValues[i];
                    maxIndex = i;
                }
            }
            
            // Update metrics
            _averageQValue += maxValue;
            _metricsUpdateCount++;
            if (_metricsUpdateCount > 1000)
            {
                // Reset metrics periodically to focus on recent performance
                _averageQValue /= _metricsUpdateCount;
                _metricsUpdateCount = 1;
            }
            
            return maxIndex;
        }
    }

    /// <summary>
    /// Adds an experience to the replay buffer
    /// </summary>
    public void StoreExperience(double[] state, int action, double reward, double[] nextState, bool done)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (nextState == null) throw new ArgumentNullException(nameof(nextState));
        if (state.Length != _stateSize) throw new ArgumentException($"State size mismatch. Expected {_stateSize}, got {state.Length}", nameof(state));
        if (nextState.Length != _stateSize) throw new ArgumentException($"Next state size mismatch. Expected {_stateSize}, got {nextState.Length}", nameof(nextState));
        if (action < 0 || action >= _actionSize) throw new ArgumentOutOfRangeException(nameof(action), $"Action must be between 0 and {_actionSize - 1}");
        
        _replayBuffer.Add(state, action, reward, nextState, done);
    }

    /// <summary>
    /// Clears all stored experiences from the replay buffer
    /// </summary>
    public void ClearExperiences()
    {
        _replayBuffer.Clear();
    }

    /// <summary>
    /// Performs one training step using a batch of experiences
    /// </summary>
    public void Train()
    {
        // Don't train until we have enough samples
        if (_replayBuffer.Count < BatchSize)
        {
            return;
        }
                
        // Sample a batch of experiences
        var batch = _replayBuffer.SampleBatch(BatchSize);
        
        // Create training data from batch
        var trainingData = new List<(double[] input, double[] target)>();
        double batchLoss = 0;
        
        foreach (var experience in batch)
        {
            // Current Q-values
            var currentQValues = _qNetwork.Forward(experience.State);
            
            // Next Q-values from target network
            var nextQValues = _targetNetwork.Forward(experience.NextState);
            
            // Copy current Q-values as targets
            var targetQValues = (double[])currentQValues.Clone();
            
            // Update target for the chosen action
            if (experience.Done)
            {
                targetQValues[experience.Action] = experience.Reward;
            }
            else
            {
                if (UseDoubleDQN)
                {
                    // Double DQN: Use Q-network to select action, target network to evaluate
                    var nextQValuesFromQNetwork = _qNetwork.Forward(experience.NextState);
                    int bestNextAction = 0;
                    double maxValue = nextQValuesFromQNetwork[0];
                    
                    // Find best action according to Q-network
                    for (int i = 1; i < nextQValuesFromQNetwork.Length; i++)
                    {
                        if (nextQValuesFromQNetwork[i] > maxValue)
                        {
                            maxValue = nextQValuesFromQNetwork[i];
                            bestNextAction = i;
                        }
                    }
                    
                    // Use Q-value of that action from target network
                    targetQValues[experience.Action] = experience.Reward + Gamma * nextQValues[bestNextAction];
                }
                else
                {
                    // Standard Q-Learning update: Q(s,a) = r + γ * max Q'(s',a')
                    int bestNextAction = 0;
                    double maxValue = nextQValues[0];
                    
                    // Find best action in a single pass
                    for (int i = 1; i < nextQValues.Length; i++)
                    {
                        if (nextQValues[i] > maxValue)
                        {
                            maxValue = nextQValues[i];
                            bestNextAction = i;
                        }
                    }
                    
                    targetQValues[experience.Action] = experience.Reward + Gamma * maxValue;
                }
            }
            
            // Calculate loss for this sample (for metrics)
            double sampleLoss = Math.Pow(targetQValues[experience.Action] - currentQValues[experience.Action], 2);
            batchLoss += sampleLoss;
            
            // Add to training batch
            trainingData.Add((experience.State, targetQValues));
        }
        
        // Update loss metrics
        _averageLoss += (batchLoss / batch.Count);
        
        // Train the Q-network on batch
        _qNetwork.Train(trainingData, LearningRate, epochs: 1);
        
        // Update target network periodically
        _trainingSteps++;
        if ((_trainingSteps % TargetUpdateFrequency) == 0)
        {
            _targetNetwork = _qNetwork.Clone();
        }
        
        // Decay exploration rate
        if (Epsilon > EpsilonMin)
        {
            Epsilon *= EpsilonDecay;
        }
    }

    /// <summary>
    /// Gets the maximum Q-value for a given state
    /// </summary>
    public double GetMaxQ(double[] state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (state.Length != _stateSize) throw new ArgumentException($"State size mismatch. Expected {_stateSize}, got {state.Length}", nameof(state));
        
        var qValues = _qNetwork.Forward(state);
        
        // Find max Q-value in a single pass
        double maxValue = qValues[0];
        for (int i = 1; i < qValues.Length; i++)
        {
            if (qValues[i] > maxValue)
            {
                maxValue = qValues[i];
            }
        }
        
        return maxValue;
    }
    
    /// <summary>
    /// Gets all Q-values for a given state
    /// </summary>
    public double[] GetAllQValues(double[] state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (state.Length != _stateSize) throw new ArgumentException($"State size mismatch. Expected {_stateSize}, got {state.Length}", nameof(state));
        
        return _qNetwork.Forward(state);
    }

    /// <summary>
    /// Resets metrics used to track agent performance
    /// </summary>
    public void ResetMetrics()
    {
        _averageLoss = 0;
        _averageQValue = 0;
        _metricsUpdateCount = 0;
    }
    
    /// <summary>
    /// Creates a deep copy of this DeepQNetwork
    /// </summary>
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
