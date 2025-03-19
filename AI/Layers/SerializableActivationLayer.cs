namespace Critters.AI.Layers;

class SerializableActivationLayer : SerializableLayer
{
	public ActivationType ActivationType { get; set; }
	public required double[] LastInputs { get; set; }
	public required double[] LastOutputs { get; set; }
}
