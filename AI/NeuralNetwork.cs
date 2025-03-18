using Critters.AI.Layers;

namespace Critters.AI;

/// <summary>
/// Neural network composed of multiple layers.
/// </summary>
public class NeuralNetwork
{
	private List<Layer> layers = new();

	/// <summary>
	/// Adds a layer to the network.
	/// </summary>
	public void AddLayer(Layer layer)
	{
		layers.Add(layer);
	}

	/// <summary>
	/// Performs forward pass through the entire network.
	/// </summary>
	public double[] Forward(double[] inputs)
	{
		var currentOutput = inputs;
		
		foreach (var layer in layers)
		{
			currentOutput = layer.Forward(currentOutput);
		}
		
		return currentOutput;
	}

	/// <summary>
	/// Trains the network on a single example.
	/// </summary>
	private double TrainSample(double[] input, double[] target, double learningRate)
	{
		// Forward pass
		var prediction = Forward(input);
		
		// Calculate loss (MSE).
		var outputGradient = new double[prediction.Length];
		var totalLoss = 0.0;
		
		for (var i = 0; i < prediction.Length; i++)
		{
			var error = prediction[i] - target[i];
			outputGradient[i] = error; // Derivative of MSE.
			totalLoss += error * error;
		}
		
		// Backward pass.
		foreach (var layer in layers.AsEnumerable().Reverse())
		{
			outputGradient = layer.Backward(outputGradient, learningRate);
		}
		
		return totalLoss;
	}

	/// <summary>
	/// Trains the network on a dataset
	/// </summary>
	public void Train(List<(double[] input, double[] target)> trainingData, double learningRate = 0.01, int epochs = 1000)
	{
		for (var epoch = 0; epoch < epochs; epoch++)
		{
			var totalLoss = 0.0;
			
			foreach (var (input, target) in trainingData)
			{
				totalLoss += TrainSample(input, target, learningRate);
			}
			
			// // Print progress periodically
			// if (epoch % 100 == 0 || epoch == epochs - 1)
			// {
			// 	var avgLoss = totalLoss / trainingData.Count;
			// 	Console.WriteLine($"Epoch {epoch}: Average Loss = {avgLoss}");
			// }
		}
	}

	/// <summary>
	/// Evaluates the model on test data.
	/// </summary>
	public double Evaluate(List<(double[] input, double[] target)> testData)
	{
		var totalLoss = 0.0;
		
		foreach (var (input, target) in testData)
		{
			var prediction = Forward(input);
			
			for (var i = 0; i < prediction.Length; i++)
			{
				var error = prediction[i] - target[i];
				totalLoss += error * error;
			}
		}
		
		return totalLoss / testData.Count;
	}
}