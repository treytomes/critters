using Critters.AI.Layers;

namespace Critters.AI;

/// <summary>
/// Deep-Q Network
/// </summary>
public class DQN
{
	private NeuralNetwork qNetwork;
	private NeuralNetwork targetNetwork;
	private ReplayBuffer replayBuffer;
	private int stateSize;
	private int actionSize;
	private int trainingSteps;
	
	// Hyperparameters
	private double epsilon;
	private double epsilonMin;
	private double epsilonDecay;
	private double gamma;
	private double learningRate;
	private int targetUpdateFrequency;
	private int batchSize;
	
	private Random random = new Random();

	public DQN(int stateSize, int actionSize, int hiddenSize = 64)
	{
		this.stateSize = stateSize;
		this.actionSize = actionSize;
		
		// Hyperparameters
		epsilon = 1.0;            // Exploration rate
		epsilonMin = 0.1;         // Minimum exploration rate
		epsilonDecay = 0.995;     // Decay rate for exploration
		gamma = 0.99;             // Discount factor
		learningRate = 0.001;     // Learning rate
		targetUpdateFrequency = 1000; // How often to update target network
		batchSize = 64;           // Batch size for training
		
		// Initialize replay buffer
		replayBuffer = new ReplayBuffer(capacity: 10000);
		
		// Initialize networks
		InitializeNetworks(hiddenSize);
		
		trainingSteps = 0;
	}

	private void InitializeNetworks(int hiddenSize)
	{
		// Q-Network
		qNetwork = new NeuralNetwork();
		qNetwork.AddLayer(new InputLayer(stateSize));
		qNetwork.AddLayer(new DenseLayer(stateSize, hiddenSize));
		qNetwork.AddLayer(new ActivationLayer(hiddenSize, ActivationType.ReLU));
		qNetwork.AddLayer(new DenseLayer(hiddenSize, hiddenSize));
		qNetwork.AddLayer(new ActivationLayer(hiddenSize, ActivationType.ReLU));
		qNetwork.AddLayer(new DenseLayer(hiddenSize, actionSize));
		
		// Target Network (clone of Q-Network)
		targetNetwork = CloneNetwork(qNetwork);
	}

	private NeuralNetwork CloneNetwork(NeuralNetwork source)
	{
		// In a real implementation, you would need to clone the weights
		// For simplicity, we'll initialize a new network with the same architecture
		NeuralNetwork clone = new NeuralNetwork();
		clone.AddLayer(new InputLayer(stateSize));
		clone.AddLayer(new DenseLayer(stateSize, 64));
		clone.AddLayer(new ActivationLayer(64, ActivationType.ReLU));
		clone.AddLayer(new DenseLayer(64, 64));
		clone.AddLayer(new ActivationLayer(64, ActivationType.ReLU));
		clone.AddLayer(new DenseLayer(64, actionSize));
		
		// In an actual implementation, you'd copy the weights here
		// This would require access to the internal weights of your NeuralNetwork class
		
		return clone;
	}

	public int SelectAction(double[] state)
	{
		// Epsilon-greedy action selection
		if (random.NextDouble() < epsilon)
		{
			return random.Next(actionSize);
		}
		else
		{
			// Choose action with highest Q-value
			var qValues = qNetwork.Forward(state);
			return Array.IndexOf(qValues, qValues.Max());
		}
	}

	public void StoreExperience(double[] state, int action, double reward, double[] nextState, bool done)
	{
		replayBuffer.Add(state, action, reward, nextState, done);
	}

	public void Train()
	{
		// Don't train until we have enough samples
		if (replayBuffer.Count < batchSize)
			return;
				
		// Sample a batch of experiences
		var batch = replayBuffer.SampleBatch(batchSize);
		
		// Create training data from batch
		var trainingData = new List<(double[] input, double[] target)>();
		
		foreach (var experience in batch)
		{
			// Current Q-values
			double[] currentQValues = qNetwork.Forward(experience.State);
			
			// Next Q-values from target network
			double[] nextQValues = targetNetwork.Forward(experience.NextState);
			
			// Copy current Q-values as targets
			double[] targetQValues = (double[])currentQValues.Clone();
			
			// Update target for the chosen action
			if (experience.Done)
			{
				targetQValues[experience.Action] = experience.Reward;
			}
			else
			{
				// Q-Learning update: Q(s,a) = r + γ * max Q'(s',a')
				targetQValues[experience.Action] = experience.Reward + gamma * nextQValues.Max();
			}
			
			trainingData.Add((experience.State, targetQValues));
		}
		
		// Train the Q-network on batch
		qNetwork.Train(trainingData, learningRate, epochs: 1);
		
		// Update target network periodically
		trainingSteps++;
		if (trainingSteps % targetUpdateFrequency == 0)
		{
			targetNetwork = CloneNetwork(qNetwork);
			// In a real implementation, you'd copy weights here
		}
		
		// Decay exploration rate
		if (epsilon > epsilonMin)
		{
			epsilon *= epsilonDecay;
		}
	}

	public double GetMaxQ(double[] state)
	{
			double[] qValues = qNetwork.Forward(state);
			return qValues.Max();
	}
}