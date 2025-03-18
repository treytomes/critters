namespace Critters.AI.Layers;

/// <summary>
/// Activation layer that applies an activation function
/// </summary>
public class ActivationLayer : Layer
{
	private ActivationType _activationType;
	private double[] _lastInputs;
	private double[] _lastOutputs;

	public ActivationLayer(int size, ActivationType activationType = ActivationType.Sigmoid) : base(size, size)
	{
		_activationType = activationType;
		_lastInputs = [];
		_lastOutputs = [];
	}

	public override void InitializeParameters()
	{
		// No parameters to initialize
	}

	public override double[] Forward(double[] inputs)
	{
		_lastInputs = inputs;
		var outputs = new double[_inputSize];

		for (int i = 0; i < _inputSize; i++)
		{
			outputs[i] = ApplyActivation(inputs[i]);
		}

		_lastOutputs = outputs;
		return outputs;
	}

	public override double[] Backward(double[] outputGradients, double learningRate)
	{
		var inputGradients = new double[_inputSize];

		for (var i = 0; i < _inputSize; i++)
		{
			inputGradients[i] = outputGradients[i] * ApplyActivationDerivative(_lastOutputs[i]);
		}

		return inputGradients;
	}

	private double ApplyActivation(double x)
	{
		return _activationType switch
		{
			ActivationType.Sigmoid => 1.0 / (1.0 + Math.Exp(-x)),
			ActivationType.Tanh => Math.Tanh(x),
			ActivationType.ReLU => Math.Max(0, x),
			ActivationType.Linear => x,
			_ => throw new ArgumentException($"Unsupported activation function: {_activationType}")
		};
	}

	private double ApplyActivationDerivative(double activatedX)
	{
		return _activationType switch
		{
			ActivationType.Sigmoid => activatedX * (1 - activatedX),
			ActivationType.Tanh => 1 - activatedX * activatedX,
			ActivationType.ReLU => activatedX > 0 ? 1 : 0,
			ActivationType.Linear => 1,
			_ => throw new ArgumentException($"Unsupported activation function: {_activationType}")
		};
	}
}