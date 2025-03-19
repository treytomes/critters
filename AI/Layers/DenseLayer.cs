namespace Critters.AI.Layers;

/// <summary>
/// Dense (fully connected) layer with weights and biases
/// </summary>
class DenseLayer : Layer<DenseLayer, SerializableDenseLayer>
{
	#region Fields

	private double[,] _weights;
	private double[] _biases;
	private double[] _lastInputs;
	private double[] _lastOutputs;

	#endregion

	#region Constructors

	public DenseLayer(int inputSize, int outputSize)
		: base(inputSize, outputSize)
	{
		_weights = new double[outputSize, inputSize];
		_biases = new double[outputSize];
		_lastOutputs = [];
		_lastInputs = [];
		InitializeParameters();
	}

	private DenseLayer(DenseLayer other)
		: base(other)
	{
		_weights = new double[OutputSize, InputSize];
		_biases = new double[OutputSize];
		_lastOutputs = new double[OutputSize];
		_lastInputs = new double[InputSize];

		Array.Copy(other._weights, _weights, other._weights.Length);
		Array.Copy(other._biases, _biases, other._biases.Length);
		Array.Copy(other._lastOutputs, _lastOutputs, other._lastOutputs.Length);
		Array.Copy(other._lastInputs, _lastInputs, other._lastInputs.Length);
	}

	private DenseLayer(SerializableDenseLayer other)
		: base(other)
	{
		_weights = new double[OutputSize, InputSize];
		_biases = new double[OutputSize];
		_lastOutputs = other.LastOutputs.ToArray();
		_lastInputs = other.LastInputs.ToArray();

		for (var o = 0; o < OutputSize; o++)
		{
			for (var i = 0; i < InputSize; i++)
			{
				_weights[o, i] = other.Weights[o, i];
			}
			_biases[o] = other.Biases[o];
		}
	}

	#endregion

	#region Methods

	public static DenseLayer Deserialize(SerializableDenseLayer data)
	{
		return new DenseLayer(data);
	}

	public override DenseLayer Clone()
	{
		return new DenseLayer(this);
	}

	public override SerializableDenseLayer Serialize()
	{
		return new SerializableDenseLayer()
		{
			Weights = _weights.ToArray(),
			Biases = _biases.ToArray(),
			LastInputs = _lastInputs.ToArray(),
			LastOutputs = _lastOutputs.ToArray(),
			InputSize = InputSize,
			OutputSize = OutputSize
		};
	}

	public override void InitializeParameters()
	{
		var random = Random.Shared;

		// Xavier initialization.
		var scale = Math.Sqrt(6.0 / (InputSize + OutputSize));

		for (var o = 0; o < OutputSize; o++)
		{
			for (var i = 0; i < InputSize; i++)
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
		var outputs = new double[OutputSize];

		for (var o = 0; o < OutputSize; o++)
		{
			outputs[o] = _biases[o];
			for (var i = 0; i < InputSize; i++)
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
		var inputGradients = new double[InputSize];
		
		for (var i = 0; i < InputSize; i++)
		{
			inputGradients[i] = 0;
			for (var o = 0; o < OutputSize; o++)
			{
				inputGradients[i] += outputGradients[o] * _weights[o, i];
			}
		}
		
		// Update weights and biases.
		for (var o = 0; o < OutputSize; o++)
		{
			for (var i = 0; i < InputSize; i++)
			{
				_weights[o, i] -= learningRate * outputGradients[o] * _lastInputs[i];
			}
			_biases[o] -= learningRate * outputGradients[o];
		}
		
		return inputGradients;
	}

	#endregion
}