namespace Critters.States.ConwayLife;

interface ICellularAutomata
{
	int Width { get; }
	int Height { get; }
	bool this[int y, int x] { get; set; }

	void Step();

	public void CopyTo(ICellularAutomata other)
	{
		if (other.Width != Width || other.Height != Height)
		{
			throw new ArgumentException("Target cellular automata dimensions must match source dimensions");
		}

		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				other[y, x] = this[y, x];
			}
		}
	}

	// Optional method for configuration
	public void Configure(SimulationConfig config) { }
}

// Configuration class for simulation parameters
public class SimulationConfig
{
	public int UpdatesPerSecond { get; set; } = 10;
	public bool WrapEdges { get; set; } = true;
	// Add other configuration options as needed
}