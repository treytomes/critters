using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.States.FloatingConwayLife;

class ConwayLifeSim
{
	#region Fields

	private ConwayLifeSimComputeShader _shader;

	#endregion

	#region Constructors

	public ConwayLifeSim(int width, int height)
	{
		Width = width;
		Height = height;
		_shader = new ConwayLifeSimComputeShader(width, height);
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
				var value = _shader[y, x];
				var color = new RadialColor(
					(byte)(value.X * 5),
					(byte)(value.Y * 5),
					(byte)(value.Z * 5)
				);
				rc.SetPixel(new Vector2(x, y), color);
			}
		}
	}

	public void Update(GameTime gameTime)
	{
		_shader.Step();
	}

	public void Randomize()
	{
		_shader.Randomize();
	}

	public void SetCell(Vector2 position, Vector4 value)
	{
		var x = (int)position.X;
		var y = (int)position.Y;

		if (x < 0 || x >= Width || y < 0 || y >= Height)
		{
			return;
		}

		_shader[y, x] = value;
	}

	public Vector4 GetCell(Vector2 position)
	{
		var x = (int)position.X;
		var y = (int)position.Y;

		if (x < 0 || x >= Width || y < 0 || y >= Height)
		{
			return Vector4.Zero;
		}

		return _shader[y, x];
	}

	#endregion
}
