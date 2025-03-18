namespace Critters.AI.Layers;

/// <summary>
/// Dense (fully connected) layer with weights and biases
/// </summary>
public class DenseLayer : Layer
{
	private double[,] _weights;
	private double[] _biases;
	private double[] _lastInputs;
	private double[] _lastOutputs;

	public DenseLayer(int inputSize, int outputSize)
		: base(inputSize, outputSize)
	{
		_weights = new double[outputSize, inputSize];
		_biases = new double[outputSize];
		_lastOutputs = [];
		_lastInputs = [];
		InitializeParameters();
	}

	public override void InitializeParameters()
	{
		var random = Random.Shared;

		// Xavier initialization.
		var scale = Math.Sqrt(6.0 / (_inputSize + _outputSize));

		for (var o = 0; o < _outputSize; o++)
		{
			for (var i = 0; i < _inputSize; i++)
			{
				_weights[o, i] = (random.NextDouble() * 2 - 1) * scale;
			}

			// Small positive bias.
			_biases[o] = 0.01;
		}
	}

	public override double[] Forward(double[] inputs)
	{
		_lastInputs = inputs;
		var outputs = new double[_outputSize];

		for (var o = 0; o < _outputSize; o++)
		{
			outputs[o] = _biases[o];
			for (var i = 0; i < _inputSize; i++)
			{
				outputs[o] += inputs[i] * _weights[o, i];
			}
		}

		_lastOutputs = outputs;
		return outputs;
	}
	
	public override double[] Backward(double[] outputGradients, double learningRate)
	{
		// Calculate gradients for the input.
		var inputGradients = new double[_inputSize];
		
		for (var i = 0; i < _inputSize; i++)
		{
			inputGradients[i] = 0;
			for (var o = 0; o < _outputSize; o++)
			{
				inputGradients[i] += outputGradients[o] * _weights[o, i];
			}
		}
		
		// Update weights and biases.
		for (var o = 0; o < _outputSize; o++)
		{
			for (var i = 0; i < _inputSize; i++)
			{
				_weights[o, i] -= learningRate * outputGradients[o] * _lastInputs[i];
			}
			_biases[o] -= learningRate * outputGradients[o];
		}
		
		return inputGradients;
	}
}