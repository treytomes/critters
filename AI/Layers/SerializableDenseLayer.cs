namespace Critters.AI.Layers;

class SerializableDenseLayer : SerializableLayer
{
	public required double[,] Weights { get; set; }
	public required double[] Biases { get; set; }
	public required double[] LastInputs { get; set; }
	public required double[] LastOutputs { get; set; }
}
