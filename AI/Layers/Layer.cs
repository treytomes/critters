namespace Critters.AI.Layers;

/// <summary>
/// Base class for all neural network layers
/// </summary>
public abstract class Layer
{
	protected int _inputSize;
	protected int _outputSize;

	public Layer(int inputSize, int outputSize)
	{
		_inputSize = inputSize;
		_outputSize = outputSize;
	}

	/// <summary>
	/// Performs forward pass through the layer
	/// </summary>
	public abstract double[] Forward(double[] inputs);

	/// <summary>
	/// Performs backward pass through the layer
	/// </summary>
	public abstract double[] Backward(double[] outputGradients, double learningRate);

	/// <summary>
	/// Initialize layer parameters
	/// </summary>
	public abstract void InitializeParameters();
}
