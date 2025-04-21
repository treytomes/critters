using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.States.ConwayLife;

class ConwayLifeSim : IDisposable
{
	#region Constants

	private static readonly RadialColor ON_COLOR = new RadialColor(5, 5, 0);
	private static readonly RadialColor OFF_COLOR = new RadialColor(0, 0, 3);

	#endregion

	#region Fields

	private ICellularAutomata _shader;
	private readonly SimulationConfig _config;
	private bool _disposedValue;

	// Predefined patterns
	private static readonly bool[,] GLIDER = new bool[3, 3]
	{
		{ false, true, false },
		{ false, false, true },
		{ true, true, true }
	};

	#endregion

	#region Constructors

	public ConwayLifeSim(int width, int height, SimulationConfig? config = null)
	{
		Width = width;
		Height = height;
		_config = config ?? new SimulationConfig();
		_shader = new ConwayLifeSimCPU(width, height);
		_shader.Configure(_config);
	}

	#endregion

	#region Properties

	public int Width { get; }
	public int Height { get; }
	public string CAType => _shader.GetType().Name;

	#endregion

	#region Methods

	public void Render(IRenderingContext rc)
	{
		rc.Clear();

		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				rc.SetPixel(new Vector2(x, y), _shader[y, x] ? ON_COLOR : OFF_COLOR);
			}
		}
	}

	public void Update(GameTime gameTime)
	{
		_shader.Step();
	}

	public void Randomize()
	{
		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				_shader[y, x] = Random.Shared.Next(2) == 0;
			}
		}
	}

	public void Clear()
	{
		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				_shader[y, x] = false;
			}
		}
	}

	public void SetCell(Vector2 position, bool value)
	{
		var x = (int)position.X;
		var y = (int)position.Y;

		if (x < 0 || x >= Width || y < 0 || y >= Height)
		{
			return;
		}

		_shader[y, x] = value;
	}

	public bool GetCell(Vector2 position)
	{
		var x = (int)position.X;
		var y = (int)position.Y;

		if (x < 0 || x >= Width || y < 0 || y >= Height)
		{
			return false;
		}

		return _shader[y, x];
	}

	public void PlacePattern(Vector2 position)
	{
		int centerX = (int)position.X;
		int centerY = (int)position.Y;

		// Place a glider pattern
		int patternHeight = GLIDER.GetLength(0);
		int patternWidth = GLIDER.GetLength(1);

		int startY = centerY - patternHeight / 2;
		int startX = centerX - patternWidth / 2;

		for (int y = 0; y < patternHeight; y++)
		{
			for (int x = 0; x < patternWidth; x++)
			{
				int targetY = startY + y;
				int targetX = startX + x;

				if (targetX >= 0 && targetX < Width && targetY >= 0 && targetY < Height)
				{
					_shader[targetY, targetX] = GLIDER[y, x];
				}
			}
		}
	}

	public void SwapGenerators()
	{
		ICellularAutomata newShader;

		if (_shader is ConwayLifeSimComputeShader)
		{
			newShader = new ConwayLifeSimCPU(Width, Height);
		}
		else
		{
			try
			{
				newShader = new ConwayLifeSimComputeShader(Width, Height);
			}
			catch (Exception ex)
			{
				// If compute shader creation fails, fall back to CPU
				Console.WriteLine($"Failed to create compute shader: {ex.Message}");
				return;
			}
		}

		// Configure the new shader with current settings
		newShader.Configure(_config);

		// Copy the current state to the new shader
		_shader.CopyTo(newShader);

		// Dispose the old shader if it's disposable
		if (_shader is IDisposable disposable)
		{
			disposable.Dispose();
		}

		_shader = newShader;
	}

	public void Configure(SimulationConfig config)
	{
		_config.UpdatesPerSecond = config.UpdatesPerSecond;
		_config.WrapEdges = config.WrapEdges;
		_shader.Configure(_config);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				// Dispose managed resources
				if (_shader is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}

			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}