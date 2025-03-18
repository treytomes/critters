namespace Critters.AI.Layers;

/// <summary>
/// Input layer that passes through values without transformation
/// </summary>
public class InputLayer : Layer
{
	private double[] _lastInputs;

	public InputLayer(int size)
		: base(size, size)
	{
		_lastInputs = [];
	}

	public override double[] Forward(double[] inputs)
	{
		_lastInputs = inputs;
		return inputs;
	}

	public override double[] Backward(double[] outputGradients, double learningRate)
	{
		// Input layer doesn't have parameters to update.
		return outputGradients;
	}

	public override void InitializeParameters()
	{
		// No parameters to initialize
	}
}