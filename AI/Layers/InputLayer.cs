namespace Critters.AI.Layers;

class SerializableInputLayer : SerializableLayer
{
	public required double[] LastInputs { get; set; }
}

/// <summary>
/// Input layer that passes through values without transformation
/// </summary>
class InputLayer : Layer<InputLayer, SerializableInputLayer>
{
	#region Fields

	private double[] _lastInputs;

	#endregion

	#region Constructors

	public InputLayer(int size)
		: base(size, size)
	{
		_lastInputs = [];
	}

	private InputLayer(InputLayer other)
		: base(other)
	{
		_lastInputs = other._lastInputs.ToArray();
	}

	private InputLayer(SerializableInputLayer other)
		: base(other)
	{
		_lastInputs = other.LastInputs.ToArray();
	}

	#endregion

	#region Methods

	public static InputLayer Deserialize(SerializableInputLayer data)
	{
		return new InputLayer(data);
	}
	
	public override SerializableInputLayer Serialize()
	{
		return new SerializableInputLayer()
		{
			InputSize = _inputSize,
			OutputSize = _outputSize,
			LastInputs = _lastInputs.ToArray(),
		};
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

	public override InputLayer Clone()
	{
		return new InputLayer(this);
	}

	#endregion
}