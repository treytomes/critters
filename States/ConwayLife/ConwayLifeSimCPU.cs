namespace Critters.States.ConwayLife;

class ConwayLifeSimCPU(int width, int height) : ICellularAutomata
{
	#region Fields

	private bool[,] _cells = new bool[height, width];

	#endregion

	#region Properties

	public int Width { get; } = width;

	public int Height { get; } = height;

	public bool this[int y, int x]
	{
		get
		{
			if (x < 0 || x >= _cells.GetLength(1) || y < 0 || y >= _cells.GetLength(0))
			{
				return false;
			}

			return _cells[y, x];
		}
		set
		{
			if (x < 0 || x >= _cells.GetLength(1) || y < 0 || y >= _cells.GetLength(0))
			{
				return;
			}

			_cells[y, x] = value;
		}
	}

	#endregion

	#region Methods

	public void Step()
	{
		var newCells = new bool[Height, Width];

		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				var neighbors = 0;
				for (var dy = -1; dy <= 1; dy++)
				{
					for (var dx = -1; dx <= 1; dx++)
					{
						if (dx == 0 && dy == 0)
						{
							continue;
						}

						var ny = y + dy;
						var nx = x + dx;

						// Wrap around the edges of the grid.
						ny = (ny + Height) % Height;
						nx = (nx + Width) % Width;

						if (ny < 0 || ny >= Height || nx < 0 || nx >= Width)
						{
							continue;
						}

						if (_cells[ny, nx])
						{
							neighbors++;
						}
					}
				}

				if (_cells[y, x])
				{
					if (neighbors == 2 || neighbors == 3)
					{
						newCells[y, x] = true;
					}
				}
				else
				{
					if (neighbors == 3)
					{
						newCells[y, x] = true;
					}
				}
			}
		}

		_cells = newCells;
	}

	#endregion
}
