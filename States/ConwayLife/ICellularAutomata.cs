namespace Critters.States.ConwayLife;

interface ICellularAutomata
{
	int Width { get; }
	int Height { get; }
	bool this[int y, int x] { get; set; }
	
	void Step();

	public void CopyTo(ICellularAutomata other)
	{
		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				other[y, x] = this[y, x];
			}
		}
	}
}
