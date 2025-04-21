namespace Critters.States.ConwayLife;

class ConwayLifeSimCPU : ICellularAutomata
{
	#region Fields

	private readonly bool[,] _buffer1;
	private readonly bool[,] _buffer2;
	private bool[,] _currentCells;
	private bool[,] _nextCells;
	private bool _wrapEdges = true;

	// Pre-computed neighbor offsets to avoid calculating them repeatedly
	private readonly (int dx, int dy)[] _neighborOffsets = new (int, int)[]
	{
		(-1, -1), (0, -1), (1, -1),
		(-1, 0),           (1, 0),
		(-1, 1),  (0, 1),  (1, 1)
	};

	#endregion

	#region Constructors

	public ConwayLifeSimCPU(int width, int height)
	{
		Width = width;
		Height = height;
		_buffer1 = new bool[height, width];
		_buffer2 = new bool[height, width];
		_currentCells = _buffer1;
		_nextCells = _buffer2;
	}

	#endregion

	#region Properties

	public int Width { get; }

	public int Height { get; }

	public bool this[int y, int x]
	{
		get
		{
			if (x < 0 || x >= Width || y < 0 || y >= Height)
			{
				return false;
			}

			return _currentCells[y, x];
		}
		set
		{
			if (x < 0 || x >= Width || y < 0 || y >= Height)
			{
				return;
			}

			_currentCells[y, x] = value;
		}
	}

	#endregion

	#region Methods

	public void Step()
	{
		if (_wrapEdges)
		{
			StepWithWrapping();
		}
		else
		{
			StepWithoutWrapping();
		}

		// Swap buffers - just swap references, no copying
		(_currentCells, _nextCells) = (_nextCells, _currentCells);
	}

	private void StepWithWrapping()
	{
		// Process the grid in blocks to improve cache locality
		const int BLOCK_SIZE = 32; // Adjust based on cache size

		// Pre-compute width and height for faster access
		int width = Width;
		int height = Height;

		// Process the grid in blocks
		for (int blockY = 0; blockY < height; blockY += BLOCK_SIZE)
		{
			for (int blockX = 0; blockX < width; blockX += BLOCK_SIZE)
			{
				int endY = Math.Min(blockY + BLOCK_SIZE, height);
				int endX = Math.Min(blockX + BLOCK_SIZE, width);

				for (int y = blockY; y < endY; y++)
				{
					for (int x = blockX; x < endX; x++)
					{
						int neighbors = 0;

						// Use pre-computed offsets
						foreach (var (dx, dy) in _neighborOffsets)
						{
							// Fast modulo for wrapping - works when width/height are powers of 2
							// For non-power of 2, we need the more expensive modulo
							int nx = (x + dx + width) % width;
							int ny = (y + dy + height) % height;

							if (_currentCells[ny, nx])
							{
								neighbors++;
								// Early exit optimization - if we already have > 3 neighbors,
								// we know the cell will die/stay dead
								if (neighbors > 3)
									break;
							}
						}

						// Apply Conway's rules using a more efficient approach
						bool isAlive = _currentCells[y, x];
						_nextCells[y, x] = neighbors == 3 || (isAlive && neighbors == 2);
					}
				}
			}
		}
	}

	private void StepWithoutWrapping()
	{
		// Process the interior of the grid (no boundary checks needed)
		for (int y = 1; y < Height - 1; y++)
		{
			for (int x = 1; x < Width - 1; x++)
			{
				int neighbors = CountNeighborsNoWrapping(x, y);
				bool isAlive = _currentCells[y, x];
				_nextCells[y, x] = neighbors == 3 || (isAlive && neighbors == 2);
			}
		}

		// Process the edges separately (with boundary checks)
		ProcessEdges();
	}

	private void ProcessEdges()
	{
		// Top and bottom edges (excluding corners)
		for (int x = 1; x < Width - 1; x++)
		{
			ProcessCell(x, 0);
			ProcessCell(x, Height - 1);
		}

		// Left and right edges (excluding corners)
		for (int y = 1; y < Height - 1; y++)
		{
			ProcessCell(0, y);
			ProcessCell(Width - 1, y);
		}

		// Corners
		ProcessCell(0, 0);
		ProcessCell(Width - 1, 0);
		ProcessCell(0, Height - 1);
		ProcessCell(Width - 1, Height - 1);
	}

	private void ProcessCell(int x, int y)
	{
		int neighbors = CountNeighborsWithBoundaryCheck(x, y);
		bool isAlive = _currentCells[y, x];
		_nextCells[y, x] = neighbors == 3 || (isAlive && neighbors == 2);
	}

	private int CountNeighborsNoWrapping(int x, int y)
	{
		// Fast neighbor counting for interior cells (no boundary checks)
		int count = 0;

		// Unroll the loop for better performance
		if (_currentCells[y - 1, x - 1]) count++;
		if (_currentCells[y - 1, x]) count++;
		if (_currentCells[y - 1, x + 1]) count++;
		if (_currentCells[y, x - 1]) count++;
		if (_currentCells[y, x + 1]) count++;
		if (_currentCells[y + 1, x - 1]) count++;
		if (_currentCells[y + 1, x]) count++;
		if (_currentCells[y + 1, x + 1]) count++;

		return count;
	}

	private int CountNeighborsWithBoundaryCheck(int x, int y)
	{
		int count = 0;

		foreach (var (dx, dy) in _neighborOffsets)
		{
			int nx = x + dx;
			int ny = y + dy;

			if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && _currentCells[ny, nx])
			{
				count++;
			}
		}

		return count;
	}

	public void Configure(SimulationConfig config)
	{
		_wrapEdges = config.WrapEdges;
	}

	#endregion
}