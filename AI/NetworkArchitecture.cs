using Critters.AI.Layers;

namespace Critters.AI;

/// <summary>
/// Defines a custom neural network architecture
/// </summary>
public class NetworkArchitecture
{
    private readonly List<Func<int, int, ILayer>> _layerFactories = new();
    
    /// <summary>
    /// Adds a dense layer with the specified activation function
    /// </summary>
    public NetworkArchitecture AddDenseLayer(int neurons, ActivationType activationType)
    {
        _layerFactories.Add((input, _) => new DenseLayer(input, neurons));
        _layerFactories.Add((_, _) => new ActivationLayer(neurons, activationType));
        return this;
    }
    
    /// <summary>
    /// Creates a neural network using this architecture
    /// </summary>
    internal NeuralNetwork CreateNetwork(int inputSize, int outputSize)
    {
        var network = new NeuralNetwork();
        network.AddLayer(new InputLayer(inputSize));
        
        int prevSize = inputSize;
        foreach (var factory in _layerFactories.Take(_layerFactories.Count - 2))  // Skip last layer
        {
            var layer = factory(prevSize, outputSize);
            network.AddLayer(layer);
            if (layer is DenseLayer denseLayer)
            {
                prevSize = denseLayer.OutputSize;
            }
        }
        
        // Add output layer
        network.AddLayer(new DenseLayer(prevSize, outputSize));
        
        return network;
    }
}
