
namespace Critters.AI.Layers;

/// <summary>
/// Base class for all neural network layers
/// </summary>
abstract class Layer<TLayer, TSerializable> : ILayer
	where TSerializable : SerializableLayer
	where TLayer : Layer<TLayer, TSerializable>
{
	#region Fields

	protected int _inputSize;
	protected int _outputSize;

	#endregion

	#region Constructors

	public Layer(int inputSize, int outputSize)
	{
		_inputSize = inputSize;
		_outputSize = outputSize;
	}

	protected Layer(TLayer other)
	{
		_inputSize = other._inputSize;
		_outputSize = other._outputSize;
	}

	protected Layer(TSerializable other)
	{
		_inputSize = other.InputSize;
		_outputSize = other.OutputSize;
	}

	#endregion

	#region Methods

	public abstract TSerializable Serialize();
	
	ISerializableLayer ILayer.Serialize() => Serialize();

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

	public abstract TLayer Clone();

	ILayer ICloneable<ILayer>.Clone()
	{
		return Clone();
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	#endregion
}
