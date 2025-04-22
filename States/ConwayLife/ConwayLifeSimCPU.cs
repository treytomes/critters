using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Critters.States.ConwayLife;

class ConwayLifeSimCPU : ICellularAutomata
{
	#region Constants

	// Block size for cache-friendly processing
	private const int BLOCK_SIZE = 32;

	// Pre-computed neighbor offsets
	private static readonly (int dx, int dy)[] _neighborOffsets = new[]
	{
		(-1, -1), (0, -1), (1, -1),
		(-1, 0),           (1, 0),
		(-1, 1),  (0, 1),  (1, 1)
	};

	#endregion

	#region Fields

	private readonly bool[,] _buffer1;
	private readonly bool[,] _buffer2;
	private bool[,] _currentCells;
	private bool[,] _nextCells;
	private bool _wrapEdges = true;

	// Activity tracking
	private HashSet<(int x, int y)> _activeCells = new();
	private HashSet<(int x, int y)> _nextActiveCells = new();
	private bool _useActivityTracking = true;

	// SIMD support detection
	private readonly bool _simdSupported;

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

		// Check if SIMD is supported on this CPU
		_simdSupported = Vector.IsHardwareAccelerated &&
						 (Avx2.IsSupported || Sse2.IsSupported);
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

			bool oldValue = _currentCells[y, x];
			_currentCells[y, x] = value;

