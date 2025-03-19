namespace Critters.AI.Layers;

static class LayerFactory
{
	public static ILayer Deserialize(ISerializableLayer data)
	{
		if (data is SerializableActivationLayer)
		{
			return ActivationLayer.Deserialize((data as SerializableActivationLayer)!);
		}
		else if (data is SerializableDenseLayer)
		{
			return DenseLayer.Deserialize((data as SerializableDenseLayer)!);
		}
		else if (data is SerializableInputLayer)
		{
			return InputLayer.Deserialize((data as SerializableInputLayer)!);
		}
		
		throw new ArgumentException($"Unable to deserialize from `{data.GetType().Name}`", nameof(data));
	}
}