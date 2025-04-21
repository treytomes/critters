using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.States.ConwayLife;

class ConwayLifeSim
{
	#region Constants

	private static readonly RadialColor ON_COLOR = new RadialColor(5, 5, 0);
	private static readonly RadialColor OFF_COLOR = new RadialColor(0, 0, 3);

	#endregion

	#region Fields

	private ICellularAutomata _shader;

	#endregion

	#region Constructors

	public ConwayLifeSim(int width, int height)
	{
		Width = width;
		Height = height;
		_shader = new ConwayLifeSimCPU(width, height);
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
				if (_shader[y, x])
				{
					rc.SetPixel(new Vector2(x, y), ON_COLOR);
				}
				else
				{
					rc.SetPixel(new Vector2(x, y), OFF_COLOR);
				}
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

	public void SwapGenerators()
	{
		if (_shader is ConwayLifeSimComputeShader)
		{
			var newShader = new ConwayLifeSimCPU(Width, Height);
			_shader.CopyTo(newShader);
			_shader = newShader;
		}
		else
		{
			var newShader = new ConwayLifeSimComputeShader(Width, Height);
			_shader.CopyTo(newShader);
			_shader = newShader;
		}
	}

	#endregion
}