			// Update activity tracking
			if (_useActivityTracking)
			{
				if (value && !oldValue)
				{
					_activeCells.Add((x, y));
					// Mark neighbors as active for next generation
					MarkNeighborsActive(x, y);
				}
				else if (!value && oldValue)
				{
					_activeCells.Remove((x, y));
					// Still mark neighbors as active since a cell disappeared
					MarkNeighborsActive(x, y);
				}
			}
		}
	}

	#endregion

	#region Methods

	public void Step()
	{
		if (_useActivityTracking && _activeCells.Count < Width * Height * 0.1)
		{
			// Use activity tracking for sparse patterns (less than 10% of cells active)
			StepWithActivityTracking();
		}
		else if (_wrapEdges)
		{
			StepWithWrapping();
		}
		else
		{
			StepWithoutWrapping();
		}

		// Swap buffers - just swap references, no copying
		(_currentCells, _nextCells) = (_nextCells, _currentCells);

		// Swap active cells sets if using activity tracking
		if (_useActivityTracking)
		{
			(_activeCells, _nextActiveCells) = (_nextActiveCells, _activeCells);
			_nextActiveCells.Clear();
		}
	}

	private void StepWithActivityTracking()
	{
		// Clear the next generation buffer
		Array.Clear(_nextCells, 0, _nextCells.Length);

		// Process only active cells and their neighbors
		HashSet<(int x, int y)> cellsToProcess = new(_activeCells);

		// Add neighbors of active cells to processing list
		foreach (var (x, y) in _activeCells)
		{
			foreach (var (dx, dy) in _neighborOffsets)
			{
				int nx = x + dx;
				int ny = y + dy;

				if (_wrapEdges)
				{
					nx = (nx + Width) % Width;
					ny = (ny + Height) % Height;
				}
				else if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
				{
					continue;
				}

				cellsToProcess.Add((nx, ny));
			}
		}

		// Process all cells in the processing list
		foreach (var (x, y) in cellsToProcess)
		{
			int neighbors = CountNeighbors(x, y);
			bool isAlive = _currentCells[y, x];
			bool willBeAlive = neighbors == 3 || (isAlive && neighbors == 2);

			_nextCells[y, x] = willBeAlive;

			if (willBeAlive)
			{
				_nextActiveCells.Add((x, y));
			}
		}
	}

	private void StepWithWrapping()
	{
		// Process the grid in blocks to improve cache locality
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

						// Unroll the loop for better performance
						foreach (var (dx, dy) in _neighborOffsets)
						{
							int nx = (x + dx + width) % width;
							int ny = (y + dy + height) % height;

							if (_currentCells[ny, nx])
							{
								neighbors++;
								// Early exit optimization
								if (neighbors > 3)
									break;
							}
						}

						// Apply Conway's rules using a more efficient approach
						bool isAlive = _currentCells[y, x];
						bool willBeAlive = neighbors == 3 || (isAlive && neighbors == 2);
						_nextCells[y, x] = willBeAlive;

						// Update activity tracking if needed
						if (_useActivityTracking && willBeAlive)
						{
							_nextActiveCells.Add((x, y));
						}
					}
				}
			}
		}
	}

	private void StepWithoutWrapping()
	{
		// Process the interior of the grid (no boundary checks needed)
		ProcessInterior();

		// Process the edges separately (with boundary checks)
		ProcessEdges();
	}

	private void ProcessInterior()
	{
		// Process the interior cells (excluding the border)
		for (int y = 1; y < Height - 1; y++)
		{
			for (int x = 1; x < Width - 1; x++)
			{
				// Fast neighbor counting for interior cells
				int neighbors = CountNeighborsInterior(x, y);

				bool isAlive = _currentCells[y, x];
				bool willBeAlive = neighbors == 3 || (isAlive && neighbors == 2);

				_nextCells[y, x] = willBeAlive;

				// Update activity tracking if needed
				if (_useActivityTracking && willBeAlive)
				{
					_nextActiveCells.Add((x, y));
				}
			}
		}
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
		int neighbors = CountNeighborsEdge(x, y);
		bool isAlive = _currentCells[y, x];
		bool willBeAlive = neighbors == 3 || (isAlive && neighbors == 2);

		_nextCells[y, x] = willBeAlive;

		// Update activity tracking if needed
		if (_useActivityTracking && willBeAlive)
		{
			_nextActiveCells.Add((x, y));
		}
	}

	private int CountNeighbors(int x, int y)
	{
		if (_wrapEdges)
		{
			return CountNeighborsWrapped(x, y);
		}
		else if (x > 0 && x < Width - 1 && y > 0 && y < Height - 1)
		{
			return CountNeighborsInterior(x, y);
		}
		else
		{
			return CountNeighborsEdge(x, y);
		}
	}

	private int CountNeighborsWrapped(int x, int y)
	{
		int count = 0;
		int width = Width;
		int height = Height;

		foreach (var (dx, dy) in _neighborOffsets)
		{
			int nx = (x + dx + width) % width;
			int ny = (y + dy + height) % height;

			if (_currentCells[ny, nx])
			{
				count++;
				// Early exit optimization
				if (count > 3)
					break;
			}
		}

		return count;
	}

	private int CountNeighborsInterior(int x, int y)
	{
		// Fast neighbor counting for interior cells (no boundary checks or modulo)
		int count = 0;

		// Manual loop unrolling for better performance
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

	private int CountNeighborsEdge(int x, int y)
	{
		int count = 0;

		foreach (var (dx, dy) in _neighborOffsets)
		{
			int nx = x + dx;
			int ny = y + dy;

			if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && _currentCells[ny, nx])
			{
				count++;
				// Early exit optimization
				if (count > 3)
					break;
			}
		}

		return count;
	}

	private void MarkNeighborsActive(int x, int y)
	{
		foreach (var (dx, dy) in _neighborOffsets)
		{
			int nx = x + dx;
			int ny = y + dy;

			if (_wrapEdges)
			{
				nx = (nx + Width) % Width;
				ny = (ny + Height) % Height;
			}
			else if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
			{
				continue;
			}

			_activeCells.Add((nx, ny));
		}
	}

	public void Configure(SimulationConfig config)
	{
		_wrapEdges = config.WrapEdges;

		// If there's a specific configuration for activity tracking
		if (config is ConwayLifeConfig conwayConfig)
		{
			_useActivityTracking = conwayConfig.UseActivityTracking;
		}
	}

	#endregion
}

// Extended configuration class specifically for Conway's Game of Life
public class ConwayLifeConfig : SimulationConfig
{
	public bool UseActivityTracking { get; set; } = true;
}