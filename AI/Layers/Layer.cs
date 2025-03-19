
namespace Critters.AI.Layers;

/// <summary>
/// Base class for all neural network layers
/// </summary>
abstract class Layer<TLayer, TSerializable> : ILayer
	where TSerializable : SerializableLayer
	where TLayer : Layer<TLayer, TSerializable>
{
	#region Constructors

	public Layer(int inputSize, int outputSize)
	{
		InputSize = inputSize;
		OutputSize = outputSize;
	}

	protected Layer(TLayer other)
	{
		InputSize = other.InputSize;
		OutputSize = other.OutputSize;
	}

	protected Layer(TSerializable other)
	{
		InputSize = other.InputSize;
		OutputSize = other.OutputSize;
	}

	#endregion

	#region Properties

	public int InputSize { get; }
	public int OutputSize { get; }

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
