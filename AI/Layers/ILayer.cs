namespace Critters.AI.Layers;

interface ILayer : ICloneable<ILayer>
{
	ISerializableLayer Serialize();
	double[] Forward(double[] inputs);
	double[] Backward(double[] outputGradients, double learningRate);
	void InitializeParameters();
}
